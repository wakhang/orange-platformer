using UnityEngine;

namespace OrangePlatformerPolish.Core
{
    [CreateAssetMenu(menuName = "OrangePlatformer/Polish Config", fileName = "OrangePlatformerPolishConfig")]
    public sealed class OrangePlatformerPolishConfig : ScriptableObject
    {
        public bool interpolationEnabled = true;
        public float teleportSnapDistance = 3f;
        public float idleSpeedThreshold = 0.1f;
        public float runSpeedThreshold = 4.5f;
        public float jumpRiseMinVy = 0.5f;
        public float animCrossFade = 0.1f;
        public float victoryCrossFade = 0.25f;
    }
}
