"""Turn NYC building footprints into the small city file the game loads.

Every building becomes one box (centre, size, rotation, height). The default
area is Midtown (about 7,000 boxes, under 200 KB), so the game needs no
streaming, no bake step and no downloads. `--area all` writes the whole
island (about 44,000 boxes, 1.1 MB) with the same code.

Input:  the `data/` folder of the old project's city source (see README.md).
Output: Game/Assets/Resources/city.bytes  (format described in README.md)

Usage:
    python tools/city/build_city.py --source path/to/data
"""

import argparse
import json
import math
import struct
from pathlib import Path

import mapbox_earcut
import numpy as np
from shapely.geometry import Polygon, box as rectangle_area

# --- Settings you may want to change -----------------------------------------

# Manhattan's street grid is turned 28.3 degrees east of north. The game turns
# the city back so avenues run along +Z and cross streets along X.
GRID_BEARING_DEGREES = 28.3

# The game's (0, 0) is the Empire State Building (its BIN in the NYC data).
ORIGIN_BIN = "1015862"

# Areas in game metres: (min X, min Z, max X, max Z). Midtown runs from about
# 29th Street to a little way into Central Park, river to river.
AREAS = {
    "midtown": (-2000.0, -450.0, 2000.0, 2400.0),
    "all": None,
}

MIN_HEIGHT_M = 3.0          # lower things are dropped (sheds, kiosks)
MIN_AREA_M2 = 25.0
# NYC feature codes that are not solid buildings standing on the ground:
# 2110 skybridge, 1001 gas station canopy, 1003 placeholder, 1005 temporary.
SKIPPED_FEATURE_CODES = {"2110", "1001", "1003", "1005"}

# A box drawn around an L-shaped building is bigger than the building. When
# the building fills less than this share of its box, the box is shrunk to
# the building's true area so streets stay open.
SHRINK_BELOW_FILL = 0.8

LAND_SIMPLIFY_M = 4.0
PARK_SIMPLIFY_M = 3.0
PARK_MIN_AREA_M2 = 4000.0

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = REPO_ROOT / "Game" / "Assets" / "Resources" / "city.bytes"

# -----------------------------------------------------------------------------


class Frame:
    """Converts source coordinates (x east, y north, metres) to game X/Z."""

    def __init__(self, origin_xy):
        angle = math.radians(GRID_BEARING_DEGREES)
        self.cos, self.sin = math.cos(angle), math.sin(angle)
        self.ox, self.oy = origin_xy

    def to_game(self, points):
        p = np.asarray(points, dtype=np.float64) - (self.ox, self.oy)
        x = p[:, 0] * self.cos - p[:, 1] * self.sin
        z = p[:, 0] * self.sin + p[:, 1] * self.cos
        return np.stack([x, z], axis=1)


def find_origin(buildings):
    candidates = [b for b in buildings if b["bin"] == ORIGIN_BIN]
    if not candidates:
        raise SystemExit(f"Origin building BIN {ORIGIN_BIN} is not in the data")
    biggest = max(candidates, key=lambda b: Polygon(b["rings"][0]).area)
    centre = Polygon(biggest["rings"][0]).centroid
    return centre.x, centre.y


def building_to_box(building, frame):
    """Returns (centre_x, centre_z, half_x, half_z, yaw_radians, height) or None."""
    if building["feature_code"] in SKIPPED_FEATURE_CODES:
        return None
    height = float(building["height_m"])
    if height < MIN_HEIGHT_M:
        return None

    footprint = Polygon(frame.to_game(building["rings"][0]))
    if not footprint.is_valid:
        footprint = footprint.buffer(0)
    if footprint.is_empty or footprint.area < MIN_AREA_M2:
        return None

    rectangle = footprint.minimum_rotated_rectangle
    if rectangle.geom_type != "Polygon":
        return None
    corners = np.asarray(rectangle.exterior.coords)[:4]
    side_a = corners[1] - corners[0]
    side_b = corners[2] - corners[1]
    half_x = float(np.linalg.norm(side_a)) / 2
    half_z = float(np.linalg.norm(side_b)) / 2
    if half_x < 1.0 or half_z < 1.0:
        return None

    # Yaw is the direction of the box's local X axis, measured from game +X
    # towards game +Z.
    yaw = math.atan2(side_a[1], side_a[0])
    # Keep yaw in [-45, 45] degrees by swapping the sides when needed, so
    # nearly all boxes read as "almost axis-aligned".
    while yaw > math.pi / 4:
        yaw -= math.pi / 2
        half_x, half_z = half_z, half_x
    while yaw < -math.pi / 4:
        yaw += math.pi / 2
        half_x, half_z = half_z, half_x

    fill = footprint.area / (4 * half_x * half_z)
    if fill < SHRINK_BELOW_FILL:
        scale = math.sqrt(fill / SHRINK_BELOW_FILL)
        half_x *= scale
        half_z *= scale

    centre = corners.mean(axis=0)
    return (float(centre[0]), float(centre[1]), half_x, half_z, yaw, height)


