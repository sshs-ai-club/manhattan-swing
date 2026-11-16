using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace ManhattanSwing.Core.Tests
{
    public class CollisionTests
    {
        // One 20 x 20 m tower, 50 m tall, centred at (100, 100).
        private static CityCollision OneTower(float yaw = 0)
        {
            var city = new CityData { Boxes = new[] { CityData.MakeBox(100, 100, 10, 10, yaw, 50) } };
            return new CityCollision(city);
        }

        [Test]
        public void RayHitsTheNearFaceOfABuilding()
        {
            RayHit hit;
            bool found = OneTower().Raycast(new Vector3(100, 20, 0), Vector3.UnitZ, 300, out hit);
            Assert.IsTrue(found);
            Assert.AreEqual(90f, hit.Distance, 0.001f);
            Assert.AreEqual(-1f, hit.Normal.Z, 0.001f);
            Assert.AreEqual(0, hit.BoxIndex);
            Assert.IsTrue(hit.CanHold);
        }

        [Test]
        public void RayOverTheRoofMisses()
        {
            RayHit hit;
            Assert.IsFalse(OneTower().Raycast(new Vector3(100, 60, 0), Vector3.UnitZ, 300, out hit));
        }

        [Test]
        public void RayDownHitsTheRoofThenTheGroundElsewhere()
        {
            RayHit hit;
            Assert.IsTrue(OneTower().Raycast(new Vector3(100, 80, 100), -Vector3.UnitY, 300, out hit));
            Assert.AreEqual(30f, hit.Distance, 0.001f);
            Assert.AreEqual(1f, hit.Normal.Y, 0.001f);

            Assert.IsTrue(OneTower().Raycast(new Vector3(0, 80, 0), -Vector3.UnitY, 300, out hit));
            Assert.AreEqual(80f, hit.Distance, 0.001f);
            Assert.AreEqual(-1, hit.BoxIndex);
            Assert.IsFalse(hit.CanHold, "webs do not hold on to the street");
        }

        [Test]
        public void RayFindsAFarBuildingAcrossManyGridCells()
        {
            var city = new CityData { Boxes = new[] { CityData.MakeBox(900, 37, 10, 10, 0, 50) } };
            RayHit hit;
            Vector3 direction = Vector3.Normalize(new Vector3(900, 0, 37));
            Assert.IsTrue(new CityCollision(city).Raycast(new Vector3(0, 10, 0), direction, 2000, out hit));
            Assert.AreEqual(0, hit.BoxIndex);
        }

        [Test]
        public void ATurnedBuildingIsHitOnItsTurnedFace()
        {
            // Turned 45 degrees, the tower shows a corner towards -Z: it reaches 14.14 m from its centre.
            RayHit hit;
            Assert.IsTrue(OneTower((float)(Math.PI / 4)).Raycast(new Vector3(100, 20, 0), Vector3.UnitZ, 300, out hit));
            Assert.AreEqual(100f - 14.142f, hit.Distance, 0.01f);
        }

        [Test]
        public void ASphereInsideAWallIsPushedOutSideways()
        {
            var centre = new Vector3(100, 20, 89.9f);   // 0.1 m outside the face at z = 90
            var contacts = new List<Vector3>();
            Assert.IsTrue(OneTower().ResolveSphere(ref centre, 0.4f, 1.3f, contacts));
            Assert.AreEqual(89.6f, centre.Z, 0.001f);
            Assert.AreEqual(-1f, contacts[0].Z, 0.001f);
        }

        [Test]
        public void ASphereStandsAtEyeHeightOnARoofAndOnTheGround()
        {
            var onRoof = new Vector3(100, 50.5f, 100);
            OneTower().ResolveSphere(ref onRoof, 0.4f, 1.3f, null);
            Assert.AreEqual(51.7f, onRoof.Y, 0.001f);

            var onGround = new Vector3(0, 0.2f, 0);
            OneTower().ResolveSphere(ref onGround, 0.4f, 1.3f, null);
            Assert.AreEqual(1.7f, onGround.Y, 0.001f);
        }

        [Test]
        public void TheCityKnowsItsOwnEdge()
        {
            CityCollision city = OneTower();   // one tower at (100, 100)
            Assert.IsTrue(city.IsInPlayArea(new Vector3(100, 0, 100), 0));
            Assert.IsFalse(city.IsInPlayArea(new Vector3(600, 0, 100), 0));
            Assert.IsTrue(city.IsInPlayArea(new Vector3(600, 0, 100), 600), "a margin widens the edge");
            float minX, minZ, maxX, maxZ;
            city.GetPlayArea(out minX, out minZ, out maxX, out maxZ);
            Assert.IsTrue(minX <= 100 && maxX >= 100 && minZ <= 100 && maxZ >= 100);
            Assert.IsTrue(maxX < 600, "the edge reported is the one IsInPlayArea uses");
        }

        [Test]
        public void ASphereInOpenAirIsNotMoved()
        {
            var centre = new Vector3(50, 30, 50);
            Assert.IsFalse(OneTower().ResolveSphere(ref centre, 0.4f, 1.3f, null));
            Assert.AreEqual(new Vector3(50, 30, 50), centre);
        }
    }
}
