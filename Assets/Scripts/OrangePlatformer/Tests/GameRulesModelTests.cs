using NUnit.Framework;
using OrangePlatformer.Core;
using OrangePlatformer.Gameplay;

namespace OrangePlatformer.Tests
{
    public class GameRulesModelTests
    {
        private static GameRulesModel Playing(int required = 8, int hearts = 3)
        {
            var m = new GameRulesModel(required, hearts);
            Assert.IsTrue(m.StartGame());
            return m;
        }

        [Test]
        public void StartsInIntro_AndStartGameOnlyFromIntro()
        {
            var m = new GameRulesModel(8, 3);
            Assert.AreEqual(GameState.Intro, m.State);
            Assert.IsTrue(m.StartGame());
            Assert.AreEqual(GameState.Playing, m.State);
            Assert.IsFalse(m.StartGame());
        }

        [Test]
        public void Timer_CountsOnlyWhilePlaying()
        {
            var m = new GameRulesModel(8, 3);
            m.Tick(1f);
            Assert.AreEqual(0f, m.Elapsed);
            m.StartGame();
            m.Tick(1.5f);
            Assert.AreEqual(1.5f, m.Elapsed, 1e-5f);
            m.Pause();
            m.Tick(5f);
            Assert.AreEqual(1.5f, m.Elapsed, 1e-5f);
            m.Resume();
            m.Tick(0.5f);
            Assert.AreEqual(2f, m.Elapsed, 1e-5f);
        }

        [Test]
        public void Hearts_TwoRespawnsThenGameOver()
        {
            var m = Playing(8, 3);
            Assert.AreEqual(GameRulesModel.Outcome.Respawn, m.PlayerFell());
            Assert.AreEqual(2, m.Hearts);
            Assert.AreEqual(GameRulesModel.Outcome.Respawn, m.PlayerFell());
            Assert.AreEqual(1, m.Hearts);
            Assert.AreEqual(GameState.Playing, m.State);
            Assert.AreEqual(GameRulesModel.Outcome.GameOver, m.PlayerFell());
            Assert.AreEqual(0, m.Hearts);
            Assert.AreEqual(GameState.GameOver, m.State);
        }

        [Test]
        public void Oranges_OpenGoalExactlyOnce_AndCap()
        {
            var m = Playing(8, 3);
            int openCount = 0;
            for (int i = 0; i < 12; i++)
                if (m.CollectOrange()) openCount++;
            Assert.AreEqual(1, openCount);
            Assert.AreEqual(8, m.Oranges);
            Assert.IsTrue(m.GoalOpen);
        }

        [Test]
        public void Goal_ClosedUntilAllOranges()
        {
            var m = Playing(8, 3);
            Assert.IsFalse(m.ReachGoal());
            Assert.AreEqual(GameState.Playing, m.State);
            for (int i = 0; i < 8; i++) m.CollectOrange();
            Assert.IsTrue(m.ReachGoal());
            Assert.AreEqual(GameState.Won, m.State);
        }

        [Test]
        public void PauseResume_Transitions_AndNoCollectWhilePaused()
        {
            var m = new GameRulesModel(8, 3);
            Assert.IsFalse(m.Pause());
            Assert.IsFalse(m.Resume());
            m.StartGame();
            Assert.IsTrue(m.Pause());
            Assert.AreEqual(GameState.Paused, m.State);
            Assert.IsFalse(m.CollectOrange());
            Assert.AreEqual(0, m.Oranges);
            Assert.IsFalse(m.Pause());
            Assert.IsTrue(m.Resume());
            Assert.AreEqual(GameState.Playing, m.State);
            m.CollectOrange();
            Assert.AreEqual(1, m.Oranges);
        }

        [Test]
        public void Fall_IgnoredOutsidePlaying()
        {
            var m = new GameRulesModel(8, 3);
            Assert.AreEqual(GameRulesModel.Outcome.Ignored, m.PlayerFell());
            m.StartGame();
            m.Pause();
            Assert.AreEqual(GameRulesModel.Outcome.Ignored, m.PlayerFell());
            Assert.AreEqual(3, m.Hearts);
        }
    }
}
