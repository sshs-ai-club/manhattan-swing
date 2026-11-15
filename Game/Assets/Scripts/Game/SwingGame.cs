using Swing.Core;
using UnityEngine;

namespace Swing.Game
{
    /// <summary>
    /// The whole game in one place: loads the city and the route, builds what is drawn, and runs
    /// each frame in a fixed order (input, player, race, sound, screen).
    ///
    /// A turn at the booth goes: type a name, countdown, run, result with the leaderboard, then
    /// Enter for the next player.
    ///
    /// Nothing is set up in the Unity scene. The scene is empty and this object creates itself
    /// when Play starts, so two people never have to edit the same scene file.
    /// </summary>
    public sealed class SwingGame : MonoBehaviour
    {
        private const string CityFile = "city";              // Assets/Resources/city.bytes
        private const string RouteFile = "route_midtown";    // Assets/Resources/route_midtown.txt

        private Camera view;
        private PlayerInput input;
        private Player player;
        private Race race;
        private RingView rings;
        private Hud hud;
        private GameAudio sound;
        private LeaderboardFile scores;
        private IHeadTracking tracking;   // null when the head-tracking package is not installed
        private int lastPlace;
        private int lastCountdownBeep;
        private float framesPerSecond = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnPlay()
        {
            if (FindFirstObjectByType<SwingGame>() == null) new GameObject("Game").AddComponent<SwingGame>();
        }

        private void Awake()
        {
            QualitySettings.vSyncCount = 1;

            CityData city = CityData.Load(Resources.Load<TextAsset>(CityFile).bytes);
            Route route = Route.Parse(Resources.Load<TextAsset>(RouteFile).text);
            var collision = new CityCollision(city);
            Shader unlit = Resources.Load<Shader>("Shaders/SwingUnlit");

            CityView.Build(city);
            Transform eyes = new GameObject("Eyes").transform;
            view = CreateCamera(eyes);
            input = new PlayerInput();
            player = new Player(collision, view, eyes, new Material(unlit) { color = Color.white });
            race = new Race(route);
            rings = new RingView(route, unlit);
            hud = new Hud();
            sound = new GameAudio(gameObject);
            scores = new LeaderboardFile("race");
            if (HeadTrackingHook.Create != null)
                tracking = HeadTrackingHook.Create(view, eyes, new Material(unlit) { color = new Color(0.05f, 0.05f, 0.06f) });

            player.PlaceAt(route.Start, route.StartYawDegrees);
            rings.Show(0);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            framesPerSecond = Mathf.Lerp(framesPerSecond, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.05f);

            bool typing = race.State == RaceState.Waiting || hud.SetupOpen;
            input.Update(deltaTime, typing);
            HandleKeys();

            // The mouse only turns the view while it is captured by the game.
            if (Cursor.lockState != CursorLockMode.Locked) input.Look = Vector2.zero;

            System.Numerics.Vector3 before = player.Body.Position;
            bool frozen = race.State == RaceState.Waiting || race.State == RaceState.Countdown;
            player.Tick(deltaTime, input, frozen);
            if (player.FiredThisFrame) sound.WebShoot();
            if (player.AttachedThisFrame) sound.WebAttach();
            sound.SetSpeed(frozen ? 0 : player.Speed);

            UpdateRace(deltaTime, before);
        }

        private void HandleKeys()
        {
            if (input.SetupPressed)
            {
                hud.SetupOpen = !hud.SetupOpen;
                if (hud.SetupOpen) Cursor.lockState = CursorLockMode.None;
            }
            if (input.CancelPressed) Cursor.lockState = CursorLockMode.None;

            if (tracking != null && input.TrackingPressed) tracking.On = !tracking.On;
            if (tracking != null && input.CaptureFacePressed)
                hud.Flash(tracking.CaptureFaceReference() ? "Face captured" : "No face found: is the tracker running?");

            switch (race.State)
            {
                case RaceState.Waiting:
                    if (hud.StartRequested && !hud.SetupOpen) StartRun();
                    break;
                case RaceState.Countdown:
                case RaceState.Running:
                    if (input.RestartPressed) StartRun();
                    else if (input.ClickPressed && !hud.SetupOpen) Cursor.lockState = CursorLockMode.Locked;
                    break;
                case RaceState.Finished:
                    if (input.RestartPressed) StartRun();
                    else if (input.EnterPressed) NextPlayer();
                    break;
            }
            hud.StartRequested = false;

            if (input.MarkPressed)
            {
                // For making routes: paste this line into a route file.
                Vector3 p = view.transform.position;
                Debug.Log(string.Format("ring {0:0} {1:0} {2:0} 11", p.x, p.y, p.z));
            }
        }

        private void UpdateRace(float deltaTime, System.Numerics.Vector3 before)
        {
            if (race.State == RaceState.Countdown)
            {
                int second = Mathf.CeilToInt(race.CountdownLeft);
                if (second != lastCountdownBeep && second > 0)
                {
                    lastCountdownBeep = second;
                    sound.CountdownBeep();
                }
            }

            RaceState stateBefore = race.State;
            bool passedRing = race.Update(deltaTime, before, player.Body.Position);
            if (stateBefore == RaceState.Countdown && race.State == RaceState.Running) sound.GoBeep();

            if (!passedRing) return;
            rings.Show(race.NextRing);
            if (race.State == RaceState.Finished)
            {
                sound.Finish();
                lastPlace = scores.Add(hud.Name, race.Time);
            }
            else sound.RingPass();
        }

        private void OnGUI()
        {
            hud.Draw(new HudInfo
            {
                Race = race, Player = player, Input = input, Camera = view, FramesPerSecond = framesPerSecond,
                Scores = scores, LastPlace = lastPlace, Tracking = tracking,
            });
        }

        /// <summary>Puts the player back on the start roof and begins the countdown.</summary>
        private void StartRun()
        {
            hud.SetupOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            player.PlaceAt(race.Route.Start, race.Route.StartYawDegrees);
            race.StartCountdown();
            rings.Show(0);
            lastPlace = 0;
            lastCountdownBeep = 0;
            if (input.Wrist.Connected) input.Wrist.Recentre();
        }

        /// <summary>Back to the name screen for whoever is next in the queue.</summary>
        private void NextPlayer()
        {
            Cursor.lockState = CursorLockMode.None;
            player.PlaceAt(race.Route.Start, race.Route.StartYawDegrees);
            race.Reset();
            rings.Show(0);
            lastPlace = 0;
            hud.Name = "";
            hud.IgnoreEnterUntil = Time.unscaledTime + 0.3f;
        }

        /// <summary>The camera hangs under "eyes", which the player moves. Head tracking, when on, shifts it within that.</summary>
        private static Camera CreateCamera(Transform eyes)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var holder = new GameObject("Camera") { tag = "MainCamera" };
                camera = holder.AddComponent<Camera>();
                holder.AddComponent<AudioListener>();
            }
            camera.transform.SetParent(eyes, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CityView.Sky;
            camera.fieldOfView = 75;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 3000;   // a little past where the haze becomes solid
            return camera;
        }
    }
}
