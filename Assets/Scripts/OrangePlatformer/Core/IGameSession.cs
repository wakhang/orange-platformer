using System;
using UnityEngine;

namespace OrangePlatformer.Core
{
    public interface IGameSession
    {
        GameState State { get; }
        int OrangesCollected { get; }
        int OrangesRequired { get; }
        int Hearts { get; }
        int MaxHearts { get; }
        float ElapsedSeconds { get; }
        bool GoalOpen { get; }
        event Action<GameState> StateChanged;
        event Action<int, int> OrangesChanged;
        event Action<int, int> HeartsChanged;
        event Action GoalOpened;
        event Action PlayerRespawned;
        event Action<RunResult> RunEnded;
        void StartGame();
        void Pause();
        void Resume();
        void Restart();
        // gameplay-only
        void CollectOrange();
        void PlayerFell();
        void SetCheckpoint(Vector3 position, Quaternion rotation);
        void ReachGoal();
    }
}
