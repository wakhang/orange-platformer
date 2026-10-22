using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    public class BreakableCrate : MonoBehaviour, IGroundPoundable
    {
        [SerializeField] private GameObject orangeChild;
        /// <summary>상자가 부서질 때(상자 중심). 이펙트용.</summary>
        public static event System.Action<Vector3> Broken;

        private Renderer[] ownRenderers;
        private Collider[] ownColliders;
        private bool broken;

        private void Awake()
        {
            // 자식(오렌지)은 건드리지 않도록 자기 자신의 컴포넌트만 캐시
            ownRenderers = GetComponents<Renderer>();
            ownColliders = GetComponents<Collider>();

            if (orangeChild == null)
            {
                // 비활성 자식 포함 검색
                OrangePickup pickup = GetComponentInChildren<OrangePickup>(true);
                if (pickup != null) orangeChild = pickup.gameObject;
            }
        }

        public void OnGroundPound(Vector3 impactPoint)
        {
            if (broken) return; // 한 번만 파괴
            broken = true;

            for (int i = 0; i < ownRenderers.Length; i++) ownRenderers[i].enabled = false;
            for (int i = 0; i < ownColliders.Length; i++) ownColliders[i].enabled = false;

            // 오렌지 카운트는 플레이어가 오렌지를 만질 때만 증가
            if (orangeChild != null) orangeChild.SetActive(true);
            Broken?.Invoke(transform.position);
        }
    }
}
