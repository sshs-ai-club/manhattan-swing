using System.Collections.Generic;
using Swing.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swing.Game
{
    /// <summary>
    /// Draws the city. Buildings are merged into one mesh per square of the map, so the whole city
    /// is a few dozen draw calls and Unity can skip the squares that are out of view.
    /// </summary>
    public static class CityView
    {
        private const float ChunkSize = 400f;
        public static readonly Color Sky = new Color(0.66f, 0.78f, 0.92f);
        private static readonly Color Land = new Color(0.17f, 0.17f, 0.19f);
        private static readonly Color Park = new Color(0.22f, 0.42f, 0.22f);
        private static readonly Color Water = new Color(0.13f, 0.24f, 0.38f);

        /// <summary>Collects one mesh's worth of triangles.</summary>
        private sealed class MeshParts
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Color32> Colours = new List<Color32>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();

            public int Add(Vector3 position, Vector3 normal, Color32 colour, Vector2 uv)
            {
                Vertices.Add(position); Normals.Add(normal); Colours.Add(colour); Uvs.Add(uv);
                return Vertices.Count - 1;
            }

            public void Triangle(int a, int b, int c)
            {
                Triangles.Add(a); Triangles.Add(b); Triangles.Add(c);
            }
        }

        public static GameObject Build(CityData city)
        {
            var root = new GameObject("City");
            var material = new Material(Resources.Load<Shader>("Shaders/SwingCity"));
            material.SetColor("_HazeColor", Sky);

            // Sort the buildings into map squares.
            var chunks = new Dictionary<long, MeshParts>();
            for (int i = 0; i < city.Boxes.Length; i++)
            {
                CityBox box = city.Boxes[i];
                long key = ((long)Mathf.FloorToInt(box.CentreX / ChunkSize) << 32) ^
                           (uint)Mathf.FloorToInt(box.CentreZ / ChunkSize);
                MeshParts parts;
                if (!chunks.TryGetValue(key, out parts)) chunks[key] = parts = new MeshParts();
                AddBuilding(parts, box, i);
            }
            foreach (MeshParts parts in chunks.Values) CreateObject("Buildings", parts, material, root.transform);

            // Ground: land and parks from the data, and one large sheet of water under them.
            var ground = new MeshParts();
            const float far = 30000f;
            AddFlatQuad(ground, -far, -far, far, far, -3f, Water);
            foreach (GroundLayer layer in city.Ground)
                AddGroundLayer(ground, layer, layer.Kind == GroundLayer.Park ? 0.15f : 0f,
                    layer.Kind == GroundLayer.Park ? Park : Land);
            CreateObject("Ground", ground, material, root.transform);
            return root;
        }

        private static void AddBuilding(MeshParts parts, CityBox box, int index)
        {
            // Corners in order around the building, anticlockwise seen from above.
            var corners = new Vector3[4];
            float[] signX = { -1, 1, 1, -1 }, signZ = { -1, -1, 1, 1 };
            for (int c = 0; c < 4; c++)
            {
                float wx, wz;
                box.ToWorld(signX[c] * box.HalfX, signZ[c] * box.HalfZ, out wx, out wz);
                corners[c] = new Vector3(box.CentreX + wx, 0, box.CentreZ + wz);
            }
            Color32 colour = BuildingColour(index);
            Vector3 up = Vector3.up * box.Height;

            for (int c = 0; c < 4; c++)
            {
                Vector3 a = corners[c], b = corners[(c + 1) % 4];
                Vector3 along = (b - a).normalized;
                Vector3 outward = new Vector3(along.z, 0, -along.x);
                float width = Vector3.Distance(a, b);
                int a0 = parts.Add(a, outward, colour, new Vector2(0, 0));
                int a1 = parts.Add(a + up, outward, colour, new Vector2(0, box.Height));
                int b1 = parts.Add(b + up, outward, colour, new Vector2(width, box.Height));
                int b0 = parts.Add(b, outward, colour, new Vector2(width, 0));
                parts.Triangle(a0, a1, b1);
                parts.Triangle(a0, b1, b0);
            }

            int r0 = parts.Add(corners[0] + up, Vector3.up, colour, Vector2.zero);
            int r1 = parts.Add(corners[1] + up, Vector3.up, colour, Vector2.zero);
            int r2 = parts.Add(corners[2] + up, Vector3.up, colour, Vector2.zero);
            int r3 = parts.Add(corners[3] + up, Vector3.up, colour, Vector2.zero);
            parts.Triangle(r0, r3, r2);
            parts.Triangle(r0, r2, r1);
        }

        /// <summary>A grey-beige shade that is always the same for the same building.</summary>
        private static Color32 BuildingColour(int index)
        {
            uint hash = (uint)index * 2654435761u;
            float shade = 0.62f + (hash >> 24) / 255f * 0.3f;
            float warmth = ((hash >> 16) & 255) / 255f * 0.08f;
            return new Color(shade + warmth, shade + warmth * 0.5f, shade, 1f);
        }

        private static void AddGroundLayer(MeshParts parts, GroundLayer layer, float y, Color32 colour)
        {
            int first = parts.Vertices.Count;
            foreach (System.Numerics.Vector2 v in layer.Vertices)
                parts.Add(new Vector3(v.X, y, v.Y), Vector3.up, colour, Vector2.zero);
            for (int t = 0; t + 2 < layer.Triangles.Length; t += 3)
            {
                int a = first + layer.Triangles[t], b = first + layer.Triangles[t + 1], c = first + layer.Triangles[t + 2];
                // Unity shows the side of a triangle whose corners run clockwise; make that the top.
                Vector3 pa = parts.Vertices[a], pb = parts.Vertices[b], pc = parts.Vertices[c];
                float turn = (pb.x - pa.x) * (pc.z - pa.z) - (pb.z - pa.z) * (pc.x - pa.x);
                if (turn > 0) parts.Triangle(a, c, b); else parts.Triangle(a, b, c);
            }
        }

        private static void AddFlatQuad(MeshParts parts, float x0, float z0, float x1, float z1, float y, Color32 colour)
        {
            int a = parts.Add(new Vector3(x0, y, z0), Vector3.up, colour, Vector2.zero);
            int b = parts.Add(new Vector3(x0, y, z1), Vector3.up, colour, Vector2.zero);
            int c = parts.Add(new Vector3(x1, y, z1), Vector3.up, colour, Vector2.zero);
            int d = parts.Add(new Vector3(x1, y, z0), Vector3.up, colour, Vector2.zero);
            parts.Triangle(a, b, c);
            parts.Triangle(a, c, d);
        }

        private static void CreateObject(string name, MeshParts parts, Material material, Transform parent)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(parts.Vertices);
            mesh.SetNormals(parts.Normals);
            mesh.SetColors(parts.Colours);
            mesh.SetUVs(0, parts.Uvs);
            mesh.SetTriangles(parts.Triangles, 0);
            mesh.RecalculateBounds();

            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = holder.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