def triangulate(polygon):
    """Returns (vertices Nx2, triangle indices) for a polygon without holes."""
    ring = np.asarray(polygon.exterior.coords)[:-1]
    indices = mapbox_earcut.triangulate_float64(ring, np.array([len(ring)]))
    return ring, indices


def ground_layer(polygons, simplify_m, area):
    vertices, indices = [], []
    for polygon in polygons:
        polygon = polygon.simplify(simplify_m)
        if area is not None:
            polygon = polygon.intersection(rectangle_area(*area))
        parts = polygon.geoms if polygon.geom_type == "MultiPolygon" else [polygon]
        for part in parts:
            if part.is_empty or part.geom_type != "Polygon" or part.area < 1:
                continue
            ring, triangles = triangulate(part)
            indices.extend(int(i) + len(vertices) for i in triangles)
            vertices.extend(ring.tolist())
    return vertices, indices


def polygons_of(geometry, frame):
    kind = geometry["type"]
    groups = [geometry["coordinates"]] if kind == "Polygon" else geometry["coordinates"]
    result = []
    for rings in groups:
        polygon = Polygon(frame.to_game(rings[0]))
        result.append(polygon if polygon.is_valid else polygon.buffer(0))
    return result


def write_city(path, boxes, layers):
    with open(path, "wb") as f:
        f.write(b"MSC1")
        f.write(struct.pack("<I", len(boxes)))
        for box in boxes:
            f.write(struct.pack("<6f", *box))
        f.write(struct.pack("<I", len(layers)))
        for kind, vertices, indices in layers:
            f.write(struct.pack("<II", kind, len(vertices)))
            for x, z in vertices:
                f.write(struct.pack("<2f", x, z))
            f.write(struct.pack("<I", len(indices)))
            f.write(struct.pack(f"<{len(indices)}I", *indices))


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--source", required=True, type=Path,
                        help="folder holding buildings_metric.json and friends")
    parser.add_argument("--area", choices=sorted(AREAS), default="midtown")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    buildings = json.loads((args.source / "buildings_metric.json").read_text())
    frame = Frame(find_origin(buildings))

    area = AREAS[args.area]
    boxes = [box for box in (building_to_box(b, frame) for b in buildings) if box]
    if area is not None:
        boxes = [b for b in boxes if area[0] <= b[0] <= area[2] and area[1] <= b[1] <= area[3]]
    # Sorting keeps the file identical between runs and neighbours together.
    boxes.sort(key=lambda b: (round(b[1] / 250), round(b[0] / 250), b[1], b[0]))

    island = json.loads((args.source / "main_island_metric.json").read_text())
    land = ground_layer(polygons_of(island, frame), LAND_SIMPLIFY_M, area)

    parks = json.loads((args.source / "parks_metric.geojson").read_text())
    park_polygons = []
    for feature in parks["features"]:
        for polygon in polygons_of(feature["geometry"], frame):
            if polygon.area >= PARK_MIN_AREA_M2:
                park_polygons.append(polygon)
    park = ground_layer(park_polygons, PARK_SIMPLIFY_M, area)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    write_city(args.output, boxes, [(0, *land), (1, *park)])

    xs = [b[0] for b in boxes]
    zs = [b[1] for b in boxes]
    print(f"buildings in source : {len(buildings)}")
    print(f"boxes written       : {len(boxes)}")
    print(f"extent X            : {min(xs):.0f} to {max(xs):.0f} m")
    print(f"extent Z            : {min(zs):.0f} to {max(zs):.0f} m")
    print(f"tallest             : {max(b[5] for b in boxes):.0f} m")
    print(f"land triangles      : {len(land[1]) // 3}")
    print(f"park triangles      : {len(park[1]) // 3}")
    print(f"output              : {args.output} ({args.output.stat().st_size / 1e6:.2f} MB)")


if __name__ == "__main__":
    main()
