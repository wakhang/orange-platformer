using OrangePlatformer.Core;
using OrangePlatformerPolish.Core;
using UnityEngine;

namespace OrangePlatformerPolish.Gameplay
{
    /// <summary>모터 상태로 Animator 상태를 결정해 CrossFadeInFixedTime으로 전환한다. 컨트롤러에는 전환/파라미터가 없다.</summary>
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private static readonly int[] StateHashes = BuildHashes();

        [SerializeField] private OrangePlatformerPolishConfig config;
        [SerializeField] private Animator animator;

        private IPlayerMotorView view;
        private bool started;

        public PlayerAnimState Current { get; private set; }

        private static int[] BuildHashes()
        {
            string[] names = System.Enum.GetNames(typeof(PlayerAnimState));
            int[] h = new int[names.Length];
            for (int i = 0; i < names.Length; i++) h[i] = Animator.StringToHash(names[i]);
            return h;
        }

        private void Awake()
        {
            view = GetComponentInParent<IPlayerMotorView>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (view != null) view.Jumped += OnJumped;
            started = false;
        }

        private void OnDisable()
        {
            if (view != null) view.Jumped -= OnJumped;
        }

        private void Update()
        {
            if (view == null || animator == null || config == null) return;

            IGameSession session = GameSessionLocator.Current;
            bool won = session != null && session.State == GameState.Won;
            Vector3 v = view.Velocity;
            float hs = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            PlayerAnimState next = PlayerAnimStateResolver.Resolve(view.State, hs, v.y, won,
                config.idleSpeedThreshold, config.runSpeedThreshold, config.jumpRiseMinVy);

            if (!started)
            {
                started = true;
                Current = next;
                animator.Play(StateHashes[(int)next], 0, 0f);
                return;
            }
            if (next != Current)
            {
                Current = next;
                float fade = next == PlayerAnimState.Victory ? config.victoryCrossFade : config.animCrossFade;
                animator.CrossFadeInFixedTime(StateHashes[(int)next], fade, 0);
            }
        }

        private void OnJumped(JumpKind kind)
        {
            if (animator == null || config == null) return;
            // 점프 직후 상태는 Jump로 고정하고 0부터 재시작한다. 다음 Update에서 Resolve가 재평가한다.
            Current = PlayerAnimState.Jump;
            started = true;
            animator.CrossFadeInFixedTime(StateHashes[(int)PlayerAnimState.Jump], config.animCrossFade, 0, 0f);
        }
    }
}
