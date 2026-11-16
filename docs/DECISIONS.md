# Decisions, assumptions and open questions

One page for what the team has decided, what the [plan](PLAN.md) still assumes, and what is still open. When a question is answered, move it to **Settled** with the date. Chat is not a record.

## Settled

Decided by @SW on Nov 15, 2026, unless another name is given.

| # | Decision |
| --- | --- |
| S1 | The booth is two parts on Dec 29: 1부 race 09:00 to 10:30, 2부 duel 10:30 to 12:00. The application has passed. |
| S2 | The game is a head-tracked monitor game set in Manhattan, in the style of Spider-Man web swinging. |
| S3 | This repository starts from scratch and is the final one. The old repository is reference only. |
| S4 | The old project was too heavy: low frame rates at home, and a setup whose "bake" step alone took over 2 hours. The new one must stay light. |
| S5 | Unity stays the engine. |
| S6 | The city does not need to be all of Manhattan now. Start small; the whole island is the eventual ideal. |
| S7 | The wrist device is a Seeed XIAO ESP32-C3 over Bluetooth, appearing to the PC as a gamepad. The device works out the hand direction and sends the result. (Proposed by 임영민.) |
| S8 | Hardware code must be readable and easy to change for someone who knows hardware. |
| S9 | The basic swing comes first; advanced moves later. |
| S10 | 2부 is the full duel. |
| S11 | The repository is public. Character work stays simple for v1; a Spider-Man-like character can come later. |
| S12 | Dates are not fixed by the old plan and exams are not a constraint. The milestones in the plan are the recommendation being followed. |
| S13 | Basic gameplay is built first, without assigning team roles. The plan states the point where the team takes over. |
| S14 | Windows only, first person, seated play, a 4-minute slot per player, a leaderboard and top-3 prizes per 부. |
| S15 | Two webcams will be bought. |
| S16 | The spectator screen is a third laptop connected to the 전자칠판 by HDMI. |
| S17 | A duel is three rounds of 90 seconds, for now. |
| S18 | In 1부 both laptops run the race. |
| S19 | The room is 외국어 강의실 4, with two player seats (A and B), a queue area and the 전자칠판. |

## Choices made while building

These were made to get a first version written. Each can be changed; say so before building on it.

