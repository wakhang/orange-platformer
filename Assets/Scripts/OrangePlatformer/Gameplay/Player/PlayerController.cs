using System;
using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    /// <summary>
    /// CharacterController 기반 플레이어 모터. 입력은 Update에서 래치하고 FixedUpdate에서 소비한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IPlayerMotorView, IRespawnable
    {
        private const float GroundStickVelocity = -2f; // 지면 접촉 유지를 위한 작은 하향 속도
        private const float GroundNormalMinY = 0.7f;
        private const float NoContactTime = 999f;
        private const float StandCastRadiusScale = 0.95f;

        [SerializeField] private OrangePlatformerConfig config;
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private IPlayerInput input;

        private PlayerMotorState state = PlayerMotorState.Grounded;
        private Vector3 velocity;
        private bool grounded = true;
        private bool controlsEnabled = true;

        // 입력 래치 (Update에서 기록, FixedUpdate에서 소비)
        private bool latchedJumpPressed;
        private bool latchedCrouchPressed;
        private bool latchedDivePressed;
        private bool latchedJumpHeld;
        private bool crouchHeld;
        private Vector2 moveInput;

        private JumpTimers timers;
        private float stateTimer;
        private float wallContactTimer = NoContactTime;
        private float wallLockTimer;
        private Vector3 wallNormal;
        private Vector3 desiredFacing = Vector3.forward;

        // 공중 시간(airtime) 플래그
        private bool doubleJumpAvailable = true;
        private bool diveUsed;
        private bool highJumpAirtime;
        private bool jumpCutAllowed;
        private bool poundImpactFired;
        private bool poundBrokeThisMove;
        private bool crouchShape;

        public event Action<JumpKind> Jumped;
        public event Action<float> Landed;
        public event Action DiveStarted;
        public event Action GroundPoundStarted;
        public event Action GroundPoundImpact;
        public event Action Teleported;

        public PlayerMotorState State { get { return state; } }
        public Vector3 Velocity { get { return velocity; } }
        public bool IsGrounded { get { return grounded; } }
        public Vector3 WallNormal { get { return wallNormal; } }

        public bool ControlsEnabled
        {
            get { return controlsEnabled; }
            set
            {
                controlsEnabled = value;
                if (!value)
                {
                    // 조작 불가: 래치된 입력을 버리고 수평 속도를 즉시 0으로
                    latchedJumpPressed = false;
                    latchedCrouchPressed = false;
                    latchedDivePressed = false;
                    latchedJumpHeld = false;
                    crouchHeld = false;
                    moveInput = Vector2.zero;
                    velocity.x = 0f;
                    velocity.z = 0f;
                }
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<IPlayerInput>();
            if (config == null)
            {
                Debug.LogError("PlayerController: config is not assigned.", this);
                enabled = false;
                return;
            }
            if (input == null)
            {
                Debug.LogError("PlayerController: no IPlayerInput found on this GameObject.", this);
            }

            controller.radius = config.controllerRadius;
            timers = new JumpTimers(config.coyoteTime, config.jumpBufferTime);
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 1e-4f) desiredFacing = fwd.normalized;
            crouchShape = true; // 아래 ApplyShape가 서 있는 형태로 강제 적용하도록
            ApplyShape(false);
        }

        // ---------------------------------------------------------------- 입력 / 시각 (Update)

        private void Update()
        {
            if (input == null || !controlsEnabled) return;

            moveInput = input.Move;
            crouchHeld = input.CrouchHeld;
            // 한 프레임짜리 입력이 FixedUpdate 사이에서 사라지지 않도록 래치
            if (input.JumpPressed) latchedJumpPressed = true;
            if (input.CrouchPressed) latchedCrouchPressed = true;
            if (input.DivePressed) latchedDivePressed = true;
            if (input.JumpHeld || input.JumpPressed) latchedJumpHeld = true;

            // 방향 갱신 지연(최대 1 틱)을 없애기 위해 조향 가능한 상태에서는 매 프레임 목표 방향을 갱신
            bool steerable = state == PlayerMotorState.Grounded || state == PlayerMotorState.Crouching
                || ((state == PlayerMotorState.Airborne || state == PlayerMotorState.WallSlide) && wallLockTimer <= 0f);
            if (steerable)
            {
                float camYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
                UpdateFacing(PlayerMovementMath.CameraRelative(moveInput, camYaw));
            }

            // 캐릭터를 이동 방향으로 회전 (시각 처리)
            if (desiredFacing.sqrMagnitude > 1e-4f)
            {
                Quaternion target = Quaternion.LookRotation(desiredFacing, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, config.turnSpeedDeg * Time.deltaTime);
            }
        }

        // ---------------------------------------------------------------- 물리 (FixedUpdate)

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            bool jumpPressed = latchedJumpPressed;
            bool crouchPressed = latchedCrouchPressed;
            bool divePressed = latchedDivePressed;
            bool jumpHeld = latchedJumpHeld;
            latchedJumpPressed = false;
            latchedCrouchPressed = false;
            latchedDivePressed = false;
            // 탭 입력도 최소 한 스텝은 '누름 상태'로 보이도록 현재 상태로 재설정
            latchedJumpHeld = controlsEnabled && input != null && input.JumpHeld;

            timers.Tick(dt);
            wallContactTimer += dt;
            if (wallLockTimer > 0f) wallLockTimer -= dt;
            if (stateTimer > 0f) stateTimer -= dt;
            if (jumpPressed) timers.PressJump();

            float yaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            Vector3 wish = controlsEnabled ? PlayerMovementMath.CameraRelative(moveInput, yaw) : Vector3.zero;

            // 1) 상태별 결정 (수평 속도, 상태 전이, 점프 시작)
            switch (state)
            {
                case PlayerMotorState.Grounded:
                case PlayerMotorState.Crouching:
                    StepGrounded(wish, dt);
                    break;
                case PlayerMotorState.Airborne:
                case PlayerMotorState.WallSlide:
                    StepAirborne(wish, dt, jumpPressed, crouchPressed, divePressed, jumpHeld);
                    break;
                case PlayerMotorState.Dive:
                    // 다이브 중에는 조향 불가, 수평 속도 유지
                    break;
                case PlayerMotorState.DiveSlide:
                    StepDiveSlide(dt);
                    break;
                case PlayerMotorState.GroundPoundHang:
                    velocity = Vector3.zero;
                    if (stateTimer <= 0f) BeginGroundPoundFall();
                    break;
                case PlayerMotorState.GroundPoundFall:
                    velocity.x = 0f;
                    velocity.z = 0f;
                    break;
                case PlayerMotorState.GroundPoundLand:
                    velocity.x = 0f;
                    velocity.z = 0f;
                    if (stateTimer <= 0f) state = PlayerMotorState.Grounded;
                    break;
            }

            if (!controlsEnabled)
            {
                velocity.x = 0f;
                velocity.z = 0f;
            }

            // 2) 수직 적분
            float dy;
            switch (state)
            {
                case PlayerMotorState.Grounded:
                case PlayerMotorState.Crouching:
                case PlayerMotorState.DiveSlide:
                case PlayerMotorState.GroundPoundLand:
                    velocity.y = GroundStickVelocity;
                    dy = velocity.y * dt;
                    break;
                case PlayerMotorState.GroundPoundHang:
                    velocity.y = 0f;
                    dy = 0f;
                    break;
                case PlayerMotorState.GroundPoundFall:
                    // 중력 없이 일정 속도 (maxFallSpeed 무시)
                    dy = velocity.y * dt;
                    break;
                default:
                    dy = IntegrateGravity(dt);
                    break;
            }

            ApplyShape(state == PlayerMotorState.Crouching);

            // 3) 이동 및 착지/이탈 판정
            if (state == PlayerMotorState.GroundPoundHang)
            {
                grounded = false;
                return;
            }

            float preMoveVy = velocity.y;
            poundBrokeThisMove = false;
            controller.Move(new Vector3(velocity.x * dt, dy, velocity.z * dt));
            bool groundedNow = controller.isGrounded;

            // 그라운드 파운드로 부순 파괴물 위에서는 착지로 보지 않고 계속 낙하 (부서진 콜라이더는 이미 꺼짐)
            if (poundBrokeThisMove && state == PlayerMotorState.GroundPoundFall) groundedNow = false;

            if (groundedNow && !grounded)
            {
                grounded = true;
                OnLanded(-preMoveVy);
            }
            else if (!groundedNow && grounded)
            {
                grounded = false;
                OnLeftGround();
            }
        }

        /// <summary>
        /// 대칭 적분: 새 속도 = v - g*dt, 이동량 = 평균 속도 * dt.
        /// 등가속 운동에서 정확하므로 정점 높이가 v^2/(2g)와 일치한다.
        /// </summary>
        private float IntegrateGravity(float dt)
        {
            float minVy = state == PlayerMotorState.WallSlide ? -config.wallSlideMaxFallSpeed : -config.maxFallSpeed;
            float vy = Mathf.Max(velocity.y, minVy);
            float newVy = Mathf.Max(vy - config.gravity * dt, minVy);
            velocity.y = newVy;
            return 0.5f * (vy + newVy) * dt;
        }

        // ---------------------------------------------------------------- 상태별 스텝

        private void StepGrounded(Vector3 wish, float dt)
        {
            bool wantsCrouch = controlsEnabled && crouchHeld;
            if (state == PlayerMotorState.Grounded && wantsCrouch)
            {
                state = PlayerMotorState.Crouching;
            }
            else if (state == PlayerMotorState.Crouching && !wantsCrouch && CanStandUp())
            {
                // 머리 위가 비어있을 때만 일어선다
                state = PlayerMotorState.Grounded;
            }

            bool crouching = state == PlayerMotorState.Crouching;
            float speed = crouching ? config.crouchMoveSpeed : config.moveSpeed;
            Vector3 target = wish * speed;
            float accel = wish.sqrMagnitude > 1e-4f ? config.groundAcceleration : config.groundDeceleration;
            velocity = PlayerMovementMath.MoveTowardHorizontal(velocity, target, accel * dt);
            UpdateFacing(wish);

            // 버퍼된 점프가 있으면 발동 (착지 직전 입력 포함). 웅크린 상태면 하이점프
            if (controlsEnabled && timers.ConsumeBuffered())
            {
                StartJump(crouching ? JumpKind.High : JumpKind.Single);
            }
        }

        private void StepAirborne(Vector3 wish, float dt, bool jumpPressed, bool crouchPressed, bool divePressed, bool jumpHeld)
        {
            bool sliding = state == PlayerMotorState.WallSlide;
            bool wallContact = wallContactTimer <= config.wallContactGraceTime;

            if (controlsEnabled)
            {
                // 점프 우선순위: 코요테 지상 점프 > 벽 점프 > 더블 점프
                if (timers.HasBufferedJump && timers.CanCoyoteJump)
                {
                    timers.ConsumeBuffered();
                    StartJump(JumpKind.Single);
                    return;
                }
                if (timers.HasBufferedJump && wallContact)
                {
                    timers.ConsumeBuffered();
                    StartJump(JumpKind.Wall);
                    return;
                }
                if (jumpPressed && doubleJumpAvailable && !highJumpAirtime)
                {
                    timers.ConsumeBuffered();
                    StartJump(JumpKind.Double);
                    return;
                }

                if (!sliding)
                {
                    // 다이브: 공중 1회, 조향/점프/그라운드파운드 불가
                    if (divePressed && !diveUsed)
                    {
                        StartDive();
                        return;
                    }
                    // 그라운드 파운드
                    if (crouchPressed)
                    {
                        StartGroundPound();
                        return;
                    }
                }
            }

            // 가변 점프: 상승 중 점프 버튼을 떼면 상승 속도를 한 번만 감쇠 (단일/더블 점프만)
            if (jumpCutAllowed && velocity.y > 0f && !jumpHeld)
            {
                velocity.y *= config.jumpCutMultiplier;
                jumpCutAllowed = false;
            }

            // 공중 수평 조향 (벽 점프 직후에는 입력 잠금)
            if (wallLockTimer <= 0f)
            {
                float speed = highJumpAirtime ? config.highJumpForwardSpeed : config.moveSpeed;
                velocity = PlayerMovementMath.MoveTowardHorizontal(velocity, wish * speed, config.airAcceleration * dt);
                UpdateFacing(wish);
            }
            if (highJumpAirtime)
            {
                velocity = PlayerMovementMath.ClampHorizontal(velocity, config.highJumpForwardSpeed);
            }

            // 벽 슬라이드 전이: 벽 접촉(유예 시간 이내) + 하강 중
            if (wallContact && velocity.y <= 0f)
            {
                state = PlayerMotorState.WallSlide;
            }
            else if (sliding)
            {
                state = PlayerMotorState.Airborne;
            }
        }

        private void StepDiveSlide(float dt)
        {
            // 남은 시간 동안 감속하며 미끄러진 뒤 Grounded로
            float decel = config.diveSpeed / Mathf.Max(0.01f, config.diveLandSlideTime);
            velocity = PlayerMovementMath.MoveTowardHorizontal(velocity, Vector3.zero, decel * dt);
            if (stateTimer <= 0f)
            {
                velocity.x = 0f;
                velocity.z = 0f;
                state = PlayerMotorState.Grounded;
            }
        }

        // ---------------------------------------------------------------- 액션 시작

        private void StartJump(JumpKind kind)
        {
            float height;
            switch (kind)
            {
                case JumpKind.Double:
                    height = config.doubleJumpHeight;
                    doubleJumpAvailable = false;
                    jumpCutAllowed = true;
                    break;
                case JumpKind.High:
                    height = config.highJumpHeight;
                    doubleJumpAvailable = false; // 하이점프 후 더블점프 없음
                    highJumpAirtime = true;
                    jumpCutAllowed = false;
                    velocity = PlayerMovementMath.ClampHorizontal(velocity, config.highJumpForwardSpeed);
                    break;
                case JumpKind.Wall:
                    height = 0f;
                    break;
                default:
                    height = config.jumpHeight;
                    doubleJumpAvailable = true; // 코요테 점프도 단일 점프이므로 더블점프 재충전
                    jumpCutAllowed = true;
                    break;
            }

            if (kind == JumpKind.Wall)
            {
                // 벽 반대 방향으로 튕겨 나가며 더블 점프 충전
                velocity = new Vector3(wallNormal.x * config.wallJumpHorizontalSpeed,
                                       config.wallJumpVerticalSpeed,
                                       wallNormal.z * config.wallJumpHorizontalSpeed);
                wallLockTimer = config.wallJumpInputLockTime;
                wallContactTimer = NoContactTime;
                doubleJumpAvailable = true;
                highJumpAirtime = false;
                jumpCutAllowed = false;
                if (wallNormal.sqrMagnitude > 1e-4f)
                {
                    desiredFacing = wallNormal;
                    transform.rotation = Quaternion.LookRotation(wallNormal, Vector3.up);
                }
            }
            else
            {
                velocity.y = PlayerMovementMath.JumpSpeed(config.gravity, height);
            }

            state = PlayerMotorState.Airborne;
            grounded = false;
            timers.NotifyJumped();
            if (Jumped != null) Jumped(kind);
        }

        private void StartDive()
        {
            Vector3 dir = desiredFacing;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
            dir.Normalize();
            velocity = new Vector3(dir.x * config.diveSpeed, config.diveUpwardSpeed, dir.z * config.diveSpeed);
            state = PlayerMotorState.Dive;
            diveUsed = true;
            jumpCutAllowed = false;
            if (DiveStarted != null) DiveStarted();
        }

        private void StartGroundPound()
        {
            velocity = Vector3.zero;
            state = PlayerMotorState.GroundPoundHang;
            stateTimer = config.groundPoundHangTime;
            doubleJumpAvailable = false;
            jumpCutAllowed = false;
            if (GroundPoundStarted != null) GroundPoundStarted();
        }

        private void BeginGroundPoundFall()
        {
            state = PlayerMotorState.GroundPoundFall;
            velocity = new Vector3(0f, -config.groundPoundSpeed, 0f);
            poundImpactFired = false;
        }

        // ---------------------------------------------------------------- 착지 / 이탈

        private void OnLanded(float impactSpeed)
        {
            PlayerMotorState previous = state;
            switch (previous)
            {
                case PlayerMotorState.GroundPoundFall:
                    state = PlayerMotorState.GroundPoundLand;
                    stateTimer = config.groundPoundLandStunTime;
                    velocity = Vector3.zero;
                    break;
                case PlayerMotorState.Dive:
                    state = PlayerMotorState.DiveSlide;
                    stateTimer = config.diveLandSlideTime;
                    break;
                default:
                    state = PlayerMotorState.Grounded;
                    break;
            }

            // 착지 시 공중 플래그 초기화 (더블점프 재충전 등)
            velocity.y = GroundStickVelocity;
            doubleJumpAvailable = true;
            diveUsed = false;
            highJumpAirtime = false;
            jumpCutAllowed = false;
            wallContactTimer = NoContactTime;
            wallLockTimer = 0f;
            timers.NotifyGrounded();

            if (previous == PlayerMotorState.GroundPoundFall && !poundImpactFired)
            {
                poundImpactFired = true;
                if (GroundPoundImpact != null) GroundPoundImpact();
            }
            if (Landed != null) Landed(impactSpeed);
        }

        private void OnLeftGround()
        {
            // 걸어서 벗어난 경우에만 코요테 시간 시작 (점프는 NotifyJumped로 이미 차단)
            switch (state)
            {
                case PlayerMotorState.Grounded:
                case PlayerMotorState.Crouching:
                case PlayerMotorState.DiveSlide:
                case PlayerMotorState.GroundPoundLand:
                    state = PlayerMotorState.Airborne;
                    // 점프 없이 걸어서 벗어난 경우: 코요테 점프만 가능, 더블점프는 소모 (인수 기준 6)
                    doubleJumpAvailable = false;
                    timers.NotifyLeftGround();
                    break;
            }
        }

        // ---------------------------------------------------------------- 충돌

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Vector3 n = hit.normal;

            // 그라운드 파운드 중 바닥 충돌: IGroundPoundable에 알리고 계속 낙하
            if (state == PlayerMotorState.GroundPoundFall && n.y > GroundNormalMinY)
            {
                if (!poundImpactFired)
                {
                    IGroundPoundable poundable = hit.collider.GetComponentInParent<IGroundPoundable>();
                    if (poundable != null)
                    {
                        poundImpactFired = true;
                        poundBrokeThisMove = true;
                        poundable.OnGroundPound(hit.point);
                        if (GroundPoundImpact != null) GroundPoundImpact();
                    }
                }
                return;
            }

            // 벽 판정: 법선이 거의 수평
            if (Mathf.Abs(n.y) < config.wallNormalMaxY)
            {
                Vector3 hn = new Vector3(n.x, 0f, n.z);
                if (hn.sqrMagnitude < 1e-6f) return;
                hn.Normalize();

                if (state == PlayerMotorState.Dive)
                {
                    // 다이브 중 벽에 부딪히면 수평 속도 0
                    velocity.x = 0f;
                    velocity.z = 0f;
                }
                else
                {
                    // 벽 안쪽으로 향하는 속도 성분 제거
                    float into = velocity.x * hn.x + velocity.z * hn.z;
                    if (into < 0f)
                    {
                        velocity.x -= hn.x * into;
                        velocity.z -= hn.z * into;
                    }
                }

                if (hit.collider.TryGetComponent<WallJumpSurface>(out _))
                {
                    wallNormal = hn;
                    wallContactTimer = 0f;
                }
            }
        }

        // ---------------------------------------------------------------- 헬퍼

        private void UpdateFacing(Vector3 wish)
        {
            if (wish.sqrMagnitude > 1e-4f) desiredFacing = wish.normalized;
        }

        /// <summary>캐릭터 컨트롤러 높이/중심을 서기 또는 웅크리기 형태로 맞춘다 (중심 = 높이/2, 발 기준).</summary>
        private void ApplyShape(bool crouched)
        {
            if (crouched == crouchShape) return;
            crouchShape = crouched;
            float h = crouched ? config.crouchHeight : config.controllerHeight;
            controller.height = h;
            controller.center = new Vector3(0f, h * 0.5f, 0f);
        }

        /// <summary>웅크린 머리 위부터 서 있는 높이까지 구체 캐스트로 장애물을 검사.</summary>
        private bool CanStandUp()
        {
            float r = config.controllerRadius * StandCastRadiusScale;
            Vector3 origin = transform.position + Vector3.up * (config.crouchHeight - r);
            float distance = config.controllerHeight - config.crouchHeight;
            if (distance <= 0f) return true;
            return !Physics.SphereCast(origin, r, Vector3.up, out _, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);

            velocity = Vector3.zero;
            state = PlayerMotorState.Grounded;
            grounded = true;
            stateTimer = 0f;
            wallContactTimer = NoContactTime;
            wallLockTimer = 0f;
            wallNormal = Vector3.zero;
            doubleJumpAvailable = true;
            diveUsed = false;
            highJumpAirtime = false;
            jumpCutAllowed = false;
            poundImpactFired = false;
            latchedJumpPressed = false;
            latchedCrouchPressed = false;
            latchedDivePressed = false;
            latchedJumpHeld = false;
            timers.Reset();

            Vector3 fwd = rotation * Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 1e-4f) desiredFacing = fwd.normalized;

            ApplyShape(false);
            controller.enabled = true;
            if (Teleported != null) Teleported();
        }
    }
}
