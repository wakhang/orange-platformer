using OrangePlatformer.Core;
using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    /// <summary>
    /// 게임 이벤트(점프·착지·다이브·엉덩방아·수집·체크포인트·낙사·승리 등)에 맞춰 파티클을 뿌리는 이펙트 관리자.
    /// 모양별 ParticleSystem 하나씩을 월드 공간에 두고 Emit()으로만 방출하므로 이벤트마다 오브젝트 생성·GC 할당이 없다.
    /// 텍스처는 3x2 아틀라스(부드러운 원, 별, 물방울, 잎, 링, 조각) 한 장과 머티리얼 하나를 공유한다.
    /// </summary>
    [DefaultExecutionOrder(500)] // OrbitCamera(0)가 위치를 정한 뒤 카메라 흔들림을 더한다
    public sealed class OrangeVfx : MonoBehaviour
    {
        private enum Tile { Soft = 0, Star = 1, Drop = 2, Leaf = 3, Ring = 4, Chunk = 5 }

        [SerializeField] private Material material;
        [SerializeField] private bool cameraShake = true;
        [SerializeField, Range(0f, 1f)] private float shakeStrength = 0.18f;
        [SerializeField] private float seaLevel = -3.45f;
        [SerializeField] private bool ambient = true;
        [SerializeField, Tooltip("모든 이펙트 입자 크기 배율")] private float sizeScale = 1.7f;
        [SerializeField, Tooltip("모든 이펙트 수명 배율")] private float lifeScale = 1.3f;

        // 팔레트
        private static readonly Color32 Dust = new Color32(255, 246, 228, 200);
        private static readonly Color32 Juice = new Color32(255, 150, 20, 255);
        private static readonly Color32 JuiceLight = new Color32(255, 205, 60, 255);
        private static readonly Color32 Sparkle = new Color32(255, 240, 150, 255);
        private static readonly Color32 Peel = new Color32(255, 128, 20, 255);
        private static readonly Color32 Wood = new Color32(170, 112, 60, 255);
        private static readonly Color32 Leaf = new Color32(120, 200, 80, 230);
        private static readonly Color32 Petal = new Color32(255, 250, 240, 220);
        private static readonly Color32[] Confetti =
        {
            new Color32(255, 140, 20, 255), new Color32(255, 220, 60, 255), new Color32(120, 205, 80, 255),
            new Color32(255, 250, 235, 255), new Color32(90, 200, 220, 255), new Color32(255, 110, 140, 255)
        };

        private ParticleSystem softPs, starPs, dropPs, chunkPs, ringPs, streakPs, confettiPs, petalPs, glintPs;
        private ParticleSystem.EmitParams ep;

        private Transform player;
        private IPlayerMotorView motor;
        private IGameSession session;
        private Transform cam;
        private Transform goal;

        private int lastOranges;
        private int lastHearts;
        private float runAcc, slideAcc, trailAcc, petalAcc, glintAcc, goalAcc, confettiAcc;
        private float goalSparkleTime, winConfettiTime;
        private float shakeTime, shakeDuration, shakeAmp;

        private void Awake()
        {
            softPs = CreateSystem("Vfx_Soft", Tile.Soft, -0.15f, ParticleSystemRenderMode.Billboard, 400, Grow(0.6f, 1.4f));
            starPs = CreateSystem("Vfx_Star", Tile.Star, 0f, ParticleSystemRenderMode.Billboard, 300, Pop());
            dropPs = CreateSystem("Vfx_Drop", Tile.Drop, 1.6f, ParticleSystemRenderMode.Billboard, 300, Shrink());
            chunkPs = CreateSystem("Vfx_Chunk", Tile.Chunk, 1.8f, ParticleSystemRenderMode.Billboard, 200, Shrink());
            ringPs = CreateSystem("Vfx_Ring", Tile.Ring, 0f, ParticleSystemRenderMode.HorizontalBillboard, 40, Grow(0.3f, 1f));
            streakPs = CreateSystem("Vfx_Streak", Tile.Soft, 0f, ParticleSystemRenderMode.Stretch, 120, Shrink());
            confettiPs = CreateSystem("Vfx_Confetti", Tile.Chunk, 0.35f, ParticleSystemRenderMode.Billboard, 400, Flat());
            petalPs = CreateSystem("Vfx_Petal", Tile.Leaf, 0.03f, ParticleSystemRenderMode.Billboard, 120, Flat());
            glintPs = CreateSystem("Vfx_Glint", Tile.Star, 0f, ParticleSystemRenderMode.Billboard, 120, Pop());

            var sr = streakPs.GetComponent<ParticleSystemRenderer>();
            sr.velocityScale = 0.06f;
            sr.lengthScale = 1.5f;

            // 회전하며 떨어지는 조각·컨페티·꽃잎
            SetSpin(chunkPs, 360f);
            SetSpin(confettiPs, 540f);
            SetSpin(petalPs, 90f);
            SetNoise(confettiPs, 0.8f);
            SetNoise(petalPs, 0.5f);

            ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
        }

        private void OnEnable()
        {
            Checkpoint.Activated += OnCheckpoint;
            BreakableCrate.Broken += OnCrateBroken;
            GameSessionLocator.Registered += BindSession;
            if (GameSessionLocator.Current != null) BindSession(GameSessionLocator.Current);
        }

        private void OnDisable()
        {
            Checkpoint.Activated -= OnCheckpoint;
            BreakableCrate.Broken -= OnCrateBroken;
            GameSessionLocator.Registered -= BindSession;
            UnbindSession();
            UnbindPlayer();
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) BindPlayer(p.transform);
            if (Camera.main != null) cam = Camera.main.transform;
            MixerGoal mg = FindAnyObjectByType<MixerGoal>();
            if (mg != null) goal = mg.transform;
        }

        // ---------- 바인딩 ----------

        private void BindPlayer(Transform p)
        {
            player = p;
            motor = p.GetComponent<IPlayerMotorView>();
            if (motor == null) return;
            motor.Jumped += OnJumped;
            motor.Landed += OnLanded;
            motor.DiveStarted += OnDiveStarted;
            motor.GroundPoundImpact += OnGroundPoundImpact;
        }

        private void UnbindPlayer()
        {
            if (motor == null) return;
            motor.Jumped -= OnJumped;
            motor.Landed -= OnLanded;
            motor.DiveStarted -= OnDiveStarted;
            motor.GroundPoundImpact -= OnGroundPoundImpact;
            motor = null;
        }

        private void BindSession(IGameSession s)
        {
            if (session == s) return;
            UnbindSession();
            session = s;
            lastOranges = s.OrangesCollected;
            lastHearts = s.Hearts;
            s.OrangesChanged += OnOrangesChanged;
            s.HeartsChanged += OnHeartsChanged;
            s.GoalOpened += OnGoalOpened;
            s.PlayerRespawned += OnRespawned;
            s.StateChanged += OnStateChanged;
        }

        private void UnbindSession()
        {
            if (session == null) return;
            session.OrangesChanged -= OnOrangesChanged;
            session.HeartsChanged -= OnHeartsChanged;
            session.GoalOpened -= OnGoalOpened;
            session.PlayerRespawned -= OnRespawned;
            session.StateChanged -= OnStateChanged;
            session = null;
        }

        // ---------- 이벤트 → 이펙트 ----------

        private Vector3 Feet => player.position + Vector3.up * 0.05f;
        private Vector3 Chest => player.position + Vector3.up * 0.6f;

        private void OnJumped(JumpKind kind)
        {
            switch (kind)
            {
                case JumpKind.Single:
                    PuffRing(Feet, 8, 1.6f, 0.35f);
                    break;
                case JumpKind.Double:
                    // 발밑 링 + 반짝이 + 귤 껍질 조각
                    Emit(ringPs, Feet, Vector3.zero, 1.4f, 0.35f, Sparkle);
                    Burst(starPs, Feet, 8, 2.5f, 0.25f, 0.5f, Sparkle, 1f);
                    Burst(chunkPs, Feet, 6, 2.2f, 0.12f, 0.6f, Peel, 1.5f);
                    break;
                case JumpKind.High:
                    // 위로 솟는 먼지 줄기
                    for (int i = 0; i < 14; i++)
                    {
                        Vector3 v = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(3f, 7f), Random.Range(-0.6f, 0.6f));
                        Emit(softPs, Feet, v, Random.Range(0.25f, 0.45f), Random.Range(0.35f, 0.6f), Dust);
                    }
                    Burst(starPs, Feet + Vector3.up * 0.5f, 10, 3f, 0.2f, 0.6f, Sparkle, 2f);
                    PuffRing(Feet, 10, 2f, 0.4f);
                    break;
                case JumpKind.Wall:
                    Vector3 n = motor.WallNormal;
                    Vector3 contact = player.position + Vector3.up * 0.5f - n * 0.3f;
                    for (int i = 0; i < 10; i++)
                        Emit(softPs, contact, n * Random.Range(1f, 2.5f) + Random.insideUnitSphere * 0.8f, Random.Range(0.2f, 0.4f), 0.45f, Dust);
                    Burst(starPs, contact, 5, 2f, 0.2f, 0.4f, Sparkle, 0f);
                    break;
            }
        }

        private void OnLanded(float impactSpeed)
        {
            if (motor.State == PlayerMotorState.GroundPoundLand) return; // 충격파는 OnGroundPoundImpact에서
            if (impactSpeed < 3f) return;
            float k = Mathf.InverseLerp(3f, 20f, impactSpeed);
            PuffRing(Feet, Mathf.RoundToInt(Mathf.Lerp(6f, 16f, k)), Mathf.Lerp(1.4f, 3.2f, k), Mathf.Lerp(0.3f, 0.55f, k));
        }

        private void OnDiveStarted()
        {
            Vector3 back = -player.forward;
            Burst(starPs, Chest + back * 0.3f, 6, 1.5f, 0.2f, 0.35f, Sparkle, 0f);
            for (int i = 0; i < 6; i++)
                Emit(softPs, Chest, back * Random.Range(1.5f, 3f) + Random.insideUnitSphere * 0.5f, 0.3f, 0.35f, Dust);
        }

        private void OnGroundPoundImpact()
        {
            Emit(ringPs, Feet, Vector3.zero, 3.6f, 0.45f, Dust);
            Emit(ringPs, Feet + Vector3.up * 0.02f, Vector3.zero, 2.2f, 0.3f, Sparkle);
            PuffRing(Feet, 18, 4f, 0.5f);
            Burst(chunkPs, Feet + Vector3.up * 0.2f, 10, 3.5f, 0.12f, 0.7f, Wood, 3f);
            Burst(starPs, Feet + Vector3.up * 0.3f, 8, 3f, 0.25f, 0.4f, Sparkle, 1.5f);
            Shake(0.22f, 1f);
        }

        private void OnOrangesChanged(int collected, int required)
        {
            bool gained = collected > lastOranges;
            lastOranges = collected;
            if (!gained || player == null) return;
            // 주스 물방울 + 반짝이
            Burst(dropPs, Chest, 16, 3.5f, 0.14f, 0.7f, Juice, 2.5f);
            Burst(dropPs, Chest, 8, 2.5f, 0.1f, 0.6f, JuiceLight, 2f);
            Burst(starPs, Chest, 8, 2f, 0.3f, 0.5f, Sparkle, 0.5f);
            Emit(ringPs, Feet, Vector3.zero, 1.6f, 0.35f, JuiceLight);
        }

        private void OnHeartsChanged(int hearts, int max)
        {
            bool lost = hearts < lastHearts;
            lastHearts = hearts;
            if (!lost || player == null) return;
            // 주스 바다에 첨벙
            Vector3 p = new Vector3(player.position.x, seaLevel, player.position.z);
            Emit(ringPs, p + Vector3.up * 0.05f, Vector3.zero, 3f, 0.6f, JuiceLight);
            for (int i = 0; i < 28; i++)
            {
                Vector3 v = new Vector3(Random.Range(-2f, 2f), Random.Range(4f, 9f), Random.Range(-2f, 2f));
                Emit(dropPs, p, v, Random.Range(0.15f, 0.35f), Random.Range(0.7f, 1.1f), i % 3 == 0 ? JuiceLight : Juice);
            }
        }

        private void OnRespawned()
        {
            if (player == null) return;
            PuffRing(Feet, 14, 2.2f, 0.45f);
            Burst(starPs, Chest, 10, 2f, 0.3f, 0.6f, Sparkle, 0f);
        }

        private void OnGoalOpened()
        {
            goalSparkleTime = 2.5f;
            if (goal != null) Burst(starPs, goal.position + Vector3.up * 2.4f, 20, 4f, 0.35f, 0.8f, Sparkle, 1f);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Won && player != null)
            {
                ConfettiBurst(Chest + Vector3.up * 0.5f, 70, 7f);
                winConfettiTime = 3f;
            }
        }

        private void OnCheckpoint(Vector3 flagPos)
        {
            ConfettiBurst(flagPos, 28, 5f);
            Burst(starPs, flagPos, 10, 2.5f, 0.3f, 0.6f, Sparkle, 0f);
        }

        private void OnCrateBroken(Vector3 cratePos)
        {
            Burst(chunkPs, cratePos, 18, 4.5f, 0.18f, 0.9f, Wood, 3f);
            for (int i = 0; i < 10; i++)
                Emit(softPs, cratePos + Random.insideUnitSphere * 0.4f, Random.insideUnitSphere * 1.5f, Random.Range(0.35f, 0.6f), 0.5f, Dust);
            Burst(chunkPs, cratePos, 6, 3f, 0.14f, 0.7f, Leaf, 2f);
        }

        // ---------- 연속 이펙트 ----------

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (motor != null && player != null)
            {
                Vector3 v = motor.Velocity;
                float hs = new Vector2(v.x, v.z).magnitude;
                PlayerMotorState s = motor.State;

                // 달리기·걷기 발밑 먼지
                if ((s == PlayerMotorState.Grounded || s == PlayerMotorState.Crouching) && hs > 1f)
                {
                    runAcc += dt * (hs > 4.5f ? 12f : 4f);
                    while (runAcc >= 1f)
                    {
                        runAcc -= 1f;
                        Vector3 back = new Vector3(-v.x, 0f, -v.z).normalized;
                        Emit(softPs, Feet + Random.insideUnitSphere * 0.1f, back * Random.Range(0.3f, 0.9f) + Vector3.up * Random.Range(0.3f, 0.8f),
                            Random.Range(0.15f, 0.28f), Random.Range(0.3f, 0.5f), Dust);
                    }
                }

                // 벽 슬라이드 긁힘 먼지
                if (s == PlayerMotorState.WallSlide)
                {
                    slideAcc += dt * 22f;
                    Vector3 n = motor.WallNormal;
                    while (slideAcc >= 1f)
                    {
                        slideAcc -= 1f;
                        Vector3 c = player.position + Vector3.up * Random.Range(0.3f, 0.8f) - n * 0.3f;
                        Emit(softPs, c, n * Random.Range(0.3f, 0.8f) + Vector3.up * Random.Range(0.5f, 1.2f), Random.Range(0.12f, 0.22f), 0.35f, Dust);
                    }
                }

                // 다이브 속도선 / 미끄럼 먼지
                if (s == PlayerMotorState.Dive)
                {
                    trailAcc += dt * 35f;
                    while (trailAcc >= 1f)
                    {
                        trailAcc -= 1f;
                        Vector3 side = Random.insideUnitCircle * 0.35f;
                        Vector3 pos = Chest + player.right * side.x + Vector3.up * side.y;
                        Emit(streakPs, pos, -v * 0.6f, 0.08f, 0.18f, new Color32(255, 255, 255, 170));
                    }
                }
                else if (s == PlayerMotorState.DiveSlide)
                {
                    trailAcc += dt * 25f;
                    while (trailAcc >= 1f)
                    {
                        trailAcc -= 1f;
                        Emit(softPs, Feet + Random.insideUnitSphere * 0.15f, Vector3.up * Random.Range(0.4f, 1f), Random.Range(0.18f, 0.3f), 0.4f, Dust);
                    }
                }
            }

            // 뚜껑 열림 후 믹서기 주변 반짝이 기둥
            if (goalSparkleTime > 0f && goal != null)
            {
                goalSparkleTime -= dt;
                goalAcc += dt * 30f;
                while (goalAcc >= 1f)
                {
                    goalAcc -= 1f;
                    Vector2 r = Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.1f);
                    Emit(starPs, goal.position + new Vector3(r.x, Random.Range(0f, 1f), r.y), Vector3.up * Random.Range(1.5f, 3f),
                        Random.Range(0.2f, 0.35f), Random.Range(0.8f, 1.3f), Sparkle);
                }
            }

            // 승리 후 계속 흩날리는 컨페티
            if (winConfettiTime > 0f && player != null)
            {
                winConfettiTime -= dt;
                confettiAcc += dt * 40f;
                while (confettiAcc >= 1f)
                {
                    confettiAcc -= 1f;
                    Vector3 p = player.position + new Vector3(Random.Range(-4f, 4f), Random.Range(3.5f, 5f), Random.Range(-4f, 4f));
                    Emit(confettiPs, p, new Vector3(0f, -0.5f, 0f), Random.Range(0.06f, 0.11f), 3f, Confetti[Random.Range(0, Confetti.Length)]);
                }
            }

            if (ambient && cam != null) UpdateAmbient(dt);
        }

        private void UpdateAmbient(float dt)
        {
            // 카메라 주변에 떠다니는 꽃잎·잎 (저밀도)
            petalAcc += dt * 3f;
            while (petalAcc >= 1f)
            {
                petalAcc -= 1f;
                Vector3 p = cam.position + cam.forward * Random.Range(4f, 18f) + new Vector3(Random.Range(-10f, 10f), Random.Range(-1f, 5f), Random.Range(-10f, 10f));
                Emit(petalPs, p, new Vector3(Random.Range(0.3f, 0.8f), -0.15f, Random.Range(-0.3f, 0.3f)),
                    Random.Range(0.1f, 0.16f), Random.Range(6f, 9f), Random.value < 0.6f ? Petal : Leaf);
            }

            // 주스 바다 위 반짝임
            glintAcc += dt * 14f;
            while (glintAcc >= 1f)
            {
                glintAcc -= 1f;
                Vector2 r = Random.insideUnitCircle * 45f;
                Vector3 p = new Vector3(cam.position.x + r.x, seaLevel + 0.05f, cam.position.z + r.y);
                Emit(glintPs, p, Vector3.zero, Random.Range(0.35f, 0.7f), Random.Range(0.6f, 1.1f), new Color32(255, 255, 230, 230));
            }
        }

        private void LateUpdate()
        {
            if (!cameraShake || shakeTime <= 0f || cam == null) return;
            shakeTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTime / shakeDuration);
            cam.position += Random.insideUnitSphere * (shakeAmp * shakeStrength * k);
        }

        private void Shake(float duration, float amplitude)
        {
            shakeDuration = duration;
            shakeTime = duration;
            shakeAmp = amplitude;
        }

        // ---------- 방출 도우미 ----------

        private void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color32 color)
        {
            ep.position = pos;
            ep.velocity = vel;
            ep.startSize = size * sizeScale;
            ep.startLifetime = life * lifeScale;
            ep.startColor = color;
            ep.rotation = Random.Range(0f, 360f);
            ps.Emit(ep, 1);
        }

        /// <summary>바닥에 깔리는 원형 먼지 퍼프.</summary>
        private void PuffRing(Vector3 center, int count, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value * 0.5f) / count * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Emit(softPs, center + dir * 0.15f, dir * speed * Random.Range(0.7f, 1.1f) + Vector3.up * Random.Range(0.2f, 0.6f),
                    size * Random.Range(0.8f, 1.2f), Random.Range(0.35f, 0.55f), Dust);
            }
        }

        /// <summary>구 방향 무작위 폭발(upBias만큼 위로 치우침).</summary>
        private void Burst(ParticleSystem ps, Vector3 center, int count, float speed, float size, float life, Color32 color, float upBias)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 v = Random.onUnitSphere * speed * Random.Range(0.5f, 1f) + Vector3.up * upBias;
                Emit(ps, center, v, size * Random.Range(0.7f, 1.3f), life * Random.Range(0.8f, 1.2f), color);
            }
        }

        private void ConfettiBurst(Vector3 center, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 v = Random.onUnitSphere * speed * Random.Range(0.3f, 1f);
                v.y = Mathf.Abs(v.y) + speed * 0.4f;
                Emit(confettiPs, center, v, Random.Range(0.06f, 0.11f), Random.Range(2f, 3f), Confetti[Random.Range(0, Confetti.Length)]);
            }
        }

        // ---------- 파티클 시스템 생성 ----------
        private ParticleSystem CreateSystem(string name, Tile tile, float gravity, ParticleSystemRenderMode mode, int maxParticles, AnimationCurve sizeCurve)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true; // 계속 재생 상태여야 Emit()한 입자가 시뮬레이션된다
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = maxParticles;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = false; // Emit()으로만 방출
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.numTilesX = 3;
            tsa.numTilesY = 2;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            tsa.startFrame = new ParticleSystem.MinMaxCurve(((float)tile + 0.5f) / 6f); // 시작 프레임은 0~1 정규화 값
            tsa.cycleCount = 1;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = gravity > 0.5f || tile == Tile.Soft;
            limit.dampen = tile == Tile.Soft ? 0.12f : 0.02f;
            limit.limit = 30f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode;
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 0.25f; // 카메라 가까이에서 화면을 덮지 않게
            r.sortingFudge = 0f;

            ps.Play();
            return ps;
        }

        private static void SetSpin(ParticleSystem ps, float degPerSec)
        {
            var rol = ps.rotationOverLifetime;
            rol.enabled = true;
            rol.z = new ParticleSystem.MinMaxCurve(-degPerSec * Mathf.Deg2Rad, degPerSec * Mathf.Deg2Rad);
        }

        private static void SetNoise(ParticleSystem ps, float strength)
        {
            var n = ps.noise;
            n.enabled = true;
            n.strength = strength;
            n.frequency = 0.6f;
            n.scrollSpeed = 0.3f;
        }

        private static AnimationCurve Grow(float from, float to) => AnimationCurve.EaseInOut(0f, from, 1f, to);
        private static AnimationCurve Shrink() => AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
        private static AnimationCurve Flat() => AnimationCurve.Constant(0f, 1f, 1f);

        private static AnimationCurve Pop()
        {
            return new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.2f, 1.2f), new Keyframe(1f, 0f));
        }
    }
}
