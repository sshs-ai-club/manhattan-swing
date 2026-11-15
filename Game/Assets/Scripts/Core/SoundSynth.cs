using System;

namespace Swing.Core
{
    /// <summary>
    /// Makes the game's sound effects from maths, so there are no audio files to download or license.
    /// Every method returns one channel of samples between -1 and 1 at <see cref="SampleRate"/>.
    /// These are placeholders: replace any of them with a recorded sound when one is ready.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 44100;

        /// <summary>A short rising hiss: the web leaving the hand.</summary>
        public static float[] WebShoot()
        {
            var random = new Random(1);
            float[] samples = Silence(0.18f);
            double phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += 2 * Math.PI * Lerp(900, 2600, t) / SampleRate;
                float noise = (float)(random.NextDouble() * 2 - 1);
                samples[i] = (0.55f * noise + 0.3f * (float)Math.Sin(phase)) * Fade(t, 0.02f) * (1 - t) * 0.6f;
            }
            return samples;
        }

        /// <summary>A low thump: the web catching a building.</summary>
        public static float[] WebAttach()
        {
            float[] samples = Silence(0.14f);
            double phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += 2 * Math.PI * Lerp(210, 80, t) / SampleRate;
                samples[i] = (float)Math.Sin(phase) * (1 - t) * (1 - t) * Fade(t, 0.01f) * 0.8f;
            }
            return samples;
        }

        /// <summary>Two bright notes: a ring passed.</summary>
        public static float[] RingPass()
        {
            return Notes(new[] { 880f, 1320f }, 0.09f, 0.45f, 0.5f);
        }

        /// <summary>A rising run of four notes: the finish.</summary>
        public static float[] Finish()
        {
            return Notes(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.13f, 0.9f, 0.5f);
        }

        /// <summary>One beep for each second of the countdown.</summary>
        public static float[] CountdownBeep()
        {
            return Notes(new[] { 660f }, 0, 0.16f, 0.4f);
        }

        /// <summary>A longer, higher beep for "go".</summary>
        public static float[] GoBeep()
        {
            return Notes(new[] { 990f }, 0, 0.5f, 0.45f);
        }

        /// <summary>
        /// Two seconds of soft rushing air that can repeat without a click. The game plays it on a
        /// loop and turns it up and raises its pitch as the player speeds up.
        /// </summary>
        public static float[] WindLoop()
        {
            var random = new Random(2);
            float[] samples = Silence(2f);
            float smooth = 0, smoother = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                // White noise passed through two gentle low-pass filters sounds like wind.
                float noise = (float)(random.NextDouble() * 2 - 1);
                smooth += (noise - smooth) * 0.08f;
                smoother += (smooth - smoother) * 0.08f;
                samples[i] = smoother * 4f;
            }
            // Blend the end into the start so the loop point cannot be heard.
            int blend = SampleRate / 4;
            for (int i = 0; i < blend; i++)
            {
                float t = i / (float)blend;
                int tail = samples.Length - blend + i;
                samples[tail] = samples[tail] * (1 - t) + samples[i] * t;
            }
            float[] loop = new float[samples.Length - blend];
            Array.Copy(samples, blend, loop, 0, loop.Length);
            for (int i = 0; i < loop.Length; i++) loop[i] = Math.Max(-1, Math.Min(1, loop[i]));
            return loop;
        }

        // --- Helpers ----------------------------------------------------------------------

        /// <summary>Bell-like notes, each starting <paramref name="gap"/> seconds after the one before.</summary>
        private static float[] Notes(float[] frequencies, float gap, float noteSeconds, float volume)
        {
            float[] samples = Silence(gap * (frequencies.Length - 1) + noteSeconds);
            int noteLength = (int)(noteSeconds * SampleRate);
            for (int n = 0; n < frequencies.Length; n++)
            {
                int start = (int)(n * gap * SampleRate);
                for (int i = 0; i < noteLength && start + i < samples.Length; i++)
                {
                    float t = i / (float)noteLength;
                    double angle = 2 * Math.PI * frequencies[n] * i / SampleRate;
                    float tone = (float)(Math.Sin(angle) + 0.3 * Math.Sin(2 * angle));
                    samples[start + i] += tone * (float)Math.Exp(-5 * t) * Fade(t, 0.01f) * (1 - t) * volume / frequencies.Length;
                }
            }
            return samples;
        }

        private static float[] Silence(float seconds)
        {
            return new float[(int)(seconds * SampleRate)];
        }

        /// <summary>A quick fade-in at the start of a sound so it does not click.</summary>
        private static float Fade(float t, float fadeIn)
        {
            return t < fadeIn ? t / fadeIn : 1;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
