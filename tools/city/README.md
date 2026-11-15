# City data tools

The game loads one small file, `Game/Assets/Resources/city.bytes`. It is already committed, so **you do not need these tools to play or to work on the game.** Run them only to change which part of the city is included or how buildings become boxes.

## What the file holds

Every building is one box: centre, size, rotation and height. Midtown is about 6,900 boxes in 170 KB. Ground shapes (land and parks) are stored as flat triangles.

Game coordinates are metres. X runs east across the avenues, Z runs north along the avenues, Y is up. The street grid is turned so it lines up with the axes, and (0, 0) is the Empire State Building.

```text
"MSC1"
uint32 boxCount
boxCount x { float centreX, centreZ, halfSizeX, halfSizeZ, yawRadians, height }
uint32 layerCount
per layer: uint32 kind (0 land, 1 park)
           uint32 vertexCount, vertexCount x { float x, z }
           uint32 indexCount,  indexCount x uint32
```

All numbers are little-endian. The reader in the game is `Game/Assets/Scripts/Core/CityData.cs`.

## Rebuilding the file

1. Download `Manhattan_Source_and_Materials.zip` from the old project's [city-source-v2 release](https://github.com/lollipop190000/manhattan-headtracked-viewer/releases/tag/city-source-v2) and unzip its `data/` folder.
2. Install the tool's libraries: `pip install -r tools/city/requirements.txt`
3. Run:

```bash
python tools/city/build_city.py --source path/to/data
```

`--area all` writes the whole island (about 44,000 boxes, 1.1 MB). The settings at the top of `build_city.py` choose the area, the minimum building size and the rotation.

## Looking at the result

```bash
python tools/city/preview.py --out map.png --route Game/Assets/Resources/route_midtown.txt
```

draws a top-down map with the ring route on it.

## Data source

Building footprints, the island outline and park outlines come from [NYC Open Data](https://opendata.cityofnewyork.us/) (NYC Office of Technology and Innovation, Department of City Planning, NYC Parks), as collected for the old project on 2026-10-09. They are used under the [NYC Open Data Terms of Use](https://data.cityofnewyork.us/stories/s/Terms-of-Use/k9k7-3cje). Building heights are roof heights above ground; the ground is treated as flat.
