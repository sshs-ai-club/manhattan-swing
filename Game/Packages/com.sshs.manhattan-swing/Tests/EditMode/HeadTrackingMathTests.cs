using System;
using System.Numerics;
using NUnit.Framework;

namespace ManhattanSwing.Core.Tests
{
    public class HeadTrackingMathTests
    {
        private const double TrackerHz = 30, RenderHz = 60, Latency = 0.05;

        /// <summary>
        /// Plays a head movement through a pretend tracker (30 reports a second, each 50 ms old)
        /// and a 60 Hz screen. Returns how far the drawn position is from the real head, and how
        /// unevenly it moves, for "jump to each report" and for the smoother.
        /// </summary>
        private static void Simulate(Func<double, double> head, out double errorPlain, out double errorSmooth,
            out double unevenPlain, out double unevenSmooth)
        {
            var smoother = new EyeSmoother();
            double nextReport = 0, plain = head(0), frame = 1 / RenderHz;
            double plain1 = plain, plain2 = plain, smooth1 = plain, smooth2 = plain, real1 = plain, real2 = plain;
            double sumErrorPlain = 0, sumErrorSmooth = 0, sumUnevenPlain = 0, sumUnevenSmooth = 0;
            int frames = 0;
            bool any = false;

            for (double now = 0; now < 20; now += frame)
            {
                while (nextReport + Latency <= now)
                {
                    plain = head(nextReport);
                    smoother.AddSample(new Vector3((float)plain, 0, 0), nextReport);
                    nextReport += 1 / TrackerHz;
                    any = true;
                }
                double smooth = any ? smoother.Update(now, (float)frame).X : plain;
                double real = head(now);
                if (now > 1)
                {
                    sumErrorPlain += Sq(plain - real); sumErrorSmooth += Sq(smooth - real);
                    // "Uneven" = change of speed between frames that the real head did not make.
                    double realChange = real - 2 * real1 + real2;
                    sumUnevenPlain += Sq(plain - 2 * plain1 + plain2 - realChange);
                    sumUnevenSmooth += Sq(smooth - 2 * smooth1 + smooth2 - realChange);
                    frames++;
                }
                plain2 = plain1; plain1 = plain; smooth2 = smooth1; smooth1 = smooth; real2 = real1; real1 = real;
            }
            errorPlain = Math.Sqrt(sumErrorPlain / frames); errorSmooth = Math.Sqrt(sumErrorSmooth / frames);
            unevenPlain = Math.Sqrt(sumUnevenPlain / frames); unevenSmooth = Math.Sqrt(sumUnevenSmooth / frames);
        }

        private static double Sq(double x) { return x * x; }

        [Test]
        public void AMovingHeadIsDrawnCloserAndMoreEvenlyThanJumpingToEachReport()
        {
            // Swaying 8 cm each way every two seconds, with a faster 3 cm wobble on top.
            double errorPlain, errorSmooth, unevenPlain, unevenSmooth;
            Simulate(t => 0.08 * Math.Sin(2 * Math.PI * 0.5 * t) + 0.03 * Math.Sin(2 * Math.PI * 1.3 * t + 1),
                out errorPlain, out errorSmooth, out unevenPlain, out unevenSmooth);
            Assert.Less(errorSmooth, errorPlain * 0.6, "the view should trail the head much less");
            Assert.Less(unevenSmooth, unevenPlain * 0.5, "the view should move far more evenly");
        }

        [Test]
        public void AStillHeadGivesAStillPicture()
        {
            var smoother = new EyeSmoother();
            var random = new Random(3);
            float lowest = float.MaxValue, highest = float.MinValue;
            for (int i = 0; i < 300; i++)
            {
                // Reports wobble by up to half a millimetre around 5 cm.
                float wobble = (float)(random.NextDouble() - 0.5) * 0.001f;
                smoother.AddSample(new Vector3(0.05f + wobble, 0, 0), i / 30.0);
                for (int f = 0; f < 2; f++)
                {
                    float x = smoother.Update(i / 30.0 + 0.05 + f / 60.0, 1f / 60f).X;
                    if (i > 10) { lowest = Math.Min(lowest, x); highest = Math.Max(highest, x); }
                }
            }
            Assert.Less(highest - lowest, 0.001f, "the picture must not wobble more than the reports do");
        }

        [Test]
        public void AfterABreakInReportsTheOldMovementIsForgotten()
        {
            var smoother = new EyeSmoother();
            smoother.AddSample(new Vector3(0, 0, 0), 0);
            smoother.AddSample(new Vector3(0.03f, 0, 0), 0.033);   // moving fast to the right
            smoother.AddSample(new Vector3(0.2f, 0, 0), 2.0);      // two seconds later, somewhere else
            Vector3 drawn = Vector3.Zero;
            for (int f = 0; f < 60; f++) drawn = smoother.Update(2.05 + f / 60.0, 1f / 60f);
            Assert.AreEqual(0.2f, drawn.X, 0.001f, "it should settle on the new position, not run on");
        }

        [Test]
        public void TheViewTurnsTowardsTheDirectionOfTravelTheShortWay()
        {
            var east = new Vector3(20, 0, 0);
            Assert.Greater(ViewFollow.YawStep(0, east, 0.1f), 0f, "facing north, moving east: turn right");
            Assert.Less(ViewFollow.YawStep(180, east, 0.1f), 0f, "facing south, moving east: turn left");
            Assert.Less(ViewFollow.YawStep(-170, new Vector3(-1, 0, -20), 0.1f), 0f, "across the 180 line the short way");
            Assert.AreEqual(0f, ViewFollow.YawStep(90, east, 0.1f), 0.001f, "already facing that way");
        }

        [Test]
        public void TheViewIsLeftAloneWhenSlowOrFallingStraightDown()
        {
            Assert.AreEqual(0f, ViewFollow.YawStep(0, new Vector3(3, 0, 0), 0.1f));
            Assert.AreEqual(0f, ViewFollow.YawStep(0, new Vector3(0, -60, 0), 0.1f));
        }

        [Test]
        public void TheViewNeverTurnsFasterThanItsLimitOrPastItsTarget()
        {
            float step = ViewFollow.YawStep(0, new Vector3(-20, 0, -1), 0.1f);   // nearly behind
            Assert.AreEqual(ViewFollow.MaxDegreesPerSecond * 0.1f, Math.Abs(step), 0.001f);

            float yaw = 0;
            for (int i = 0; i < 600; i++) yaw += ViewFollow.YawStep(yaw, new Vector3(20, 0, 0), 1f / 60f);
            Assert.AreEqual(90f, yaw, 0.5f, "it settles on the travel direction without overshooting");
        }

        [Test]
        public void SmallHeadTurnsDoNothingAndLargerOnesTurnTheView()
        {
            Assert.AreEqual(0f, HeadTurn.DegreesPerSecond(0));
            Assert.AreEqual(0f, HeadTurn.DegreesPerSecond(9));
            Assert.AreEqual(0f, HeadTurn.DegreesPerSecond(-9));
            Assert.Greater(HeadTurn.DegreesPerSecond(20), 0f);
            Assert.Less(HeadTurn.DegreesPerSecond(-20), 0f);
            Assert.Less(HeadTurn.DegreesPerSecond(15), HeadTurn.DegreesPerSecond(25), "further turns faster");
            Assert.AreEqual(HeadTurn.FullTurnDegreesPerSecond, HeadTurn.DegreesPerSecond(30), 0.001f);
            Assert.AreEqual(HeadTurn.FullTurnDegreesPerSecond, HeadTurn.DegreesPerSecond(60), 0.001f, "there is a top speed");
        }
    }
}
