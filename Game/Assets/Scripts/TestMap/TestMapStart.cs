using ManhattanSwing.Core;
using ManhattanSwing.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ManhattanSwing.TestMap
{
    /// <summary>
    /// Starts the game on the light test map: Midtown as boxes, loaded from city.bytes.
    ///
    /// This is the only part of the start-up that belongs to this repository. The game itself is in
    /// the package <c>Packages/com.sshs.manhattan-swing</c>, which a detailed map starts the same way
    /// with its own world and camera (see docs/PORTING.md).
    ///
    /// The scene is empty; this creates everything when Play starts, so two people never edit the
    /// same scene file.
    ///
    /// F4 switches collision between the test map's own boxes and Unity physics, which is what a
    /// detailed map uses. Both should feel the same; if they do not, the difference is a bug.
    /// </summary>
    public sealed class TestMapStart : MonoBehaviour
    {
        private const string CityFile = "city";              // Assets/Resources/city.bytes
        private const string RouteFile = "route_midtown";    // Assets/Resources/route_midtown.txt

        private SwingGame game;
        private GameObject cityObject;
        private CityCollision boxes;
        private PhysicsWorld physics;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnPlay()
        {
            // A scene that already starts the game its own way is left alone.
            if (FindFirstObjectByType<SwingGame>() != null || FindFirstObjectByType<TestMapStart>() != null) return;
            new GameObject("Test map").AddComponent<TestMapStart>();
        }

        private void Awake()
        {
            CityData city = CityData.Load(Resources.Load<TextAsset>(CityFile).bytes);
            boxes = new CityCollision(city);
            cityObject = CityView.Build(city);

            game = SwingGame.Create(new SwingGameSetup
            {
                World = boxes,
                Route = Route.Parse(Resources.Load<TextAsset>(RouteFile).text),
                Sky = CityView.Sky,
            });
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f4Key.wasPressedThisFrame) return;

            if (game.World == boxes)
            {
                if (physics == null)
                {
                    CityView.AddColliders(cityObject);
                    physics = new PhysicsWorld { PlayArea = PlayAreaOf(boxes) };
                }
                game.World = physics;
                game.Flash("Collision: Unity physics (as on a detailed map)");
            }
            else
            {
                game.World = boxes;
                game.Flash("Collision: test map boxes");
            }
        }

        /// <summary>The same edge the box collision uses, so being sent back works the same in both.</summary>
        private static Bounds PlayAreaOf(CityCollision city)
        {
            float minX, minZ, maxX, maxZ;
            city.GetPlayArea(out minX, out minZ, out maxX, out maxZ);
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(minX, 0, minZ), new Vector3(maxX, 1, maxZ));
            return bounds;
        }
    }
}
