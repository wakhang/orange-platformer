using UnityEngine;

namespace OrangePlatformer.Core
{
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        Vector2 LookDelta { get; } // deg this frame, sensitivity applied
        bool JumpPressed { get; }
        bool JumpHeld { get; }
        bool CrouchPressed { get; }
        bool CrouchHeld { get; }
        bool DivePressed { get; }
        bool PausePressed { get; }
        bool ConfirmPressed { get; }
    }
}
