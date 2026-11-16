using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ManhattanSwing.Core
{
    public struct LeaderboardEntry
    {
        public string Name;
        public float Seconds;
    }

    /// <summary>
    /// Finish times, fastest first. It turns itself into text and back so the game can keep it in a
    /// file that survives a crash. One line per run: the time in seconds, a tab, then the name.
    /// </summary>
    public sealed class Leaderboard
    {
        public const int MaxNameLength = 12;
        public const string DefaultName = "PLAYER";

        private readonly List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        public int Count { get { return entries.Count; } }
        public LeaderboardEntry this[int index] { get { return entries[index]; } }

        /// <summary>Adds a run and returns its place, where 1 is the fastest.</summary>
        public int Add(string name, float seconds)
        {
            var entry = new LeaderboardEntry { Name = CleanName(name), Seconds = seconds };
            int place = 0;
            while (place < entries.Count && entries[place].Seconds <= seconds) place++;   // ties keep the earlier run ahead
            entries.Insert(place, entry);
            return place + 1;
        }

        public void Clear()
        {
            entries.Clear();
        }

        /// <summary>Makes a typed name safe to show and store: one line, no tabs, not empty, not too long.</summary>
        public static string CleanName(string name)
        {
            var clean = new StringBuilder();
            foreach (char c in name ?? "")
                if (!char.IsControl(c)) clean.Append(c);
            string result = clean.ToString().Trim();
            if (result.Length > MaxNameLength) result = result.Substring(0, MaxNameLength).Trim();
            return result.Length == 0 ? DefaultName : result;
        }

        public string ToText()
        {
            var text = new StringBuilder();
            foreach (LeaderboardEntry entry in entries)
                text.Append(entry.Seconds.ToString("0.000", CultureInfo.InvariantCulture)).Append('\t').Append(entry.Name).Append('\n');
            return text.ToString();
        }

        /// <summary>Reads text written by <see cref="ToText"/>. Lines that cannot be read are skipped.</summary>
        public static Leaderboard FromText(string text)
        {
            var board = new Leaderboard();
            foreach (string line in (text ?? "").Split('\n'))
            {
                int tab = line.IndexOf('\t');
                float seconds;
                if (tab <= 0) continue;
                if (!float.TryParse(line.Substring(0, tab), NumberStyles.Float, CultureInfo.InvariantCulture, out seconds)) continue;
                if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) continue;
                board.Add(line.Substring(tab + 1), seconds);
            }
            return board;
        }
    }
}
