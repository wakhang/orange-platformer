using UnityEngine;

namespace OrangePlatformer.Core
{
    public interface IRespawnable
    {
        bool ControlsEnabled { get; set; }
        void Respawn(Vector3 position, Quaternion rotation);
    }
}
