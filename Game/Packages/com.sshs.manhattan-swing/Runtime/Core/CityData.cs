using System;
using System.IO;
using System.Numerics;

namespace ManhattanSwing.Core
{
    /// <summary>One building: a box standing on the ground (y = 0), turned by <see cref="Yaw"/> around the up axis.</summary>
    public struct CityBox
    {
        public float CentreX, CentreZ;
        public float HalfX, HalfZ;
        /// <summary>Radians. The box's own X axis points along (cos yaw, sin yaw) in world (X, Z).</summary>
        public float Yaw;
        public float Height;
        // Cached so collision does not call Sin/Cos for every test.
        public float Cos, Sin;

        /// <summary>World (X, Z) offset from the box centre to box-local (x, z).</summary>
        public void ToLocal(float dx, float dz, out float lx, out float lz)
        {
            lx = dx * Cos + dz * Sin;
            lz = -dx * Sin + dz * Cos;
        }

        /// <summary>Box-local direction (x, z) to world (X, Z).</summary>
        public void ToWorld(float lx, float lz, out float wx, out float wz)
        {
            wx = lx * Cos - lz * Sin;
            wz = lx * Sin + lz * Cos;
        }
    }

    /// <summary>A flat ground shape (land or park) as triangles in the XZ plane.</summary>
    public sealed class GroundLayer
    {
        public const int Land = 0, Park = 1;
        public int Kind;
        public Vector2[] Vertices;   // X = world X, Y = world Z
        public int[] Triangles;
    }

    /// <summary>
    /// The city as loaded from <c>Resources/city.bytes</c>. The file is written by
    /// <c>tools/city/build_city.py</c>; its layout is described in <c>tools/city/README.md</c>.
    /// </summary>
    public sealed class CityData
    {
        public CityBox[] Boxes = new CityBox[0];
        public GroundLayer[] Ground = new GroundLayer[0];

        public static CityData Load(byte[] bytes)
        {
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
            {
                if (bytes.Length < 8 || reader.ReadByte() != 'M' || reader.ReadByte() != 'S' ||
                    reader.ReadByte() != 'C' || reader.ReadByte() != '1')
                    throw new InvalidDataException("Not a city file (expected the MSC1 header).");

                var city = new CityData();
                city.Boxes = new CityBox[reader.ReadUInt32()];
                for (int i = 0; i < city.Boxes.Length; i++)
                {
                    city.Boxes[i] = MakeBox(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                        reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }

                city.Ground = new GroundLayer[reader.ReadUInt32()];
                for (int i = 0; i < city.Ground.Length; i++)
                {
                    var layer = new GroundLayer();
                    layer.Kind = (int)reader.ReadUInt32();
                    layer.Vertices = new Vector2[reader.ReadUInt32()];
                    for (int v = 0; v < layer.Vertices.Length; v++)
                        layer.Vertices[v] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    layer.Triangles = new int[reader.ReadUInt32()];
                    for (int t = 0; t < layer.Triangles.Length; t++)
                        layer.Triangles[t] = (int)reader.ReadUInt32();
                    city.Ground[i] = layer;
                }
                return city;
            }
        }

        public static CityBox MakeBox(float centreX, float centreZ, float halfX, float halfZ, float yaw, float height)
        {
            return new CityBox
            {
                CentreX = centreX, CentreZ = centreZ, HalfX = halfX, HalfZ = halfZ, Yaw = yaw, Height = height,
                Cos = (float)Math.Cos(yaw), Sin = (float)Math.Sin(yaw),
            };
        }
    }
}
