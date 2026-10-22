using UnityEngine;

namespace OrangePlatformer.Core
{
    [CreateAssetMenu(menuName = "OrangePlatformer/Config")]
    public class OrangePlatformerConfig : ScriptableObject
    {
        public float moveSpeed = 6f;
        public float crouchMoveSpeed = 2.5f;
        public float groundAcceleration = 40f;
        public float groundDeceleration = 50f;
        public float airAcceleration = 20f;
        public float turnSpeedDeg = 720f;
        public float gravity = 30f;
        public float maxFallSpeed = 25f;
        public float jumpHeight = 1.6f;
        public float doubleJumpHeight = 1.3f;
        public float highJumpHeight = 4f;
        public float highJumpForwardSpeed = 2f;
        public float jumpCutMultiplier = 0.5f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.15f;
        public float diveSpeed = 12f;
        public float diveUpwardSpeed = 4f;
        public float diveLandSlideTime = 0.35f;
        public float wallSlideMaxFallSpeed = 3f;
        public float wallJumpVerticalSpeed = 10f;
        public float wallJumpHorizontalSpeed = 7f;
        public float wallJumpInputLockTime = 0.2f;
        public float wallContactGraceTime = 0.12f;
        public float wallNormalMaxY = 0.3f;
        public float groundPoundHangTime = 0.25f;
        public float groundPoundSpeed = 25f;
        public float groundPoundLandStunTime = 0.2f;
        public float controllerHeight = 1f;
        public float crouchHeight = 0.6f;
        public float controllerRadius = 0.3f;
        public float cameraDistance = 6f;
        public float cameraMinDistance = 1f;
        public float cameraPitchMin = -20f;
        public float cameraPitchMax = 65f;
        public float cameraDefaultPitch = 20f;
        public float mouseSensitivity = 0.12f;
        public float stickSensitivity = 180f;
        public float cameraCollisionRadius = 0.25f;
        public float cameraFollowSharpness = 12f;
        public float cameraTargetHeight = 1f;
        public float respawnDelay = 0.6f;
        public float lidOpenDuration = 1f;
        public float lidOpenHeight = 0.8f;
        public float lidOpenAngle = 110f;
        public float pickupRadius = 0.6f;
        public float pickupSpinDegPerSec = 90f;
        public float pickupBobAmplitude = 0.15f;
        public float jumpStretch = 1.25f;
        public float landSquash = 0.7f;
        public float crouchScaleY = 0.7f;
        public float squashRecoverSharpness = 12f;
        public float flipDuration = 0.35f;
        public float diveTiltDeg = 80f;
        public int orangesRequired = 8;
        public int maxHearts = 3;
    }
}
