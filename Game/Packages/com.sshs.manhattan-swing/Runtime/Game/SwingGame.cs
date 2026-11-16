using ManhattanSwing.Core;
using UnityEngine;
using Num = System.Numerics;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// The game in one place: the race, the player, the HUD, sound and the leaderboard. Runs each
    /// frame in a fixed order (input, player, race, sound, screen).
    ///
    /// A turn at the booth goes: type a name, countdown, run, result with the leaderboard, then
    /// Enter for the next player.
    ///
    /// The game does not build a city or know which map it is on. A scene starts it with
    /// <see cref="Create"/> and a <see cref="SwingGameSetup"/>: the test map does this in
    /// TestMapStart, and a detailed map does it in its own start-up script.
    /// </summary>
    public sealed class SwingGame : MonoBehaviour
    {
        private SwingGameSetup setup;
        private Camera view;
        private PlayerInput input;
        private Player player;
        private Race race;
        private RingView rings;
        private Hud hud;
        private GameAudio sound;
        private LeaderboardFile scores;
        private IHeadTracking tracking;   // null when the head-tracking package is not installed
        private GameOptions options;
        private int lastPlace;
        private int lastCountdownBeep;
        private float framesPerSecond = 60;
        private Vector3 totalShift;       // how far the scene has moved its origin since the start

        /// <summary>Starts the game in the current scene.</summary>
        public static SwingGame Create(SwingGameSetup setup)
        {
            if (setup == null || setup.World == null || setup.Route == null)
                throw new System.ArgumentException("SwingGameSetup needs a World and a Route.");
            var game = new GameObject("Swing game").AddComponent<SwingGame>();
            game.Begin(setup);
            return game;
        }

        /// <summary>The map the player is on. Can be swapped while playing, for example to compare two kinds of collision.</summary>
        public ISwingWorld World
        {
            get { return player.Body.World; }
            set { player.Body.World = value; }
        }

        /// <summary>
        /// Call when the scene moves its whole world by <paramref name="delta"/> to keep numbers small
        /// (a "floating origin"): everything that was at p is now at p + delta. The player, the web,
        /// the rings and the checkpoints move with it.
        /// </summary>
        public void ShiftOrigin(Vector3 delta)
        {
            Num.Vector3 d = delta.ToCore();
            totalShift += delta;
            player.ShiftOrigin(d);
            race.ShiftOrigin(d);
            rings.ShiftOrigin(delta);
            var physics = player.Body.World as PhysicsWorld;
            if (physics != null) physics.ShiftOrigin(delta);
        }

        /// <summary>Shows a short message in the middle of the screen.</summary>
        public void Flash(string message)
        {
            hud.Flash(message);
        }

        private void Begin(SwingGameSetup gameSetup)
        {
            setup = gameSetup;
            QualitySettings.vSyncCount = 1;

            Route route = setup.Route.InScene(setup.Frame);
            options = new GameOptions();
            Shader unlit = UnlitShader.Find();

            Transform eyes = new GameObject("Eyes").transform;
            view = PrepareCamera(setup.Camera, eyes, setup.Sky);
            input = new PlayerInput();
            player = new Player(setup.World, view, eyes, new Material(unlit) { color = Color.white });
            race = new Race(route);
            rings = new RingView(route, unlit);
            hud = new Hud();
            sound = new GameAudio(gameObject);
            scores = new LeaderboardFile(setup.LeaderboardName);
            if (setup.HeadTracking && HeadTrackingHook.Create != null)
                tracking = HeadTrackingHook.Create(view, eyes, new Material(unlit) { color = new Color(0.05f, 0.05f, 0.06f) });

            player.PlaceAt(route.Start, route.StartYawDegrees);
            rings.Show(0);
        }

        private void Update()
        {
            if (setup == null) return;   // not started with Create
            float deltaTime = Time.deltaTime;
            framesPerSecond = Mathf.Lerp(framesPerSecond, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.05f);

            bool typing = race.State == RaceState.Waiting || hud.SetupOpen;
            input.Update(deltaTime, typing);
            HandleKeys();

            // The mouse only turns the view while it is captured by the game.
            if (Cursor.lockState != CursorLockMode.Locked) input.Look = Vector2.zero;

            System.Numerics.Vector3 before = player.Body.Position;
            bool frozen = race.State == RaceState.Waiting || race.State == RaceState.Countdown || race.State == RaceState.TimedOut;
            if (tracking != null && tracking.TurnWithHead && race.State != RaceState.Waiting)
                input.Look.x += HeadTurn.DegreesPerSecond(tracking.HeadYawDegrees) * deltaTime;
            player.Tick(deltaTime, input, frozen, options.FollowTravel);
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
                hud.TuningOpen = false;
            }
            if (input.TuningPressed && !options.BoothMode)
            {
                hud.TuningOpen = !hud.TuningOpen;
                hud.SetupOpen = false;
            }
            // In booth mode a player cannot restart or take another turn alone: R needs Ctrl held.
            bool restart = input.RestartPressed && (!options.BoothMode || input.CtrlHeld);
            if (hud.SetupOpen || hud.TuningOpen) Cursor.lockState = CursorLockMode.None;
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
                    if (restart) StartRun();
                    else if (input.ClickPressed && !hud.SetupOpen && !hud.TuningOpen) Cursor.lockState = CursorLockMode.Locked;
                    bool lost = !player.Body.World.IsInPlayArea(player.Body.Position, setup.PlayAreaMargin);
                    if (race.State == RaceState.Running && (input.CheckpointPressed || lost)) BackToCheckpoint();
                    break;
                case RaceState.Finished:
                case RaceState.TimedOut:
                    if (restart) StartRun();
                    else if (input.EnterPressed) NextPlayer();
                    break;
            }
            hud.StartRequested = false;

            if (input.MarkPressed && !options.BoothMode)
            {
                // For making routes: paste this line into a route file. It is written in route
                // coordinates, so it is right even after the scene has moved its origin.
                Num.Vector3 p = setup.Frame.PointToRoute((view.transform.position - totalShift).ToCore());
                Debug.Log(string.Format("ring {0:0} {1:0} {2:0} 11", p.X, p.Y, p.Z));
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

            if (stateBefore == RaceState.Running && race.State == RaceState.TimedOut) sound.CountdownBeep();

            if (!passedRing) return;
            rings.Show(race.NextRing);
            if (race.State == RaceState.Finished)
            {
                sound.Finish();
                lastPlace = scores.Add(hud.Name, race.Time);
            }
            else
            {
                sound.RingPass();
                hud.RingPassed(race.NextRing, race.Time);
            }
        }

        private void OnGUI()
        {
            if (setup == null) return;
            hud.Draw(new HudInfo
            {
                Race = race, Player = player, Input = input, Camera = view, FramesPerSecond = framesPerSecond,
                Scores = scores, LastPlace = lastPlace, Tracking = tracking, Options = options,
            });
        }

        /// <summary>Puts the player back on the start roof and begins the countdown.</summary>
        private void StartRun()
        {
            hud.SetupOpen = hud.TuningOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            player.PlaceAt(race.Route.Start, race.Route.StartYawDegrees);
            race.StartCountdown();
            rings.Show(0);
            lastPlace = 0;
            lastCountdownBeep = 0;
            if (input.Wrist.Connected) input.Wrist.Recentre();
        }

        /// <summary>For a player who is lost or stuck: back to the last ring passed. The clock keeps running.</summary>
        private void BackToCheckpoint()
        {
            System.Numerics.Vector3 position, velocity;
            float yaw;
            race.Checkpoint(out position, out yaw, out velocity);
            player.PlaceAt(position, yaw, velocity);
            hud.Flash("Back to the last ring");
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

        /// <summary>
        /// The camera hangs under "eyes", which the player moves. Head tracking, when on, shifts it
        /// within that. A camera the scene gave keeps its own settings.
        /// </summary>
        private static Camera PrepareCamera(Camera camera, Transform eyes, Color sky)
        {
            bool configure = camera == null;   // only a camera the scene did not hand over is set up here
            if (camera == null) camera = Camera.main;
            if (camera == null)
            {
                var holder = new GameObject("Camera") { tag = "MainCamera" };
                camera = holder.AddComponent<Camera>();
                holder.AddComponent<AudioListener>();
            }
            camera.transform.SetParent(eyes, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            if (configure)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = sky;
                camera.fieldOfView = 75;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 3000;   // a little past where the test map's haze becomes solid
            }
            return camera;
        }
    }
}
