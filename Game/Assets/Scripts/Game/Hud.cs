using System.Globalization;
using Swing.Core;
using UnityEngine;

namespace Swing.Game
{
    /// <summary>What the HUD needs to know to draw one frame.</summary>
    public struct HudInfo
    {
        public Race Race;
        public Player Player;
        public PlayerInput Input;
        public Camera Camera;
        public float FramesPerSecond;
        public LeaderboardFile Scores;
        /// <summary>Place of the run that just finished (1 is fastest), or 0.</summary>
        public int LastPlace;
        /// <summary>Null when head tracking is not installed.</summary>
        public IHeadTracking Tracking;
    }

    /// <summary>
    /// Everything drawn on top of the view: name entry, clock, ring count, speed, frame rate,
    /// reticle, the marker for the next ring, the leaderboard and the setup panel. Uses Unity's
    /// immediate-mode GUI so the whole screen is described in this one file, with no scene objects.
    /// </summary>
    public sealed class Hud
    {
        private const int RowsShown = 10;

        /// <summary>The name being typed on the start screen.</summary>
        public string Name = "";
        /// <summary>Set when Enter is pressed on the start screen. The game clears it.</summary>
        public bool StartRequested;
        /// <summary>The setup panel (F2) for booth staff.</summary>
        public bool SetupOpen;
        /// <summary>The tuning panel (F3): sliders for how the swing feels.</summary>
        public bool TuningOpen;
        /// <summary>Enter is ignored until this time, so the key that ended one run cannot start the next.</summary>
        public float IgnoreEnterUntil;

        private GUIStyle small, medium, large, huge, field, panelText, panelButton, panelField;
        private float scale = 1, stylesMadeForScale;
        private string message = "";
        private float messageUntil;
        private string widthText, heightText, distanceText;
        private float confirmResetUntil;

        /// <summary>Shows a short message in the middle of the screen for a few seconds.</summary>
        public void Flash(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 3f;
        }

        public void Draw(HudInfo info)
        {
            // Positions and sizes below are written as if the screen were 1080 pixels tall. They are
            // multiplied by "scale" when drawn, and fonts are made at their real pixel size, so text
            // stays sharp on any screen. (Stretching a finished 1080p picture made it blurry.)
            scale = Screen.height / 1080f;
            float width = Screen.width / scale;
            const float height = 1080f;
            MakeStyles();
            Race race = info.Race;

            Text(new Rect(24, 16, 600, 40), string.Format("{0:0} fps    {1:0} m/s", info.FramesPerSecond, info.Player.Speed), small, TextAnchor.UpperLeft);
            string status = DeviceStatus(info.Input.Wrist);
            if (info.Tracking != null) status += "\n" + info.Tracking.Status;
            Text(new Rect(24, height - 96, 1200, 80), status, small, TextAnchor.LowerLeft);

            if (race.State == RaceState.Running || race.State == RaceState.Finished)
            {
                Text(new Rect(0, 20, width, 70), FormatTime(race.Time), large, TextAnchor.UpperCenter);
                Text(new Rect(0, 90, width, 40),
                    string.Format("Ring {0} / {1}", Mathf.Min(race.NextRing + 1, race.Route.Rings.Length), race.Route.Rings.Length),
                    medium, TextAnchor.UpperCenter);
            }

            var centre = new Rect(0, height * 0.2f, width, 200);
            var below = new Rect(0, height * 0.2f + 190, width, 260);
            switch (race.State)
            {
                case RaceState.Waiting:
                    Text(centre, "MANHATTAN SWING", huge, TextAnchor.MiddleCenter);
                    Text(new Rect(0, below.y, width, 50), "Type your name and press Enter", medium, TextAnchor.UpperCenter);
                    DrawNameField(new Rect(width / 2 - 230, below.y + 60, 460, 70));
                    Text(new Rect(0, below.y + 160, width, 200),
                        "Hold left mouse: web      Right mouse / E: reel in      Space: jump      W A S D: move\nR: restart      Esc: free the mouse      F2: setup      F3: tuning",
                        small, TextAnchor.UpperCenter);
                    DrawLeaderboard(info, new Rect(width - 470, 150, 440, 600), 0);
                    break;
                case RaceState.Countdown:
                    Text(centre, Mathf.CeilToInt(race.CountdownLeft).ToString(), huge, TextAnchor.MiddleCenter);
                    Text(below, "Point at a building and hold to swing", medium, TextAnchor.UpperCenter);
                    break;
                case RaceState.Running:
                    if (race.Time < 1f) Text(centre, "GO", huge, TextAnchor.MiddleCenter);
                    break;
                case RaceState.Finished:
                    Text(centre, "FINISH  " + FormatTime(race.Time), huge, TextAnchor.MiddleCenter);
                    Text(below, string.Format("{0}: place {1} of {2}\n\nR: run again      Enter: next player",
                            Leaderboard.CleanName(Name), info.LastPlace, info.Scores.Board.Count),
                        medium, TextAnchor.UpperCenter);
                    DrawLeaderboard(info, new Rect(width - 470, 150, 440, 600), info.LastPlace);
                    break;
            }

            if (Time.unscaledTime < messageUntil)
                Text(new Rect(0, height * 0.72f, width, 60), message, medium, TextAnchor.MiddleCenter);

            if (race.State == RaceState.Running) DrawRingMarker(info, width, height);
            if (race.State != RaceState.Waiting && !SetupOpen && !TuningOpen) DrawReticle(info.Input.Aim, info.Player.CanAttach, width, height);
            if (SetupOpen) DrawSetup(info);
            if (TuningOpen) DrawTuning(info);
        }

