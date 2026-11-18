// EditMode tests for KavachVfxProfileSO's pure mappings: no Kavach shows no shell, 1 Kavach shows Min Strength, Full
// Block and above show full strength, strength never falls as Kavach rises, and absorb ripples are never weaker than the
// minimum nor stronger than 1.
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.Tests
{
    public sealed class KavachVfxProfileTests
    {
        private const float MinStrength = 0.4f;
        private const int FullBlock = 20;

        private KavachVfxProfileSO _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<KavachVfxProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
        }

        [Test]
        public void No_Kavach_Shows_No_Shell()
        {
            Assert.AreEqual(0f, _profile.StrengthFor(0));
            Assert.AreEqual(0f, _profile.StrengthFor(-3));
        }

        [Test]
        public void One_Kavach_Shows_Min_Strength_And_Full_Block_Shows_Full()
        {
            Assert.AreEqual(MinStrength, _profile.StrengthFor(1), 1e-5f);
            Assert.AreEqual(1f, _profile.StrengthFor(FullBlock), 1e-5f);
            Assert.AreEqual(1f, _profile.StrengthFor(FullBlock * 3), 1e-5f);
        }

        [Test]
        public void Strength_Never_Falls_As_Kavach_Rises()
        {
            float previous = 0f;
            for (int block = 0; block <= FullBlock + 5; block++)
            {
                float strength = _profile.StrengthFor(block);
                Assert.GreaterOrEqual(strength, previous);
                previous = strength;
            }
        }

        [Test]
        public void Ripple_Strength_Is_Clamped()
        {
            Assert.AreEqual(0f, _profile.RippleStrengthFor(0));
            Assert.GreaterOrEqual(_profile.RippleStrengthFor(1), 0.4f);
            Assert.AreEqual(1f, _profile.RippleStrengthFor(500), 1e-5f);
        }
    }
}
