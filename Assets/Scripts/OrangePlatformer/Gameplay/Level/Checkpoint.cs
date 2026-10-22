using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class Checkpoint : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color InactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color ActiveColor = new Color(1f, 0.847f, 0.29f, 1f); // #FFD84A

        [SerializeField] private Transform respawnPoint;
        [SerializeField] private Renderer flagRenderer;

        private MaterialPropertyBlock block;
        private bool active;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            block = new MaterialPropertyBlock();
            ApplyColor(InactiveColor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

            IGameSession session = GameSessionLocator.Current;
            if (session == null) return;

            // 스폰 위치는 respawnPoint, 없으면 자기 자신의 트랜스폼
            Transform t = respawnPoint != null ? respawnPoint : transform;
            session.SetCheckpoint(t.position, t.rotation);

            // 한 번 켜지면 계속 켜둠 (이전 체크포인트도 점등 유지)
            if (!active)
            {
                active = true;
                ApplyColor(ActiveColor);
            }
        }

        private void ApplyColor(Color c)
        {
            if (flagRenderer == null) return;
            flagRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            flagRenderer.SetPropertyBlock(block);
        }
    }
}
