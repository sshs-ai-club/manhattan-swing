# 천년제 부스 To-Do (Dec 29)

Nov 15, 2026 · @SW · replaces the Nov 11 plan written for the old repository

## Why we restarted

The old project ([manhattan-headtracked-viewer](https://github.com/sshs-ai-club/manhattan-headtracked-viewer)) built a heavy city before it had a game, so almost nobody on the team could run it at home. This repository starts again in the opposite order: a game that runs on every team laptop first, then the city's look.

| | Old repository | This repository |
| --- | --- | --- |
| City | All of Manhattan: 10,038 streamed tiles, 68.7 million triangles, a multi-GB source download and a bake step | One Midtown district made of simple boxes, built when the game starts from one small data file |
| Target machine | RTX 4070 Laptop, 8 GB VRAM | The weakest laptop on the team |
| Order of work | Engine and city first; rings, timer and score never started | Swing, rings and timer first; looks last |
| Wrist device | Arduino + HC-05, then ESP32 over Wi-Fi UDP | ESP32 over Bluetooth |
| Getting started | Git LFS, GitHub CLI, Python, Unity, setup, bake, build | Clone, open in Unity, press Play |

The goal has not changed: a two-part booth on **Dec 29, 09:00 to 12:00**. 1부 is a ring race for one player, 2부 is 1v1 on two laptops. The game is a head-tracked window into Manhattan where you swing on webs, aimed with a wrist device.

Several choices in this plan are assumptions that the team has not confirmed. They are listed with the open questions in [DECISIONS](DECISIONS.md), and marked here as (A1), (A2) and so on.

## Rules for the restart

These four rules exist so the old problem cannot come back.

1. **Every laptop runs it.** A change that breaks the weight budget below does not merge, however good it looks.
2. **Game before graphics.** Nothing is added to the city's look until a player can finish a timed ring race on grey boxes.
3. **`main` is playable every Sunday.** Each milestone ends with a build that someone outside the team can play.
4. **The mouse always works.** Head tracking and the wrist device are added on top and can each be turned off, so nobody is blocked by missing hardware.

### Weight budget

The limits are proposals until the weakest laptop is named (Q2 in [DECISIONS](DECISIONS.md)).

| What | Limit |
| --- | --- |
| Frame rate | 60 fps at 1920×1080 on the weakest team laptop, with head tracking on |
| Triangles on screen | Under 300,000 |
| From clone to playing | Under 10 minutes, with nothing to download except Unity itself |
| Repository size | Under 200 MB |
| Start-up | Playable within 10 seconds of launching |

## What we take from the old repository

The old repository stays as a read-only archive. Code is ported one piece at a time when a task needs it, never copied as a whole.

| Piece | Decision | Reason |
| --- | --- | --- |
| [head-tracked-display](https://github.com/lollipop190000/head-tracked-display) package and its webcam tracker | Keep, pinned to one commit | It is a separate, small package and already works |
| Swing physics (`SwingPhysics`, about 640 lines of core code) | Port the basics: attach, swing, release, reel, jump | It is plain code that does not depend on the old city |
| Wall run, rope wrap, two-hand launch, air-chain assist | Leave behind; port later only as stretch | They took most of the old effort and the race does not need them |
| Wrist aim maths (angles to a ray from the tracked eye through the screen) | Port | Still correct whichever way the device connects |
| First-person hands and wind sound | Port if they fit the budget | Saves the Look and sound tasks some work |
| City streaming, bake, Addressables, source packs, benchmark suite, ML-Agents lab, second Unity profile | Leave behind | This is the weight |
| Team rules (branches, pull requests, an issue as the lock for AI agents) | Keep in short form in [AGENTS.md](../AGENTS.md) | They worked |

## Milestones

Dates stay on the old weekly rhythm. The restart moves the ring route back one week and puts a playable greybox first.

| | Date | Milestone | Done when |
| --- | --- | --- | --- |
| M0 | Tue Nov 17 | **Questions answered** | The open questions in [DECISIONS](DECISIONS.md) have answers, and each task below has an owner and a GitHub issue |
| M1 | Sun Nov 22 | **Greybox swing on every laptop** | Everyone has swung between grey boxes with the mouse at 60 fps on their own laptop; an ESP32 shows up on a laptop over Bluetooth |
| M2 | Sun Nov 29 | **The race is a game** | 10 rings, timer, finish and restart; the window effect is on; the wrist device holds and releases the web, with mouse fallback |
| M3 | Sun Dec 6 | **Booth flow and two laptops** | Name to result in one 4-minute slot, leaderboard, two laptops see each other, hands and sounds in, the city reads as Manhattan |
| M4 | Sun Dec 13 | **Feature complete** | Match rules, 3 devices, spectator screen, 60 fps on the booth laptops. 1v1 go or no-go on **Dec 15** |
| M5 | Sun Dec 20 | **Code freeze** | Booth release on all three laptops, comfort playtest done. Only crash fixes merge after this |
| M6 | Tue Dec 29 | **Booth day** | Rehearsal Dec 22 to 23, day-of checklist Dec 27, booth 09:00 to 12:00 |

If a date slips, cut stretch tasks first, then the full duel. The Dec 20 freeze does not move.

## Development tasks

33 tasks in seven areas. The ones due Nov 22 unblock everything else. Owners are not listed yet; they come from the answer to Q10.

### Foundation and city

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Unity project that opens and plays with no extra downloads, plus a one-page quickstart | Nov 22 | Not started | Same Unity version as before (A1). Everyone must be able to press Play by M1. |
| Greybox district: Manhattan-sized blocks of boxes with colliders, built at start-up from one small data file | Nov 22 | Not started | Week 1 uses a generated grid (blocks about 80 m × 270 m, random heights). One scene, plain metre coordinates, no streaming (A2, A4). |
| Frame-rate readout in the game, and a check on the weakest laptop every Sunday | Nov 22, then weekly | Not started | This is how the weight budget is enforced. Write the number in the pull request. |
| Window effect: add the head-tracking package and webcam tracker, with an on/off switch | Nov 29 | Not started | The game stays fully playable with tracking off, so people without a webcam can still work (A5). |
| Make it Manhattan: real Midtown street layout and building heights, 5 to 8 landmark shapes, sky and fog, one shared window material | Dec 6 | Not started | Swaps the data file, not the code. Box buildings with a good skyline read as Manhattan and cost almost nothing. |

### Swing

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Basic swing with the mouse: aim, hold to attach, release to let go, reel in, jump, land | Nov 22 | Not started | Ported from the old `SwingPhysics`. Only the basics (A6). |
| Aim help: the nearest good anchor is picked, and the reticle turns solid when a hit is possible | Nov 29 | Not started | New players must always find something to grab in their first 10 seconds. |
| Outside playtests, then freeze the tuning | Dec 13 | Not started | Tuning that changes after this date invalidates route times and the comfort test. |
| Advanced moves from the old repo (wall run, rope wrap, two-hand launch) | Dec 13 | Not started | Stretch. Only if the race is already fun without them. |

### 1부 race mode

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Ring pass detection, timer, finish line, one-key restart | Nov 29 | Not started | The core loop of 1부. A one-key restart lets the next player start within seconds. |
| One ring route: 10 rings, about 3 minutes | Nov 29 | Not started | Stored as a list of positions in a data file, so several people can make routes. Difficulty rises along the route; it starts on a rooftop. |
| Booth flow: name entry, face calibration, countdown, run, result | Dec 6 | Not started | Fits each player into a 4-minute slot so staff never open Unity during the booth. |
| Leaderboard per 부, saved to a local file, with a reset | Dec 6 | Not started | Prizes go to the top 3 of each 부; a file survives a crash or restart. |
| Head-dodge obstacles (pigeons, a helicopter) | Dec 13 | Not started | Stretch. Uses the tracked eye position as the hit test, so ducking for real is the input. |
| Hidden items that only show when the player leans | Dec 13 | Not started | Stretch. Rewards moving the head, which is what makes the window effect visible. |

### 2부 1v1

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Position sync between the two laptops over LAN | Dec 6 | Not started | UDP at 20 to 30 updates a second. The small city has one shared coordinate system, so positions are sent as they are. Sync alone already gives a "race together" mode. |
| Opponent avatar and their web line | Dec 6 | Not started | Players need to see who they are chasing. |
| Match rules: web pulls the opponent, rooftop capture points, 3 rounds of 90 seconds | Dec 13 | Not started | One button: aiming at a building swings, aiming at the opponent pulls them. One laptop decides hits and score. |
| Go or no-go on the full duel | Dec 15 | Not started | If the rules are not stable by then, run 2부 as a two-player race on the synced build. |

### Web shooter (Bluetooth)

The plan assumes the ESP32 appears to Windows as a **Bluetooth gamepad** (A7): aim is two stick axes, the trigger is a button. Windows pairs it like any controller and the game reads it directly, with no bridge program and no Wi-Fi hotspot. Details are in [hardware/README](../hardware/README.md).

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| ESP32 firmware: sensor to yaw and pitch, trigger to a held button, sent as a Bluetooth gamepad named `SWING-1`, `SWING-2`, `SWING-3` | Nov 22 | In progress | The Arduino and HC-05 prototype works. This test also proves or rejects A7; the fallback is a Bluetooth serial link with a small bridge. Firmware lives in `hardware/` from its first commit. |
| Game reads the device: axes to an aim ray, button down to hold the web, button up to let go | Nov 29 | Not started | Any ordinary gamepad can stand in for the device, so this can be built and tested without hardware. |
| Find the device automatically, show connection status, fall back to the mouse | Nov 29 | Not started | A dropped device must never stop a run. |
| Recentre with its own button, and at every countdown | Nov 29 | Not started | The prototype recentres when the IR sensor is covered for 2 seconds, which would also happen in a long swing. |
| Measure aim drift over 3 minutes and choose MPU-6050 or BNO085 | Nov 29 | Not started | If drift stays small with a recentre per run, skip the new sensor. |
| Bluetooth soak test: 2 devices and 2 laptops side by side for 30 minutes, with phones nearby | Dec 6 | Not started | A festival room is full of phones. Count drop-outs and reconnect time; if it is unreliable, decide on a cable fallback now, not on Dec 28. |
| Build 3 devices with straps and power, optional vibration; wiring and pin table written down | Dec 13 | Not started | Two for 1v1 and one spare. Straps keep a swung arm from throwing the device. Check the power bank does not switch itself off at the ESP32's low current. |

### Look and sound

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Simple first-person hands with a firing pose | Dec 6 | Not started | The club agreed simple rounded hands look better than half-finished detailed ones. |
| Web shoot, attach, ring and finish sounds, wind that follows speed, one music loop with a clear licence | Dec 6 | Not started | The wind sound can be ported from the old repository. |
| Ring pass flash, finish and result screen effects | Dec 13 | Not started | Clear feedback is what makes players ask for another run. |

### Spectator screen and release

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Spectator view on the 전자칠판: play feed, face tracking, speed and angle, leaderboard | Dec 13 | Not started | The window effect only works for the player, so the queue needs its own screen. A second view costs frame rate; measure it. |
| 60 fps on both booth laptops at the booth resolution, with tracking and the spectator screen on | Dec 13 | Not started | Frame drops cause dizziness. |
| Portable offline booth release on both laptops and the spare, started by one double-click | Dec 20 | Not started | Code freeze. Nothing merges after this except crash fixes. |
| Comfort playtest with 5 outside players | Dec 20 | Not started | Note who gets dizzy or confused, then fix only the top issues. |

## Booth preparation

The two early items matter most: the team agrees on this plan, and the webcams are bought so tracking can be tested on the real cameras.

| Task | Due | Status | Why and details |
| --- | --- | --- | --- |
| Answer the open questions, confirm who owns each area, and make one GitHub issue per task | Nov 17 | Not started | AI agents read the rules and the issue list at the start of each session, which stops people's agents from editing the same files. |
| Buy the 2 webcams | Nov 22 | Not started | Tracking at the second seat (about 1.3 m) needs testing on the actual camera, weeks before the booth. |
| Check the application result and the assigned room | When announced | Not started | The room decides where the monitors, the 전자칠판 and the queue go. |
| Line up equipment: 2 monitors, 2 laptops plus a spare, LAN cable, power strips, HDMI | Dec 13 | Not started | Write down who brings what; the booth build is installed on all three laptops. |
| Window frame from 우드락 and 시트지, sized to the play monitor | Dec 20 | Not started | Measure the monitor first; the frame must not cover the webcam. |
| One-page poster: how to play, and why the screen looks like a window | Dec 20 | Not started | Lets people in the queue learn the controls before they sit down. |
| Buy snacks the week of the festival | Dec 22 | Not started | Items from the application budget. |
| Full rehearsal in a classroom: set up in 30 minutes, run the 1부 flow for 20 minutes with outside players | Dec 22 to 23 | Not started | Finds setup problems while there is still time to fix them. Pair every wrist device with its laptop here, not on the day. |
| Day-of checklist: setup order for 08:30 to 09:00, who stands where, what to do if a laptop or device fails | Dec 27 | Not started | The booth runs 09:00 to 12:00; there is no time to work things out on the day. |

## Risks and decisions

The biggest risk is still time. The restart leaves six weeks, and the first of them rebuilds things the old repository already had.

| Risk or decision | What to do |
| --- | --- |
| Six weeks from an empty repository | Hold the order: greybox, race, flow, then looks. Hold a 30-minute check-in every Sunday until Dec 20 where each person reports what merged, not what was tried. |
| The city gets heavy again | The weight budget is checked in every pull request and on the weakest laptop every Sunday. |
| Too much of the old swing is ported | Basics only until the race works. Advanced moves are stretch. |
| Bluetooth is unreliable in a crowded room | Soak test by Dec 6, mouse fallback with no restart, a charged spare device, devices paired at the rehearsal. |
| The Bluetooth gamepad approach (A7) does not work | It is tested in the first week. The fallback is a Bluetooth serial link plus a small bridge program. |
| One person writes almost all the code again | Everyone runs the project by Nov 22, and tasks are split so the race, the device, the sync and the look can move at the same time. |
| The full duel swallows the schedule | Go or no-go on Dec 15; the fallback is a two-player race on the synced build. |
| Spider-Man name, suit or logo in a public repository | Keep the old rule: an original web-swinging hero and a neutral name in everything that is committed (A11). |
| Players get dizzy | Seated play only, 3-minute runs, the window frame as a fixed reference, and the comfort test on Dec 20 decides the final tuning. |
| Exams or school events inside the six weeks | Not known yet (Q15). If a week is lost, cut stretch tasks first and the duel second. |
