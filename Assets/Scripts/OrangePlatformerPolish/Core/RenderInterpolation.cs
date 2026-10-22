using UnityEngine;

namespace OrangePlatformerPolish.Core
{
    public static class RenderInterpolation
    {
        public static float Alpha(float time, float fixedTime, float fixedDeltaTime)
        {
            if (fixedDeltaTime <= 0f) return 1f;
            return Mathf.Clamp01((time - fixedTime) / fixedDeltaTime);
        }

        public static bool IsTeleport(Vector3 prevTick, Vector3 currTick, float snapDistance)
        {
            return (currTick - prevTick).sqrMagnitude > snapDistance * snapDistance;
        }
    }
}