        public static string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60);
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00.00}", minutes, seconds - minutes * 60);
        }

        private static string DeviceStatus(WristDevice wrist)
        {
            if (!wrist.Connected) return "Mouse (no wrist device connected)";
            return (wrist.IsStandIn ? "Stand-in gamepad: " : "Wrist device: ") + wrist.DeviceName;
        }

        // --- Start screen ------------------------------------------------------------------

        private void DrawNameField(Rect area)
        {
            // Look for Enter before the text box sees the key.
            Event e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
            if (enter && !SetupOpen)
            {
                if (Time.unscaledTime >= IgnoreEnterUntil) StartRequested = true;
                e.Use();
            }

            GUI.SetNextControlName("name");
            Name = GUI.TextField(Scaled(area), Name, Leaderboard.MaxNameLength, field);
            if (!SetupOpen && GUI.GetNameOfFocusedControl() != "name") GUI.FocusControl("name");
        }

        /// <summary>The fastest runs. <paramref name="highlightPlace"/> marks the run that just finished.</summary>
        private void DrawLeaderboard(HudInfo info, Rect area, int highlightPlace)
        {
            Leaderboard board = info.Scores.Board;
            Text(new Rect(area.x, area.y, area.width, 44), "FASTEST RUNS", medium, TextAnchor.UpperLeft);
            if (board.Count == 0)
            {
                Text(new Rect(area.x, area.y + 50, area.width, 40), "No runs yet. Be the first.", small, TextAnchor.UpperLeft);
                return;
            }
            for (int i = 0; i < board.Count && i < RowsShown; i++)
            {
                var row = new Rect(area.x, area.y + 54 + i * 40, area.width, 40);
                if (i + 1 == highlightPlace) Fill(new Rect(row.x - 10, row.y, row.width + 20, 38), new Color(1f, 0.85f, 0.1f, 0.35f));
                Text(row, string.Format("{0,2}.  {1}", i + 1, board[i].Name), small, TextAnchor.UpperLeft);
                Text(row, FormatTime(board[i].Seconds), small, TextAnchor.UpperRight);
            }
        }

        // --- During a run ------------------------------------------------------------------

        /// <summary>Four marks around the aim point; a filled dot when a web would catch.</summary>
        private void DrawReticle(Vector2 aim, bool canAttach, float width, float height)
        {
            float x = aim.x * width, y = (1 - aim.y) * height;
            Color colour = canAttach ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 1f, 1f, 0.6f);
            const float gap = 14, length = 10, thick = 3;
            Fill(new Rect(x - gap - length, y - thick / 2, length, thick), colour);
            Fill(new Rect(x + gap, y - thick / 2, length, thick), colour);
            Fill(new Rect(x - thick / 2, y - gap - length, thick, length), colour);
            Fill(new Rect(x - thick / 2, y + gap, thick, length), colour);
            if (canAttach) Fill(new Rect(x - 4, y - 4, 8, 8), colour);
        }

        /// <summary>A small marker with the distance to the next ring, kept on screen when the ring is not.</summary>
        private void DrawRingMarker(HudInfo info, float width, float height)
        {
            Vector3 ring = info.Race.Route.Rings[info.Race.NextRing].Centre.ToUnity();
            Vector3 view = info.Camera.WorldToViewportPoint(ring);
            if (view.z < 0) { view.x = 1 - view.x; view.y = 0; }   // behind the player: point down the screen
            float x = Mathf.Clamp(view.x, 0.04f, 0.96f) * width;
            float y = (1 - Mathf.Clamp(view.y, 0.06f, 0.94f)) * height;
            Fill(new Rect(x - 6, y - 6, 12, 12), new Color(1f, 0.85f, 0.1f));
            float metres = Vector3.Distance(ring, info.Camera.transform.position);
            Text(new Rect(x - 100, y + 8, 200, 34), string.Format("{0:0} m", metres), small, TextAnchor.UpperCenter);
        }

        // --- Setup panel (F2) --------------------------------------------------------------

        private void DrawSetup(HudInfo info)
        {
            var area = new Rect(24, 70, 640, 800);
            Fill(area, new Color(0, 0, 0, 0.8f));
            GUILayout.BeginArea(Scaled(new Rect(area.x + 20, area.y + 16, area.width - 40, area.height - 32)));
            GUILayout.Label("SETUP   (F2 closes)", panelText);
            GUILayout.Space(12 * scale);

            IHeadTracking tracking = info.Tracking;
            if (tracking == null)
            {
                GUILayout.Label("Head tracking is not installed.\nSee docs/HEAD-TRACKING.md.", panelText);
            }
            else
            {
                if (GUILayout.Button("Head tracking: " + (tracking.On ? "ON" : "OFF"), panelButton)) tracking.On = !tracking.On;
                GUILayout.Label(tracking.Status, panelText);
                GUILayout.Space(8 * scale);

                if (widthText == null)
                {
                    widthText = Number(tracking.ScreenWidthCm);
                    heightText = Number(tracking.ScreenHeightCm);
                    distanceText = Number(tracking.EyeDistanceCm);
                }
                GUILayout.Label("Measure the lit part of the monitor with a ruler (cm):", panelText);
                widthText = LabelledField("Screen width", widthText);
                heightText = LabelledField("Screen height", heightText);
                distanceText = LabelledField("Eyes to screen", distanceText);
                if (GUILayout.Button("Apply sizes", panelButton))
                {
                    float w, h, d;
                    if (Parse(widthText, out w) && Parse(heightText, out h) && Parse(distanceText, out d))
                    {
                        tracking.ScreenWidthCm = w; tracking.ScreenHeightCm = h; tracking.EyeDistanceCm = d;
                        tracking.Apply();
                        Flash("Screen sizes saved");
                    }
                    else Flash("Sizes must be numbers between 10 and 300");
                }
                GUILayout.Space(8 * scale);
                if (GUILayout.Button("Capture face (sit normally, look at the screen)", panelButton))
                    Flash(tracking.CaptureFaceReference() ? "Face captured" : "No face found: is the tracker running?");
                GUILayout.Label("Start the webcam tracker with:\n" + tracking.TrackerCommand, panelText);
            }

            GUILayout.Space(20 * scale);
            GUILayout.Label("Leaderboard: " + info.Scores.Board.Count + " runs saved", panelText);
            bool confirming = Time.unscaledTime < confirmResetUntil;
            if (GUILayout.Button(confirming ? "Click again to empty the leaderboard" : "Reset leaderboard", panelButton))
            {
                if (confirming)
                {
                    info.Scores.Reset();
                    confirmResetUntil = 0;
                    Flash("Leaderboard emptied (the old file was kept)");
                }
                else confirmResetUntil = Time.unscaledTime + 3f;
            }
            GUILayout.EndArea();
        }

        // --- Tuning panel (F3) -------------------------------------------------------------

        /// <summary>
        /// Sliders for the numbers in SwingSettings.cs, so the swing can be tuned while playing.
        /// Changes last until the game is closed. When it feels right, press "Write values to the
        /// Console" and copy them into SwingSettings.cs so everyone gets them.
        /// </summary>
        private void DrawTuning(HudInfo info)
        {
            SwingSettings s = info.Player.Body.Settings;
            var area = new Rect(24, 70, 700, 900);
            Fill(area, new Color(0, 0, 0, 0.8f));
            GUILayout.BeginArea(Scaled(new Rect(area.x + 20, area.y + 16, area.width - 40, area.height - 32)));
            GUILayout.Label("TUNING   (F3 closes, changes last until you quit)", panelText);
            GUILayout.Space(10 * scale);

            float before = info.Input.MouseDegreesPerPixel;
            info.Input.MouseDegreesPerPixel = Slider("Mouse sensitivity", before, 0.02f, 0.4f, "0.00");
            if (info.Input.MouseDegreesPerPixel != before) info.Input.SaveSensitivity();
            GUILayout.Space(10 * scale);

            s.Gravity = Slider("Gravity", s.Gravity, 8, 40, "0");
            s.SwingPullSpeed = Slider("Swing pull (height gained per swing)", s.SwingPullSpeed, 0, 10, "0.0");
            s.ReleaseBoost = Slider("Release boost", s.ReleaseBoost, 0, 0.3f, "0.00");
            s.SwingBoostSpeed = Slider("Forward key speeds up to", s.SwingBoostSpeed, 10, 80, "0");
            s.SwingBoostAcceleration = Slider("Forward key strength", s.SwingBoostAcceleration, 0, 40, "0");
            s.AirControl = Slider("Air steering", s.AirControl, 0, 40, "0");
            s.GroundClearance = Slider("Keep swings above street by", s.GroundClearance, 0, 20, "0");
            s.ReelSpeed = Slider("Reel speed", s.ReelSpeed, 8, 60, "0");
            s.JumpSpeed = Slider("Jump", s.JumpSpeed, 5, 35, "0");
            s.MaxWebLength = Slider("Longest web", s.MaxWebLength, 60, 300, "0");
            s.MaxSpeed = Slider("Top speed", s.MaxSpeed, 30, 120, "0");
            s.AutoReelOnGround = GUILayout.Toggle(s.AutoReelOnGround, "  Web lifts you off the ground by itself", panelText);

            GUILayout.Space(12 * scale);
            if (GUILayout.Button("Back to the built-in values", panelButton))
            {
                var d = new SwingSettings();
                s.Gravity = d.Gravity; s.SwingPullSpeed = d.SwingPullSpeed; s.ReleaseBoost = d.ReleaseBoost;
                s.SwingBoostSpeed = d.SwingBoostSpeed; s.SwingBoostAcceleration = d.SwingBoostAcceleration;
                s.AirControl = d.AirControl; s.GroundClearance = d.GroundClearance; s.ReelSpeed = d.ReelSpeed;
                s.JumpSpeed = d.JumpSpeed; s.MaxWebLength = d.MaxWebLength; s.MaxSpeed = d.MaxSpeed;
                s.AutoReelOnGround = d.AutoReelOnGround;
            }
            if (GUILayout.Button("Write values to the Console", panelButton))
            {
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "Swing tuning: Gravity {0}, SwingPullSpeed {1}, ReleaseBoost {2}, SwingBoostSpeed {3}, SwingBoostAcceleration {4}, " +
                    "AirControl {5}, GroundClearance {6}, ReelSpeed {7}, JumpSpeed {8}, MaxWebLength {9}, MaxSpeed {10}, AutoReelOnGround {11}, mouse {12}",
                    s.Gravity, s.SwingPullSpeed, s.ReleaseBoost, s.SwingBoostSpeed, s.SwingBoostAcceleration, s.AirControl,
                    s.GroundClearance, s.ReelSpeed, s.JumpSpeed, s.MaxWebLength, s.MaxSpeed, s.AutoReelOnGround, info.Input.MouseDegreesPerPixel));
                Flash("Values written to the Console");
            }
            GUILayout.EndArea();
        }

        private float Slider(string label, float value, float min, float max, string format)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, panelText, GUILayout.Width(330 * scale));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(230 * scale), GUILayout.Height(28 * scale));
            GUILayout.Label(value.ToString(format, CultureInfo.InvariantCulture), panelText, GUILayout.Width(70 * scale));
            GUILayout.EndHorizontal();
            return value;
        }

        private string LabelledField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, panelText, GUILayout.Width(260 * scale));
            value = GUILayout.TextField(value, 6, panelField, GUILayout.Width(140 * scale));
            GUILayout.EndHorizontal();
            return value;
        }

        private int FontSize(float sizeAt1080)
        {
            return Mathf.Max(8, Mathf.RoundToInt(sizeAt1080 * scale));
        }

        private static string Number(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static bool Parse(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 10 && value <= 300;
        }

        // --- Drawing helpers ---------------------------------------------------------------

        /// <summary>Turns a rectangle written for a 1080-pixel-tall screen into real pixels.</summary>
        private Rect Scaled(Rect area)
        {
            return new Rect(area.x * scale, area.y * scale, area.width * scale, area.height * scale);
        }

        private void Fill(Rect area, Color colour)
        {
            Color before = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(Scaled(area), Texture2D.whiteTexture);
            GUI.color = before;
        }

        /// <summary>White text with a dark copy behind it so it reads over any background.</summary>
        private void Text(Rect area, string text, GUIStyle style, TextAnchor anchor)
        {
            style.alignment = anchor;
            style.normal.textColor = new Color(0, 0, 0, 0.75f);
            GUI.Label(Scaled(new Rect(area.x + 2, area.y + 2, area.width, area.height)), text, style);
            style.normal.textColor = Color.white;
            GUI.Label(Scaled(area), text, style);
        }

        private void MakeStyles()
        {
            if (small != null && Mathf.Approximately(stylesMadeForScale, scale)) return;
            stylesMadeForScale = scale;
            small = new GUIStyle(GUI.skin.label) { fontSize = FontSize(24) };
            medium = new GUIStyle(GUI.skin.label) { fontSize = FontSize(32) };
            large = new GUIStyle(GUI.skin.label) { fontSize = FontSize(56), fontStyle = FontStyle.Bold };
            huge = new GUIStyle(GUI.skin.label) { fontSize = FontSize(120), fontStyle = FontStyle.Bold };
            field = new GUIStyle(GUI.skin.textField) { fontSize = FontSize(40), alignment = TextAnchor.MiddleCenter };
            panelText = new GUIStyle(GUI.skin.label) { fontSize = FontSize(22), wordWrap = true };
            panelButton = new GUIStyle(GUI.skin.button) { fontSize = FontSize(22) };
            panelField = new GUIStyle(GUI.skin.textField) { fontSize = FontSize(22) };
        }
    }
}
