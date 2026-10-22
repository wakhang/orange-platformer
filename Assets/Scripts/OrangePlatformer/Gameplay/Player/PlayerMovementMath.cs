using UnityEngine;

namespace OrangePlatformer.Gameplay
{
    /// <summary>플레이어 이동에 쓰이는 순수 수학 함수 모음 (씬 오브젝트 의존 없음).</summary>
    public static class PlayerMovementMath
    {
        /// <summary>점프 높이 h를 만드는 초기 수직 속도: v = sqrt(2*g*h).</summary>
        public static float JumpSpeed(float gravity, float height)
        {
            return Mathf.Sqrt(2f * Mathf.Max(0f, gravity) * Mathf.Max(0f, height));
        }

        /// <summary>
        /// 입력(x=오른쪽, y=앞)을 카메라 yaw(도) 기준 월드 XZ 방향으로 변환. 크기는 최대 1로 제한.
        /// </summary>
        public static Vector3 CameraRelative(Vector2 input, float yawDeg)
        {
            float rad = yawDeg * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            // 카메라 forward = (sin, 0, cos), right = (cos, 0, -sin)
            Vector3 dir = new Vector3(
                input.x * cos + input.y * sin,
                0f,
                -input.x * sin + input.y * cos);
            return Vector3.ClampMagnitude(dir, 1f);
        }

        public static float MoveToward(float current, float target, float maxDelta)
        {
            return Mathf.MoveTowards(current, target, maxDelta);
        }

        /// <summary>XZ 평면 성분만 target 쪽으로 이동시킨다 (Y는 그대로 유지).</summary>
        public static Vector3 MoveTowardHorizontal(Vector3 current, Vector3 target, float maxDelta)
        {
            Vector3 cur = new Vector3(current.x, 0f, current.z);
            Vector3 tgt = new Vector3(target.x, 0f, target.z);
            Vector3 next = Vector3.MoveTowards(cur, tgt, maxDelta);
            return new Vector3(next.x, current.y, next.z);
        }

        /// <summary>XZ 평면 속도 크기를 max로 제한한다 (Y는 그대로 유지).</summary>
        public static Vector3 ClampHorizontal(Vector3 v, float max)
        {
            float sqr = v.x * v.x + v.z * v.z;
            if (sqr <= max * max) return v;
            float scale = max / Mathf.Sqrt(sqr);
            return new Vector3(v.x * scale, v.y, v.z * scale);
        }
    }

    /// <summary>
    /// 코요테 타임 / 점프 버퍼 타이머.
    /// 코요테: 점프 없이 지면을 벗어난 뒤 coyoteTime 동안만 지상 점프 허용.
    /// 버퍼: 점프 입력을 bufferTime 동안 기억했다가 착지 시 발동.
    /// </summary>
    public struct JumpTimers
    {
        private readonly float coyoteTime;
        private readonly float bufferTime;
        private float sinceLeftGround;
        private bool coyoteArmed;
        private bool jumpedThisAirtime;
        private float bufferRemaining;

        public JumpTimers(float coyoteTime, float bufferTime)
        {
            this.coyoteTime = coyoteTime;
            this.bufferTime = bufferTime;
            sinceLeftGround = 0f;
            coyoteArmed = false;
            jumpedThisAirtime = false;
            bufferRemaining = 0f;
        }

        public void Tick(float dt)
        {
            if (coyoteArmed) sinceLeftGround += dt;
            if (bufferRemaining > 0f) bufferRemaining -= dt;
        }

        /// <summary>걸어서 지면을 벗어났을 때 호출. 이미 점프한 공중 시간이면 코요테를 주지 않는다.</summary>
        public void NotifyLeftGround()
        {
            if (jumpedThisAirtime) return;
            coyoteArmed = true;
            sinceLeftGround = 0f;
        }

        /// <summary>점프(어떤 종류든)로 지면을 떠났을 때 호출: 코요테 해제.</summary>
        public void NotifyJumped()
        {
            jumpedThisAirtime = true;
            coyoteArmed = false;
        }

        public void NotifyGrounded()
        {
            jumpedThisAirtime = false;
            coyoteArmed = false;
        }

        public void PressJump()
        {
            bufferRemaining = bufferTime;
        }

        public bool CanCoyoteJump
        {
            get { return coyoteArmed && sinceLeftGround <= coyoteTime; }
        }

        public bool HasBufferedJump
        {
            get { return bufferRemaining > 0f; }
        }

        /// <summary>버퍼된 입력이 있으면 소비하고 true.</summary>
        public bool ConsumeBuffered()
        {
            bool had = bufferRemaining > 0f;
            bufferRemaining = 0f;
            return had;
        }

        public void Reset()
        {
            sinceLeftGround = 0f;
            coyoteArmed = false;
            jumpedThisAirtime = false;
            bufferRemaining = 0f;
        }
    }
}
