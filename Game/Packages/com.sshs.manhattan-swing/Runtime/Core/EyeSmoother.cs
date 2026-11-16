using System;
using System.Numerics;

namespace ManhattanSwing.Core
{
    /// <summary>
    /// Makes head tracking look smooth on screen.
    ///
    /// The webcam tracker reports the eye position about 30 times a second, and each report is
    /// already a little old when it arrives. The screen redraws 60 or more times a second. If the
    /// view only moved when a report arrived, it would move in small jumps and trail behind the
    /// head. This class is asked for a position on every redraw: it carries on in the direction
    /// the head was last moving, to cover the time since the last report, and glides to that point
    /// instead of jumping.
    /// </summary>
    public sealed class EyeSmoother
    {
        /// <summary>The most time (seconds) it will carry a movement on past the last report.</summary>
        public float MaxLeadSeconds = 0.08f;
        /// <summary>How quickly the view glides to its target. Smaller is snappier; larger is softer.</summary>
        public float FollowSeconds = 0.02f;
        /// <summary>
        /// Below this speed (m/s) the head counts as still and nothing is carried on, so a player
        /// sitting still sees a steady picture and not the tracker's small wobble.
        /// </summary>
        public float StillSpeed = 0.03f;
        /// <summary>How much each new report changes the estimated speed (0 to 1). Lower is steadier.</summary>
        public float VelocityBlend = 0.7f;

        private Vector3 sample, velocity, output;
        private double sampleTime;
        private bool hasSample, hasOutput;

        public void Reset()
        {
            hasSample = hasOutput = false;
            velocity = Vector3.Zero;
        }

        /// <summary>Give it a new report. <paramref name="time"/> is when the webcam saw the face, in seconds.</summary>
        public void AddSample(Vector3 position, double time)
        {
            double gap = time - sampleTime;
            if (hasSample && gap > 1e-4 && gap < 0.3)
            {
                Vector3 measured = (position - sample) / (float)gap;
                velocity += (measured - velocity) * VelocityBlend;
            }
            else if (!hasSample || gap >= 0.3)
            {
                velocity = Vector3.Zero;   // first report, or the first after a break
            }
            sample = position;
            sampleTime = time;
            hasSample = true;
        }

        /// <summary>
        /// Use on redraws while no face is being tracked: glides to <paramref name="target"/> and
        /// forgets the last movement, so tracking starts fresh when the face comes back.
        /// </summary>
        public Vector3 Glide(Vector3 target, float deltaTime)
        {
            hasSample = false;
            velocity = Vector3.Zero;
            if (!hasOutput)
            {
                output = target;
                hasOutput = true;
                return output;
            }
            float follow = FollowSeconds <= 0 ? 1 : 1 - (float)Math.Exp(-deltaTime / FollowSeconds);
            output += (target - output) * follow;
            return output;
        }

        /// <summary>Call on every redraw while a face is tracked. Returns where to draw the view from.</summary>
        public Vector3 Update(double now, float deltaTime)
        {
            if (!hasSample) return output;

            float lead = (float)Math.Max(0, Math.Min(now - sampleTime, MaxLeadSeconds));
            float speed = velocity.Length();
            // Fade the carry-on in between "still" and twice that speed, so it does not switch on suddenly.
            float trust = speed <= StillSpeed ? 0 : Math.Min(1, (speed - StillSpeed) / StillSpeed);
            Vector3 target = sample + velocity * (lead * trust);

            if (!hasOutput)
            {
                output = target;
                hasOutput = true;
                return output;
            }
            float follow = FollowSeconds <= 0 ? 1 : 1 - (float)Math.Exp(-deltaTime / FollowSeconds);
            output += (target - output) * follow;
            return output;
        }
    }
}
