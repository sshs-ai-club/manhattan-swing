# Rules for people and AI agents

Read [docs/PLAN.md](docs/PLAN.md) and [docs/DECISIONS.md](docs/DECISIONS.md) before changing anything. [docs/QUICKSTART.md](docs/QUICKSTART.md) says how to open and run the project.

## The game stays light

- Every change must keep the [weight budget](docs/PLAN.md#weight-budget). State the measured frame rate in the pull request, or write "not measured".
- Game before graphics: do not add detail to the city until the task you are on needs it.
- The mouse always works. Head tracking and the wrist device can each be turned off.
- No streaming, no bake step, no large downloads. If a feature needs one, it needs a decision first.
- Port from the old repository one piece at a time, for a task that needs it. Do not copy folders across.
- Game rules live in `Game/Assets/Scripts/Core` and use no Unity types, so they can be tested without Unity. Keep it that way, and add a test with every rule you add.
- The Unity scene stays empty. Create things from code so two people never edit the same scene file.
- Use Unity 6000.3.22f1 exactly.

## Working together

- Work on a branch and open a pull request; a person reviews and merges it. (The first version was pushed straight to `main` before anyone else was working here.)
- One task is one GitHub issue, and the issue is the lock: only work on an issue assigned to your user. Check open pull requests for the same files first.
- Keep pull requests small, and keep `main` playable.
- A choice that affects other people goes in [docs/DECISIONS.md](docs/DECISIONS.md), not only in chat.
- Do not invent test results. Write "not tested".
- Do not commit builds, caches, logs, personal calibration, passwords or tool-specific agent files.
- Every asset needs a source and a licence. Nothing with the Spider-Man name, suit, logo, music or sounds is committed.
