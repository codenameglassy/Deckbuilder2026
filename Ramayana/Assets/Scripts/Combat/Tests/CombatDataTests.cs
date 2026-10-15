// EditMode tests for the definition assets: card targeting and effect order, upgrade links,
// deck counting, and enemy move patterns with a starting offset.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatDataTests
    {
        private readonly List<Object> _created = new List<Object>();

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
        public void Card_RequiresChosenTarget_WhenAnyEffectNeedsOne()
        {
            DamageEffectSO damage = CreateDamage(6);
            BlockEffectSO block = CreateBlock(5);

            CardDefinitionSO strike = CreateCard(damage);
            CardDefinitionSO guard = CreateCard(block);
            CardDefinitionSO mixed = CreateCard(block, damage);
            CardDefinitionSO empty = CreateCard(new Object[] { null });

            Assert.IsTrue(strike.RequiresChosenTarget);
            Assert.IsFalse(guard.RequiresChosenTarget);
            Assert.IsTrue(mixed.RequiresChosenTarget);
            Assert.IsFalse(empty.RequiresChosenTarget);
        }

        [Test]
        public void Card_ApplyEffects_RunsInOrderAndSkipsEmptySlots()
        {
            var actions = new FakeCombatActions();
            var hero = new FakeCombatant("hero", 78);
            var foe = new FakeCombatant("foe", 30);
            CardDefinitionSO card = CreateCard(CreateDamage(6), null, CreateBlock(5));

            card.ApplyEffects(new EffectContext(actions, hero, foe, new ICombatant[] { foe }));

            CollectionAssert.AreEqual(new[] { "damage:foe:6", "block:hero:5" }, actions.Log);
        }

        [Test]
        public void Card_CanUpgrade_OnlyWhenUpgradedFormIsSet()
        {
            CardDefinitionSO upgraded = CreateCard(CreateDamage(8));
            CardDefinitionSO card = CreateCard(CreateDamage(6));
            SerializedTestUtility.SetObject(card, "_upgradedForm", upgraded);

            Assert.IsTrue(card.CanUpgrade);
            Assert.AreSame(upgraded, card.UpgradedForm);
            Assert.IsFalse(upgraded.CanUpgrade);
        }

        [Test]
        public void StartingDeck_TotalCount_IgnoresEmptyEntries()
        {
            CardDefinitionSO strike = CreateCard(CreateDamage(6));
            CardDefinitionSO guard = CreateCard(CreateBlock(5));
            StartingDeckSO deck = Create<StartingDeckSO>();
            SerializedTestUtility.SetDeckEntries(deck, "_entries",
                new Object[] { strike, null, guard, strike },
                new[] { 5, 3, 5, 0 });

            Assert.AreEqual(10, deck.TotalCount);
        }

        [Test]
        public void Enemy_GetMove_WrapsAroundThePattern()
        {
            EnemyMoveSO first = Create<EnemyMoveSO>();
            EnemyMoveSO second = Create<EnemyMoveSO>();
            EnemyMoveSO third = Create<EnemyMoveSO>();
            EnemyDefinitionSO enemy = Create<EnemyDefinitionSO>();
            SerializedTestUtility.SetObjectArray(enemy, "_moves", first, second, third);

            Assert.AreSame(first, enemy.GetMove(0));
            Assert.AreSame(third, enemy.GetMove(2));
            Assert.AreSame(first, enemy.GetMove(3));
            Assert.AreSame(second, enemy.GetMove(4));
        }

        [Test]
        public void Enemy_StartMoveIndex_IsReadFromData()
        {
            EnemyDefinitionSO enemy = Create<EnemyDefinitionSO>();
            SerializedTestUtility.SetObjectArray(enemy, "_moves", Create<EnemyMoveSO>(), Create<EnemyMoveSO>(), Create<EnemyMoveSO>());
            SerializedTestUtility.SetInt(enemy, "_startMoveIndex", 2);

            Assert.AreEqual(2, enemy.StartMoveIndex);
            Assert.AreEqual(3, enemy.MoveCount);
        }

        [Test]
        public void Enemy_GetMove_WithNoMoves_ReturnsNull()
        {
            EnemyDefinitionSO enemy = Create<EnemyDefinitionSO>();

            Assert.IsNull(enemy.GetMove(0));
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        private DamageEffectSO CreateDamage(int amount)
        {
            DamageEffectSO effect = Create<DamageEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            SerializedTestUtility.SetInt(effect, "_hits", 1);
            SerializedTestUtility.SetObject(effect, "_targets", Create<ChosenTargetRuleSO>());
            return effect;
        }

        private BlockEffectSO CreateBlock(int amount)
        {
            BlockEffectSO effect = Create<BlockEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            return effect;
        }

        private CardDefinitionSO CreateCard(params Object[] effects)
        {
            CardDefinitionSO card = Create<CardDefinitionSO>();
            SerializedTestUtility.SetObjectArray(card, "_effects", effects);
            return card;
        }
    }
}
