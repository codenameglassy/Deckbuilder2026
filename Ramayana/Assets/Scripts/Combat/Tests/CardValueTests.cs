// EditMode tests for card description values: each effect adds its numbers in order (damage per hit then hits,
// Kavach, cards drawn), damage shows its live previewed value against the context's target, a context without a
// preview shows authored bases, and CombatResolver reports modifier changes so card text knows when to refresh.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CardValueTests
    {
        private CombatTestAssets _assets;
        private CombatResolver _resolver;
        private Combatant _player;
        private Combatant _enemy;
        private List<CardValue> _values;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _resolver = new CombatResolver();
            _player = new Combatant("player", 78, 78);
            _enemy = new Combatant("enemy", 30, 30);
            _values = new List<CardValue>();
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void DefaultContext_ShowsAuthoredBaseValues()
        {
            CardDefinitionSO card = _assets.Card(1, _assets.Damage(6));

            card.CollectCardValues(default, _values);

            Assert.AreEqual(2, _values.Count);
            Assert.AreEqual(6, _values[0].Base);
            Assert.AreEqual(6, _values[0].Current);
            Assert.IsFalse(_values[0].IsBuffed);
            Assert.IsFalse(_values[0].IsWeakened);
            Assert.AreEqual(1, _values[1].Current);
        }

        [Test]
        public void Damage_WithModifiers_ShowsPreviewedValueAgainstTheTarget()
        {
            _resolver.AddModifier(FakeDamageModifier.Add(3));
            CardDefinitionSO card = _assets.Card(1, _assets.Damage(6));

            card.CollectCardValues(new CardTextContext(_resolver, _player, _enemy), _values);

            Assert.AreEqual(6, _values[0].Base);
            Assert.AreEqual(9, _values[0].Current);
            Assert.IsTrue(_values[0].IsBuffed);
        }

        [Test]
        public void Damage_WeakenedBelowBase_IsMarkedWeakened_AndNullTargetIsAllowed()
        {
            _resolver.AddModifier(FakeDamageModifier.Percent(50));
            CardDefinitionSO card = _assets.Card(1, _assets.Damage(7));

            card.CollectCardValues(new CardTextContext(_resolver, _player, null), _values);

            Assert.AreEqual(3, _values[0].Current);
            Assert.IsTrue(_values[0].IsWeakened);
        }

        [Test]
        public void MultiHitDamage_AddsPerHitThenHits_AndHitsNeverChange()
        {
            _resolver.AddModifier(FakeDamageModifier.Add(1));
            CardDefinitionSO card = _assets.Card(2, _assets.Damage(3, 4));

            card.CollectCardValues(new CardTextContext(_resolver, _player, _enemy), _values);

            Assert.AreEqual(2, _values.Count);
            Assert.AreEqual(4, _values[0].Current);
            Assert.AreEqual(4, _values[1].Base);
            Assert.AreEqual(4, _values[1].Current);
        }

        [Test]
        public void SeveralEffects_AddTheirValuesInEffectOrder()
        {
            DrawCardsEffectSO draw = _assets.Create<DrawCardsEffectSO>();
            SerializedTestUtility.SetInt(draw, "_count", 1);
            CardDefinitionSO card = _assets.Card(1, _assets.Block(8), draw);

            card.CollectCardValues(new CardTextContext(_resolver, _player, null), _values);

            Assert.AreEqual(2, _values.Count);
            Assert.AreEqual(8, _values[0].Current);
            Assert.AreEqual(1, _values[1].Current);
        }

        [Test]
        public void CollectCardValues_AppendsWithoutClearing()
        {
            _values.Add(CardValue.Fixed(99));
            CardDefinitionSO card = _assets.Card(1, _assets.Block(5));

            card.CollectCardValues(default, _values);

            Assert.AreEqual(2, _values.Count);
            Assert.AreEqual(5, _values[1].Current);
        }

        [Test]
        public void Resolver_RaisesModifiersChanged_OnAddRemoveAndNotify_ButNotOnDuplicates()
        {
            int changes = 0;
            _resolver.ModifiersChanged += () => changes++;
            FakeDamageModifier bonus = FakeDamageModifier.Add(2);

            _resolver.AddModifier(bonus);
            _resolver.AddModifier(bonus);
            Assert.AreEqual(1, changes);

            _resolver.NotifyModifiersChanged();
            Assert.AreEqual(2, changes);

            Assert.IsTrue(_resolver.RemoveModifier(bonus));
            Assert.IsFalse(_resolver.RemoveModifier(bonus));
            Assert.AreEqual(3, changes);
        }
    }
}
