"""Draw a top-down map of city.bytes (and optionally a route) as a PNG.

Usage:
    python tools/city/preview.py --out map.png
    python tools/city/preview.py --out midtown.png --centre 0 1200 --size 2600 \
        --route Game/Assets/Resources/route_midtown.txt
"""

import argparse
import math
import struct
from pathlib import Path

from PIL import Image, ImageDraw

REPO_ROOT = Path(__file__).resolve().parents[2]
CITY = REPO_ROOT / "Game" / "Assets" / "Resources" / "city.bytes"


def read_city(path):
    data = path.read_bytes()
    assert data[:4] == b"MSC1", "not a city file"
    (count,) = struct.unpack_from("<I", data, 4)
    boxes = [struct.unpack_from("<6f", data, 8 + 24 * i) for i in range(count)]
    offset = 8 + 24 * count
    (layer_count,) = struct.unpack_from("<I", data, offset)
    offset += 4
    layers = []
    for _ in range(layer_count):
        kind, vertex_count = struct.unpack_from("<II", data, offset)
        offset += 8
        vertices = [struct.unpack_from("<2f", data, offset + 8 * i) for i in range(vertex_count)]
        offset += 8 * vertex_count
        (index_count,) = struct.unpack_from("<I", data, offset)
        offset += 4
        indices = struct.unpack_from(f"<{index_count}I", data, offset)
        offset += 4 * index_count
        layers.append((kind, vertices, indices))
    return boxes, layers


def read_route(path):
    start, rings = None, []
    for line in Path(path).read_text().splitlines():
        parts = line.split("#")[0].split()
        if not parts:
            continue
        if parts[0] == "start":
            start = tuple(map(float, parts[1:4]))
        elif parts[0] == "ring":
            rings.append(tuple(map(float, parts[1:5])))
    return start, rings


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--out", required=True)
    parser.add_argument("--centre", nargs=2, type=float, default=None, metavar=("X", "Z"))
    parser.add_argument("--size", type=float, default=None, help="metres across the image height")
    parser.add_argument("--pixels", type=int, default=1600)
    parser.add_argument("--route", default=None)
    args = parser.parse_args()

    boxes, layers = read_city(CITY)
    if args.centre is None:
        xs = [b[0] for b in boxes]
        zs = [b[1] for b in boxes]
        centre = ((min(xs) + max(xs)) / 2, (min(zs) + max(zs)) / 2)
        size = args.size or (max(zs) - min(zs)) * 1.05
    else:
        centre, size = tuple(args.centre), args.size or 2000
    scale = args.pixels / size
    width = height = args.pixels

    def px(x, z):
        return (width / 2 + (x - centre[0]) * scale, height / 2 - (z - centre[1]) * scale)

    image = Image.new("RGB", (width, height), (40, 60, 90))
    draw = ImageDraw.Draw(image)
    for kind, vertices, indices in layers:
        colour = (70, 70, 74) if kind == 0 else (60, 110, 60)
        for i in range(0, len(indices), 3):
            draw.polygon([px(*vertices[indices[i + k]]) for k in range(3)], fill=colour)
    for cx, cz, hx, hz, yaw, h in sorted(boxes, key=lambda b: b[5]):
        c, s = math.cos(yaw), math.sin(yaw)
        corners = [px(cx + c * dx * hx - s * dz * hz, cz + s * dx * hx + c * dz * hz)
                   for dx, dz in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        shade = int(110 + min(h, 300) / 300 * 145)
        draw.polygon(corners, fill=(shade, shade, shade - 20))

    if args.route:
        start, rings = read_route(args.route)
        points = [px(start[0], start[2])] + [px(r[0], r[2]) for r in rings]
        draw.line(points, fill=(255, 80, 60), width=3)
        sx, sy = points[0]
        draw.rectangle([sx - 7, sy - 7, sx + 7, sy + 7], fill=(60, 255, 90))
        for number, (x, y) in enumerate(points[1:], start=1):
            draw.ellipse([x - 9, y - 9, x + 9, y + 9], outline=(255, 220, 0), width=3)
            draw.text((x + 12, y - 8), str(number), fill=(255, 255, 0))
    image.save(args.out)
    print(f"wrote {args.out}")


if __name__ == "__main__":
    main()
