using System;

namespace Swing.Core
{
    /// <summary>
    /// Turning the view by turning the head. A screen cannot show what is behind you however you
    /// move, so (as in flight and racing games with head trackers) a small turn of the head is
    /// taken as "keep turning that way": look a little to the right and the view swings right
    /// until you face the screen again.
    /// </summary>
    public static class HeadTurn
    {
        /// <summary>Head turns smaller than this (degrees) do nothing, so normal small movements do not turn the view.</summary>
        public const float DeadZoneDegrees = 10f;
        /// <summary>At this head angle the view turns at full speed.</summary>
        public const float FullDegrees = 30f;
        public const float FullTurnDegreesPerSecond = 140f;

        /// <summary>How fast to turn the view (degrees per second) for a head turned by <paramref name="headYawDegrees"/>.</summary>
        public static float DegreesPerSecond(float headYawDegrees)
        {
            float beyond = Math.Abs(headYawDegrees) - DeadZoneDegrees;
            if (beyond <= 0) return 0;
            float amount = Math.Min(1, beyond / (FullDegrees - DeadZoneDegrees));
            // Squared, so the turn starts gently just outside the dead zone.
            return Math.Sign(headYawDegrees) * amount * amount * FullTurnDegreesPerSecond;
        }
    }
}
