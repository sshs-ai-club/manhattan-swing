using Swing.Core;
using UnityEngine;
using Num = System.Numerics;

namespace Swing.Game
{
    /// <summary>
    /// Connects input, the swing rules and the camera: turns the view, fires and lets go of the web,
    /// moves the body and draws the web line. First person: the camera is the player's eyes.
    /// </summary>
    public sealed class Player
    {
        /// <summary>
        /// With a wrist device, pointing near the side of the screen turns the view, so the game can
        /// be played with one hand. This is how far from the middle (0 to 0.5) turning starts.
        /// </summary>
        public float EdgeTurnStart = 0.3f;
        public float EdgeTurnDegreesPerSecond = 90f;

        public readonly SwingBody Body;
        private readonly CityCollision city;
        private readonly Camera camera;
        private readonly LineRenderer webLine;

        private float yaw, pitch;   // view direction in degrees; pitch is positive looking up

        /// <summary>True when holding now would catch a building. The HUD fills the reticle.</summary>
        public bool CanAttach { get; private set; }
        public float Speed { get { return Body.Velocity.Length(); } }

        public Player(CityCollision city, Camera camera, Material lineMaterial)
        {
            this.city = city;
            this.camera = camera;
            Body = new SwingBody(city);

            webLine = new GameObject("Web").AddComponent<LineRenderer>();
            webLine.sharedMaterial = lineMaterial;
            webLine.positionCount = 2;
            webLine.useWorldSpace = true;
            webLine.widthMultiplier = 0.07f;
            webLine.enabled = false;
        }

        public void PlaceAt(Num.Vector3 position, float yawDegrees)
        {
            Body.Teleport(position);
            yaw = yawDegrees;
            pitch = 0;
            ApplyToCamera();
        }

        /// <summary>One frame. When <paramref name="frozen"/> the player can look and aim but not move.</summary>
        public void Tick(float deltaTime, PlayerInput input, bool frozen)
        {
            Turn(deltaTime, input);
            ApplyToCamera();   // the aim ray below needs the camera's new direction

            Ray aim = camera.ViewportPointToRay(new Vector3(input.Aim.x, input.Aim.y, 0));
            RayHit hit;
            CanAttach = AimAssist.FindAnchor(city, Body.Position, aim.direction.ToCore(),
                Body.Settings.MaxWebLength, Body.Settings.MinWebLength, out hit);

            if (frozen)
            {
                Body.Release();
            }
            else
            {
                // Holding keeps trying until the web catches something; letting go releases it.
                if (!input.Hold) Body.Release();
                else if (!Body.Attached && CanAttach) Body.Attach(hit.Point);

                Body.Step(deltaTime, new SwingInput
                {
                    Move = WorldDirection(input.Move),
                    Reel = input.Reel,
                    Jump = input.Jump,
                });
            }

            ApplyToCamera();
            DrawWeb();
        }

        private void Turn(float deltaTime, PlayerInput input)
        {
            yaw += input.Look.x;
            pitch = Mathf.Clamp(pitch + input.Look.y, -85f, 85f);

            if (input.Wrist.Connected)
            {
                float offCentre = input.Aim.x - 0.5f;
                float beyond = Mathf.Abs(offCentre) - EdgeTurnStart;
                if (beyond > 0)
                {
                    float strength = beyond / (0.5f - EdgeTurnStart);
                    yaw += Mathf.Sign(offCentre) * strength * EdgeTurnDegreesPerSecond * deltaTime;
                }
            }
        }

        /// <summary>Turns "right / forward" input into a flat world direction for the current view.</summary>
        private Num.Vector3 WorldDirection(Vector2 move)
        {
            float radians = yaw * Mathf.Deg2Rad;
            var forward = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
            var right = new Vector3(forward.z, 0, -forward.x);
            return (forward * move.y + right * move.x).ToCore();
        }

        private void ApplyToCamera()
        {
            camera.transform.SetPositionAndRotation(Body.Position.ToUnity(), Quaternion.Euler(-pitch, yaw, 0));
        }

        private void DrawWeb()
        {
            webLine.enabled = Body.Attached;
            if (!Body.Attached) return;
            // The line starts a little below and to the right of the eyes, where a hand would be.
            Transform eyes = camera.transform;
            webLine.SetPosition(0, eyes.position + eyes.right * 0.25f - eyes.up * 0.3f + eyes.forward * 0.4f);
            webLine.SetPosition(1, Body.Anchor.ToUnity());
        }
    }
}
