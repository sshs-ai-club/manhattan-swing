using Swing.Core;
using UnityEngine;

namespace Swing.Game
{
    /// <summary>
    /// The whole game in one place: loads the city and the route, builds what is drawn, and runs
    /// each frame in a fixed order (input, player, race, screen).
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
        private float bestTime = float.PositiveInfinity;
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
            view = CreateCamera();
            input = new PlayerInput();
            player = new Player(collision, view, new Material(unlit) { color = Color.white });
            race = new Race(route);
            rings = new RingView(route, unlit);
            hud = new Hud();

            player.PlaceAt(route.Start, route.StartYawDegrees);
            rings.Show(0);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            framesPerSecond = Mathf.Lerp(framesPerSecond, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.05f);

            input.Update(deltaTime);
            if (input.CancelPressed) Cursor.lockState = CursorLockMode.None;
            if (input.RestartPressed || (race.State == RaceState.Waiting && input.ConfirmPressed)) StartRun();
            else if (input.ConfirmPressed) Cursor.lockState = CursorLockMode.Locked;

            System.Numerics.Vector3 before = player.Body.Position;
            bool frozen = race.State == RaceState.Waiting || race.State == RaceState.Countdown;
            player.Tick(deltaTime, input, frozen);

            if (race.Update(deltaTime, before, player.Body.Position))
            {
                rings.Show(race.NextRing);
                if (race.State == RaceState.Finished) bestTime = Mathf.Min(bestTime, race.Time);
            }

            if (input.MarkPressed)
            {
                // For making routes: paste this line into a route file.
                Vector3 p = view.transform.position;
                Debug.Log(string.Format("ring {0:0} {1:0} {2:0} 11", p.x, p.y, p.z));
            }
        }

        private void OnGUI()
        {
            hud.Draw(race, player, input, view, framesPerSecond, bestTime);
        }

        /// <summary>Puts the player back on the start roof and begins the countdown.</summary>
        private void StartRun()
        {
            Cursor.lockState = CursorLockMode.Locked;
            player.PlaceAt(race.Route.Start, race.Route.StartYawDegrees);
            race.StartCountdown();
            rings.Show(0);
            if (input.Wrist.Connected) input.Wrist.Recentre();
        }

        private static Camera CreateCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var holder = new GameObject("Camera") { tag = "MainCamera" };
                camera = holder.AddComponent<Camera>();
                holder.AddComponent<AudioListener>();
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CityView.Sky;
            camera.fieldOfView = 75;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 3000;   // a little past where the haze becomes solid
            return camera;
        }
    }
}
