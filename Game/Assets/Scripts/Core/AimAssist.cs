using System;
using System.Numerics;

namespace Swing.Core
{
    /// <summary>
    /// Chooses where a web lands. A direct hit is always used as it is. When the aim just misses,
    /// nearby directions are tried so a new player still finds something to hold.
    /// </summary>
    public static class AimAssist
    {
        /// <summary>How far from the aim direction the search may go, in degrees.</summary>
        public const float ConeDegrees = 9f;
        private const int Rings = 3, RaysPerRing = 8;

        public static bool FindAnchor(CityCollision city, Vector3 origin, Vector3 direction, float maxDistance,
            float minDistance, out RayHit hit)
        {
            direction = Vector3.Normalize(direction);
            if (Usable(city, origin, direction, maxDistance, minDistance, out hit)) return true;

            // Two directions at right angles to the aim, to step sideways and up from it.
            Vector3 side = Vector3.Cross(Vector3.UnitY, direction);
            side = side.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(side);
            Vector3 up = Vector3.Cross(direction, side);

            for (int ring = 1; ring <= Rings; ring++)
            {
                float tilt = (float)Math.Tan(ConeDegrees / Rings * ring * Math.PI / 180.0);
                for (int k = 0; k < RaysPerRing; k++)
                {
                    double angle = 2 * Math.PI * k / RaysPerRing;
                    Vector3 offset = side * (float)Math.Cos(angle) + up * (float)Math.Sin(angle);
                    Vector3 tryDirection = Vector3.Normalize(direction + offset * tilt);
                    if (Usable(city, origin, tryDirection, maxDistance, minDistance, out hit)) return true;
                }
            }
            return false;
        }

        /// <summary>A web may hold on to a building, not the ground, and not closer than the minimum.</summary>
        private static bool Usable(CityCollision city, Vector3 origin, Vector3 direction, float maxDistance,
            float minDistance, out RayHit hit)
        {
            return city.Raycast(origin, direction, maxDistance, out hit) && hit.BoxIndex >= 0 &&
                hit.Distance >= minDistance;
        }
    }
}
