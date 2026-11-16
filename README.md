# Manhattan Swing

A head-tracked monitor game: the screen becomes a window into Manhattan, and you swing between buildings on webs aimed with a wrist device. Built by the SSHS AI club for the 천년제 booth on **Dec 29, 2026**.

Status: **the race runs in Unity** on the light test map (Midtown as box buildings): web swing, 10 rings, name entry, sounds and head tracking. Later batches (swing help, tuning panel, checkpoints, booth mode, and the move to a shared package) are written and tested outside Unity but not yet run in it. The 1v1 duel is not started. See the [plan](docs/PLAN.md#where-we-are) for what has and has not been checked.

**How this repository relates to the original one.** The original repository, [manhattan-headtracked-viewer](https://github.com/sshs-ai-club/manhattan-headtracked-viewer), has a detailed city and is still being developed. This one is where gameplay is built and tested on a light map that every laptop can run. When a piece plays well, the original repository picks it up and tests it on the detailed city. Which one the booth runs is not decided yet. The gameplay is a Unity package so it moves with one line: see [PORTING](docs/PORTING.md).

## Start here

| Read | For |
| --- | --- |
| [docs/QUICKSTART.md](docs/QUICKSTART.md) | Installing Unity, opening the project, controls |
| [docs/PLAN.md](docs/PLAN.md) | The booth to-do: milestones, tasks, risks, and where the team takes over |
| [docs/DECISIONS.md](docs/DECISIONS.md) | What is decided, the choices made while building, and open questions |
| [docs/PORTING.md](docs/PORTING.md) | Using this gameplay on the detailed city in the original repository |
| [docs/HEAD-TRACKING.md](docs/HEAD-TRACKING.md) | Setting up the webcam window effect |
| [hardware/README.md](hardware/README.md) | The wrist device and how it talks to the game |
| [AGENTS.md](AGENTS.md) | Rules for everyone, and for AI agents, working here |

## The booth

- **1부, race (09:00 to 10:30):** one player swings a 10-ring route in about 3 minutes. Top 3 on the leaderboard win a prize.
- **2부, duel (10:30 to 12:00):** two players on two laptops over LAN.
- Spectators watch on the 전자칠판.

## The one rule that matters

The game must run at 60 fps on the weakest laptop on the team. The last attempt built a city too heavy for anyone to run at home before it had a game. This time the game comes first and the city stays light: the whole of Midtown is a 170 KB file. The limits are in the [plan](docs/PLAN.md#weight-budget).

## Layout

```text
Game/        the Unity project (Unity 6000.3.22f1)
  Packages/com.sshs.manhattan-swing/   the gameplay, as a package the original repository can add
  Assets/                              the light test map that starts it
docs/        quickstart, plan and decisions
hardware/    wrist device firmware and wiring
tools/city/  rebuilds the city file from NYC open data (not needed to play)
```

## Credits

City data comes from [NYC Open Data](https://opendata.cityofnewyork.us/) under its terms of use; see [tools/city/README.md](tools/city/README.md). Head tracking uses the [Head Tracked Display](https://github.com/lollipop190000/head-tracked-display) package (MIT licence), copied into `Game/Packages/`.
