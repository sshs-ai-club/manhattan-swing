using UnityEngine;

namespace Swing.Game
{
    /// <summary>
    /// Choices that belong to a laptop, not to the game's design: they are remembered on this
    /// laptop between runs and are changed in the F2 and F3 panels.
    /// </summary>
    public sealed class GameOptions
    {
        private const string BoothKey = "boothMode", FollowKey = "followTravel";

        /// <summary>
        /// For the festival. Players cannot restart or take a second turn on their own (R needs
        /// Ctrl held), and the developer keys (F3 tuning, P) are switched off.
        /// </summary>
        public bool BoothMode = PlayerPrefs.GetInt(BoothKey, 0) == 1;

        /// <summary>The view turns by itself to face the way you are travelling.</summary>
        public bool FollowTravel = PlayerPrefs.GetInt(FollowKey, 0) == 1;

        public void Save()
        {
            PlayerPrefs.SetInt(BoothKey, BoothMode ? 1 : 0);
            PlayerPrefs.SetInt(FollowKey, FollowTravel ? 1 : 0);
        }
    }
}
