using OrangePlatformer.Core;
using OrangePlatformerPolish.Core;
using UnityEngine;

namespace OrangePlatformerPolish.Gameplay
{
    /// <summary>FixedUpdate 모터 위치를 렌더 프레임에 맞게 보간해 Visual에 적용한다. 루트(Transform)는 건드리지 않는다.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerRenderInterpolator : MonoBehaviour
    {
        [SerializeField] private OrangePlatformerPolishConfig config;
        [SerializeField] private Transform visual;

        private IPlayerMotorView view;
        private Vector3 baseLocalPos;
        private Vector3 prevTick;
        private bool snap;

        public Vector3 RenderPosition { get; private set; }

        private void Awake()
        {
            view = GetComponent<IPlayerMotorView>();
            if (visual != null) baseLocalPos = visual.localPosition;
            prevTick = transform.position;
            RenderPosition = prevTick;
        }

        private void OnEnable()
        {
            if (view != null) view.Teleported += OnTeleported;
            prevTick = transform.position;
        }

        private void OnDisable()
        {
            if (view != null) view.Teleported -= OnTeleported;
            if (visual != null) visual.localPosition = baseLocalPos;
        }

        private void OnTeleported()
        {
            snap = true;
        }

        // 순서 -200: PlayerController.FixedUpdate보다 먼저 실행되어 틱 직전 위치를 저장한다.
        private void FixedUpdate()
        {
            prevTick = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 curr = transform.position;
            Vector3 render;
            if (config == null || !config.interpolationEnabled)
            {
                render = curr;
            }
            else if (snap || RenderInterpolation.IsTeleport(prevTick, curr, config.teleportSnapDistance))
            {
                prevTick = curr;
                render = curr;
            }
            else
            {
                float a = RenderInterpolation.Alpha(Time.time, Time.fixedTime, Time.fixedDeltaTime);
                render = Vector3.Lerp(prevTick, curr, a);
            }
            snap = false;
            RenderPosition = render;

            if (visual != null)
                visual.localPosition = baseLocalPos + transform.InverseTransformVector(render - curr);
        }
    }
}
