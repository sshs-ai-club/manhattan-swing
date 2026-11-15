using UnityEngine;
using UnityEngine.InputSystem;

namespace Swing.Game
{
    /// <summary>
    /// Everything the player can ask for in one frame, whichever device it came from.
    /// The rest of the game reads only this, so adding a device means changing only this file.
    ///
    /// Mouse and keyboard always work. When a wrist device is connected it moves the reticle and
    /// holds the web as well; the mouse still looks around.
    /// </summary>
    public sealed class PlayerInput
    {
        public float MouseDegreesPerPixel = 0.12f;
        public readonly WristDevice Wrist = new WristDevice();

        /// <summary>How far to turn the view this frame, in degrees (x = right, y = up).</summary>
        public Vector2 Look;
        /// <summary>Where the web is aimed on the screen: (0.5, 0.5) is the middle.</summary>
        public Vector2 Aim = new Vector2(0.5f, 0.5f);
        /// <summary>Walking and steering: x = right, y = forward, each -1 to 1.</summary>
        public Vector2 Move;
        public bool Hold, Reel, Jump;
        public bool RestartPressed, ConfirmPressed, CancelPressed, RecentrePressed;
        /// <summary>Developer key: write the current position to the log as a route line.</summary>
        public bool MarkPressed;

        public void Update(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Wrist.Update(deltaTime);

            Look = Vector2.zero;
            Move = Vector2.zero;
            Hold = Reel = Jump = false;
            RestartPressed = ConfirmPressed = CancelPressed = RecentrePressed = MarkPressed = false;

            if (mouse != null)
            {
                Look = mouse.delta.ReadValue() * MouseDegreesPerPixel;
                Hold = mouse.leftButton.isPressed;
                Reel = mouse.rightButton.isPressed;
                ConfirmPressed = mouse.leftButton.wasPressedThisFrame;
            }
            if (keyboard != null)
            {
                Move = new Vector2(
                    (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                Reel |= keyboard.eKey.isPressed;
                Jump = keyboard.spaceKey.wasPressedThisFrame;
                RestartPressed = keyboard.rKey.wasPressedThisFrame;
                RecentrePressed = keyboard.cKey.wasPressedThisFrame;
                CancelPressed = keyboard.escapeKey.wasPressedThisFrame;
                MarkPressed = keyboard.pKey.wasPressedThisFrame;
                ConfirmPressed |= keyboard.enterKey.wasPressedThisFrame;
            }

            if (Wrist.Connected)
            {
                Aim = Wrist.Aim;
                Hold |= Wrist.Hold;
                if (RecentrePressed) Wrist.Recentre();
            }
            else
            {
                Aim = new Vector2(0.5f, 0.5f);
            }
        }
    }
}
