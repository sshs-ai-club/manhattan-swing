using System;
using System.Collections.Generic;
using System.Numerics;

namespace ManhattanSwing.Core
{
    /// <summary>What the player asks for during one frame.</summary>
    public struct SwingInput
    {
        /// <summary>Wanted movement direction in the world, flat on the ground, length 0 to 1.</summary>
        public Vector3 Move;
        public bool Reel;
        public bool Jump;
    }

    /// <summary>
    /// The player as a point with a web: gravity, a web that cannot stretch, running, jumping and
    /// collision with the city. Plain code with no Unity types, so it gives the same result every
    /// time and can be tested without opening Unity. The city is whatever <see cref="ISwingWorld"/>
    /// it is given, so the same swing runs on the test map and on a detailed map.
    /// </summary>
    public sealed class SwingBody
    {
        public readonly SwingSettings Settings;
        /// <summary>The map the body collides with. Can be swapped while playing.</summary>
        public ISwingWorld World;
        private readonly List<Vector3> contacts = new List<Vector3>();

        /// <summary>Eye position.</summary>
        public Vector3 Position;
        public Vector3 Velocity;
        public bool Grounded { get; private set; }
        public bool Attached { get; private set; }
        public Vector3 Anchor { get; private set; }
        public float WebLength { get; private set; }

        public SwingBody(ISwingWorld world, SwingSettings settings = null)
        {
            World = world;
            Settings = settings ?? new SwingSettings();
        }

        public void Teleport(Vector3 position)
        {
            Position = position;
            Velocity = Vector3.Zero;
            Grounded = false;
            Release();
        }

        /// <summary>Fixes a web to a point. Returns false when the point is too near or too far.</summary>
        public bool Attach(Vector3 anchor)
        {
            float distance = Vector3.Distance(Position, anchor);
            if (distance < Settings.MinWebLength || distance > Settings.MaxWebLength) return false;
            Anchor = anchor;
            WebLength = distance;
            Attached = true;
            return true;
        }

        /// <summary>
        /// Call when the host scene moves its whole world by <paramref name="delta"/> to keep numbers
        /// small (a "floating origin"). The body and its web move with everything else.
        /// </summary>
        public void ShiftOrigin(Vector3 delta)
        {
            Position += delta;
            Anchor += delta;
        }

        public void Release()
        {
            if (Attached && !Grounded && Velocity.Y > 0 && Settings.ReleaseBoost > 0)
            {
                // A small reward for letting go on the way up, which is the right moment.
                Velocity *= 1 + Settings.ReleaseBoost;
                float speed = Velocity.Length();
                if (speed > Settings.MaxSpeed) Velocity *= Settings.MaxSpeed / speed;
            }
            Attached = false;
        }

        /// <summary>Moves time forward by one frame.</summary>
        public void Step(float deltaTime, SwingInput input)
        {
            if (deltaTime <= 0) return;
            deltaTime = Math.Min(deltaTime, 0.1f);   // after a long pause, do not jump far ahead

            if (input.Jump && Grounded)
            {
                Velocity.Y = Settings.JumpSpeed;
                Grounded = false;
            }

            // Small steps keep the web stable and stop the body passing through a wall in one step.
            float speed = Velocity.Length();
            int steps = (int)Math.Ceiling(Math.Max(deltaTime / Settings.MaxStep,
                speed * deltaTime / (Settings.Radius * 0.8f)));
            steps = Math.Max(1, Math.Min(steps, 64));
            float h = deltaTime / steps;
            for (int i = 0; i < steps; i++) Substep(h, input);
        }

