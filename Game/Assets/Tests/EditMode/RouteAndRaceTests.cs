using System;
using System.Numerics;
using NUnit.Framework;

namespace Swing.Core.Tests
{
    public class RouteAndRaceTests
    {
        private const string TwoRings =
            "# a comment\n" +
            "name Test route\n" +
            "start 0 10 0 90\n" +
            "ring 0 10 100 10   # straight ahead\r\n" +
            "ring 0 10 200\n";

        [Test]
        public void RouteTextIsRead()
        {
            Route route = Route.Parse(TwoRings);
            Assert.AreEqual("Test route", route.Name);
            Assert.AreEqual(new Vector3(0, 10, 0), route.Start);
            Assert.AreEqual(90f, route.StartYawDegrees);
            Assert.AreEqual(2, route.Rings.Length);
            Assert.AreEqual(10f, route.Rings[0].Radius);
            Assert.AreEqual(12f, route.Rings[1].Radius, "radius has a default");
            Assert.AreEqual(1f, route.Rings[0].Facing.Z, 0.001f, "rings face along the path");
        }

        [Test]
        public void ABrokenRouteNamesTheLine()
        {
            var error = Assert.Throws<FormatException>(() => Route.Parse("start 0 0 0\nring 1 two 3\n"));
            StringAssert.Contains("line 2", error.Message);
            Assert.Throws<FormatException>(() => Route.Parse("ring 1 2 3\n"));
        }

        [Test]
        public void RingsCountOnlyWhenPassedThroughTheMiddle()
        {
            RouteRing ring = Route.Parse(TwoRings).Rings[0];
            Assert.IsTrue(Race.Crosses(ring, new Vector3(0, 10, 99), new Vector3(0, 10, 101)));
            Assert.IsTrue(Race.Crosses(ring, new Vector3(9, 10, 99), new Vector3(9, 10, 101)));
            Assert.IsFalse(Race.Crosses(ring, new Vector3(20, 10, 99), new Vector3(20, 10, 101)), "outside the ring");
            Assert.IsFalse(Race.Crosses(ring, new Vector3(0, 10, 90), new Vector3(0, 10, 95)), "not reached yet");
        }

        [Test]
        public void ARaceCountsDownRunsAndStopsTheClockAtTheLastRing()
        {
            var race = new Race(Route.Parse(TwoRings));
            Assert.AreEqual(RaceState.Waiting, race.State);
            race.StartCountdown();
            for (int i = 0; i < 3; i++) race.Update(1f, Vector3.Zero, Vector3.Zero);
            Assert.AreEqual(RaceState.Running, race.State);
            Assert.AreEqual(0f, race.Time, "the clock starts at go");

            // Fly along the path at 50 m/s in half-second frames.
            Vector3 position = new Vector3(0, 10, 0);
            int passed = 0;
            for (int i = 0; i < 10 && race.State == RaceState.Running; i++)
            {
                Vector3 next = position + new Vector3(0, 0, 25);
                if (race.Update(0.5f, position, next)) passed++;
                position = next;
            }
            Assert.AreEqual(2, passed);
            Assert.AreEqual(RaceState.Finished, race.State);
            Assert.AreEqual(4.0f, race.Time, 0.001f);
            Assert.AreEqual(2, race.Splits.Count);

            race.Update(1f, position, position);
            Assert.AreEqual(4.0f, race.Time, 0.001f, "the clock stays stopped");
        }

        [Test]
        public void TheSecondRingDoesNotCountBeforeTheFirst()
        {
            var race = new Race(Route.Parse(TwoRings));
            race.StartCountdown();
            race.Update(5f, Vector3.Zero, Vector3.Zero);
            Assert.IsFalse(race.Update(0.1f, new Vector3(0, 10, 199), new Vector3(0, 10, 201)));
            Assert.AreEqual(0, race.NextRing);
        }
    }
}
