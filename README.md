# Manhattan Swing

A head-tracked monitor game: the screen becomes a window into Manhattan, and you swing between buildings on webs aimed with a wrist device. Built by the SSHS AI club for the 천년제 booth on **Dec 29, 2026**.

Status: **a first playable version is written but has not been opened in Unity yet.** Midtown as box buildings, a mouse-controlled web swing, a 10-ring timed race and a HUD are in place. Head tracking, sound, the leaderboard and the 1v1 duel are not started. See the [plan](docs/PLAN.md#where-we-are) for what has and has not been checked.

This repository is a fresh start. The earlier attempt, [manhattan-headtracked-viewer](https://github.com/sshs-ai-club/manhattan-headtracked-viewer), is kept as a reference.

## Start here

| Read | For |
| --- | --- |
| [docs/QUICKSTART.md](docs/QUICKSTART.md) | Installing Unity, opening the project, controls |
| [docs/PLAN.md](docs/PLAN.md) | The booth to-do: milestones, tasks, risks, and where the team takes over |
| [docs/DECISIONS.md](docs/DECISIONS.md) | What is decided, the choices made while building, and open questions |
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
docs/        quickstart, plan and decisions
hardware/    wrist device firmware and wiring
tools/city/  rebuilds the city file from NYC open data (not needed to play)
```

## Credits

City data comes from [NYC Open Data](https://opendata.cityofnewyork.us/) under its terms of use; see [tools/city/README.md](tools/city/README.md).
