# Manhattan Swing

A head-tracked monitor game: the screen becomes a window into Manhattan, and you swing between buildings on webs aimed with a wrist device. Built by the SSHS AI club for the 천년제 booth on **Dec 29, 2026**.

Status: **planning.** There is no game code yet. This repository is a fresh start; the earlier attempt, [manhattan-headtracked-viewer](https://github.com/sshs-ai-club/manhattan-headtracked-viewer), is kept as a reference.

## Start here

| Read | For |
| --- | --- |
| [docs/PLAN.md](docs/PLAN.md) | The booth to-do: milestones, tasks, risks |
| [docs/DECISIONS.md](docs/DECISIONS.md) | What is decided, what the plan assumes, and the open questions |
| [hardware/README.md](hardware/README.md) | The wrist device and how it talks to the game |
| [AGENTS.md](AGENTS.md) | Rules for everyone, and for AI agents, working here |

## The booth

- **1부, race:** one player swings a 10-ring route in about 3 minutes. Top 3 on the leaderboard win a prize.
- **2부, 1v1:** two players on two laptops over LAN.
- Spectators watch on the 전자칠판.

## The one rule that matters

The game must run at 60 fps on the weakest laptop on the team. The last attempt built a city too heavy for anyone to run at home before it had a game. This time the game comes first and the city stays light. The limits are in the [plan](docs/PLAN.md#weight-budget).

## Layout

```text
docs/        plan and decisions
hardware/    wrist device firmware and wiring
Game/        the Unity project (added at the first milestone)
```
