# 천년제 부스 To-Do (Dec 29)

Nov 15, 2026 · @SW · third revision, after the first run in Unity

## Where we are

The first version opened and ran in Unity on Nov 15 (@SW: "works well"). A second batch was added the same day and has **not been run in Unity yet**: name entry, leaderboard, sounds and head tracking. Code is written without a Unity Editor, so each batch needs someone to press Play and report what is wrong.

| Part | State | Checked how |
| --- | --- | --- |
| Midtown as 6,899 box buildings in a 170 KB file | Runs | Opened in Unity by @SW |
| City drawing, first-person swing, mouse input, rings, timer, HUD | Runs | Opened in Unity by @SW; feel and frame rate not yet reported |
| Game rules: collision, swing, aim help, route, race, leaderboard, sound synthesis | Written | 36 automated tests pass outside Unity |
| Draft route: 10 rings, 2.8 km, 6th Avenue → 57th Street → Park Avenue | Written | Every ring is in open air; not yet reported as playable |
| Booth turn: type a name, countdown, run, result, next player | Written | Compiles; not run |
| Leaderboard saved to a file, with a reset that keeps the old file | Written | Rules tested; file and screen not run |
| Sounds: web, catch, ring, finish, countdown, wind that follows speed | Written | Samples tested; not heard |
| Head tracking with an on/off switch and a setup panel | Written | Compiles against the package; no webcam |
| Wrist device firmware for the XIAO ESP32-C3 | Starting point | Not compiled, no device |
| Hands, music, 1v1, spectator screen | Not started | |

The goal has not changed: a two-part booth on **Dec 29, 09:00 to 12:00**. 1부 (09:00 to 10:30) is a ring race for one player. 2부 (10:30 to 12:00) is a 1v1 duel on two laptops. The game is a head-tracked window into Manhattan where you swing on webs, aimed with a wrist device.

Choices the team has confirmed, choices still assumed, and open questions are in [DECISIONS](DECISIONS.md).

## Rules for the restart

The old project built a heavy city before it had a game, and almost nobody could run it at home. These rules stop that happening again.

1. **Every laptop runs it.** A change that breaks the weight budget does not merge.
2. **Game before graphics.** The city stays as boxes until the race is fun.
3. **`main` is playable every Sunday.**
4. **The mouse always works.** Head tracking and the wrist device are extras that can each be turned off.

### Weight budget

| What | Limit | Now |
| --- | --- | --- |
| Frame rate | 60 fps at 1920×1080 on the weakest team laptop, head tracking on | Not measured |
| Triangles in the whole city | Under 300,000 | About 69,000 |
| From clone to playing | Under 10 minutes, nothing to download except Unity | No downloads, no bake, no build step |
| Repository size | Under 200 MB | Under 1 MB |
| Start-up | Playable within 10 seconds | Not measured |

The weakest laptop has not been named, so the frame-rate target is checked on whichever laptop turns out slowest in the first week.

### Why the city is small on purpose

The whole island would be the ideal, and the same tool can already produce it (44,273 boxes, 1.1 MB). It is left out for now because Midtown is enough to race in, and a smaller city is quicker to load, test and tune. Widening the area later is one command and no code change.

## Where the team takes over

**Now.** Everything that could be written and tested without Unity is done. The next steps need a person at a laptop: seeing the city, feeling the swing, holding the device. From here the work splits into pieces that touch different files, so several people and their AI agents can work at once.

| Piece | Files it owns | First step |
| --- | --- | --- |
| First run and swing feel | `Scripts/Core/SwingSettings.cs`, `SwingBody.cs`, `Scripts/Game/Player.cs` | Open the project, press Play, list what is broken |
| Head tracking | `Scripts/HeadTracking/`, `Packages/com.headtracked.display/` | Run it with a webcam and fix what is wrong; see [HEAD-TRACKING](HEAD-TRACKING.md) |
| Wrist device | `hardware/`, `Scripts/Game/WristDevice.cs` | Flash one board and see it in Windows as a controller |
| Race flow and leaderboard | `Scripts/Core/Race.cs`, `Leaderboard.cs`, `Scripts/Game/Hud.cs`, `SwingGame.cs` | Play a full turn from name to next player and fix what is awkward |
| Routes | `Resources/route_*.txt` | Play the draft route and move the rings |
| 1v1 | a new `Scripts/Net/` folder | Send one laptop's position to the other |
| Look and sound | `Scripts/Game/CityView.cs`, `Resources/Shaders/`, `Scripts/Core/SoundSynth.cs`, `Scripts/Game/GameAudio.cs` | Listen to the placeholder sounds and replace the weak ones |
| Booth | no code | Webcams, room, equipment |

