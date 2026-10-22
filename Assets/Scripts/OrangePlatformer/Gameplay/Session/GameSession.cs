using System;
using System.Collections;
using OrangePlatformer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OrangePlatformer.Gameplay
{
    /// <summary>GameRulesModel을 감싸는 얇은 어댑터. 시간/커서/플레이어 제어/씬 재시작을 담당.</summary>
    public class GameSession : MonoBehaviour, IGameSession
    {
        [SerializeField] private OrangePlatformerConfig config;

        private GameRulesModel model;
        private IRespawnable player;
        private Vector3 checkpointPosition;
        private Quaternion checkpointRotation = Quaternion.identity;
        private bool respawnPending;
        private WaitForSeconds respawnWait;

        public GameState State => model.State;
        public int OrangesCollected => model.Oranges;
        public int OrangesRequired => model.Required;
        public int Hearts => model.Hearts;
        public int MaxHearts => model.MaxHearts;
        public float ElapsedSeconds => model.Elapsed;
        public bool GoalOpen => model.GoalOpen;

        public event Action<GameState> StateChanged;
        public event Action<int, int> OrangesChanged;
        public event Action<int, int> HeartsChanged;
        public event Action GoalOpened;
        public event Action PlayerRespawned;
        public event Action<RunResult> RunEnded;

        private void Awake()
        {
            model = new GameRulesModel(config.orangesRequired, config.maxHearts);
            respawnWait = new WaitForSeconds(config.respawnDelay);
            Time.timeScale = 1f;
            GameSessionLocator.Register(this);
        }

        private void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.GetComponentInParent<IRespawnable>();
                if (player == null) player = playerObject.GetComponent<IRespawnable>();
                // 플레이어 시작 위치/회전을 최초 체크포인트로 기록
                Transform t = playerObject.transform;
                checkpointPosition = t.position;
                checkpointRotation = t.rotation;
            }
            ApplyState(model.State);
        }

        private void OnDestroy()
        {
            GameSessionLocator.Unregister(this);
        }

        private void Update()
        {
            model.Tick(Time.deltaTime);
            HandleInput();
        }

        private void HandleInput()
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            bool pausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame)
                                || (pad != null && pad.startButton.wasPressedThisFrame);
            bool confirmPressed = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                                  || (pad != null && pad.buttonSouth.wasPressedThisFrame);

            if (pausePressed)
            {
                if (model.State == GameState.Playing) Pause();
                else if (model.State == GameState.Paused) Resume();
            }
            else if (confirmPressed)
            {
                switch (model.State)
                {
                    case GameState.Intro: StartGame(); break;
                    case GameState.Won:
                    case GameState.GameOver: Restart(); break;
                }
            }
        }

        public void StartGame()
        {
            GameState prev = model.State;
            if (model.StartGame()) OnStateChanged(prev);
        }

        public void Pause()
        {
            GameState prev = model.State;
            if (model.Pause()) OnStateChanged(prev);
        }

        public void Resume()
        {
            GameState prev = model.State;
            if (model.Resume()) OnStateChanged(prev);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void CollectOrange()
        {
            if (model.State != GameState.Playing) return;
            int before = model.Oranges;
            bool opened = model.CollectOrange();
            if (model.Oranges != before) OrangesChanged?.Invoke(model.Oranges, model.Required);
            if (opened) GoalOpened?.Invoke();
        }

        public void PlayerFell()
        {
            // 리스폰 대기 중에는 KillZone이 반복 호출해도 무시
            if (respawnPending) return;

            GameRulesModel.Outcome outcome = model.PlayerFell();
            if (outcome == GameRulesModel.Outcome.Ignored) return;

            HeartsChanged?.Invoke(model.Hearts, model.MaxHearts);

            if (outcome == GameRulesModel.Outcome.GameOver)
            {
                // 모델이 이미 GameOver로 전이했으므로 상태 적용 후 결과 발행
                OnStateChanged(GameState.Playing);
            }
            else
            {
                respawnPending = true;
                if (player != null) player.ControlsEnabled = false;
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return respawnWait;
            respawnPending = false;
            if (player != null) player.Respawn(checkpointPosition, checkpointRotation);
            // 대기 중 일시정지/종료 상태가 되었다면 제어는 상태 적용 쪽에서 관리
            if (model.State == GameState.Playing && player != null) player.ControlsEnabled = true;
            PlayerRespawned?.Invoke();
        }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            checkpointPosition = position;
            checkpointRotation = rotation;
        }

        public void ReachGoal()
        {
            GameState prev = model.State;
            if (model.ReachGoal()) OnStateChanged(prev);
        }

        private void OnStateChanged(GameState previous)
        {
            GameState state = model.State;
            ApplyState(state);
            StateChanged?.Invoke(state);

            if (state == GameState.Won || state == GameState.GameOver)
            {
                RunEnded?.Invoke(new RunResult(state == GameState.Won, model.Elapsed,
                    model.Oranges, model.Required, model.Hearts));
            }
        }

        private void ApplyState(GameState state)
        {
            bool playing = state == GameState.Playing;
            // Paused만 timeScale 0. 나머지는 1 유지(리스폰/애니메이션 정상 동작)
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            if (player != null) player.ControlsEnabled = playing && !respawnPending;
        }
    }
}
