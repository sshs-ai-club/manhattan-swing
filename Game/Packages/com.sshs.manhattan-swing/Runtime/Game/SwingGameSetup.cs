using ManhattanSwing.Core;
using UnityEngine;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// What a scene gives the game when it starts it with <see cref="SwingGame.Create"/>.
    ///
    /// The test map in this repository fills it in <c>Assets/Scripts/TestMap/TestMapStart.cs</c>.
    /// A detailed map fills it in its own start-up script; see docs/PORTING.md.
    /// </summary>
    public sealed class SwingGameSetup
    {
        /// <summary>The city the player collides with and fires webs at. Required.</summary>
        public ISwingWorld World;

        /// <summary>The race, in route coordinates (see <see cref="WorldFrame"/>). Required.</summary>
        public Route Route;

        /// <summary>Where route coordinates are in this scene. The test map needs no change.</summary>
        public WorldFrame Frame = WorldFrame.Identity;

        /// <summary>
        /// The camera to use as the player's eyes. Null makes a new one. A given camera is moved
        /// under the player's "Eyes" object but otherwise left as the scene set it up.
        /// </summary>
        public Camera Camera;

        /// <summary>Background colour of a camera the game makes itself.</summary>
        public Color Sky = new Color(0.66f, 0.78f, 0.92f);

        /// <summary>
        /// Start this game's own head tracking (when the package is installed). Turn it off in a
        /// scene that already runs head tracking its own way.
        /// </summary>
        public bool HeadTracking = true;

        /// <summary>The leaderboard file's name, so different maps or routes keep separate boards.</summary>
        public string LeaderboardName = "race";

        /// <summary>Metres past the edge of the play area before a player is sent back to the last ring.</summary>
        public float PlayAreaMargin = 100f;
    }
}
