using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    /// <summary>Visual 오브젝트용: 애니메이터 파라미터, 스쿼시/스트레치, 공중 회전(플립)/기울기 연출.</summary>
    public sealed class PlayerVisuals : MonoBehaviour
    {
        private const float TiltSharpnessScale = 1f;
        private const float WallSlideTiltDeg = 15f;
        private const float HighJumpFlipDeg = 720f;
        private const float FullFlipDeg = 360f;
        private const float HighJumpDurationScale = 1.5f;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private OrangePlatformerConfig config;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform spin;

        private IPlayerMotorView view;
        private Vector3 baseScale;
        private float currentScaleY = 1f;

        // 플립: 시간 기반 각도 애니메이션 (축 * 총 각도, smoothstep 이징)
        private bool flipActive;
        private Vector3 flipAxis;
        private float flipTotalDeg;
        private float flipDuration;
        private float flipTime;

        private Quaternion currentTilt = Quaternion.identity;

        private void Awake()
        {
            view = GetComponentInParent<IPlayerMotorView>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spin == null) spin = transform.Find("Spin");
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (view == null) return;
            view.Jumped += OnJumped;
            view.Landed += OnLanded;
            view.GroundPoundStarted += OnGroundPoundStarted;
        }

        private void OnDisable()
        {
            if (view == null) return;
            view.Jumped -= OnJumped;
            view.Landed -= OnLanded;
            view.GroundPoundStarted -= OnGroundPoundStarted;
            // 비활성화 시 연출 상태 초기화
            flipActive = false;
            currentTilt = Quaternion.identity;
            currentScaleY = 1f;
            ApplyScale();
            if (spin != null) spin.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (view == null) return;
            float dt = Time.deltaTime;

            UpdateAnimator();
            UpdateSquash(dt);
            UpdateSpin(dt);
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;
            Vector3 v = view.Velocity;
            animator.SetFloat(SpeedHash, Mathf.Sqrt(v.x * v.x + v.z * v.z));
        }

        private void UpdateSquash(float dt)
        {
            // 웅크리기 중에는 crouchScaleY가 목표, 그 외에는 1로 지수 복귀
            float targetY = view.State == PlayerMotorState.Crouching ? config.crouchScaleY : 1f;
            float t = 1f - Mathf.Exp(-config.squashRecoverSharpness * dt);
            currentScaleY = Mathf.Lerp(currentScaleY, targetY, t);
            ApplyScale();
        }

        private void ApplyScale()
        {
            // 부피 보존 근사: XZ = 1/sqrt(Y)
            float y = Mathf.Max(0.05f, currentScaleY);
            float xz = 1f / Mathf.Sqrt(y);
            transform.localScale = new Vector3(baseScale.x * xz, baseScale.y * y, baseScale.z * xz);
        }

        private void UpdateSpin(float dt)
        {
            if (spin == null) return;

            // 상태 기반 기울기 목표: 다이브는 X축 앞으로, 벽 슬라이드는 벽 반대쪽 Z 기울기
            Quaternion targetTilt = Quaternion.identity;
            PlayerMotorState state = view.State;
            if (state == PlayerMotorState.Dive)
            {
                targetTilt = Quaternion.Euler(config.diveTiltDeg, 0f, 0f);
            }
            else if (state == PlayerMotorState.WallSlide)
            {
                // 벽 법선이 플레이어 오른쪽을 향하면(벽이 왼쪽) 오른쪽으로 기울어야 함 → 음의 Z 회전
                float side = Vector3.Dot(view.WallNormal, transform.right);
                float sign = side >= 0f ? 1f : -1f;
                targetTilt = Quaternion.Euler(0f, 0f, -sign * WallSlideTiltDeg);
            }
            float t = 1f - Mathf.Exp(-config.squashRecoverSharpness * TiltSharpnessScale * dt);
            currentTilt = Quaternion.Slerp(currentTilt, targetTilt, t);

            Quaternion flip = Quaternion.identity;
            if (flipActive)
            {
                flipTime += dt;
                if (flipTime >= flipDuration)
                {
                    // 종료 시 정확히 identity로 복귀
                    flipActive = false;
                }
                else
                {
                    float u = flipTime / flipDuration;
                    float eased = u * u * (3f - 2f * u);
                    flip = Quaternion.AngleAxis(flipTotalDeg * eased, flipAxis);
                }
            }

            spin.localRotation = currentTilt * flip;
        }

        private void StartFlip(Vector3 axis, float totalDeg, float duration)
        {
            flipActive = true;
            flipAxis = axis;
            flipTotalDeg = totalDeg;
            flipDuration = Mathf.Max(0.01f, duration);
            flipTime = 0f;
        }

        private void OnJumped(JumpKind kind)
        {
            currentScaleY = config.jumpStretch;
            switch (kind)
            {
                case JumpKind.Double:
                    StartFlip(Vector3.right, FullFlipDeg, config.flipDuration);
                    break;
                case JumpKind.High:
                    StartFlip(Vector3.up, HighJumpFlipDeg, config.flipDuration * HighJumpDurationScale);
                    break;
            }
        }

        private void OnLanded(float impactSpeed)
        {
            currentScaleY = config.landSquash;
            flipActive = false;
        }

        private void OnGroundPoundStarted()
        {
            StartFlip(Vector3.right, FullFlipDeg, config.groundPoundHangTime);
        }
    }
}
