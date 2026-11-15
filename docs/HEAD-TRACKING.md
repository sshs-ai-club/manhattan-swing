# Head tracking (the window effect)

With head tracking on, a webcam finds the player's eyes and the game redraws the view from where they really are, so the monitor behaves like a window into the city. With it off, the game is an ordinary first-person game. It starts off, and everything works without it.

Status: **runs with a webcam** (@SW, Nov 15), but it was not clearly smooth. The smoothing, the recording and head-turning described below were added after that and have not been run yet.

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

## Making it smooth

The package moves the view only when the webcam tracker sends a report, about 30 times a second, and each report is already about 50 ms old. On a 60 Hz screen that shows as small jumps and a view that trails the head. The game now adds its own step (`EyeSmoother`): on every redraw it carries on in the direction the head was moving and glides there.

In a simulation of a 30-reports-a-second tracker with 50 ms delay and 1.5 mm of wobble, compared with the package alone:

| | Package alone | With extra smoothing |
| --- | --- | --- |
| How far the view trails a moving head | about 24 mm | about 15 mm |
| Unevenness of the movement | 8.3 | 2.7 |
| Wobble when sitting still | 0.50 | 0.17 |

These are simulated numbers with assumed delay and wobble, not measurements from a real webcam. To check on a real laptop:

- **F2 → "Extra smoothing"** switches it off and on, so you can compare by eye.
- **F2** shows how many reports a second the tracker sends and how old its picture is. Under about 25 reports a second, or over about 80 ms, the limit is the webcam or the CPU, not the game.
- **F2 → "Record 20 seconds"** writes every frame's eye positions to a CSV file in the game's data folder (the path is shown, and printed in the Console). Sit still for 5 seconds, then move your head side to side. That file is real data that the smoothing can be tuned against.

Some trailing always remains: the webcam, the face finder and the screen each add delay, and guessing further ahead makes the picture shake.

## Seeing behind you

A screen is a window, and a window only shows what is in front of it. Leaning changes what you see through it, but nothing shows what is behind the wall the window is in. Head tracking cannot change that.

Games with head trackers handle it in one of two ways:

- **Turn the window with something else.** The head moves the viewpoint, and a mouse, stick or steering wheel turns the body the window is attached to. This game does this: the mouse turns the view, and with the wrist device, pointing near the screen edge turns it.
- **Exaggerate the head turn.** Flight and racing games with head trackers turn the view much further than the head: look 20° to the side and the view swings round 90° or more, while your eyes stay on the screen. This game has a version of it, off by default: **F2 → "Turn the view by turning your head"**. Turn your head more than 10° and the view keeps turning that way until you face the screen again. If it turns the wrong way, tick the flip box.

The old project tried head steering and removed it, so treat this as an experiment: it may be uncomfortable, and it fights with the window effect a little because a turned head is also a moved head.

## Things to know

- With tracking on, the width of the view comes from the real monitor and how far you sit from it, so it is narrower than the ordinary camera. That is what makes it a window.
- Only one person can use the window at a time. Spectators see a skewed picture, which is why the booth has a separate spectator screen.
- If the face is lost, the view eases back to the middle; it does not jump.
- The tracker uses the CPU. Check the frame rate with it running before deciding it is fine on a laptop.

## For a build

The head-tracked-display repository has a launcher, `Run-HeadTrackedDemo.cmd`, that starts a built game and the tracker together and stops both. It takes `-UnityProject`, `-PlayerPath` and `-TrackerRoot`. The booth release should use it; that is the "one double-click" task in the plan.
