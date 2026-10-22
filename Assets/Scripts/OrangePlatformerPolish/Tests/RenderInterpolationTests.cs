using NUnit.Framework;
using OrangePlatformerPolish.Core;
using UnityEngine;

namespace OrangePlatformerPolish.Tests
{
    public class RenderInterpolationTests
    {
        [Test]
        public void Alpha_ClampedToUnitRange()
        {
            Assert.AreEqual(0f, RenderInterpolation.Alpha(1f, 1f, 0.02f), 1e-6f);
            Assert.AreEqual(0.5f, RenderInterpolation.Alpha(1.01f, 1f, 0.02f), 1e-4f);
            Assert.AreEqual(1f, RenderInterpolation.Alpha(1.5f, 1f, 0.02f), 1e-6f);
            Assert.AreEqual(0f, RenderInterpolation.Alpha(0.9f, 1f, 0.02f), 1e-6f);
        }

        [Test]
        public void IsTeleport_AboveSnapDistanceOnly()
        {
            Vector3 a = Vector3.zero;
            Assert.IsFalse(RenderInterpolation.IsTeleport(a, new Vector3(0f, 0f, 3f), 3f));
            Assert.IsTrue(RenderInterpolation.IsTeleport(a, new Vector3(0f, 0f, 3.01f), 3f));
            Assert.IsFalse(RenderInterpolation.IsTeleport(a, new Vector3(0f, 0f, 0.12f), 3f));
        }
    }
}
