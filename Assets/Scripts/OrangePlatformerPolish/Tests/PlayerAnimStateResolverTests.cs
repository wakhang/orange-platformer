using NUnit.Framework;
using OrangePlatformer.Core;
using OrangePlatformerPolish.Core;

namespace OrangePlatformerPolish.Tests
{
    public class PlayerAnimStateResolverTests
    {
        private static PlayerAnimState R(PlayerMotorState m, float hs = 0f, float vy = 0f, bool won = false)
        {
            return PlayerAnimStateResolver.Resolve(m, hs, vy, won, 0.1f, 4.5f, 0.5f);
        }

        [Test]
        public void Won_OverridesEverything()
        {
            Assert.AreEqual(PlayerAnimState.Victory, R(PlayerMotorState.Dive, 8f, 5f, true));
        }

        [TestCase(PlayerMotorState.GroundPoundHang)]
        [TestCase(PlayerMotorState.GroundPoundFall)]
        public void GroundPound(PlayerMotorState s)
        {
            Assert.AreEqual(PlayerAnimState.GroundPound, R(s, 5f, 5f));
        }

        [TestCase(PlayerMotorState.Dive)]
        [TestCase(PlayerMotorState.DiveSlide)]
        public void Dive(PlayerMotorState s)
        {
            Assert.AreEqual(PlayerAnimState.Dive, R(s, 5f));
        }

        [Test]
        public void WallSlide()
        {
            Assert.AreEqual(PlayerAnimState.WallSlide, R(PlayerMotorState.WallSlide, 0f, 5f));
        }

        [Test]
        public void Airborne_RiseBoundary()
        {
            Assert.AreEqual(PlayerAnimState.Jump, R(PlayerMotorState.Airborne, 0f, 0.51f));
            Assert.AreEqual(PlayerAnimState.Fall, R(PlayerMotorState.Airborne, 0f, 0.5f));
            Assert.AreEqual(PlayerAnimState.Fall, R(PlayerMotorState.Airborne, 0f, -3f));
        }

        [TestCase(PlayerMotorState.Grounded)]
        [TestCase(PlayerMotorState.Crouching)]
        [TestCase(PlayerMotorState.GroundPoundLand)]
        public void Ground_Thresholds(PlayerMotorState s)
        {
            Assert.AreEqual(PlayerAnimState.Idle, R(s, 0.099f));
            Assert.AreEqual(PlayerAnimState.Walk, R(s, 0.1f));
            Assert.AreEqual(PlayerAnimState.Walk, R(s, 4.499f));
            Assert.AreEqual(PlayerAnimState.Run, R(s, 4.5f));
        }
    }
}
