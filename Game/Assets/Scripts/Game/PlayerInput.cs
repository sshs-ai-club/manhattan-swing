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
        private const string SensitivityKey = "mouseDegreesPerPixel";

        /// <summary>Mouse sensitivity. Set in the tuning panel (F3) and remembered on this laptop.</summary>
        public float MouseDegreesPerPixel = PlayerPrefs.GetFloat(SensitivityKey, 0.12f);

        public void SaveSensitivity()
        {
            PlayerPrefs.SetFloat(SensitivityKey, MouseDegreesPerPixel);
        }

        public readonly WristDevice Wrist = new WristDevice();

        /// <summary>How far to turn the view this frame, in degrees (x = right, y = up).</summary>
        public Vector2 Look;
        /// <summary>Where the web is aimed on the screen: (0.5, 0.5) is the middle.</summary>
        public Vector2 Aim = new Vector2(0.5f, 0.5f);
        /// <summary>Walking and steering: x = right, y = forward, each -1 to 1.</summary>
        public Vector2 Move;
        public bool Hold, Reel, Jump;

        // Keys that act once when pressed.
        public bool RestartPressed;         // R
        public bool CheckpointPressed;      // Backspace: back to the last ring
        public bool CtrlHeld;               // Ctrl, held with R in booth mode
        public bool EnterPressed;           // Enter
        public bool ClickPressed;           // left mouse button
        public bool CancelPressed;          // Esc
        public bool RecentrePressed;        // C: wrist device
        public bool TrackingPressed;        // H: head tracking on/off
        public bool CaptureFacePressed;     // K: head tracking face reference
        public bool SetupPressed;           // F2: setup panel
        public bool TuningPressed;          // F3: tuning panel
        public bool ResetBoardPressed;      // Ctrl + Shift + Delete
        /// <summary>P, a developer key: write the current position to the log as a route line.</summary>
        public bool MarkPressed;

        /// <param name="typing">
        /// True while the player is typing in a text box. Letter keys then go to the text box only,
        /// so typing a name with an R in it does not restart the run.
        /// </param>
        public void Update(float deltaTime, bool typing)
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Wrist.Update(deltaTime);

            Look = Vector2.zero;
            Move = Vector2.zero;
            Hold = Reel = Jump = false;
            RestartPressed = EnterPressed = ClickPressed = CancelPressed = RecentrePressed = false;
            CheckpointPressed = CtrlHeld = false;
            TrackingPressed = CaptureFacePressed = SetupPressed = TuningPressed = ResetBoardPressed = MarkPressed = false;

            if (mouse != null)
            {
                Look = mouse.delta.ReadValue() * MouseDegreesPerPixel;
                Hold = mouse.leftButton.isPressed;
                Reel = mouse.rightButton.isPressed;
                ClickPressed = mouse.leftButton.wasPressedThisFrame;
            }
            if (keyboard != null)
            {
                CancelPressed = keyboard.escapeKey.wasPressedThisFrame;
                SetupPressed = keyboard.f2Key.wasPressedThisFrame;
                TuningPressed = keyboard.f3Key.wasPressedThisFrame;
                EnterPressed = keyboard.enterKey.wasPressedThisFrame;
                ResetBoardPressed = keyboard.ctrlKey.isPressed && keyboard.shiftKey.isPressed &&
                                    keyboard.deleteKey.wasPressedThisFrame;
                if (!typing)
                {
                    Move = new Vector2(
                        (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                        (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                    Reel |= keyboard.eKey.isPressed;
                    Jump = keyboard.spaceKey.wasPressedThisFrame;
                    RestartPressed = keyboard.rKey.wasPressedThisFrame;
                    CheckpointPressed = keyboard.backspaceKey.wasPressedThisFrame;
                    CtrlHeld = keyboard.ctrlKey.isPressed;
                    RecentrePressed = keyboard.cKey.wasPressedThisFrame;
                    TrackingPressed = keyboard.hKey.wasPressedThisFrame;
                    CaptureFacePressed = keyboard.kKey.wasPressedThisFrame;
                    MarkPressed = keyboard.pKey.wasPressedThisFrame;
                }
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