| # | Choice | Why | What it costs |
| --- | --- | --- | --- |
| C1 | Unity **6000.3.22f1**, the same version as the old project. | It is the Unity 6.3 long-term-support line, the head-tracking package was made for it, and some members already have it. Every laptop must use the same version anyway, and Unity Hub installs the exact one the project asks for. | Which version Unity Hub offers first to a new user today was not checked. |
| C2 | The city is Midtown only: 6,899 box buildings between about 29th Street and the bottom of Central Park. | Enough to race in, quick to load and test. | Flying past the edge shows empty land. `--area all` gives the whole island. |
| C3 | One scene, plain coordinates, no streaming. In plain words: the whole city is loaded once when the game starts, and a position is just three numbers in metres that mean the same thing on every laptop. The old project loaded and unloaded pieces of the city as you moved and kept shifting its coordinates to stay precise. | The small city makes that machinery unnecessary. It also makes the duel simpler, because both laptops agree on where everything is. | A much larger or more detailed city would need streaming again. |
| C4 | The game rules (collision, swing, race) are plain C# with no Unity types, in `Scripts/Core`. | They can be tested without opening Unity, which is how this version was checked. | Positions are converted between two vector types at the edge. |
| C5 | Collision is our own code against the boxes. Unity's physics engine is not used. | No object per building, the same result on every laptop, and fast. | Anything that should collide must be added to this code. |
| C6 | Unity's built-in render pipeline with one simple shader: no lights, shadows or textures. | The lightest option, and nothing to configure. | Fancier effects later may mean moving to URP. The shader was written to work there too, but that is not tested. |
| C7 | The scene file is empty; the game builds itself from code when Play starts. | Two people never edit the same scene file. | Things are placed in code, not by dragging in the editor. |
| C8 | Swing numbers (gravity 24, top speed 95, jump 20, reel 24) are taken from the old project's tuning. | A known starting point. | None has been felt in this game yet. |
| C9 | Holding a web while standing reels you in by itself. | A one-button device can then start a swing from the ground. | May feel odd with the mouse; it is one switch in `SwingSettings.cs`. |
| C10 | With a wrist device, pointing near the screen edge turns the view. | The game can then be played with one hand. | Unproven; needs a playtest. |
| C11 | The flex sensor is the hold trigger, and recentring gets its own button and also happens at each countdown. | The infrared sensor could only fire once, and its 2-second cover would recentre during a long swing. | One more button to wire. |
| C18 | The game adds its own smoothing after the head-tracking package instead of changing the package. | The package stays an untouched copy, and the extra step can be switched off to compare. | Two layers of filtering to understand. |
| C19 | Swinging is helped: webs shorten to clear the street and to gain height, and letting go on the way up adds 8% speed. | Plain pendulum physics was too hard: an automatic pilot could not get past ring 2. | Less realistic. Every one is a slider on F3 and a number in `SwingSettings.cs`. |
| C20 | Booth mode is a switch on each laptop (F2). In it, R needs Ctrl held, and the tuning panel and route key are off. | Fair turns at the festival without removing the keys for development. | Staff must remember to switch it on. |
| C21 | A run ends as "time up" after 5 minutes and is not put on the leaderboard. | The queue must keep moving. | The number is a guess until a full turn is timed. |
| C12 | Nothing committed uses the Spider-Man name, suit, logo, music or sounds. The working title is Manhattan Swing. | The repository is public. | Follows from S11; revisit when character work starts. |
| C13 | The head-tracking package is copied into `Game/Packages/` instead of being downloaded by Unity. | Opening the project then needs no Git download and cannot fail on a laptop without Git on its path. | Updating the package means copying a newer version in. |
| C14 | Head tracking is compiled separately and only when the package is present; it starts switched off. | The game must run without a webcam or the package. | One extra small interface (`IHeadTracking`). |
| C15 | Sound effects are made from maths at start-up, not from audio files. | No downloads, no licences, and they could be tested here. | They are plain; replace them with recordings when someone has better ones. |
| C16 | The leaderboard is a text file in Unity's per-user data folder on each laptop, written after every run. Reset renames the old file. | Survives a crash; a reset by mistake can be undone. | Two laptops keep two files (see Q7). |
| C17 | After a finish, R runs again with the same name and Enter goes to the next player. | Fast retries while testing, a clean hand-over at the booth. | At the booth, staff may want R disabled so nobody takes two turns. |

## Open questions

| # | Question | What depends on it |
| --- | --- | --- |
| Q1 | Which laptop on the team is the weakest (CPU, graphics, RAM)? It will show up when everyone reports their frame rate. | The frame-rate target in the budget. |
| Q2 | Does head tracking run fast enough on the booth laptops with the game? Not known until it is added and measured. | Whether the existing tracker is kept or changed. |
| Q3 | What exactly should improve in the swing and controls? For example: hard to gain height, hard to turn corners, hits walls, too fast or slow, mouse too sensitive, the web misses. | What to tune first. |
| Q4 | With head tracking on, the view is as wide as the real monitor looks from the seat, which is narrower than the ordinary view. Is that good for swinging, or should the window be made to act wider? | Head tracking tuning. |
| Q5 | Does typing a Korean name work on the name screen? (The name screen works; Hangul is not confirmed.) | Booth turn. |
| Q6 | Who owns which piece of work? Deferred. | Issues and reviews. |
| Q7 | In 1부 the two laptops keep separate leaderboards. Compare the top 3 by hand at 10:30, or send both to one file over the LAN? | Prizes. |
| Q8 | Is 5 minutes the right time limit for a 4-minute slot? | Booth turn. |
| Q9 | The old repository is still being developed (one-button grip swing, camera that follows travel, two-glove input). Should that work move here, and who decides? | Whether two versions of the swing keep diverging. |
