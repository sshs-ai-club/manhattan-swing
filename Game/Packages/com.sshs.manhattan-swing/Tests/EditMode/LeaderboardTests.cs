using NUnit.Framework;

namespace ManhattanSwing.Core.Tests
{
    public class LeaderboardTests
    {
        [Test]
        public void RunsAreKeptFastestFirstAndAddReturnsThePlace()
        {
            var board = new Leaderboard();
            Assert.AreEqual(1, board.Add("Ann", 150));
            Assert.AreEqual(1, board.Add("Bo", 120));
            Assert.AreEqual(3, board.Add("Cy", 200));
            Assert.AreEqual(2, board.Add("Di", 130));
            Assert.AreEqual("Bo", board[0].Name);
            Assert.AreEqual("Di", board[1].Name);
            Assert.AreEqual("Ann", board[2].Name);
            Assert.AreEqual("Cy", board[3].Name);
        }

        [Test]
        public void ATieKeepsTheEarlierRunAhead()
        {
            var board = new Leaderboard();
            board.Add("First", 100);
            Assert.AreEqual(2, board.Add("Second", 100));
        }

        [Test]
        public void NamesAreMadeSafe()
        {
            Assert.AreEqual("PLAYER", Leaderboard.CleanName("   "));
            Assert.AreEqual("PLAYER", Leaderboard.CleanName(null));
            Assert.AreEqual("ab", Leaderboard.CleanName(" a\tb\n"));
            Assert.AreEqual(12, Leaderboard.CleanName("abcdefghijklmnopqrstuvwxyz").Length);
            Assert.AreEqual("한승규", Leaderboard.CleanName("한승규"));
        }

        [Test]
        public void TheBoardSurvivesBeingSavedAndLoaded()
        {
            var board = new Leaderboard();
            board.Add("한승규", 123.456f);
            board.Add("Ann B", 98.7f);
            Leaderboard loaded = Leaderboard.FromText(board.ToText());
            Assert.AreEqual(2, loaded.Count);
            Assert.AreEqual("Ann B", loaded[0].Name);
            Assert.AreEqual(98.7f, loaded[0].Seconds, 0.001f);
            Assert.AreEqual("한승규", loaded[1].Name);
            Assert.AreEqual(123.456f, loaded[1].Seconds, 0.001f);
        }

        [Test]
        public void ADamagedFileLosesOnlyItsBadLines()
        {
            Leaderboard loaded = Leaderboard.FromText("100.000\tAnn\nrubbish\n\n-5\tNeg\nNaN\tNan\n90.5\tBo\r\n");
            Assert.AreEqual(2, loaded.Count);
            Assert.AreEqual("Bo", loaded[0].Name);
            Assert.AreEqual(0, Leaderboard.FromText(null).Count);
        }
    }
}
