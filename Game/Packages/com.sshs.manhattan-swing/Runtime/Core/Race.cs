using System.Collections.Generic;
using System.Numerics;

namespace ManhattanSwing.Core
{
    public enum RaceState { Waiting, Countdown, Running, Finished, TimedOut }

    /// <summary>
    /// The rules of one timed run: a countdown, rings passed in order, and a clock that stops at the
    /// last ring. It knows nothing about drawing or input.
    /// </summary>
    public sealed class Race
    {
        public const float CountdownSeconds = 3f;
        /// <summary>Extra metres around a ring that still count, so a near miss is not punished.</summary>
        public const float Forgiveness = 1.5f;

        /// <summary>
        /// A run that takes longer than this many seconds ends as "time up", so one player cannot
        /// hold up the queue. 0 means no limit.
        /// </summary>
        public float TimeLimitSeconds = 300f;

        /// <summary>A player sent back to a checkpoint starts moving at this speed (m/s), so they do not just drop.</summary>
        public const float CheckpointSpeed = 18f;

        public readonly Route Route;
        public RaceState State { get; private set; }
        /// <summary>Index of the ring to pass next.</summary>
        public int NextRing { get; private set; }
        public float CountdownLeft { get; private set; }
        /// <summary>Seconds since "go". Stops at the finish.</summary>
        public float Time { get; private set; }
        /// <summary>The clock when each ring was passed.</summary>
        public readonly List<float> Splits = new List<float>();

        public Race(Route route)
        {
            Route = route;
            State = RaceState.Waiting;
        }

        /// <summary>Back to waiting, for the next player.</summary>
        public void Reset()
        {
            State = RaceState.Waiting;
            NextRing = 0;
            Time = 0;
            Splits.Clear();
        }

        public void StartCountdown()
        {
            State = RaceState.Countdown;
            CountdownLeft = CountdownSeconds;
            NextRing = 0;
            Time = 0;
            Splits.Clear();
        }

        /// <summary>
        /// Call once per frame with where the player was and is now.
        /// Returns true on the frame a ring is passed.
        /// </summary>
        public bool Update(float deltaTime, Vector3 previousPosition, Vector3 position)
        {
            if (State == RaceState.Countdown)
            {
                CountdownLeft -= deltaTime;
                if (CountdownLeft <= 0) State = RaceState.Running;
                return false;
            }
            if (State != RaceState.Running) return false;

            Time += deltaTime;
            if (TimeLimitSeconds > 0 && Time >= TimeLimitSeconds)
            {
                Time = TimeLimitSeconds;
                State = RaceState.TimedOut;
                return false;
            }
            if (!Crosses(Route.Rings[NextRing], previousPosition, position)) return false;

            Splits.Add(Time);
            NextRing++;
            if (NextRing >= Route.Rings.Length) State = RaceState.Finished;
            return true;
        }

        /// <summary>Call when the scene moves its whole world by <paramref name="delta"/> (a floating origin).</summary>
        public void ShiftOrigin(Vector3 delta)
        {
            Route.ShiftOrigin(delta);
        }

        /// <summary>
        /// Where to put a player who is lost or stuck: the last ring they passed, facing along the
        /// route, or the start if they have passed none. The clock keeps running.
        /// </summary>
        public void Checkpoint(out Vector3 position, out float yawDegrees, out Vector3 velocity)
        {
            if (NextRing == 0)
            {
                position = Route.Start;
                yawDegrees = Route.StartYawDegrees;
                velocity = Vector3.Zero;
                return;
            }
            RouteRing ring = Route.Rings[NextRing - 1];
            position = ring.Centre;
            yawDegrees = (float)(System.Math.Atan2(ring.Facing.X, ring.Facing.Z) * 180 / System.Math.PI);
            velocity = ring.Facing * CheckpointSpeed;
        }

        /// <summary>True when the straight move from a to b goes through the ring's disc.</summary>
        public static bool Crosses(RouteRing ring, Vector3 a, Vector3 b)
        {
            float sideA = Vector3.Dot(a - ring.Centre, ring.Facing);
            float sideB = Vector3.Dot(b - ring.Centre, ring.Facing);
            if (sideA * sideB > 0 || sideA == sideB) return false;   // both on one side: no crossing

            float t = sideA / (sideA - sideB);
            Vector3 crossing = a + (b - a) * t;
            return Vector3.Distance(crossing, ring.Centre) <= ring.Radius + Forgiveness;
        }
    }
}
