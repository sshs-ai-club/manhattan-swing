using System;
using System.IO;
using ManhattanSwing.Core;
using UnityEngine;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// Keeps a leaderboard in a text file on this laptop, written after every run so a crash loses
    /// nothing. Each mode has its own file: <c>leaderboard_race.txt</c> for 1부 and, later,
    /// <c>leaderboard_duel.txt</c> for 2부. The folder is printed in the Unity console at start.
    /// </summary>
    public sealed class LeaderboardFile
    {
        public readonly Leaderboard Board;
        private readonly string path;

        public LeaderboardFile(string mode)
        {
            path = Path.Combine(Application.persistentDataPath, "leaderboard_" + mode + ".txt");
            Board = File.Exists(path) ? Leaderboard.FromText(File.ReadAllText(path)) : new Leaderboard();
            Debug.Log("Leaderboard file: " + path);
        }

        /// <summary>Adds a run, saves the file and returns the run's place (1 is fastest).</summary>
        public int Add(string name, float seconds)
        {
            int place = Board.Add(name, seconds);
            Save();
            return place;
        }

        /// <summary>
        /// Empties the board for a new session. The old file is renamed, not deleted, so a reset by
        /// mistake can be undone by renaming it back.
        /// </summary>
        public void Reset()
        {
            try
            {
                if (File.Exists(path))
                    File.Move(path, path.Replace(".txt", "_before_reset_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"));
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not keep a copy of the old leaderboard: " + error.Message);
            }
            Board.Clear();
            Save();
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(path, Board.ToText());
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not save the leaderboard: " + error.Message);
            }
        }
    }
}
