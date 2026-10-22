using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

            IGameSession session = GameSessionLocator.Current;
            if (session == null || session.State != GameState.Playing) return;

            session.PlayerFell();
        }
    }
}
