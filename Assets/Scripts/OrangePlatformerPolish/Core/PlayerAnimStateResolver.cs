using OrangePlatformer.Core;

namespace OrangePlatformerPolish.Core
{
    public static class PlayerAnimStateResolver
    {
        public static PlayerAnimState Resolve(PlayerMotorState motor, float horizontalSpeed, float verticalSpeed, bool won,
            float idleSpeedThreshold, float runSpeedThreshold, float jumpRiseMinVy)
        {
            if (won) return PlayerAnimState.Victory;
            switch (motor)
            {
                case PlayerMotorState.GroundPoundHang:
                case PlayerMotorState.GroundPoundFall:
                    return PlayerAnimState.GroundPound;
                case PlayerMotorState.Dive:
                case PlayerMotorState.DiveSlide:
                    return PlayerAnimState.Dive;
                case PlayerMotorState.WallSlide:
                    return PlayerAnimState.WallSlide;
                case PlayerMotorState.Airborne:
                    return verticalSpeed > jumpRiseMinVy ? PlayerAnimState.Jump : PlayerAnimState.Fall;
                default:
                    if (horizontalSpeed < idleSpeedThreshold) return PlayerAnimState.Idle;
                    return horizontalSpeed < runSpeedThreshold ? PlayerAnimState.Walk : PlayerAnimState.Run;
            }
        }
    }
}
