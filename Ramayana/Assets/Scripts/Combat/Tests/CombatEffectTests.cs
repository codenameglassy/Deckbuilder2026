// EditMode tests for the effect strategies and targeting rules: hits, early stop on a fallen target,
// all-opponent targeting, a missing rule, and Kavach on the actor.
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JourneyToLanka.Combat.Effects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatEffectTests
    {
        private readonly List<Object> _created = new List<Object>();
        private FakeCombatActions _actions;
        private FakeCombatant _hero;

        [SetUp]
        public void SetUp()
        {
            _actions = new FakeCombatActions();
            _hero = new FakeCombatant("hero", 78);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void Damage_SingleHit_DamagesChosenTarget()
        {
            var foe = new FakeCombatant("foe", 30);
            DamageEffectSO effect = CreateDamage(6, 1, Create<ChosenTargetRuleSO>());

            effect.Apply(new EffectContext(_actions, _hero, foe, new ICombatant[] { foe }));

            Assert.AreEqual(24, foe.Health);
            CollectionAssert.AreEqual(new[] { "damage:foe:6" }, _actions.Log);
        }

        [Test]
        public void Damage_MultiHit_HitsEachTime()
        {
            var foe = new FakeCombatant("foe", 30);
            DamageEffectSO effect = CreateDamage(4, 2, Create<ChosenTargetRuleSO>());

            effect.Apply(new EffectContext(_actions, _hero, foe, new ICombatant[] { foe }));

            CollectionAssert.AreEqual(new[] { "damage:foe:4", "damage:foe:4" }, _actions.Log);
        }

        [Test]
        public void Damage_MultiHit_StopsWhenTargetFalls()
        {
            var foe = new FakeCombatant("foe", 4);
            DamageEffectSO effect = CreateDamage(4, 2, Create<ChosenTargetRuleSO>());

            effect.Apply(new EffectContext(_actions, _hero, foe, new ICombatant[] { foe }));

            CollectionAssert.AreEqual(new[] { "damage:foe:4" }, _actions.Log);
        }

        [Test]
        public void Damage_AllOpponents_HitsOnlyLivingOpponentsInOrder()
        {
            var first = new FakeCombatant("first", 10);
            var fallen = new FakeCombatant("fallen", 0);
            var third = new FakeCombatant("third", 10);
            DamageEffectSO effect = CreateDamage(3, 1, Create<AllOpponentsTargetRuleSO>());

            effect.Apply(new EffectContext(_actions, _hero, null, new ICombatant[] { first, fallen, third }));

            CollectionAssert.AreEqual(new[] { "damage:first:3", "damage:third:3" }, _actions.Log);
        }

        [Test]
        public void Damage_WithoutTargetRule_LogsErrorAndDealsNothing()
        {
            var foe = new FakeCombatant("foe", 30);
            DamageEffectSO effect = CreateDamage(6, 1, null);
            LogAssert.Expect(LogType.Error, new Regex("no target rule"));

            effect.Apply(new EffectContext(_actions, _hero, foe, new ICombatant[] { foe }));

            CollectionAssert.IsEmpty(_actions.Log);
            Assert.AreEqual(30, foe.Health);
        }

        [Test]
        public void Damage_RequiresChosenTarget_FollowsItsRule()
        {
            Assert.IsTrue(CreateDamage(6, 1, Create<ChosenTargetRuleSO>()).RequiresChosenTarget);
            Assert.IsFalse(CreateDamage(6, 1, Create<AllOpponentsTargetRuleSO>()).RequiresChosenTarget);
            Assert.IsFalse(CreateDamage(6, 1, null).RequiresChosenTarget);
        }

        [Test]
        public void Block_GainsKavachOnSource()
        {
            var foe = new FakeCombatant("foe", 30);
            BlockEffectSO effect = CreateBlock(5);

            effect.Apply(new EffectContext(_actions, _hero, foe, new ICombatant[] { foe }));

            Assert.AreEqual(5, _hero.Block);
            Assert.AreEqual(0, foe.Block);
            CollectionAssert.AreEqual(new[] { "block:hero:5" }, _actions.Log);
            Assert.IsFalse(effect.RequiresChosenTarget);
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        private DamageEffectSO CreateDamage(int amount, int hits, TargetRuleSO targets)
        {
            DamageEffectSO effect = Create<DamageEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            SerializedTestUtility.SetInt(effect, "_hits", hits);
            SerializedTestUtility.SetObject(effect, "_targets", targets);
            return effect;
        }

        private BlockEffectSO CreateBlock(int amount)
        {
            BlockEffectSO effect = Create<BlockEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            return effect;
        }
    }
}
