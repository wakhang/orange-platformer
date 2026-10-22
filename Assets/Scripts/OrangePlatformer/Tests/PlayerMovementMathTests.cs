using NUnit.Framework;
using OrangePlatformer.Gameplay;
using UnityEngine;

namespace OrangePlatformer.Tests
{
    public class PlayerMovementMathTests
    {
        private const float G = 30f;

        [Test]
        public void JumpSpeed_MatchesDesignValues()
        {
            Assert.AreEqual(9.80f, PlayerMovementMath.JumpSpeed(G, 1.6f), 0.01f);
            Assert.AreEqual(8.83f, PlayerMovementMath.JumpSpeed(G, 1.3f), 0.01f);
            Assert.AreEqual(15.49f, PlayerMovementMath.JumpSpeed(G, 4.0f), 0.01f);
        }

        [Test]
        public void CameraRelative_YawZero_ForwardIsPlusZ()
        {
            Vector3 v = PlayerMovementMath.CameraRelative(new Vector2(0f, 1f), 0f);
            Assert.AreEqual(0f, v.x, 1e-4f);
            Assert.AreEqual(1f, v.z, 1e-4f);
            Vector3 r = PlayerMovementMath.CameraRelative(new Vector2(1f, 0f), 0f);
            Assert.AreEqual(1f, r.x, 1e-4f);
            Assert.AreEqual(0f, r.z, 1e-4f);
        }

        [Test]
        public void CameraRelative_Yaw90_ForwardIsPlusX()
        {
            Vector3 v = PlayerMovementMath.CameraRelative(new Vector2(0f, 1f), 90f);
            Assert.AreEqual(1f, v.x, 1e-4f);
            Assert.AreEqual(0f, v.z, 1e-4f);
            Vector3 r = PlayerMovementMath.CameraRelative(new Vector2(1f, 0f), 90f);
            Assert.AreEqual(0f, r.x, 1e-4f);
            Assert.AreEqual(-1f, r.z, 1e-4f);
        }

        [Test]
        public void CameraRelative_ClampsMagnitude()
        {
            Vector3 v = PlayerMovementMath.CameraRelative(new Vector2(1f, 1f), 0f);
            Assert.AreEqual(1f, v.magnitude, 1e-4f);
        }

        [Test]
        public void Coyote_AllowsAt008_NotAt03()
        {
            var t = new JumpTimers(0.12f, 0.15f);
            t.NotifyGrounded();
            t.NotifyLeftGround();
            t.Tick(0.08f);
            Assert.IsTrue(t.CanCoyoteJump);
            t.Tick(0.22f);
            Assert.IsFalse(t.CanCoyoteJump);
        }

        [Test]
        public void Coyote_NotGrantedAfterJump()
        {
            var t = new JumpTimers(0.12f, 0.15f);
            t.NotifyGrounded();
            t.NotifyJumped();
            t.NotifyLeftGround();
            t.Tick(0.01f);
            Assert.IsFalse(t.CanCoyoteJump);
        }

        [Test]
        public void Coyote_RearmedAfterLanding()
        {
            var t = new JumpTimers(0.12f, 0.15f);
            t.NotifyJumped();
            t.NotifyGrounded();
            t.NotifyLeftGround();
            t.Tick(0.05f);
            Assert.IsTrue(t.CanCoyoteJump);
        }

        [Test]
        public void Buffer_HoldsAt01_ExpiresAt02()
        {
            var t = new JumpTimers(0.12f, 0.15f);
            t.PressJump();
            t.Tick(0.1f);
            Assert.IsTrue(t.HasBufferedJump);
            t.Tick(0.1f);
            Assert.IsFalse(t.HasBufferedJump);
            Assert.IsFalse(t.ConsumeBuffered());
        }

        [Test]
        public void Buffer_ConsumeClears()
        {
            var t = new JumpTimers(0.12f, 0.15f);
            t.PressJump();
            Assert.IsTrue(t.ConsumeBuffered());
            Assert.IsFalse(t.ConsumeBuffered());
        }
    }
}