Owners are not assigned yet. That is deliberately left until the first run works.

## Milestones

| | Date | Milestone | Done when |
| --- | --- | --- | --- |
| M1 | Sun Nov 22 | **It runs everywhere** | The project opens and plays on every laptop; frame rate written down for each; one ESP32 shows up in Windows as a controller |
| M2 | Sun Nov 29 | **The race is fun** | Swing tuned from playtests, route adjusted, window effect on, the wrist device aims and holds the web, basic sounds |
| M3 | Sun Dec 6 | **Booth flow and two laptops** | Name to result in one 4-minute slot, leaderboard, two laptops see each other, hands in |
| M4 | Sun Dec 13 | **Feature complete** | Duel rules, 3 devices, spectator screen, 60 fps on the booth laptops |
| | Tue Dec 15 | **Duel check** | Two outside players finish a duel without help. If not, the rules are cut down, not the date |
| M5 | Sun Dec 20 | **Code freeze** | Booth release on all three laptops, comfort playtest done. Only crash fixes after this |
| M6 | Tue Dec 29 | **Booth day** | Rehearsal Dec 22 to 23, day-of checklist Dec 27, booth 09:00 to 12:00 |

If a date slips, cut stretch tasks first, then simplify the duel. The Dec 20 freeze does not move.

## Development tasks

### Foundation and city

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Open the project in Unity 6000.3.22f1 and press Play | Nov 15 | Done | @SW: works. The files Unity generated on first open are not committed yet; commit them once. |
| Run the second batch (name entry, leaderboard, sounds, head tracking) and report errors | Nov 18 | Not started | None of it has run in Unity. |
| Everyone runs it on their own laptop and writes down their frame rate | Nov 22 | Not started | Names the weakest laptop and proves the budget. |
| Midtown as boxes, loaded from one small file | Nov 15 | Done | 6,899 buildings from NYC open data. `tools/city/` rebuilds it. |
| Window effect: the head-tracking package with an on/off switch (H) and a setup panel (F2) | Nov 15 | Written, not run | The game stays fully playable with it off, and still compiles if the package is removed. See [HEAD-TRACKING](HEAD-TRACKING.md). |
| Test head tracking with a webcam; measure the frame rate with the tracker running | Nov 29 | Not started | Also decide whether the narrower window view suits swinging. |
| Make it read as Manhattan: stepped tops for the tallest towers, a few landmark shapes, street markings | Dec 6 | Not started | Only after the race is fun. Each addition is measured against the budget. |
| Widen the city beyond Midtown | After the booth | Not started | Stretch. One command (`--area all`). |

### Swing

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Basic swing with the mouse: aim, hold to attach, release, reel in, jump, run | Nov 15 | Runs, not tuned | New, simple code that keeps the old project's numbers (gravity, speeds). All tuning is in `SwingSettings.cs`. |
| Aim help and a reticle that fills when a web would catch | Nov 15 | Runs, not tuned | Tries nearby directions when the aim just misses. |
| First playtest: list the five worst things about the swing and fix them | Nov 22 | Not started | Likely suspects: starting from the ground, gaining height, turning corners. |
| Outside playtests, then freeze the tuning | Dec 13 | Not started | Tuning that changes after this invalidates route times and the comfort test. |
| Advanced moves from the old project (wall run, rope wrap, two-hand launch) | Dec 13 | Not started | Stretch. Only if the race is already fun without them. |

