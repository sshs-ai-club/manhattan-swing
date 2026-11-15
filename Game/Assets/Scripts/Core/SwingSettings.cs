namespace Swing.Core
{
    /// <summary>
    /// Every number that changes how swinging feels. Lengths are metres, times are seconds.
    /// Starting values come from the tuning of the old project; none has been playtested here.
    /// </summary>
    public sealed class SwingSettings
    {
        // Falling and flying
        public float Gravity = 24f;             // m/s², stronger than real gravity so swings feel fast
        public float AirDrag = 0.01f;           // share of speed lost per second
        public float MaxSpeed = 95f;
        public float AirControl = 12f;          // m/s² of steering while in the air

        // The body
        public float Radius = 0.4f;             // collision sphere around the eyes
        public float EyeHeight = 1.7f;          // eyes above the feet when standing

        // On the ground and on roofs
        public float RunSpeed = 9f;
        public float RunAcceleration = 60f;
        public float GroundFriction = 14f;      // how quickly you stop when no key is held (1/s)
        public float JumpSpeed = 20f;           // upward speed of a jump (about 8 m high)

        // The web
        public float MaxWebLength = 250f;
        public float MinWebLength = 2f;
        public float ReelSpeed = 24f;           // m/s the web shortens while reeling
        /// <summary>
        /// While you stand on something and hold a web, the web reels in by itself and lifts you off.
        /// This lets a one-button device start a swing from the ground.
        /// </summary>
        public bool AutoReelOnGround = true;
        /// <summary>Holding forward while swinging speeds the swing up until this speed.</summary>
        public float SwingBoostSpeed = 34f;
        public float SwingBoostAcceleration = 12f;

        // Simulation accuracy
        public float MaxStep = 1f / 120f;       // longest physics step; long frames are split up
    }
}
