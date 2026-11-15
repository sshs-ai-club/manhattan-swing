using System;
using System.Numerics;
using NUnit.Framework;

namespace Swing.Core.Tests
{
    public class SwingBodyTests
    {
        private const float Frame = 1f / 60f;

        private static SwingBody BodyInEmptyWorld()
        {
            return new SwingBody(new CityCollision(new CityData()));
        }

        [Test]
        public void FallingBodyLandsAndStandsAtEyeHeight()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 10, 0));
            for (int i = 0; i < 180; i++) body.Step(Frame, new SwingInput());
            Assert.IsTrue(body.Grounded);
            Assert.AreEqual(body.Settings.EyeHeight, body.Position.Y, 0.01f);
            Assert.AreEqual(0f, body.Velocity.Length(), 0.01f);
        }

        [Test]
        public void TheWebNeverGetsLongerWhileSwinging()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 60, 0));
            body.Velocity = new Vector3(0, 0, 15);
            Assert.IsTrue(body.Attach(new Vector3(0, 100, 30)));
            float length = body.WebLength;
            for (int i = 0; i < 600; i++)
            {
                body.Step(Frame, new SwingInput());
                Assert.LessOrEqual(Vector3.Distance(body.Position, body.Anchor), length + 0.01f);
            }
        }

        [Test]
        public void ASwingDoesNotGainEnergyOnItsOwn()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 100, 0));
            Assert.IsTrue(body.Attach(new Vector3(0, 100, 50)));   // web level: a full half-circle swing
            float g = body.Settings.Gravity;
            float startEnergy = g * body.Position.Y;
            float fastest = 0;
            for (int i = 0; i < 1200; i++)
            {
                body.Step(Frame, new SwingInput());
                float energy = g * body.Position.Y + 0.5f * body.Velocity.LengthSquared();
                Assert.LessOrEqual(energy, startEnergy * 1.01f, "frame " + i);
                fastest = Math.Max(fastest, body.Velocity.Length());
            }
            // Falling 50 m on the web should give close to sqrt(2 g h) at the bottom.
            Assert.AreEqual(Math.Sqrt(2 * g * 50), fastest, 3.0);
        }

        [Test]
        public void ReleasingKeepsTheSpeed()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 100, 0));
            body.Attach(new Vector3(0, 100, 50));
            for (int i = 0; i < 90; i++) body.Step(Frame, new SwingInput());
            Vector3 before = body.Velocity;
            body.Release();
            Assert.AreEqual(before, body.Velocity);
            Assert.IsFalse(body.Attached);
        }

        [Test]
        public void ReelingShortensTheWebAndPullsTheBodyIn()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 50, 0));
            body.Attach(new Vector3(0, 100, 0));   // hanging straight down
            for (int i = 0; i < 60; i++) body.Step(Frame, new SwingInput { Reel = true });
            Assert.AreEqual(50 - body.Settings.ReelSpeed, body.WebLength, 0.5f);
            Assert.Greater(body.Position.Y, 70f);
        }

        [Test]
        public void HoldingAWebOnTheGroundLiftsThePlayerOff()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 1.7f, 0));
            body.Step(Frame, new SwingInput());
            Assert.IsTrue(body.Grounded);
            body.Attach(new Vector3(0, 60, 40));
            for (int i = 0; i < 60; i++) body.Step(Frame, new SwingInput());
            Assert.Greater(body.Position.Y, 5f);
        }

        [Test]
        public void WebsCannotReachFurtherThanTheirMaximumLength()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 50, 0));
            Assert.IsFalse(body.Attach(new Vector3(0, 50, 251)));
            Assert.IsFalse(body.Attach(new Vector3(0, 50, 1)));
        }

        [Test]
        public void JumpOnlyWorksFromTheGround()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 1.7f, 0));
            body.Step(Frame, new SwingInput());
            body.Step(Frame, new SwingInput { Jump = true });
            Assert.Greater(body.Velocity.Y, 10f);
            float rising = body.Velocity.Y;
            body.Step(Frame, new SwingInput { Jump = true });   // already in the air
            Assert.Less(body.Velocity.Y, rising);
        }

        [Test]
        public void AFastBodyDoesNotPassThroughABuilding()
        {
            var city = new CityData { Boxes = new[] { CityData.MakeBox(0, 100, 30, 5, 0, 200) } };
            var body = new SwingBody(new CityCollision(city));
            body.Teleport(new Vector3(0, 100, 0));
            body.Velocity = new Vector3(0, 0, 90);
            for (int i = 0; i < 120; i++) body.Step(Frame, new SwingInput());
            Assert.Less(body.Position.Z, 95f);
        }

        [Test]
        public void RunningReachesRunSpeedAndStopsWhenReleased()
        {
            SwingBody body = BodyInEmptyWorld();
            body.Teleport(new Vector3(0, 1.7f, 0));
            for (int i = 0; i < 60; i++) body.Step(Frame, new SwingInput { Move = Vector3.UnitZ });
            Assert.AreEqual(body.Settings.RunSpeed, body.Velocity.Z, 0.2f);
            for (int i = 0; i < 60; i++) body.Step(Frame, new SwingInput());
            Assert.AreEqual(0f, body.Velocity.Length(), 0.05f);
        }
    }
}
