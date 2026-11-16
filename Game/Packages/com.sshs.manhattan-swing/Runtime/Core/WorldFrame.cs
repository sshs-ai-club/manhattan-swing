using System;
using System.Numerics;

namespace ManhattanSwing.Core
{
    /// <summary>
    /// Converts between route coordinates and the coordinates of the scene the game runs in.
    ///
    /// Routes are written in the test map's coordinates: metres, origin at the Empire State Building,
    /// +Z north along the avenues, +X east across them, +Y up. Another map (such as the detailed
    /// city in the original repository) may put its origin elsewhere or turn its grid differently.
    /// Measure one point in both and fill in <see cref="Origin"/> and <see cref="YawDegrees"/>.
    ///
    /// The default frame changes nothing, which is right for the test map.
    /// </summary>
    public struct WorldFrame
    {
        /// <summary>Where the route's origin (0, 0, 0) is in the scene.</summary>
        public Vector3 Origin;
        /// <summary>How far the route's +Z axis is turned clockwise from the scene's +Z, seen from above.</summary>
        public float YawDegrees;

        public static readonly WorldFrame Identity = new WorldFrame();

        public Vector3 PointToScene(Vector3 routePoint) { return Origin + DirectionToScene(routePoint); }

        public Vector3 PointToRoute(Vector3 scenePoint) { return DirectionToRoute(scenePoint - Origin); }

        public Vector3 DirectionToScene(Vector3 d) { return Turn(d, YawDegrees); }

        public Vector3 DirectionToRoute(Vector3 d) { return Turn(d, -YawDegrees); }

        public float YawToScene(float routeYawDegrees) { return routeYawDegrees + YawDegrees; }

        public float YawToRoute(float sceneYawDegrees) { return sceneYawDegrees - YawDegrees; }

        /// <summary>Turns clockwise seen from above, the same sense as Unity's yaw.</summary>
        private static Vector3 Turn(Vector3 d, float degrees)
        {
            if (degrees == 0) return d;
            double r = degrees * Math.PI / 180.0;
            float c = (float)Math.Cos(r), s = (float)Math.Sin(r);
            return new Vector3(d.X * c + d.Z * s, d.Y, -d.X * s + d.Z * c);
        }
    }
}
