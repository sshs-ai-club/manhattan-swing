using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace ManhattanSwing.Core.Tests
{
    /// <summary>
    /// The swing, aim help and race must work on any map that answers <see cref="ISwingWorld"/>,
    /// not only the test map's boxes. These tests use a made-up map: a street at height 20 and one
    /// endless wall facing west at x = 30. A detailed map has the same street and walls, only more.
    /// Also checks moving routes into another scene's coordinates and following a floating origin.
    /// </summary>
    public class WorldTests
    {
        private const float Frame = 1f / 60f;

        /// <summary>A stand-in for another map, written without boxes or a grid.</summary>
        private sealed class StandInWorld : ISwingWorld
        {
            public float Street = 20, WallX = 30;

            public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit)
            {
                hit = new RayHit { Distance = maxDistance, BoxIndex = -1 };
                bool found = false;
                if (direction.Y < 0 && origin.Y > Street)
                {
                    float t = (Street - origin.Y) / direction.Y;
                    if (t < hit.Distance) { hit.Distance = t; hit.Normal = Vector3.UnitY; hit.CanHold = false; found = true; }
                }
                if (direction.X > 0 && origin.X < WallX)
                {
                    float t = (WallX - origin.X) / direction.X;
                    if (t < hit.Distance) { hit.Distance = t; hit.Normal = -Vector3.UnitX; hit.CanHold = true; found = true; }
                }
                hit.Point = origin + direction * hit.Distance;
                return found;
            }

            public bool ResolveSphere(ref Vector3 centre, float radius, float footDrop, List<Vector3> contacts)
            {
                bool touched = false;
                if (centre.X > WallX - radius) { centre.X = WallX - radius; contacts.Add(-Vector3.UnitX); touched = true; }
                if (centre.Y < Street + radius + footDrop) { centre.Y = Street + radius + footDrop; contacts.Add(Vector3.UnitY); touched = true; }
                return touched;
            }

            public float GroundHeight(Vector3 near) { return Street; }

            public bool IsInPlayArea(Vector3 position, float margin) { return position.X > -500 - margin; }
        }

        [Test]
        public void BodyStandsOnAnotherMapsStreet()
        {
            var body = new SwingBody(new StandInWorld());
            body.Teleport(new Vector3(0, 40, 0));
            for (int i = 0; i < 240; i++) body.Step(Frame, new SwingInput());
            Assert.IsTrue(body.Grounded);
            Assert.AreEqual(20 + body.Settings.EyeHeight, body.Position.Y, 0.05f);
        }

        [Test]
        public void BodyStopsAtAnotherMapsWall()
        {
            var body = new SwingBody(new StandInWorld());
            body.Teleport(new Vector3(20, 40, 0));
            body.Velocity = new Vector3(30, 0, 0);
            for (int i = 0; i < 60; i++) body.Step(Frame, new SwingInput());
            Assert.LessOrEqual(body.Position.X, 30 - body.Settings.Radius + 1e-3f);
        }

        [Test]
        public void AimHelpHoldsTheWallButNotTheStreet()
        {
            var world = new StandInWorld();
            RayHit hit;
            Assert.IsTrue(AimAssist.FindAnchor(world, new Vector3(0, 30, 0), new Vector3(1, 0.2f, 0), 250, 3, out hit));
            Assert.AreEqual(30, hit.Point.X, 1e-3f);
            Assert.IsFalse(AimAssist.FindAnchor(world, new Vector3(0, 30, 0), new Vector3(-0.2f, -1, 0), 250, 3, out hit),
                "straight down at the street, away from the wall");
        }

        [Test]
        public void SwingsClearAStreetThatIsNotAtHeightZero()
        {
            var world = new StandInWorld();
            var body = new SwingBody(world);
            body.Teleport(new Vector3(-10, 60, 0));
            Assert.IsTrue(body.Attach(new Vector3(30, 65, 0)));
            float lowest = float.MaxValue;
            for (int i = 0; i < 240; i++)
            {
                body.Step(Frame, new SwingInput());
                lowest = System.Math.Min(lowest, body.Position.Y);
            }
            // Without the help the web (about 40 m) would swing the body down to the street at 20.
            Assert.Greater(lowest, world.Street + body.Settings.GroundClearance * 0.5f);
        }

        [Test]
        public void IdentityFrameChangesNothing()
        {
            var p = new Vector3(12, 34, -56);
            Assert.AreEqual(p, WorldFrame.Identity.PointToScene(p));
            Assert.AreEqual(90f, WorldFrame.Identity.YawToScene(90f));
        }

        [Test]
        public void FrameTurnsAndMovesPointsAndRoundTrips()
        {
            var frame = new WorldFrame { Origin = new Vector3(1000, 5, 2000), YawDegrees = 90 };
            Vector3 north = frame.DirectionToScene(Vector3.UnitZ);
            Assert.AreEqual(1, north.X, 1e-5f, "a quarter turn clockwise sends route north to scene east");
            Assert.AreEqual(0, north.Z, 1e-5f);

            Vector3 scene = frame.PointToScene(new Vector3(0, 10, 100));
            Assert.AreEqual(1100, scene.X, 1e-3f);
            Assert.AreEqual(15, scene.Y, 1e-3f);
            Assert.AreEqual(2000, scene.Z, 1e-3f);

            var p = new Vector3(-197, 87, 468);
            Vector3 back = frame.PointToRoute(frame.PointToScene(p));
            Assert.AreEqual(0, Vector3.Distance(p, back), 1e-3f);
            Assert.AreEqual(30f, frame.YawToRoute(frame.YawToScene(30f)), 1e-4f);
        }

        private const string RouteText = "start 0 10 0 0\nring 0 10 100 10\nring 0 10 200 10\n";

        [Test]
        public void ARouteMovedIntoAnotherSceneStillCounts()
        {
            var frame = new WorldFrame { Origin = new Vector3(500, 0, -300), YawDegrees = 28.3f };
            Route original = Route.Parse(RouteText);
            Route moved = original.InScene(frame);
            Assert.AreEqual(0, original.Rings[0].Centre.X, "the original is left as it was");

            var race = new Race(moved);
            race.StartCountdown();
            race.Update(Race.CountdownSeconds + 0.01f, Vector3.Zero, Vector3.Zero);
            Vector3 before = frame.PointToScene(new Vector3(0, 10, 95));
            Vector3 after = frame.PointToScene(new Vector3(0, 10, 105));
            Assert.IsTrue(race.Update(Frame, before, after), "flying through the moved ring counts");
            Assert.AreEqual(28.3f, moved.StartYawDegrees, 1e-4f);
        }

        [Test]
        public void FloatingOriginShiftKeepsRaceAndBodyTogether()
        {
            var race = new Race(Route.Parse(RouteText));
            race.StartCountdown();
            race.Update(Race.CountdownSeconds + 0.01f, Vector3.Zero, Vector3.Zero);
            var body = new SwingBody(new StandInWorld { Street = -1000, WallX = 1e6f });
            body.Teleport(new Vector3(0, 10, 95));
            body.Attach(new Vector3(0, 60, 120));

            var delta = new Vector3(-4000, 0, 2500);
            race.ShiftOrigin(delta);
            body.ShiftOrigin(delta);

            Assert.AreEqual(new Vector3(-4000, 10, 2595), body.Position);
            Assert.AreEqual(new Vector3(-4000, 60, 2620), body.Anchor);
            Assert.IsTrue(race.Update(Frame, body.Position, body.Position + new Vector3(0, 0, 10)));

            Vector3 position, velocity; float yaw;
            race.Checkpoint(out position, out yaw, out velocity);
            Assert.AreEqual(new Vector3(-4000, 10, 2600), position, "the checkpoint moved too");
        }
    }
}
