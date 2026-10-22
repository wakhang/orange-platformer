using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    /// <summary>플레이어를 따라 도는 3인칭 궤도 카메라. 메인 카메라에 부착.</summary>
    public sealed class OrbitCamera : MonoBehaviour
    {
        private const int HitBufferSize = 8;

        [SerializeField] private OrangePlatformerConfig config;
        [SerializeField] private Transform target;
        [SerializeField] private Transform ignoreRoot;

        private readonly RaycastHit[] hits = new RaycastHit[HitBufferSize];
        private IPlayerInput input;
        private Vector3 focus;
        private IPlayerMotorView motorView;
        private bool snapFocus;
        private float yaw;
        private float pitch;

        public float Yaw => yaw;
        public float Pitch => pitch;

        private void Awake()
        {
            if (target == null) return;
            // 초기 yaw는 타겟이 바라보는 방향, pitch는 기본값
            Vector3 fwd = target.forward;
            fwd.y = 0f;
            yaw = fwd.sqrMagnitude > 1e-6f ? Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg : 0f;
            pitch = config.cameraDefaultPitch;
            focus = target.position + Vector3.up * config.cameraTargetHeight;
        }

        private void Start()
        {
            if (target != null)
            {
                input = target.GetComponentInParent<IPlayerInput>();
                motorView = target.GetComponentInParent<IPlayerMotorView>();
                if (motorView != null && isActiveAndEnabled) motorView.Teleported += OnTeleported;
            }
            if (ignoreRoot == null) ignoreRoot = target;
            ApplyTransform();
        }

        private void OnEnable()
        {
            if (motorView != null) motorView.Teleported += OnTeleported;
        }

        private void OnDisable()
        {
            if (motorView != null) motorView.Teleported -= OnTeleported;
        }

        private void OnTeleported()
        {
            snapFocus = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            // 세션이 Playing일 때만 시점 회전 적용
            IGameSession session = GameSessionLocator.Current;
            if (input != null && session != null && session.State == GameState.Playing)
            {
                Vector2 look = input.LookDelta;
                yaw += look.x;
                // 마우스를 위로 올리면 카메라가 아래로 내려가 위를 보게 됨 (pitch 감소)
                pitch = Mathf.Clamp(pitch - look.y, config.cameraPitchMin, config.cameraPitchMax);
            }

            // 포커스 지점만 지수 스무딩 (프레임률 독립)
            float t = 1f - Mathf.Exp(-config.cameraFollowSharpness * dt);
            Vector3 desiredFocus = target.position + Vector3.up * config.cameraTargetHeight;
            if (snapFocus)
            {
                focus = desiredFocus;
                snapFocus = false;
            }
            else focus = Vector3.Lerp(focus, desiredFocus, t);

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rot * Vector3.back;
            float distance = config.cameraDistance;

            // 포커스에서 원하는 위치로 SphereCast, 플레이어 콜라이더/트리거 제외한 가장 가까운 충돌 사용
            int count = Physics.SphereCastNonAlloc(focus, config.cameraCollisionRadius, back, hits,
                config.cameraDistance, ~0, QueryTriggerInteraction.Ignore);
            Transform ignore = ignoreRoot != null ? ignoreRoot : target;
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = hits[i];
                if (h.collider.transform.IsChildOf(ignore)) continue;
                // 시작 지점이 콜라이더 내부면 distance==0 → 최소 거리로 클램프됨
                if (h.distance < nearest) nearest = h.distance;
            }
            if (nearest < distance) distance = Mathf.Max(config.cameraMinDistance, nearest);

            transform.SetPositionAndRotation(focus + back * distance, rot);
        }
    }
}
