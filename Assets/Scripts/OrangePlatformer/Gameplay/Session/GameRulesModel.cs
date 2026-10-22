using OrangePlatformer.Core;

namespace OrangePlatformer.Gameplay
{
    /// <summary>게임 규칙(상태 전이, 오렌지/하트/타이머)을 담는 순수 C# 모델. UnityEngine 의존 없음.</summary>
    public sealed class GameRulesModel
    {
        public enum Outcome { Ignored, Respawn, GameOver }

        public GameState State { get; private set; } = GameState.Intro;
        public int Oranges { get; private set; }
        public int Required { get; }
        public int Hearts { get; private set; }
        public int MaxHearts { get; }
        public bool GoalOpen { get; private set; }
        public float Elapsed { get; private set; }

        public GameRulesModel(int orangesRequired, int maxHearts)
        {
            Required = orangesRequired;
            MaxHearts = maxHearts;
            Hearts = maxHearts;
        }

        /// <summary>Intro 상태에서만 Playing으로 전이.</summary>
        public bool StartGame()
        {
            if (State != GameState.Intro) return false;
            State = GameState.Playing;
            return true;
        }

        public bool Pause()
        {
            if (State != GameState.Playing) return false;
            State = GameState.Paused;
            return true;
        }

        public bool Resume()
        {
            if (State != GameState.Paused) return false;
            State = GameState.Playing;
            return true;
        }

        /// <summary>Playing 중에만 집계. 필요 개수에서 상한 처리하고, 처음 도달한 순간에만 true 반환(목표 개방).</summary>
        public bool CollectOrange()
        {
            if (State != GameState.Playing) return false;
            if (Oranges < Required) Oranges++;
            if (!GoalOpen && Oranges >= Required)
            {
                GoalOpen = true;
                return true;
            }
            return false;
        }

        /// <summary>Playing 중에만 처리. 하트 1 감소, 0이 되면 GameOver 상태로 전이.</summary>
        public Outcome PlayerFell()
        {
            if (State != GameState.Playing) return Outcome.Ignored;
            if (Hearts > 0) Hearts--;
            if (Hearts <= 0)
            {
                State = GameState.GameOver;
                return Outcome.GameOver;
            }
            return Outcome.Respawn;
        }

        /// <summary>Playing이면서 목표가 열려 있을 때만 Won 전이.</summary>
        public bool ReachGoal()
        {
            if (State != GameState.Playing || !GoalOpen) return false;
            State = GameState.Won;
            return true;
        }

        /// <summary>Playing 상태에서만 경과 시간 누적. 그 외 상태에서는 고정.</summary>
        public void Tick(float dt)
        {
            if (State == GameState.Playing) Elapsed += dt;
        }
    }
}
