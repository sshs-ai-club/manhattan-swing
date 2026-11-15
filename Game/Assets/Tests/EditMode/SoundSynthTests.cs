using System;
using NUnit.Framework;

namespace Swing.Core.Tests
{
    public class SoundSynthTests
    {
        private static void AssertSensible(float[] samples, float minSeconds, float maxSeconds)
        {
            float seconds = samples.Length / (float)SoundSynth.SampleRate;
            Assert.GreaterOrEqual(seconds, minSeconds);
            Assert.LessOrEqual(seconds, maxSeconds);
            float loudest = 0;
            foreach (float s in samples)
            {
                Assert.IsFalse(float.IsNaN(s));
                loudest = Math.Max(loudest, Math.Abs(s));
            }
            Assert.LessOrEqual(loudest, 1f, "samples must stay between -1 and 1");
            Assert.Greater(loudest, 0.05f, "the sound must not be silent");
        }

        [Test]
        public void EveryEffectIsAudibleShortAndInRange()
        {
            AssertSensible(SoundSynth.WebShoot(), 0.1f, 0.3f);
            AssertSensible(SoundSynth.WebAttach(), 0.1f, 0.3f);
            AssertSensible(SoundSynth.RingPass(), 0.3f, 0.8f);
            AssertSensible(SoundSynth.Finish(), 0.8f, 2f);
            AssertSensible(SoundSynth.CountdownBeep(), 0.1f, 0.3f);
            AssertSensible(SoundSynth.GoBeep(), 0.3f, 0.8f);
        }

        [Test]
        public void EffectsStartAndEndQuietlySoTheyDoNotClick()
        {
            foreach (float[] samples in new[] { SoundSynth.WebShoot(), SoundSynth.WebAttach(), SoundSynth.RingPass(), SoundSynth.Finish() })
            {
                Assert.Less(Math.Abs(samples[0]), 0.05f);
                Assert.Less(Math.Abs(samples[samples.Length - 1]), 0.05f);
            }
        }

        [Test]
        public void TheWindLoopJoinsUpWithoutAJump()
        {
            float[] wind = SoundSynth.WindLoop();
            AssertSensible(wind, 1.5f, 2f);
            // The largest step between neighbouring samples anywhere in the loop...
            float largestStep = 0;
            for (int i = 1; i < wind.Length; i++) largestStep = Math.Max(largestStep, Math.Abs(wind[i] - wind[i - 1]));
            // ...should not be exceeded by much where the end meets the start.
            Assert.Less(Math.Abs(wind[0] - wind[wind.Length - 1]), largestStep * 3);
        }
    }
}
