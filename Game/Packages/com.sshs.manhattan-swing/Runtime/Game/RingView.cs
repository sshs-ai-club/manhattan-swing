using ManhattanSwing.Core;
using UnityEngine;

namespace ManhattanSwing.Game
{
    /// <summary>Draws the route's rings: the next one bright, later ones dim, passed ones hidden.</summary>
    public sealed class RingView
    {
        private static readonly Color NextColour = new Color(1f, 0.85f, 0.1f);
        private static readonly Color LaterColour = new Color(0.45f, 0.5f, 0.6f);
        private static readonly Color FinishColour = new Color(0.2f, 1f, 0.4f);
        private const float TubeRadius = 0.5f;

        private readonly GameObject[] rings;
        private readonly Transform root;
        private readonly Material nextMaterial, laterMaterial, finishMaterial;

        public RingView(Route route, Shader unlit)
        {
            nextMaterial = new Material(unlit) { color = NextColour };
            laterMaterial = new Material(unlit) { color = LaterColour };
            finishMaterial = new Material(unlit) { color = FinishColour };

            root = new GameObject("Rings").transform;
            rings = new GameObject[route.Rings.Length];
            for (int i = 0; i < rings.Length; i++)
            {
                RouteRing ring = route.Rings[i];
                var holder = new GameObject("Ring " + (i + 1));
                holder.transform.SetParent(root, false);
                holder.transform.SetPositionAndRotation(ring.Centre.ToUnity(), Quaternion.LookRotation(ring.Facing.ToUnity()));
                holder.AddComponent<MeshFilter>().sharedMesh = Torus(ring.Radius, TubeRadius);
                holder.AddComponent<MeshRenderer>();
                rings[i] = holder;
            }
        }

        /// <summary>Call when the next ring changes.</summary>
        public void Show(int nextRing)
        {
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i].SetActive(i >= nextRing);
                bool isFinish = i == rings.Length - 1;
                rings[i].GetComponent<MeshRenderer>().sharedMaterial =
                    i == nextRing ? (isFinish ? finishMaterial : nextMaterial) : laterMaterial;
            }
        }

        /// <summary>The scene moved its whole world by <paramref name="delta"/> (a floating origin).</summary>
        public void ShiftOrigin(Vector3 delta)
        {
            root.position += delta;
        }

        /// <summary>A ring lying in its own XY plane, so its hole faces along Z.</summary>
        private static Mesh Torus(float radius, float tube)
        {
            const int around = 48, across = 8;
            var vertices = new Vector3[around * across];
            var triangles = new int[around * across * 6];
            for (int a = 0; a < around; a++)
            {
                float angle = a * 2 * Mathf.PI / around;
                var outward = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                for (int c = 0; c < across; c++)
                {
                    float turn = c * 2 * Mathf.PI / across;
                    vertices[a * across + c] = outward * (radius + tube * Mathf.Cos(turn)) + Vector3.forward * (tube * Mathf.Sin(turn));

                    int a2 = (a + 1) % around, c2 = (c + 1) % across;
                    int t = (a * across + c) * 6;
                    triangles[t] = a * across + c; triangles[t + 1] = a2 * across + c; triangles[t + 2] = a2 * across + c2;
                    triangles[t + 3] = a * across + c; triangles[t + 4] = a2 * across + c2; triangles[t + 5] = a * across + c2;
                }
            }
            var mesh = new Mesh { name = "Ring" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