### 1부 race mode

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Ring pass detection, countdown, timer, finish, one-key restart | Nov 15 | Runs | R restarts at once. |
| Draft route: 10 rings in Midtown | Nov 15 | Written, not played | Rings follow real street centres. Heights and spacing are guesses. |
| Play the route and move the rings until a new player finishes in about 3 minutes | Nov 29 | Not started | Press P in the game to log a ring line for the route file. |
| Booth turn: name entry, countdown, run, result, next player | Nov 15 | Written, not run | Enter starts; after the finish R runs again and Enter is the next player. |
| Leaderboard for 1부, saved to a local file after every run, with a reset | Nov 15 | Written, not run | The top 10 show on the start and result screens. Reset is in the setup panel (F2) and keeps the old file under a new name. |
| Time a full turn with outside players and fit it into 4 minutes | Dec 6 | Not started | Includes sitting down, the face capture and the name. |
| Korean names on the name screen | Dec 6 | Not started | Typing Hangul into the name box is untested. |
| Leaderboard for 2부 | Dec 13 | Not started | Same file code with a different name; needs the duel's scoring first. |
| Head-dodge obstacles (pigeons, a helicopter) | Dec 13 | Not started | Stretch. The tracked eye position is the hit test. |
| Hidden items that only show when the player leans | Dec 13 | Not started | Stretch. Rewards moving the head. |

### 2부 1v1 duel

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Position sync between the two laptops over LAN | Dec 6 | Not started | UDP, 20 to 30 updates a second. Both laptops load the same city file, so positions are sent as they are. |
| Opponent avatar and their web line | Dec 6 | Not started | A simple shape is enough for v1. |
| Duel rules: web pulls the opponent, rooftop capture points, 3 rounds of 90 seconds | Dec 13 | Not started | One button: aiming at a building swings, aiming at the opponent pulls them. One laptop decides hits and score. |
| Duel check with two outside players | Dec 15 | Not started | If they cannot finish a duel unaided, cut rules until they can. |

### Web shooter (Bluetooth)

The ESP32 appears to Windows as a Bluetooth gamepad, as the hardware owner proposed: the device works out the hand direction and sends two axes and two buttons. Details are in [hardware/README](../hardware/README.md).

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Game reads the device: axes to the reticle, button down holds the web, button up lets go | Nov 15 | Written, not run | `WristDevice.cs`. Any ordinary gamepad stands in for the device. |
| Device status on screen, mouse fallback | Nov 15 | Written, not run | The mouse keeps working whether or not a device is connected. |
| Firmware starting point for the XIAO ESP32-C3 | Nov 15 | Written, not compiled | MPU-6050 for direction, flex sensor for hold, a recentre button. |
| Flash one board; confirm Windows and Unity see it as a controller | Nov 22 | Not started | Proves the Bluetooth gamepad approach before three devices are built. |
| Wire the flex sensor and set its thresholds; bent finger holds, straight finger lets go | Nov 29 | Not started | Replaces the infrared sensor, which could only fire once. |
| Check the reticle directions and ranges with a real device | Nov 29 | Not started | Flip `InvertYaw` / `InvertPitch` and set the ranges in `WristDevice.cs`. |
| Measure aim drift over 3 minutes and choose MPU-6050 or BNO085 | Nov 29 | Not started | The game recentres at every countdown; if drift stays small within one run, skip the new sensor. |
| Decide how the view turns when playing with the device only | Nov 29 | Not started | Written now: pointing near the screen edge turns the view. Needs a playtest. |
| Bluetooth soak test: 2 devices and 2 laptops side by side for 30 minutes, with phones nearby | Dec 6 | Not started | A festival room is full of phones. If it is unreliable, decide on a cable fallback now. |
| Build 3 devices with straps and batteries; optional vibration; wiring written down | Dec 13 | Not started | Two for the duel and one spare. |

### Look and sound

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Web shoot, attach, ring, finish and countdown sounds, wind that follows speed | Nov 15 | Written, not heard | Made from maths in `SoundSynth.cs`, so there are no audio files or licences. |
| Listen to the placeholders; replace the weak ones with recorded sounds | Dec 6 | Not started | Each sound is one line in `GameAudio.cs`. |
| Simple first-person hands with a firing pose | Dec 6 | Not started | The club agreed simple rounded hands look better than half-finished detail. Keep the character simple for v1. |
| One music loop with a clear licence | Dec 6 | Not started | |
| Ring pass flash, finish and result screen effects | Dec 13 | Not started | Clear feedback is what makes players ask for another run. |

