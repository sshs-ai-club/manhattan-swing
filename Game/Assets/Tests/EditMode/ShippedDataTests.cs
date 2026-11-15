using System.Collections.Generic;
using System.IO;
using System.Numerics;
using NUnit.Framework;

namespace Swing.Core.Tests
{
    /// <summary>Checks the real city and route files that ship with the game.</summary>
    public class ShippedDataTests
    {
        // Unity runs tests with the project folder (Game/) as the working directory.
        private const string Resources = "Assets/Resources/";

        private static CityData City() { return CityData.Load(File.ReadAllBytes(Resources + "city.bytes")); }
        private static Route MidtownRoute() { return Route.Parse(File.ReadAllText(Resources + "route_midtown.txt")); }

        [Test]
        public void TheCityLoadsAndStaysWithinTheWeightBudget()
        {
            CityData city = City();
            Assert.Greater(city.Boxes.Length, 1000);
            // Each box draws 10 triangles (4 walls and a roof). The budget is 300,000 on screen.
            Assert.Less(city.Boxes.Length * 10, 300000);
            Assert.AreEqual(2, city.Ground.Length);
        }

        [Test]
        public void EveryRingHangsInOpenAir()
        {
            var collision = new CityCollision(City());
            Route route = MidtownRoute();
            Assert.AreEqual(10, route.Rings.Length);
            foreach (RouteRing ring in route.Rings)
            {
                Vector3 centre = ring.Centre;
                Assert.IsFalse(collision.ResolveSphere(ref centre, ring.Radius, 0, null),
                    "A ring touches a building at " + ring.Centre);
            }
        }

        [Test]
        public void ThePlayerStartsStandingOnARoof()
        {
            var body = new SwingBody(new CityCollision(City()));
            Route route = MidtownRoute();
            body.Teleport(route.Start);
            for (int i = 0; i < 60; i++) body.Step(1f / 60f, new SwingInput());
            Assert.IsTrue(body.Grounded);
            Assert.AreEqual(route.Start.Y, body.Position.Y, 0.2f, "the start should already be at standing height");
            Assert.Greater(body.Position.Y, 30f, "the start should be on a roof, not the street");
        }

        /// <summary>
        /// A rough check that the route can be finished. Several crude pilots fly it, each letting
        /// go of the web at a different point in the swing. One pilot's flight depends on tiny
        /// details, so the test looks at the group: most must finish. If this fails after a tuning
        /// or route change, the change has probably made the game harder for beginners; play the
        /// route before loosening the numbers.
        /// </summary>
        [Test]
        public void MostCrudeAutomaticPilotsFinishTheMidtownRoute()
        {
            var collision = new CityCollision(City());
            Route route = MidtownRoute();
            int pilots = 0, finished = 0;
            string report = "";
            for (float release = 25; release <= 49; release += 3)
            {
                TestPilot pilot = TestPilot.Fly(collision, route, new SwingSettings(), 480, release);
                pilots++;
                if (pilot.Finished) finished++;
                report += string.Format("\n  release at {0} degrees: {1} rings, {2:0} s, ended at {3:0}, {4:0}, {5:0}", release, pilot.RingsPassed, pilot.Seconds,
                    pilot.LastPosition.X, pilot.LastPosition.Y, pilot.LastPosition.Z);
            }
            Assert.GreaterOrEqual(finished, pilots * 2 / 3, "Too few pilots finished within 8 minutes:" + report);
            System.Console.WriteLine("Pilots:" + report);
        }

        [Test]
        public void AWebCanBeFiredFromTheStartTowardsTheFirstRing()
        {
            var collision = new CityCollision(City());
            Route route = MidtownRoute();
            Vector3 aim = Vector3.Normalize(route.Rings[0].Centre + new Vector3(0, 60, 0) - route.Start);
            RayHit hit;
            Assert.IsTrue(AimAssist.FindAnchor(collision, route.Start, aim, 250, 2, out hit));
        }
    }
}
