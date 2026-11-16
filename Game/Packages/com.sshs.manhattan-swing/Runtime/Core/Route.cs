using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace ManhattanSwing.Core
{
    public struct RouteRing
    {
        public Vector3 Centre;
        public float Radius;
        /// <summary>The direction of travel through the ring, length 1.</summary>
        public Vector3 Facing;
    }

    /// <summary>
    /// A race: where the player starts and the rings to pass in order. Loaded from a text file such
    /// as <c>Resources/route_midtown.txt</c>; the format is explained at the top of that file.
    /// </summary>
    public sealed class Route
    {
        public string Name = "Route";
        public Vector3 Start;
        /// <summary>Degrees, clockwise seen from above. 0 faces +Z (north along the avenues).</summary>
        public float StartYawDegrees;
        public RouteRing[] Rings = new RouteRing[0];

        public static Route Parse(string text)
        {
            var route = new Route();
            var rings = new List<RouteRing>();
            bool hasStart = false;
            string[] lines = text.Split('\n');

            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                int comment = line.IndexOf('#');
                if (comment >= 0) line = line.Substring(0, comment);
                string[] parts = line.Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                try
                {
                    if (parts[0] == "name") route.Name = line.Trim().Substring(4).Trim();
                    else if (parts[0] == "start")
                    {
                        route.Start = new Vector3(Number(parts[1]), Number(parts[2]), Number(parts[3]));
                        route.StartYawDegrees = parts.Length > 4 ? Number(parts[4]) : 0;
                        hasStart = true;
                    }
                    else if (parts[0] == "ring")
                    {
                        rings.Add(new RouteRing
                        {
                            Centre = new Vector3(Number(parts[1]), Number(parts[2]), Number(parts[3])),
                            Radius = parts.Length > 4 ? Number(parts[4]) : 12f,
                        });
                    }
                    else throw new FormatException("unknown word '" + parts[0] + "'");
                }
                catch (Exception error)
                {
                    throw new FormatException("Route line " + (n + 1) + ": " + error.Message);
                }
            }

            if (!hasStart) throw new FormatException("The route has no 'start' line.");
            if (rings.Count == 0) throw new FormatException("The route has no 'ring' lines.");

            // A ring faces along the path: from the point before it towards the point after it.
            for (int i = 0; i < rings.Count; i++)
            {
                Vector3 before = i == 0 ? route.Start : rings[i - 1].Centre;
                Vector3 after = i == rings.Count - 1 ? rings[i].Centre : rings[i + 1].Centre;
                Vector3 facing = Direction(before, rings[i].Centre) + Direction(rings[i].Centre, after);
                RouteRing ring = rings[i];
                ring.Facing = facing.LengthSquared() > 1e-6f ? Vector3.Normalize(facing) : Vector3.UnitZ;
                rings[i] = ring;
            }
            route.Rings = rings.ToArray();
            return route;
        }

        /// <summary>A copy of this route placed in a scene with a different origin or grid direction.</summary>
        public Route InScene(WorldFrame frame)
        {
            var moved = new Route
            {
                Name = Name,
                Start = frame.PointToScene(Start),
                StartYawDegrees = frame.YawToScene(StartYawDegrees),
                Rings = new RouteRing[Rings.Length],
            };
            for (int i = 0; i < Rings.Length; i++)
            {
                moved.Rings[i] = new RouteRing
                {
                    Centre = frame.PointToScene(Rings[i].Centre),
                    Radius = Rings[i].Radius,
                    Facing = frame.DirectionToScene(Rings[i].Facing),
                };
            }
            return moved;
        }

        /// <summary>Moves the whole route by <paramref name="delta"/>, when the scene shifts its origin.</summary>
        public void ShiftOrigin(Vector3 delta)
        {
            Start += delta;
            for (int i = 0; i < Rings.Length; i++) Rings[i].Centre += delta;
        }

        private static Vector3 Direction(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            return d.LengthSquared() > 1e-6f ? Vector3.Normalize(d) : Vector3.Zero;
        }

        private static float Number(string text)
        {
            return float.Parse(text, CultureInfo.InvariantCulture);
        }
    }
}
