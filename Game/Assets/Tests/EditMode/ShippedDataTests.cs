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
