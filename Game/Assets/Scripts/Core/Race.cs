using System.Collections.Generic;
using System.Numerics;

namespace Swing.Core
{
    public enum RaceState { Waiting, Countdown, Running, Finished }

    /// <summary>
    /// The rules of one timed run: a countdown, rings passed in order, and a clock that stops at the
    /// last ring. It knows nothing about drawing or input.
    /// </summary>
    public sealed class Race
    {
        public const float CountdownSeconds = 3f;
        /// <summary>Extra metres around a ring that still count, so a near miss is not punished.</summary>
        public const float Forgiveness = 1.5f;

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
            if (!Crosses(Route.Rings[NextRing], previousPosition, position)) return false;

            Splits.Add(Time);
            NextRing++;
            if (NextRing >= Route.Rings.Length) State = RaceState.Finished;
            return true;
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
