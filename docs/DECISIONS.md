# Decisions, assumptions and open questions

One page for everything the team has decided, everything the [plan](PLAN.md) assumes without having asked, and every question that is still open. When a question is answered, move it to **Settled** with the date and who decided. Chat is not a record.

## Settled

Stated by @SW on Nov 15, 2026.

| # | Decision |
| --- | --- |
| S1 | The booth goal is unchanged: two parts, on Dec 29. |
| S2 | The game is a head-tracked monitor game set in Manhattan, in the style of Spider-Man web swinging. |
| S3 | This repository starts from scratch and is the final one. The old repository is reference only. |
| S4 | The old project failed by building a heavy city before testing game elements. The new one must run on team members' home laptops. |
| S5 | The wrist device uses an ESP32 board and connects over Bluetooth, not Wi-Fi. The rest of the hardware plan changes little. |

## Assumptions

The plan is written as if these were true. None has been confirmed. Each links to the question that would confirm or change it.

| # | Assumption | Why it was chosen | Question |
| --- | --- | --- | --- |
| A1 | The engine stays Unity 6000.3.22f1 with URP. | The head-tracking package and the swing code are Unity C# and carry over. The weight was the city, not the engine. | Q1 |
| A2 | The city is one Midtown district of about 1,000 to 2,000 box buildings, built at start-up from a data file under 1 MB. Week 1 uses a generated grid; real street layout and heights replace it by Dec 6. | Boxes cost almost nothing to draw, and a skyline of correct heights still reads as Manhattan. | Q8 |
| A3 | The weight budget is 60 fps at 1920×1080 on the weakest team laptop, under 300,000 triangles on screen, under 10 minutes from clone to playing, repository under 200 MB. | Numbers are a first guess until the weakest laptop is known. | Q2, Q3 |
| A4 | One scene, plain metre coordinates, no streaming and no floating origin. | The district is small enough, and it makes 1v1 sync simple because both laptops share the same coordinates. | |
| A5 | Head tracking reuses the `head-tracked-display` package and its Python webcam tracker, and can be switched off in the game. | It already works and is separate from the heavy city. Whether it runs well on weak laptops is not known. | Q3 |
| A6 | Only the basic swing is ported first (attach, swing, release, reel, jump). Wall run, rope wrap and two-hand launch are stretch. | They took most of the old effort and a ring race does not need them. | Q7 |
| A7 | The ESP32 presents itself as a Bluetooth Low Energy gamepad. Yaw and pitch are two axes, hold and recentre are two buttons. Windows pairs it as a controller and Unity reads it directly. | No bridge program, no hotspot, works on every ESP32 type, and any ordinary gamepad can stand in for testing. Not yet tested by this team. | Q4, Q5 |
| A8 | The device keeps its current sensor and trigger for now, and gains one extra button for recentring. | Smallest change from the working prototype. | Q6 |
| A9 | Milestones stay on Sundays with the same dates as the old plan, and the Dec 20 freeze does not move. | The booth date is fixed; only the contents of the first two weeks changed. | Q15 |
| A10 | 2부 aims for the full duel, with a go or no-go on Dec 15 and a synced two-player race as the fallback. | Unchanged from the old plan. | Q9 |
| A11 | Nothing committed uses the Spider-Man name, suit, logo, music or sounds. The working title is Manhattan Swing and the hero is original. | The old repository proposed this (its decision 0002) because the repository and releases are public. | Q13 |
| A12 | Windows only. First person only. Seated play only. | Unchanged from the old plan. | |
| A13 | The spectator screen is a second display driven by the play laptop. | Simplest wiring; costs some frame rate. | Q11 |
| A14 | Each player gets a 4-minute slot, and each 부 has its own leaderboard and top-3 prizes. | Unchanged from the old plan. | Q12 |
| A15 | Team rules stay: no direct pushes to `main`, one issue per task as the lock, a person reviews every pull request. | They are the part of the old process that worked. | Q10 |

## Open questions

Ordered by how much of the plan depends on the answer.

| # | Question | What depends on it |
| --- | --- | --- |
| Q1 | Do we stay on Unity (same version as before), or do you want a different engine? The main alternative is a browser game: nothing to install, but head tracking and the swing would be rewritten. | Everything. A1. |
| Q2 | What is the weakest laptop that must run the game (CPU, graphics, RAM)? What are the two booth laptops? | The weight budget, A3. |
| Q3 | What exactly was too heavy on home laptops: low frame rate, the download and build, or installing Unity itself? Did head tracking alone run acceptably? | Whether Unity and the Python tracker stay. A1, A5. |
| Q4 | Which ESP32 board do you have: the original ESP32 (WROOM-32 DevKit), an S3, or a C3? | Only the original ESP32 supports classic Bluetooth serial like the HC-05. All of them support Bluetooth Low Energy. A7. |
| Q5 | Is "Bluetooth gamepad" acceptable, or do you already have Bluetooth serial code you want to keep? | Whether the laptop needs a bridge program. A7. |
| Q6 | Which motion sensor is on the device now, and what does the player physically do to fire: cover the IR sensor, or press something? Can a button be added? | Hold and release, recentring, drift. A8. |
| Q7 | Is the person who wrote the old swing (한승규) available to port it, and do you agree to start with the basic swing only? | The Nov 22 milestone. A6. |
| Q8 | Is a city of boxes with correct heights and a few landmark shapes enough for the booth, or must specific places be recognisable? Which ones? | The Dec 6 city task. A2. |
| Q9 | Should 2부 still aim for the full duel, or do we decide now that it is a two-player race? | About two weeks of work in December. A10. |
| Q10 | Who is on the team now, and who owns which area (foundation and city, swing, race, 1v1, device, look and sound, booth)? Are the old roles still right? | Owners on every task; M0. A15. |
| Q11 | How is the 전자칠판 connected: HDMI from the play laptop, or its own computer? | The spectator screen design. A13. |
| Q12 | How are the three hours split between 1부 and 2부, and is 4 minutes per player still right? | Booth flow and how many people can play (one seat at 4 minutes is at most 45 players in 3 hours). A14. |
| Q13 | Is this repository public? How close to Spider-Man may the hero, name and sounds be? | Art, audio and the poster. A11. |
| Q14 | Have the webcams been bought, and has the booth application result arrived? | Booth preparation. |
| Q15 | Are there exams or school events between now and Dec 20? | Whether the weekly milestones are realistic. A9. |
