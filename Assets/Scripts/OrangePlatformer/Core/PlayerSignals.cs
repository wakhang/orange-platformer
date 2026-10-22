using System;
using UnityEngine;

namespace OrangePlatformer.Core
{
    public enum PlayerMotorState { Grounded, Crouching, Airborne, WallSlide, Dive, DiveSlide, GroundPoundHang, GroundPoundFall, GroundPoundLand }
    public enum JumpKind { Single, Double, High, Wall }

    public interface IPlayerMotorView
    {
        PlayerMotorState State { get; }
        Vector3 Velocity { get; }
        bool IsGrounded { get; }
        Vector3 WallNormal { get; }
        event Action<JumpKind> Jumped;
        event Action<float> Landed; // impact speed
        event Action DiveStarted;
        event Action GroundPoundStarted;
        event Action GroundPoundImpact;
    }
}
