using System;
using System.Numerics;

namespace Swing.Core
{
    /// <summary>
    /// Turning the view to face the way the player is travelling. With this on, a player who never
    /// touches the mouse still ends up looking where they are going, which matters when the only
    /// control is the wrist device.
    /// </summary>
    public static class ViewFollow
    {
        /// <summary>Below this speed over the ground (m/s) the view is left alone.</summary>
        public const float MinSpeed = 8f;
        /// <summary>The view closes this share of the remaining angle each second...</summary>
        public const float Gain = 1.6f;
        /// <summary>...but never turns faster than this (degrees per second), to stay comfortable.</summary>
        public const float MaxDegreesPerSecond = 80f;

        /// <summary>
        /// How many degrees to turn the view this frame. Yaw is in degrees, clockwise seen from
        /// above, 0 facing +Z. Positive results turn right.
        /// </summary>
        public static float YawStep(float viewYawDegrees, Vector3 velocity, float deltaTime)
        {
            double groundSpeed = Math.Sqrt(velocity.X * velocity.X + velocity.Z * velocity.Z);
            if (groundSpeed < MinSpeed || deltaTime <= 0) return 0;

            float travelYaw = (float)(Math.Atan2(velocity.X, velocity.Z) * 180 / Math.PI);
            float difference = DeltaAngle(viewYawDegrees, travelYaw);
            float rate = Math.Min(Math.Abs(difference) * Gain, MaxDegreesPerSecond);
            float step = Math.Min(rate * deltaTime, Math.Abs(difference));
            return Math.Sign(difference) * step;
        }

        /// <summary>The shortest turn from one angle to another, between -180 and 180 degrees.</summary>
        public static float DeltaAngle(float from, float to)
        {
            float difference = (to - from) % 360f;
            if (difference > 180f) difference -= 360f;
            if (difference < -180f) difference += 360f;
            return difference;
        }
    }
}
