using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// Reads the wrist web shooter. The device is an ESP32 that Windows sees as a Bluetooth gamepad,
    /// so there is no serial port or network code here: Unity's Input System hands us two axes and
    /// two buttons. The contract with the firmware is written in hardware/README.md:
    ///
    ///     stick X   = yaw   (hand turned left/right),  -1..+1 means -180..+180 degrees
    ///     stick Y   = pitch (hand tilted down/up),     -1..+1 means  -90..+90 degrees
    ///     button 1  = HOLD     (down while the web is held)
    ///     button 2  = RECENTRE (the hand's direction becomes the middle of the screen)
    ///
    /// To change how the device feels, change the settings below. To change what the device sends,
    /// change <see cref="ReadDevice"/> and the firmware together.
    ///
    /// NOT TESTED with a real device yet.
    /// </summary>
    public sealed class WristDevice
    {
        // ---- Settings ---------------------------------------------------------------------

        /// <summary>A device is taken as the web shooter when its name contains this text.</summary>
        public string NameContains = "SWING";

        /// <summary>Turning the hand this many degrees from the centre reaches the screen edge.</summary>
        public float YawRangeDegrees = 25f;
        public float PitchRangeDegrees = 15f;

        /// <summary>Flip these if the reticle moves the wrong way.</summary>
        public bool InvertYaw = false;
        public bool InvertPitch = false;

        /// <summary>How quickly the reticle follows the hand. 0 follows at once; larger is smoother but slower.</summary>
        public float SmoothingSeconds = 0.04f;

        /// <summary>
        /// When no web shooter is paired, use any ordinary gamepad or joystick in its place, so the
        /// game side can be worked on without the hardware. Its stick moves the reticle directly.
        /// </summary>
        public bool AllowStandInGamepad = true;

        // What full stick deflection means. These two numbers must match the firmware.
        private const float DegreesAtFullYawAxis = 180f;
        private const float DegreesAtFullPitchAxis = 90f;

        // ---- What the rest of the game reads ------------------------------------------------

        public bool Connected { get; private set; }
        /// <summary>True when the connected device is an ordinary gamepad standing in for the web shooter.</summary>
        public bool IsStandIn { get; private set; }
        public string DeviceName { get; private set; }
        /// <summary>Where the hand points on the screen: (0, 0) bottom left, (1, 1) top right.</summary>
        public Vector2 Aim { get; private set; }
        public bool Hold { get; private set; }

        // ---- Internal state -----------------------------------------------------------------

        private float centreYaw, centrePitch;   // the direction that counts as "screen centre"
        private float lastYaw, lastPitch;
        private bool wasRecentreDown;
        private bool hasCentre;

        public WristDevice()
        {
            Aim = new Vector2(0.5f, 0.5f);
            DeviceName = "";
        }

        /// <summary>Makes the hand's current direction the middle of the screen.</summary>
        public void Recentre()
        {
            centreYaw = lastYaw;
            centrePitch = lastPitch;
            hasCentre = true;
            Aim = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Call once per frame.</summary>
        public void Update(float deltaTime)
        {
            float axisX, axisY; bool hold, recentre;
            InputDevice device = FindDevice();
            if (device == null || !ReadDevice(device, out axisX, out axisY, out hold, out recentre))
            {
                Connected = false;
                Hold = false;
                return;
            }
            Connected = true;
            DeviceName = device.displayName;
            Hold = hold;

            Vector2 target;
            if (IsStandIn)
            {
                // An ordinary stick: centre is centre, full deflection is the screen edge.
                target = new Vector2(0.5f + axisX * 0.5f, 0.5f + axisY * 0.5f);
            }
            else
            {
                lastYaw = axisX * DegreesAtFullYawAxis * (InvertYaw ? -1 : 1);
                lastPitch = axisY * DegreesAtFullPitchAxis * (InvertPitch ? -1 : 1);
                if (!hasCentre || (recentre && !wasRecentreDown)) Recentre();
                // DeltaAngle handles yaw wrapping round from +180 to -180.
                float yaw = Mathf.DeltaAngle(centreYaw, lastYaw);
                float pitch = lastPitch - centrePitch;
                target = new Vector2(0.5f + yaw / (2 * YawRangeDegrees), 0.5f + pitch / (2 * PitchRangeDegrees));
            }
            wasRecentreDown = recentre;

            target = new Vector2(Mathf.Clamp01(target.x), Mathf.Clamp01(target.y));
            float follow = SmoothingSeconds <= 0 ? 1 : 1 - Mathf.Exp(-deltaTime / SmoothingSeconds);
            Aim = Vector2.Lerp(Aim, target, follow);
        }

        /// <summary>Picks the web shooter if one is paired, otherwise (if allowed) any gamepad.</summary>
        private InputDevice FindDevice()
        {
            InputDevice standIn = null;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Joystick) && !(device is Gamepad)) continue;
                string product = device.description.product ?? "";
                if (device.displayName.Contains(NameContains) || product.Contains(NameContains))
                {
                    IsStandIn = false;
                    return device;
                }
                if (standIn == null) standIn = device;
            }
            IsStandIn = standIn != null;
            return AllowStandInGamepad ? standIn : null;
        }

        /// <summary>
        /// Takes the two axes and two buttons from the device. Windows may present the ESP32 as a
        /// "joystick" or as a "gamepad"; both are handled. Raw values are read so that no dead zone
        /// is applied (a dead zone would make the reticle stick near the centre).
        /// </summary>
        private static bool ReadDevice(InputDevice device, out float axisX, out float axisY, out bool hold, out bool recentre)
        {
            axisX = axisY = 0; hold = recentre = false;

            var joystick = device as Joystick;
            if (joystick != null)
            {
                axisX = joystick.stick.x.ReadUnprocessedValue();
                axisY = joystick.stick.y.ReadUnprocessedValue();
                hold = joystick.trigger.isPressed;
                ButtonControl second = joystick.TryGetChildControl<ButtonControl>("button2");
                recentre = second != null && second.isPressed;
                return true;
            }

            var gamepad = device as Gamepad;
            if (gamepad != null)
            {
                axisX = gamepad.leftStick.x.ReadUnprocessedValue();
                axisY = gamepad.leftStick.y.ReadUnprocessedValue();
                hold = gamepad.buttonSouth.isPressed;
                recentre = gamepad.buttonEast.isPressed;
                return true;
            }
            return false;
        }
    }
}
