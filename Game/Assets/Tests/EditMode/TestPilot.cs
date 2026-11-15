using System;
using System.Numerics;

namespace Swing.Core.Tests
{
    /// <summary>
    /// A crude automatic player used only by tests. It heads for the next ring, fires a web at a
    /// wall ahead and above when it is falling, and lets go on the way up. It plays far worse than
    /// a person, which is the point: if this can finish a route, the route and the swing rules
    /// leave room for a beginner.
    /// </summary>
    public sealed class TestPilot
    {
        private const float Frame = 1f / 60f;

        public int RingsPassed;
        public float Seconds;
        public bool Finished;
        /// <summary>Where the pilot was when it finished or ran out of time, to find places it gets stuck.</summary>
        public Vector3 LastPosition;

        /// <param name="releaseDegrees">How far past the bottom of a swing the pilot lets go. Vary it to fly different lines.</param>
        public static TestPilot Fly(CityCollision city, Route route, SwingSettings settings, float timeLimitSeconds, float releaseDegrees)
        {
            var pilot = new TestPilot();
            var body = new SwingBody(city, settings);
            var race = new Race(route);
            body.Teleport(route.Start);
            race.StartCountdown();
            race.Update(Race.CountdownSeconds + 1, body.Position, body.Position);

            float held = 0;
            for (float t = 0; t < timeLimitSeconds && race.State == RaceState.Running; t += Frame)
            {
                RouteRing ring = route.Rings[race.NextRing];
                Vector3 toRing = ring.Centre - body.Position;
                var flat = new Vector3(toRing.X, 0, toRing.Z);
                float flatDistance = flat.Length();
                flat = flatDistance > 0.01f ? flat / flatDistance : Vector3.UnitZ;

                var input = new SwingInput { Move = flat, Jump = body.Grounded && !body.Attached };
                if (body.Attached)
                {
                    held += Frame;
                    Vector3 fromAnchor = body.Position - body.Anchor;
                    float ahead = Vector3.Dot(new Vector3(fromAnchor.X, 0, fromAnchor.Z), flat);
                    double swingAngle = Math.Atan2(ahead, -fromAnchor.Y);   // 0 when hanging straight down
                    bool risingPastTheBottom = body.Velocity.Y > 0 && swingAngle > Radians(releaseDegrees);
                    bool tooHigh = body.Position.Y > ring.Centre.Y + 18 && swingAngle > 0;
                    if (risingPastTheBottom || tooHigh || held > 6) body.Release();
                    input.Reel = body.Position.Y < 15 || body.Velocity.Length() < 4;
                }
                else if (body.Position.Y < ring.Centre.Y + 8 && (body.Velocity.Y < 2 || body.Position.Y < ring.Centre.Y - 5))
                {
                    Vector3 anchor;
                    bool slow = body.Velocity.Length() < 10;
                    if (ChooseAnchor(city, body.Position, flat, flatDistance, slow, out anchor) && body.Attach(anchor)) held = 0;
                }

                Vector3 before = body.Position;
                body.Step(Frame, input);
                race.Update(Frame, before, body.Position);
            }

            pilot.RingsPassed = race.NextRing;
            pilot.Seconds = race.Time;
            pilot.Finished = race.State == RaceState.Finished;
            pilot.LastPosition = body.Position;
            return pilot;
        }

        private static bool ChooseAnchor(CityCollision city, Vector3 from, Vector3 flat, float flatDistance, bool slow, out Vector3 anchor)
        {
            anchor = Vector3.Zero;
            float best = float.NegativeInfinity;
            var side = new Vector3(flat.Z, 0, -flat.X);
            RayHit hit;

            // Walls ahead, to either side, well above.
            for (float yaw = -50; yaw <= 50; yaw += 10)
            for (float pitch = 25; pitch <= 65; pitch += 10)
            {
                if (yaw == 0) continue;
                Vector3 level = flat * (float)Math.Cos(Radians(yaw)) + side * (float)Math.Sin(Radians(yaw));
                Vector3 aim = Vector3.Normalize(level * (float)Math.Cos(Radians(pitch)) + Vector3.UnitY * (float)Math.Sin(Radians(pitch)));
                if (!city.Raycast(from, aim, 150, out hit) || hit.BoxIndex < 0 || hit.Distance < 20) continue;
                Vector3 offset = hit.Point - from;
                float forward = Vector3.Dot(offset, flat), sideways = Math.Abs(Vector3.Dot(offset, side));
                if (offset.Y < 12 || forward < 10 || forward > flatDistance + 30) continue;
                float score = forward * 0.6f + offset.Y * 0.5f - sideways * 0.4f;
                if (score > best) { best = score; anchor = hit.Point; }
            }
            if (best > float.NegativeInfinity) return true;
            if (!slow) return false;

            // Slow with nothing ahead (boxed in on a low roof or in a gap): take the highest wall
            // point in any direction and climb out.
            for (int k = 0; k < 12; k++)
            for (float pitch = 50; pitch <= 80; pitch += 15)
            {
                var level = new Vector3((float)Math.Cos(k * Math.PI / 6), 0, (float)Math.Sin(k * Math.PI / 6));
                Vector3 aim = Vector3.Normalize(level * (float)Math.Cos(Radians(pitch)) + Vector3.UnitY * (float)Math.Sin(Radians(pitch)));
                if (!city.Raycast(from, aim, 150, out hit) || hit.BoxIndex < 0 || hit.Distance < 6) continue;
                if (hit.Point.Y > best) { best = hit.Point.Y; anchor = hit.Point; }
            }
            return best > float.NegativeInfinity;
        }

        private static double Radians(float degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
