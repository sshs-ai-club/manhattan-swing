# Quickstart

From nothing to swinging in about 10 minutes, not counting the Unity download.

## 1. Install Unity

1. Install [Unity Hub](https://unity.com/download).
2. Install the editor version **6000.3.22f1**. With Unity Hub installed, this link opens it at the right version: `unityhub://6000.3.22f1/1c726e1fb402`. Otherwise use Hub → Installs → Install Editor → Archive.
3. Tick **Windows Build Support** only if you will make builds. Nothing else is needed.

Everyone must use this exact version. A different version rewrites project files and causes conflicts.

## 2. Open the project

```bash
git clone https://github.com/sshs-ai-club/manhattan-swing.git
```

In Unity Hub choose **Add → Add project from disk** and pick the `Game` folder inside the clone. Open it. The first open takes a few minutes while Unity imports.

## 3. Play

Open `Assets/Scenes/Game.unity` and press **Play**. The scene looks empty in the editor; the game builds itself when Play starts.

Type a name and press **Enter** to start. After the finish, **R** runs again and **Enter** goes back to the name screen for the next player.

| Input | Action |
| --- | --- |
| Enter | Start (on the name screen); next player (after the finish) |
| Click | Capture the mouse again after Esc |
| Mouse | Look around |
| Hold left mouse | Fire a web at the reticle and hold it |
| Release left mouse | Let go |
| Right mouse or E | Reel in |
| W A S D | Run on roofs and the ground; steer in the air |
| Space | Jump |
| R | Restart the run (Ctrl+R in booth mode) |
| Backspace | Back to the last ring you passed, if you are lost or stuck |
| C | Recentre the wrist device |
| H | Head tracking on or off |
| K | Capture the face reference for head tracking |
| F2 | Setup panel: head tracking, monitor size, booth mode, leaderboard reset |
| F3 | Tuning panel: sliders for the swing, mouse sensitivity, zip style, view follows travel |
| F4 | Switch collision between the test map's boxes and Unity physics (as on the detailed city), to compare |
| P | Log your position as a `ring` line, for making routes |
| Esc | Free the mouse |

A gamepad, if one is connected, moves the reticle with its left stick and holds the web with its bottom face button. It stands in for the wrist device.

## 4. Run the tests

**Window → General → Test Runner → EditMode → Run All.** There are 59 tests: the game rules (under the package) and the test map's files (under Assets).

A run ends as "time up" after 5 minutes (`TimeLimitSeconds` in `Race.cs`).

## When something is wrong

The code is written without a Unity Editor, so new parts may not work the first time they are run.

1. Copy any red error from the Console into a GitHub issue, with the Unity version.
2. Unity creates some files on first open (`Packages/packages-lock.json` and more files under `ProjectSettings/`). They are not committed yet; commit them once so everyone shares them.
3. Write down your frame rate (top left of the game) and your laptop's CPU and graphics.

Head tracking needs its own setup; see [HEAD-TRACKING](HEAD-TRACKING.md). The leaderboard file's location is printed in the Console when the game starts.

What was checked before this point is listed in the [plan](PLAN.md#where-we-are).

## Where things are

| Path | What it holds |
| --- | --- |
| `Game/Packages/com.sshs.manhattan-swing/` | The gameplay as a package, shared with the original repository ([PORTING](PORTING.md)) |
| `.../Runtime/Core/` | The game rules as plain C#: swing, aim help, route, race, box collision. No Unity types. |
| `.../Runtime/Core/ISwingWorld.cs` | The four questions the game asks a city; the only link to a map |
| `.../Runtime/Core/SwingSettings.cs` | Every number that changes how swinging feels. Try values live with F3, then write the good ones here. |
| `.../Runtime/Game/` | The Unity side: input, camera, HUD, rings, `PhysicsWorld` |
| `.../Runtime/Game/WristDevice.cs` | Reading the wrist device, with its settings at the top |
| `.../Runtime/Core/SoundSynth.cs` | The sound effects, made from maths |
| `.../Runtime/HeadTracking/` | Head tracking set-up for this game |
| `.../Tests/EditMode/` | Tests of the game rules |
| `Game/Assets/Scripts/TestMap/` | The light test map: draws the box city and starts the game on it |
| `Game/Packages/com.headtracked.display/` | The head-tracking package (a copy; do not edit) |
| `Game/Assets/Resources/route_midtown.txt` | The ring route, a text file you can edit |
| `Game/Assets/Resources/city.bytes` | The city; rebuilt by `tools/city/` |
| `Game/Assets/Tests/EditMode/` | Tests of the test map's city and route files, including the automatic pilots |
| `hardware/` | Wrist device firmware and wiring |
