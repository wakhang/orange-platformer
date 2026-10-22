using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    /// <summary>믹서 내부 목표 트리거. 골이 열린 상태에서 플레이어가 들어오면 클리어.</summary>
    [RequireComponent(typeof(Collider))]
    public class GoalTrigger : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

            IGameSession session = GameSessionLocator.Current;
            if (session == null || !session.GoalOpen || session.State != GameState.Playing) return;

            session.ReachGoal();
        }
    }
}
