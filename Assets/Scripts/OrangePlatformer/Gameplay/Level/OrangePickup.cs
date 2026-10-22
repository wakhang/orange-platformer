using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    public class OrangePickup : MonoBehaviour
    {
        [SerializeField] private OrangePlatformerConfig config;

        private SphereCollider sphere;
        private Vector3 baseLocalPosition;
        private float phase;

        private void Awake()
        {
            sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            if (config != null) sphere.radius = config.pickupRadius;
        }

        private void OnEnable()
        {
            // 보빙 기준 위치 캐시 (활성화 시점 기준)
            baseLocalPosition = transform.localPosition;
            // 오렌지마다 위상을 다르게 해서 동시에 출렁이지 않게 함
            phase = baseLocalPosition.x + baseLocalPosition.z;
        }

        private void Update()
        {
            if (config == null) return;

            // Y축 회전 + 사인파 상하 보빙 (스케일된 시간 사용)
            transform.Rotate(0f, config.pickupSpinDegPerSec * Time.deltaTime, 0f, Space.Self);
            Vector3 p = baseLocalPosition;
            p.y += Mathf.Sin(Time.time * 2f + phase) * config.pickupBobAmplitude;
            transform.localPosition = p;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

            IGameSession session = GameSessionLocator.Current;
            if (session == null || session.State != GameState.Playing) return;

            session.CollectOrange();
            gameObject.SetActive(false);
        }
    }
}
