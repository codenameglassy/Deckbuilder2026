// EditMode tests for CombatHitBuffer: hits keep their order and results, an announced source tags only hits still
// untagged, the fallback covers only hits nobody tagged, draining empties the buffer, and HitSource defaults to None.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.Tests
{
    public sealed class CombatHitBufferTests
    {
        private readonly List<HitRecord> _output = new List<HitRecord>();
        private CardTypeSO _cardKey;
        private CardTypeSO _enemyKey;

        [SetUp]
        public void SetUp()
        {
            _output.Clear();
            _cardKey = ScriptableObject.CreateInstance<CardTypeSO>();
            _enemyKey = ScriptableObject.CreateInstance<CardTypeSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cardKey);
            Object.DestroyImmediate(_enemyKey);
        }

        [Test]
        public void Default_HitSource_Is_None()
        {
            HitSource source = default;

            Assert.IsNull(source.Key);
            Assert.AreEqual(HitSource.NoSlot, source.AttackerSlot);
            Assert.IsFalse(source.HasAttacker);
        }

        [Test]
        public void HitSource_Keeps_Player_And_Enemy_Slots()
        {
            Assert.AreEqual(HitSource.PlayerSlot, new HitSource(_cardKey, HitSource.PlayerSlot).AttackerSlot);
            Assert.AreEqual(3, new HitSource(_enemyKey, 3).AttackerSlot);
        }

        [Test]
        public void Drain_Keeps_Order_And_Results()
        {
            var buffer = new CombatHitBuffer();
            buffer.Record(0, new DamageResult(6, 0, 6, false));
            buffer.Record(1, new DamageResult(9, 2, 7, true));

            buffer.Drain(_output);

            Assert.AreEqual(2, _output.Count);
            Assert.AreEqual(0, _output[0].TargetSlot);
            Assert.AreEqual(6, _output[0].Result.HealthLost);
            Assert.AreEqual(1, _output[1].TargetSlot);
            Assert.AreEqual(2, _output[1].Result.Blocked);
            Assert.IsTrue(_output[1].Result.Killed);
        }

        [Test]
        public void Untagged_Hit_Without_Fallback_Has_No_Source()
        {
            var buffer = new CombatHitBuffer();
            buffer.Record(0, new DamageResult(4, 0, 4, false));

            buffer.Drain(_output);

            Assert.IsNull(_output[0].Source.Key);
            Assert.IsFalse(_output[0].Source.HasAttacker);
        }

        [Test]
        public void TagUntagged_Tags_Only_Hits_Not_Yet_Tagged()
        {
            var buffer = new CombatHitBuffer();
            buffer.Record(0, new DamageResult(4, 0, 4, false));
            buffer.TagUntagged(new HitSource(_cardKey, HitSource.PlayerSlot));
            buffer.Record(1, new DamageResult(5, 0, 5, false));
            buffer.TagUntagged(new HitSource(_enemyKey, HitSource.NoSlot));

            buffer.Drain(_output);

            Assert.AreSame(_cardKey, _output[0].Source.Key);
            Assert.AreEqual(HitSource.PlayerSlot, _output[0].Source.AttackerSlot);
            Assert.AreSame(_enemyKey, _output[1].Source.Key);
            Assert.IsFalse(_output[1].Source.HasAttacker);
        }

        [Test]
        public void Fallback_Covers_Only_Untagged_Hits()
        {
            var buffer = new CombatHitBuffer();
            buffer.SetFallback(new HitSource(_enemyKey, 2));
            buffer.Record(HitSource.PlayerSlot, new DamageResult(7, 0, 7, false));
            buffer.Record(0, new DamageResult(3, 0, 3, false));
            buffer.TagUntagged(new HitSource(_cardKey, HitSource.NoSlot));
            buffer.Record(HitSource.PlayerSlot, new DamageResult(2, 0, 2, false));

            buffer.Drain(_output);

            Assert.AreSame(_cardKey, _output[0].Source.Key);
            Assert.AreSame(_cardKey, _output[1].Source.Key);
            Assert.AreSame(_enemyKey, _output[2].Source.Key);
            Assert.AreEqual(2, _output[2].Source.AttackerSlot);
        }

        [Test]
        public void Fallback_Persists_Across_Drains_Until_Cleared()
        {
            var buffer = new CombatHitBuffer();
            buffer.SetFallback(new HitSource(_enemyKey, 0));
            buffer.Drain(_output);
            buffer.Record(HitSource.PlayerSlot, new DamageResult(5, 0, 5, false));
            buffer.Drain(_output);

            Assert.AreSame(_enemyKey, _output[0].Source.Key);

            buffer.ClearFallback();
            buffer.Record(HitSource.PlayerSlot, new DamageResult(5, 0, 5, false));
            buffer.Drain(_output);

            Assert.IsFalse(buffer.HasFallback);
            Assert.IsNull(_output[1].Source.Key);
        }

        [Test]
        public void Drain_Empties_The_Buffer()
        {
            var buffer = new CombatHitBuffer();
            buffer.Record(0, new DamageResult(4, 0, 4, false));

            buffer.Drain(_output);
            buffer.Drain(_output);

            Assert.AreEqual(0, buffer.Count);
            Assert.AreEqual(1, _output.Count);
        }

        [Test]
        public void Clear_Drops_Hits_And_Fallback()
        {
            var buffer = new CombatHitBuffer();
            buffer.SetFallback(new HitSource(_enemyKey, 1));
            buffer.Record(0, new DamageResult(4, 0, 4, false));

            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);
            Assert.IsFalse(buffer.HasFallback);
        }
    }
}
