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

| Input | Action |
| --- | --- |
| Click | Start the run (and capture the mouse) |
| Mouse | Look around |
| Hold left mouse | Fire a web at the reticle and hold it |
| Release left mouse | Let go |
| Right mouse or E | Reel in |
| W A S D | Run on roofs and the ground; steer in the air |
| Space | Jump |
| R | Restart the run |
| C | Recentre the wrist device |
| P | Log your position as a `ring` line, for making routes |
| Esc | Free the mouse |

A gamepad, if one is connected, moves the reticle with its left stick and holds the web with its bottom face button. It stands in for the wrist device.

## 4. Run the tests

**Window → General → Test Runner → EditMode → Run All.** There are 27 tests of the game rules.

## The very first time anyone opens it

This project was written without a Unity Editor and has never been opened in one. Expect problems, and please:

1. Copy any red error from the Console into a GitHub issue, with the Unity version.
2. Unity will create some files on first open (`Packages/packages-lock.json` and more files under `ProjectSettings/`). Commit them once so everyone shares them.
3. Write down your frame rate (top left of the game) and your laptop's CPU and graphics.

What was checked before this point is listed in the [plan](PLAN.md#where-we-are).

## Where things are

| Path | What it holds |
| --- | --- |
| `Game/Assets/Scripts/Core/` | The game rules as plain C#: collision, swing, route, race. No Unity types. |
| `Game/Assets/Scripts/Game/` | The Unity side: drawing, input, camera, HUD |
| `Game/Assets/Scripts/Core/SwingSettings.cs` | Every number that changes how swinging feels |
| `Game/Assets/Scripts/Game/WristDevice.cs` | Reading the wrist device, with its settings at the top |
| `Game/Assets/Resources/route_midtown.txt` | The ring route, a text file you can edit |
| `Game/Assets/Resources/city.bytes` | The city; rebuilt by `tools/city/` |
| `Game/Assets/Tests/EditMode/` | Tests |
| `hardware/` | Wrist device firmware and wiring |
