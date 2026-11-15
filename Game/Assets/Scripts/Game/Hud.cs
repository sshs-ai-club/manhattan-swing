using Swing.Core;
using UnityEngine;

namespace Swing.Game
{
    /// <summary>
    /// Everything drawn on top of the view: clock, ring count, speed, frame rate, reticle, the
    /// marker for the next ring and the start and finish messages. Uses Unity's immediate-mode GUI
    /// so the whole screen is described in this one file, with no scene objects to edit.
    /// </summary>
    public sealed class Hud
    {
        private GUIStyle small, medium, large, huge;

        public void Draw(Race race, Player player, PlayerInput input, Camera camera, float framesPerSecond, float bestTime)
        {
            // Lay everything out as if the screen were 1080 pixels tall, then scale to the real size.
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            const float height = 1080f;
            MakeStyles();

            Text(new Rect(24, 16, 600, 40), string.Format("{0:0} fps    {1:0} m/s", framesPerSecond, player.Speed), small, TextAnchor.UpperLeft);
            Text(new Rect(24, height - 56, 900, 40), DeviceStatus(input.Wrist), small, TextAnchor.LowerLeft);

            if (race.State == RaceState.Running || race.State == RaceState.Finished)
            {
                Text(new Rect(0, 20, width, 70), FormatTime(race.Time), large, TextAnchor.UpperCenter);
                Text(new Rect(0, 90, width, 40),
                    string.Format("Ring {0} / {1}", Mathf.Min(race.NextRing + 1, race.Route.Rings.Length), race.Route.Rings.Length),
                    medium, TextAnchor.UpperCenter);
            }

            var centre = new Rect(0, height * 0.28f, width, 200);
            var below = new Rect(0, height * 0.28f + 190, width, 220);
            switch (race.State)
            {
                case RaceState.Waiting:
                    Text(centre, "MANHATTAN SWING", huge, TextAnchor.MiddleCenter);
                    Text(below, "Click to start\n\nHold left mouse: web      Right mouse / E: reel in      Space: jump\nW A S D: move      R: restart      C: recentre wrist device      Esc: free the mouse",
                        medium, TextAnchor.UpperCenter);
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
                    Text(below, "Best this session: " + FormatTime(bestTime) + "\n\nPress R to run again", medium, TextAnchor.UpperCenter);
                    break;
            }

            if (race.State == RaceState.Running) DrawRingMarker(race, player, camera, width, height);
            DrawReticle(input.Aim, player.CanAttach, width, height);
        }

        public static string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60);
            return string.Format("{0}:{1:00.00}", minutes, seconds - minutes * 60);
        }

        private static string DeviceStatus(WristDevice wrist)
        {
            if (!wrist.Connected) return "Mouse (no wrist device connected)";
            return (wrist.IsStandIn ? "Stand-in gamepad: " : "Wrist device: ") + wrist.DeviceName;
        }

        /// <summary>Four corner marks around the aim point; a filled dot when a web would catch.</summary>
        private static void DrawReticle(Vector2 aim, bool canAttach, float width, float height)
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
        private void DrawRingMarker(Race race, Player player, Camera camera, float width, float height)
        {
            Vector3 ring = race.Route.Rings[race.NextRing].Centre.ToUnity();
            Vector3 view = camera.WorldToViewportPoint(ring);
            if (view.z < 0) { view.x = 1 - view.x; view.y = 0; }   // behind the player: point down the screen
            float x = Mathf.Clamp(view.x, 0.04f, 0.96f) * width;
            float y = (1 - Mathf.Clamp(view.y, 0.06f, 0.94f)) * height;
            Fill(new Rect(x - 6, y - 6, 12, 12), new Color(1f, 0.85f, 0.1f));
            float metres = Vector3.Distance(ring, camera.transform.position);
            Text(new Rect(x - 100, y + 8, 200, 34), string.Format("{0:0} m", metres), small, TextAnchor.UpperCenter);
        }

        private static void Fill(Rect area, Color colour)
        {
            Color before = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = before;
        }

        /// <summary>White text with a dark copy behind it so it reads over any background.</summary>
        private static void Text(Rect area, string text, GUIStyle style, TextAnchor anchor)
        {
            style.alignment = anchor;
            style.normal.textColor = new Color(0, 0, 0, 0.75f);
            GUI.Label(new Rect(area.x + 2, area.y + 2, area.width, area.height), text, style);
            style.normal.textColor = Color.white;
            GUI.Label(area, text, style);
        }

        private void MakeStyles()
        {
            if (small != null) return;
            small = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            medium = new GUIStyle(GUI.skin.label) { fontSize = 32 };
            large = new GUIStyle(GUI.skin.label) { fontSize = 56, fontStyle = FontStyle.Bold };
            huge = new GUIStyle(GUI.skin.label) { fontSize = 120, fontStyle = FontStyle.Bold };
        }
    }
}
