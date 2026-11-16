# Manhattan Swing gameplay

The game without a map: web swinging, the ring race, HUD, sounds, leaderboard, the wrist device and head-tracking set-up.

It is developed and tested in [manhattan-swing](https://github.com/sshs-ai-club/manhattan-swing) on a light test map, and is meant to be added to the detailed city in [manhattan-headtracked-viewer](https://github.com/sshs-ai-club/manhattan-headtracked-viewer) once it plays well. How to do that is in [docs/PORTING.md](https://github.com/sshs-ai-club/manhattan-swing/blob/main/docs/PORTING.md).

| Folder | What it holds |
| --- | --- |
| `Runtime/Core` | The rules as plain C# with no Unity types: swing, aim help, race, route, leaderboard, sounds, smoothing. `ISwingWorld` is the only link to a map. |
| `Runtime/Game` | The Unity side: `SwingGame` (start it with `SwingGame.Create`), player, input, wrist device, HUD, `PhysicsWorld` (collision with Unity colliders) |
| `Runtime/HeadTracking` | Head-tracking set-up; compiled only when `com.headtracked.display` is installed |
| `Tests/EditMode` | Tests of the rules; none needs a map |

Unity 6000.3. Works in the built-in render pipeline and URP. Input System only.
