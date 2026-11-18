// EditMode tests for card outcome previews: a dry run deals damage through modifiers and then Kavach one hit at a
// time, stops at zero health and flags it lethal, previews Kavach gained, covers every enemy for cards that hit all,
// ignores draws, and never changes the real fighters; the threat panel's numbers follow a previewed outcome.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatOutcomePreviewTests
    {
        private CombatTestAssets _assets;
        private CombatResolver _resolver;
        private CombatOutcomePreview _outcome;
        private Combatant _player;
        private Combatant _enemy;
        private Combatant _other;
        private ICombatant[] _enemies;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _resolver = new CombatResolver();
            _outcome = new CombatOutcomePreview();
            _player = new Combatant("player", 78, 78);
            _enemy = new Combatant("enemy", 30, 30);
            _other = new Combatant("other", 20, 20);
            _enemies = new ICombatant[] { _enemy, _other };
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        private void Preview(CardDefinitionSO card, ICombatant target)
        {
            _outcome.Begin(_resolver);
            card.ApplyEffects(new EffectContext(_outcome, _player, target, _enemies));
        }

        [Test]
        public void SingleHit_PreviewsHealthLost_AndChangesNothingReal()
        {
            Preview(_assets.Card(1, _assets.Damage(6)), _enemy);

            Assert.IsTrue(_outcome.TryGet(_enemy, out CombatantOutcome outcome));
            Assert.AreEqual(30, outcome.StartHealth);
            Assert.AreEqual(24, outcome.EndHealth);
            Assert.AreEqual(6, outcome.HealthLost);
            Assert.IsFalse(outcome.IsLethal);
            Assert.AreEqual(30, _enemy.Health);
            Assert.IsFalse(_outcome.TryGet(_other, out _));
        }

        [Test]
        public void MultiHit_GoesThroughKavachOneHitAtATime()
        {
            _enemy.GainBlock(5);

            Preview(_assets.Card(1, _assets.Damage(4, 2)), _enemy);

            _outcome.TryGet(_enemy, out CombatantOutcome outcome);
            Assert.AreEqual(0, outcome.EndBlock);
            Assert.AreEqual(5, outcome.BlockLost);
            Assert.AreEqual(27, outcome.EndHealth);
            Assert.AreEqual(5, _enemy.Block);
        }

        [Test]
        public void Modifiers_ApplyBeforeKavach()
        {
            _resolver.AddModifier(FakeDamageModifier.Add(2));
            _enemy.GainBlock(3);

            Preview(_assets.Card(1, _assets.Damage(6)), _enemy);

            _outcome.TryGet(_enemy, out CombatantOutcome outcome);
            Assert.AreEqual(0, outcome.EndBlock);
            Assert.AreEqual(25, outcome.EndHealth);
        }

        [Test]
        public void LethalDamage_StopsAtZero_AndIgnoresLaterHits()
        {
            Preview(_assets.Card(2, _assets.Damage(12, 4)), _enemy);

            _outcome.TryGet(_enemy, out CombatantOutcome outcome);
            Assert.AreEqual(0, outcome.EndHealth);
            Assert.AreEqual(30, outcome.HealthLost);
            Assert.IsTrue(outcome.IsLethal);
        }

        [Test]
        public void AllOpponentsDamage_PreviewsEveryLivingEnemy()
        {
            DamageEffectSO volley = _assets.Create<DamageEffectSO>();
            SerializedTestUtility.SetInt(volley, "_amount", 5);
            SerializedTestUtility.SetInt(volley, "_hits", 1);
            SerializedTestUtility.SetObject(volley, "_targets", _assets.Create<AllOpponentsTargetRuleSO>());

            Preview(_assets.Card(1, volley), null);

            Assert.IsTrue(_outcome.TryGet(_enemy, out CombatantOutcome first));
            Assert.IsTrue(_outcome.TryGet(_other, out CombatantOutcome second));
            Assert.AreEqual(25, first.EndHealth);
            Assert.AreEqual(15, second.EndHealth);
        }

        [Test]
        public void BlockCard_PreviewsKavachGained_AndDrawsAreIgnored()
        {
            DrawCardsEffectSO draw = _assets.Create<DrawCardsEffectSO>();
            SerializedTestUtility.SetInt(draw, "_count", 1);
            _player.GainBlock(2);

            Preview(_assets.Card(1, _assets.Block(8), draw), null);

            Assert.IsTrue(_outcome.TryGet(_player, out CombatantOutcome outcome));
            Assert.AreEqual(2, outcome.StartBlock);
            Assert.AreEqual(10, outcome.EndBlock);
            Assert.AreEqual(8, outcome.BlockGained);
            Assert.IsTrue(outcome.HasChange);
            Assert.AreEqual(2, _player.Block);
        }

        [Test]
        public void Begin_ClearsThePreviousPreview()
        {
            Preview(_assets.Card(1, _assets.Damage(6)), _enemy);

            _outcome.Begin(_resolver);

            Assert.AreEqual(0, _outcome.Count);
            Assert.IsFalse(_outcome.TryGet(_enemy, out _));
        }

        [Test]
        public void Threat_AfterPreview_DropsEnemiesTheCardKills_AndUsesTheNewKavach()
        {
            var scout = new EnemyCombatant(_assets.Enemy(10, 10, 0, _assets.Move(_assets.Damage(7))), 0, new CombatRandom(1));
            var hound = new EnemyCombatant(_assets.Enemy(20, 20, 0, _assets.Move(_assets.Damage(4, 2))), 1, new CombatRandom(2));
            var fighters = new List<EnemyCombatant> { scout, hound };
            _enemies = new ICombatant[] { scout.Fighter, hound.Fighter };

            Preview(_assets.Card(1, _assets.Damage(10)), scout.Fighter);
            IncomingThreat afterKill = IncomingThreat.Calculate(fighters, _resolver, _player, _outcome);
            Assert.AreEqual(8, afterKill.Incoming);

            Preview(_assets.Card(1, _assets.Block(5)), null);
            IncomingThreat afterGuard = IncomingThreat.Calculate(fighters, _resolver, _player, _outcome);
            Assert.AreEqual(15, afterGuard.Incoming);
            Assert.AreEqual(5, afterGuard.Blocked);
            Assert.AreEqual(10, afterGuard.Taken);
        }
    }
}