        private void Substep(float h, SwingInput input)
        {
            Vector3 move = input.Move;
            if (move.LengthSquared() > 1) move = Vector3.Normalize(move);

            if (Grounded && !Attached) Run(h, move);
            else Velocity += move * (Settings.AirControl * h);

            if (Attached) BoostSwing(h, move);

            Velocity.Y -= Settings.Gravity * h;
            Velocity *= (float)Math.Exp(-Settings.AirDrag * h);
            float speed = Velocity.Length();
            if (speed > Settings.MaxSpeed) Velocity *= Settings.MaxSpeed / speed;

            Position += Velocity * h;

            if (Attached) ApplyWeb(h, input.Reel || Settings.HoldReels || (Settings.AutoReelOnGround && Grounded));
            Collide();
        }

        private void Run(float h, Vector3 move)
        {
            Vector3 flat = new Vector3(Velocity.X, 0, Velocity.Z);
            if (move.LengthSquared() > 1e-4f)
            {
                Vector3 wanted = move * Settings.RunSpeed;
                Vector3 change = wanted - flat;
                float maxChange = Settings.RunAcceleration * h;
                if (change.Length() > maxChange) change = Vector3.Normalize(change) * maxChange;
                flat += change;
            }
            else
            {
                flat *= (float)Math.Exp(-Settings.GroundFriction * h);
            }
            Velocity = new Vector3(flat.X, Velocity.Y, flat.Z);
        }

        /// <summary>Holding a direction while on the web adds speed along the swing, up to a limit.</summary>
        private void BoostSwing(float h, Vector3 move)
        {
            if (move.LengthSquared() < 1e-4f || Velocity.Length() >= Settings.SwingBoostSpeed) return;
            Vector3 alongWeb = Vector3.Normalize(Position - Anchor);
            Vector3 push = move - alongWeb * Vector3.Dot(move, alongWeb);   // only the part across the web
            if (push.LengthSquared() > 1e-4f)
                Velocity += Vector3.Normalize(push) * (Settings.SwingBoostAcceleration * h);
        }

        /// <summary>The web cannot get longer than <see cref="WebLength"/>; it can go slack.</summary>
        private void ApplyWeb(float h, bool reel)
        {
            if (reel) WebLength = Math.Max(Settings.MinWebLength, WebLength - Settings.ReelSpeed * h);

            Vector3 fromAnchor = Position - Anchor;
            float distance = fromAnchor.Length();
            if (!Grounded && distance >= WebLength - 0.05f) ShortenWhileSwinging(h);
            if (distance <= WebLength || distance < 1e-5f) return;   // slack: the web does nothing

            Vector3 outward = fromAnchor / distance;
            Position = Anchor + outward * WebLength;
            // Remove the speed that points away from the anchor. While reeling, the web also pulls in.
            float outwardSpeed = Vector3.Dot(Velocity, outward);
            float allowed = reel ? -Settings.ReelSpeed : 0;
            if (outwardSpeed > allowed) Velocity -= outward * (outwardSpeed - allowed);
        }

        /// <summary>Help that only applies while the web is tight and the body is in the air.</summary>
        private void ShortenWhileSwinging(float h)
        {
            float shorten = Settings.SwingPullSpeed * h;

            // Keep the bottom of the swing off the street. Roofs are not considered.
            float longestSafe = Anchor.Y - World.GroundHeight(Anchor) - Settings.GroundClearance;
            if (WebLength > longestSafe)
                shorten = Math.Max(shorten, Math.Min(WebLength - longestSafe, Settings.ClearanceReelSpeed * h));

            WebLength = Math.Max(Settings.MinWebLength, WebLength - shorten);
        }

        private void Collide()
        {
            contacts.Clear();
            float footDrop = Settings.EyeHeight - Settings.Radius;
            World.ResolveSphere(ref Position, Settings.Radius, footDrop, contacts);

            Grounded = false;
            for (int i = 0; i < contacts.Count; i++)
            {
                Vector3 normal = contacts[i];
                float into = Vector3.Dot(Velocity, normal);
                if (into < 0) Velocity -= normal * into;      // stop moving into the surface, keep sliding
                if (normal.Y > 0.65f) Grounded = true;
            }
        }
    }
}
