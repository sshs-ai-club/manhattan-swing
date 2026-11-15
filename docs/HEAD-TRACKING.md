# Head tracking (the window effect)

With head tracking on, a webcam finds the player's eyes and the game redraws the view from where they really are, so the monitor behaves like a window into the city. With it off, the game is an ordinary first-person game. It starts off, and everything works without it.

Status: **written, not tested.** The game side compiles against the package, but it has not been run with a webcam.

## The parts

| Part | Where | What it does |
| --- | --- | --- |
| Head Tracked Display package | `Game/Packages/com.headtracked.display/` | Moves the camera and skews its view from the eye position. A copy of [lollipop190000/head-tracked-display](https://github.com/lollipop190000/head-tracked-display) at commit `c5ddab1` (MIT licence, the version the old project used), kept in this repository so opening the project needs no extra download. |
| Game glue | `Game/Assets/Scripts/HeadTracking/HeadTracking.cs` | Sets the package up for this game and stores the monitor's measurements on the laptop. |
| Webcam tracker | A Python program in the head-tracked-display repository | Watches the webcam and sends the eye position to the game. It runs beside the game. |

If the package folder is removed, the game still compiles and runs; the setup panel then says head tracking is not installed.

## Setting up the tracker (once per laptop)

In PowerShell, somewhere outside this repository:

```powershell
git clone https://github.com/lollipop190000/head-tracked-display.git
```

```powershell
cd head-tracked-display
```

```powershell
git checkout c5ddab14d12dcc8acc8b31048f5752686984f3ab
```

```powershell
py -3 -m venv python_tracker/.venv
```

```powershell
./python_tracker/.venv/Scripts/python.exe -m pip install -r python_tracker/requirements.txt
```

```powershell
py -3 python_tracker/download_model.py
```

The last two steps need the internet. After that the tracker works offline.

## Using it

1. Start the game and press **F2** for the setup panel.
2. Measure the lit part of the monitor with a ruler and type the width, height and your eye-to-screen distance in centimetres. Press **Apply sizes**. These are saved on the laptop.
3. Turn **Head tracking** on (the button, or **H** during a run).
4. Start the tracker. The setup panel shows the exact command with the right file path for this laptop; run it from the `python_tracker` folder with the virtual environment's Python.
5. Sit normally, look at the screen and press **Capture face** (or **K**). The status line at the bottom left should say "face found".

The webcam is assumed to sit on top of the monitor, in the middle.

## Things to know

- With tracking on, the width of the view comes from the real monitor and how far you sit from it, so it is narrower than the ordinary camera. That is what makes it a window.
- Only one person can use the window at a time. Spectators see a skewed picture, which is why the booth has a separate spectator screen.
- If the face is lost, the view eases back to the middle; it does not jump.
- The tracker uses the CPU. Check the frame rate with it running before deciding it is fine on a laptop.

## For a build

The head-tracked-display repository has a launcher, `Run-HeadTrackedDemo.cmd`, that starts a built game and the tracker together and stops both. It takes `-UnityProject`, `-PlayerPath` and `-TrackerRoot`. The booth release should use it; that is the "one double-click" task in the plan.