### Spectator screen and release

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Spectator view on the 전자칠판 | Dec 13 | Not started | A third laptop is connected to the 전자칠판 by HDMI. It runs the same game in a "watch" mode and receives the players' positions over the LAN, like the second laptop in a duel, and shows the leaderboard between runs. |
| 60 fps on both booth laptops at the booth resolution, tracking on | Dec 13 | Not started | Frame drops cause dizziness. |
| Booth release on both laptops and the spare, started by one double-click | Dec 20 | Not started | Code freeze. |
| Comfort playtest with 5 outside players | Dec 20 | Not started | Note who gets dizzy or confused, then fix only the top issues. |

## Booth preparation

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Buy the 2 webcams | Nov 22 | Not started | Head tracking must be tested on the real cameras. |
| Order the wrist device parts (XIAO ESP32-C3, flex sensors, batteries, bands, motors) | Nov 18 | In progress | The list is chosen. One board is needed by Nov 22. |
| Assign owners to the pieces above and make one GitHub issue per task | Nov 22 | Not started | After the first run works. |
| Room layout | Nov 15 | Done | 외국어 강의실 4: two player seats (A and B), a waiting area between them and the 전자칠판, and a marked way in and out. The team's diagram is the reference. |
| Line up equipment: 2 monitors, 2 play laptops, a third laptop for the 전자칠판, a network switch and 3 LAN cables, power strips, HDMI | Dec 13 | Not started | Write down who brings what. The third laptop doubles as the spare. |
| Window frame from 우드락 and 시트지, sized to the play monitor | Dec 20 | Not started | The frame must not cover the webcam. |
| One-page poster: how to play, and why the screen looks like a window | Dec 20 | Not started | Lets the queue learn the controls before sitting down. |
| Buy snacks the week of the festival | Dec 22 | Not started | |
| Full rehearsal in a classroom: set up in 30 minutes, run 1부 for 20 minutes and one duel with outside players | Dec 22 to 23 | Not started | Pair every wrist device with its laptop here, not on the day. |
| Day-of checklist: setup order for 08:30 to 09:00, the 10:30 switch to 2부, what to do if a laptop or device fails | Dec 27 | Not started | |

### How many people can play

| Part | Time | Per turn | Players |
| --- | --- | --- | --- |
| 1부 race | 90 minutes | 4 minutes per player | About 44: both laptops run the race |
| 2부 duel | 90 minutes | About 6.5 minutes (3 × 90 seconds plus changeover) | About 13 matches, 26 players |

## Risks

| Risk | What to do |
| --- | --- |
| Code written without Unity breaks when first run | Each batch is run by a person before more is built on it. The first batch ran; the second has not. |
| Six weeks to the booth | Hold the order: runs, fun, flow, duel, looks. A 30-minute check-in every Sunday where each person reports what merged. |
| The city gets heavy again | The weight budget is checked in every pull request and on the weakest laptop every Sunday. |
| The swing is not fun with basic physics | Playtest in the first week. Port one advanced move at a time only if needed. |
| Head tracking costs too much on a weak laptop | It has an off switch, and the booth laptops are the ones that must run it. Measure on Nov 29. |
| Two laptops in 1부 keep two separate leaderboard files | Before prizes, staff compare the two top-3 lists by hand, or the files are merged. Decide which by Dec 13. |
| Bluetooth is unreliable in a crowded room | Soak test by Dec 6, mouse fallback with no restart, a charged spare, devices paired at the rehearsal. |
| The duel swallows the schedule | Sync first, because a synced race is already a two-player mode. The duel check on Dec 15 cuts rules, not the date. |
| One person writes almost all the code again | The work is split by file in "Where the team takes over". |
| Players get dizzy | Seated play, 3-minute runs, the window frame as a fixed reference, the comfort test on Dec 20. |
