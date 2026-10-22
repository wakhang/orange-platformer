using OrangePlatformer.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrangePlatformer.Gameplay
{
    /// <summary>코드로 InputAction을 구성하는 얇은 입력 어댑터 (IPlayerInput 구현).</summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        private const float FallbackMouseSensitivity = 0.12f;
        private const float FallbackStickSensitivity = 180f;

        [SerializeField] private OrangePlatformerConfig config;

        private InputAction move;
        private InputAction lookMouse;
        private InputAction lookStick;
        private InputAction jump;
        private InputAction crouch;
        private InputAction dive;
        private InputAction pause;
        private InputAction confirm;

        public Vector2 Move => move.ReadValue<Vector2>();

        // 마우스: 프레임당 픽셀 delta * 감도(deg/px), 스틱: 값 * 감도(deg/s) * dt
        public Vector2 LookDelta
        {
            get
            {
                float mouseSens = config != null ? config.mouseSensitivity : FallbackMouseSensitivity;
                float stickSens = config != null ? config.stickSensitivity : FallbackStickSensitivity;
                return lookMouse.ReadValue<Vector2>() * mouseSens
                     + lookStick.ReadValue<Vector2>() * (stickSens * Time.deltaTime);
            }
        }

        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool JumpHeld => jump.IsPressed();
        public bool CrouchPressed => crouch.WasPressedThisFrame();
        public bool CrouchHeld => crouch.IsPressed();
        public bool DivePressed => dive.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool ConfirmPressed => confirm.WasPressedThisFrame();

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            lookMouse = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            lookStick = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            jump = new InputAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            crouch = new InputAction("Crouch", InputActionType.Button);
            crouch.AddBinding("<Keyboard>/leftShift");
            crouch.AddBinding("<Keyboard>/c");
            crouch.AddBinding("<Gamepad>/buttonEast");
            crouch.AddBinding("<Gamepad>/leftTrigger");

            dive = new InputAction("Dive", InputActionType.Button);
            dive.AddBinding("<Keyboard>/e");
            dive.AddBinding("<Mouse>/leftButton");
            dive.AddBinding("<Gamepad>/buttonWest");

            pause = new InputAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            confirm = new InputAction("Confirm", InputActionType.Button);
            confirm.AddBinding("<Keyboard>/enter");
            confirm.AddBinding("<Keyboard>/numpadEnter");
            confirm.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            move.Enable();
            lookMouse.Enable();
            lookStick.Enable();
            jump.Enable();
            crouch.Enable();
            dive.Enable();
            pause.Enable();
            confirm.Enable();
        }

        private void OnDisable()
        {
            move.Disable();
            lookMouse.Disable();
            lookStick.Disable();
            jump.Disable();
            crouch.Disable();
            dive.Disable();
            pause.Disable();
            confirm.Disable();
        }

        private void OnDestroy()
        {
            move?.Dispose();
            lookMouse?.Dispose();
            lookStick?.Dispose();
            jump?.Dispose();
            crouch?.Dispose();
            dive?.Dispose();
            pause?.Dispose();
            confirm?.Dispose();
        }
    }
}
