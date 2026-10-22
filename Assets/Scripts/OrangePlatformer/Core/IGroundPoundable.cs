using UnityEngine;

namespace OrangePlatformer.Core
{
    public interface IGroundPoundable
    {
        void OnGroundPound(Vector3 impactPoint);
    }
}
