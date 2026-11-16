# Moving the game to the original repository

This repository is where gameplay is **built and tested** on a light map that any laptop can run. The original repository, [manhattan-headtracked-viewer](https://github.com/sshs-ai-club/manhattan-headtracked-viewer), has the detailed city and is still being developed. Which repository the booth runs is not decided yet (see [DECISIONS](DECISIONS.md) Q10).

The plan: once a piece of gameplay plays well here, the original repository picks it up and tests it on the detailed city. Nothing is copied by hand. The gameplay is one Unity package, and the original repository refers to it with one line.

## What goes across and what stays

| Part | Where | Goes to the original repository? |
| --- | --- | --- |
| Swing, aim help, race, route, leaderboard, sounds, eye smoothing (plain C#) | `Game/Packages/com.sshs.manhattan-swing/Runtime/Core` | Yes |
| Player, input, wrist device, HUD, rings, web line, `PhysicsWorld` | `.../Runtime/Game` | Yes |
| Head-tracking set-up for this game | `.../Runtime/HeadTracking` | Yes; can be switched off if the original repository keeps its own |
| Tests of the rules | `.../Tests/EditMode` | Yes |
| The light Midtown map, its drawing and box collision start-up | `Game/Assets/Scripts/TestMap`, `Game/Assets/Resources` | No |
| Route files | `Game/Assets/Resources/route_*.txt` | Copied once; they are small text files |

The package knows nothing about which city it is on. It asks the city four questions through `ISwingWorld` (`Runtime/Core/ISwingWorld.cs`):

1. Where does this ray hit, and can a web hold there?
2. Push the player's body out of whatever it overlaps.
3. How high is the street here?
4. Is the player still on the map?

On the test map, `CityCollision` answers them from the boxes. On the detailed city, `PhysicsWorld` answers them from Unity colliders. **Press F4 on the test map** to switch to `PhysicsWorld` and check that it feels the same before taking it across.

## Steps in the original repository

Someone who works on the original repository does these once. None has been tried yet.

### 1. Add the package

In `ManhattanViewer/Packages/manifest.json`, under `dependencies`, add one line, with the commit you want to use:

```json
"com.sshs.manhattan-swing": "https://github.com/sshs-ai-club/manhattan-swing.git?path=/Game/Packages/com.sshs.manhattan-swing#<commit>"
```

This works the same way the head-tracking package is already added there. To update, change the commit. Pinning a commit means a change here never breaks the original repository by surprise.

While working on both at once, with both clones in the same folder, point at the local clone instead, so edits show up immediately (do not commit this line):

```json
"com.sshs.manhattan-swing": "file:../../../manhattan-swing/Game/Packages/com.sshs.manhattan-swing"
```

### 2. Start the game from a script

Make one script in the original repository (not in the package) that starts the game when the city is ready:

```csharp
using ManhattanSwing.Core;
using ManhattanSwing.Game;
using UnityEngine;

public sealed class StartSwingGame : MonoBehaviour
{
    public Camera playerCamera;          // the scene's camera
    public TextAsset routeFile;          // a copy of route_midtown.txt
    public LayerMask buildingLayers = ~(1 << 2);

    private SwingGame game;

    private void Start()
    {
        var world = new PhysicsWorld
        {
            Layers = buildingLayers,
            Street = 0f,                                                   // street height in this scene
            PlayArea = new Bounds(Vector3.zero, new Vector3(4000, 1, 6000)),   // or leave empty for no edge
        };

        game = SwingGame.Create(new SwingGameSetup
        {
            World = world,
            Route = Route.Parse(routeFile.text),
            Frame = new WorldFrame { Origin = new System.Numerics.Vector3(0, 0, 0), YawDegrees = 0 },   // see step 3
            Camera = playerCamera,
            HeadTracking = true,                                           // false if the scene runs its own
            LeaderboardName = "race-detailed",
        });
    }
}
```

While this runs, switch off the scene's own player controller and swing, so two things do not move the camera.

### 3. Line up the coordinates

Routes are written in the test map's coordinates: metres, origin at the Empire State Building at street level, +Z along the avenues (about 28.3° east of true north), +X across them, +Y up. The detailed city very likely uses a different origin and direction.

1. Find the Empire State Building at street level in the detailed scene. That position is `Frame.Origin`.
2. If the detailed scene's +Z points along the avenues, `YawDegrees` is 0. If its +Z points to true north, it is about 28.3. Check by flying along an avenue: the rings must sit in the middle of the street.
3. In the game, press **P** at a spot and compare the printed `ring` line with the same spot on the test map. The P line is always in route coordinates, so the two should match.

### 4. Pass on origin shifts

The detailed city moves its whole world to keep numbers small (a floating origin). Every time it does, call:

```csharp
game.ShiftOrigin(delta);   // delta: everything that was at p is now at p + delta
```

The player, web, rings, checkpoints and `PhysicsWorld`'s play area and street height move with it. If the shift is reported the other way round (the camera moves by `-delta`), pass `-delta`. Getting the sign wrong shows up as rings jumping away at the first shift.

### 5. Check

| Check | What right looks like |
| --- | --- |
| The game starts | Name screen, rings in the streets |
| Webs | Catch walls and roofs, never the street |
| Standing | Feet on roofs and the street, not sinking or floating |
| Edge | Flying far off the map sends you back to the last ring |
| Origin shift | Rings stay put, the clock keeps counting, passing a ring after a shift counts |
| Backspace after a shift | Back to the last ring, not somewhere else |
| Frame rate | Write it down with head tracking on and off |

Write down what breaks in an issue in **this** repository if it is the package's fault, or in the original one if it is the scene's.

## Rules that keep moving cheap

- Nothing in the package may know about a particular map. Map-specific things go in that map's start-up script.
- `Runtime/Core` has no Unity types and every rule there has a test.
- Both repositories use Unity 6000.3.22f1. The package works in the built-in pipeline (here) and URP (there): its one shader has no lighting passes, and if it fails, Unity's own unlit shaders are used.
- Input System only. The package never uses the old `Input` class, which the original repository has switched off.

## Known gaps

- `PhysicsWorld` compiles but has never run. F4 on the test map is the first test.
- The swing help was tuned against boxes. Real building shapes (ledges, spires) may need different settings; they are all on the F3 panel.
- The original repository has its own swing (`WebShooter.Swing`), input (`WebShooter.Input`) and camera help. Which swing the booth uses is open (Q9 in [DECISIONS](DECISIONS.md)). Both can stay in the original repository while it is decided.
- How the detailed city reports origin shifts, and in which direction, has not been looked up.
