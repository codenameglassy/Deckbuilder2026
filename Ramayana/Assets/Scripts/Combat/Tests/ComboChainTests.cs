// EditMode tests for the combo chain (Bow Chain, Ring Chain). The chain itself: the ladder steps 100, 100, 150, 200,
// 300 and scales only the number its kind names, the finisher starts the chain again at 1, Peek matches Register
// without changing anything, switching kind breaks the chain and starts the new one at 1, any other card breaks it,
// zero-cost cards break it even when they name a chain, and Reset clears it without a break. Through the combat
// model: a chained Strike's damage rounds down, the multiplier lands after every damage modifier, a chained Guard
// scales its own Kavach, special arrows neither build nor break the chain, the chain resets at the end of the
// player's turn, PreviewPlay and card text show the chained numbers without touching the chain, and a chained play
// raises ModifiersChanged once.
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class ComboChainTests
    {
        private const int Seed = 2026;
        private static readonly int[] Ladder = { 100, 100, 150, 200, 300 };

        private CombatTestAssets _assets;
        private CombatRulesSO _rules;
        private ChainKindSO _bow;
        private ChainKindSO _ring;
        private CardDefinitionSO _strike;
        private CardDefinitionSO _guard;
        private CardDefinitionSO _other;
        private CardDefinitionSO _free;
        private CardDefinitionSO _freeChained;
        private EnemyDefinitionSO _dummy;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
            _rules = _assets.Rules(10, 10, 10);
            _bow = Kind(true, false);
            _ring = Kind(false, true);
            _strike = Chained(1, _bow, _assets.Damage(7));
            _guard = Chained(1, _ring, _assets.Block(5));
            _other = _assets.Card(1, _assets.Damage(3));
            _free = _assets.Card(0, _assets.Damage(2));
            _freeChained = Chained(0, _bow, _assets.Damage(7));
            _dummy = _assets.Enemy(500, 500, 0, _assets.Move(_assets.Block(1)));
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Ladder_StepsThroughEveryTier_AndScalesOnlyItsOwnNumber()
        {
            var chain = new ComboChain();

            for (int i = 0; i < Ladder.Length; i++)
            {
                ChainScale bowScale = chain.Register(_strike);
                Assert.AreEqual(Ladder[i], bowScale.DamagePercent, $"Bow step {i + 1}");
                Assert.AreEqual(ChainScale.NeutralPercent, bowScale.KavachPercent, $"Bow step {i + 1} Kavach");
            }

            for (int i = 0; i < Ladder.Length; i++)
            {
                ChainScale ringScale = chain.Register(_guard);
                Assert.AreEqual(Ladder[i], ringScale.KavachPercent, $"Ring step {i + 1}");
                Assert.AreEqual(ChainScale.NeutralPercent, ringScale.DamagePercent, $"Ring step {i + 1} damage");
            }
        }

        [Test]
        public void Finisher_StartsTheChainAgainAtOne()
        {
            var chain = new ComboChain();
            int lastStep = 0;
            int lastPercent = 0;
            int broken = 0;
            chain.Advanced += (_, step, percent) =>
            {
                lastStep = step;
                lastPercent = percent;
            };
            chain.Broken += (_, __) => broken++;

            for (int i = 0; i < Ladder.Length; i++)
            {
                chain.Register(_strike);
            }

            Assert.AreEqual(5, lastStep);
            Assert.AreEqual(300, lastPercent);
            Assert.IsTrue(_bow.IsFinisher(lastStep));
            Assert.AreEqual(0, chain.Count);
            Assert.IsNull(chain.Kind);
            Assert.AreEqual(1, chain.PeekStep(_strike));

            Assert.AreEqual(100, chain.Register(_strike).DamagePercent);
            Assert.AreEqual(1, chain.Count);
            Assert.AreSame(_bow, chain.Kind);
            Assert.AreEqual(0, broken);
        }

        [Test]
        public void Peek_MatchesRegister_WithoutChangingTheChain()
        {
            var chain = new ComboChain();
            chain.Register(_strike);
            chain.Register(_strike);
            int changes = 0;
            chain.Changed += () => changes++;

            Assert.AreEqual(3, chain.PeekStep(_strike));
            Assert.AreEqual(150, chain.Peek(_strike));
            Assert.AreEqual(150, chain.PeekScale(_strike).DamagePercent);
            Assert.AreEqual(1, chain.PeekStep(_guard));
            Assert.AreEqual(100, chain.Peek(_guard));
            Assert.AreEqual(0, chain.PeekStep(_other));
            Assert.IsTrue(chain.PeekScale(_other).IsNeutral);
            Assert.AreEqual(2, chain.Count);
            Assert.AreEqual(0, changes);

            Assert.AreEqual(150, chain.Register(_strike).DamagePercent);
        }

        [Test]
        public void SwitchingKind_BreaksTheChain_AndStartsTheNewOneAtOne()
        {
            var chain = new ComboChain();
            ChainKindSO brokenKind = null;
            int brokenLength = 0;
            chain.Broken += (kind, length) =>
            {
                brokenKind = kind;
                brokenLength = length;
            };

            chain.Register(_strike);
            chain.Register(_strike);
            chain.Register(_strike);
            ChainScale guardScale = chain.Register(_guard);

            Assert.AreSame(_bow, brokenKind);
            Assert.AreEqual(3, brokenLength);
            Assert.AreEqual(100, guardScale.KavachPercent);
            Assert.AreSame(_ring, chain.Kind);
            Assert.AreEqual(1, chain.Count);

            chain.Register(_guard);
            Assert.AreEqual(150, chain.Register(_guard).KavachPercent);
        }

        [Test]
        public void AnyOtherCard_BreaksTheChain()
        {
            var chain = new ComboChain();
            int brokenLength = 0;
            chain.Broken += (_, length) => brokenLength = length;

            chain.Register(_strike);
            chain.Register(_strike);
            Assert.IsTrue(chain.Register(_other).IsNeutral);

            Assert.AreEqual(2, brokenLength);
            Assert.AreEqual(0, chain.Count);
            Assert.IsNull(chain.Kind);
            Assert.AreEqual(100, chain.Register(_strike).DamagePercent);
            Assert.AreEqual(1, chain.Count);
        }

        [Test]
        public void ZeroCostCards_BreakTheChain_EvenWhenTheyNameAChain()
        {
            var chain = new ComboChain();
            int breaks = 0;
            chain.Broken += (_, __) => breaks++;

            chain.Register(_strike);
            chain.Register(_strike);
            Assert.IsTrue(chain.Register(_free).IsNeutral);
            Assert.AreEqual(0, chain.Count);

            chain.Register(_strike);
            chain.Register(_strike);
            Assert.AreEqual(0, chain.PeekStep(_freeChained));
            Assert.IsTrue(chain.Register(_freeChained).IsNeutral);
            Assert.AreEqual(0, chain.Count);
            Assert.AreEqual(2, breaks);
        }

        [Test]
        public void Reset_ClearsTheChain_WithoutABreak()
        {
            var chain = new ComboChain();
            int breaks = 0;
            int changes = 0;
            chain.Broken += (_, __) => breaks++;
            chain.Register(_strike);
            chain.Register(_strike);
            chain.Changed += () => changes++;

            chain.Reset();
            chain.Reset();

            Assert.AreEqual(0, chain.Count);
            Assert.IsNull(chain.Kind);
            Assert.AreEqual(0, breaks);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void Model_ChainedStrikes_FollowTheLadder_RoundedDown()
        {
            CombatModel model = CreateModel(Deck(_strike, 10));
            model.StartPlayerTurn();
            Combatant enemy = model.Enemies[0].Fighter;
            int[] expected = { 7, 7, 10, 14, 21, 7 };

            for (int i = 0; i < expected.Length; i++)
            {
                int before = enemy.Health;
                Play(model, _strike, 0);
                Assert.AreEqual(expected[i], before - enemy.Health, $"Strike {i + 1}");
            }
        }

        [Test]
        public void Model_Multiplier_LandsAfterEveryDamageModifier()
        {
            CombatModel model = CreateModel(Deck(_strike, 10));
            model.Resolver.AddModifier(FakeDamageModifier.Add(2));
            model.StartPlayerTurn();
            Combatant enemy = model.Enemies[0].Fighter;

            Play(model, _strike, 0);
            Play(model, _strike, 0);
            int before = enemy.Health;
            Play(model, _strike, 0);

            // (7 + 2) x 1.5 = 13.5, rounded down. Scaling before the +2 would have given 12.
            Assert.AreEqual(13, before - enemy.Health);
        }

        [Test]
        public void Model_ChainedGuards_ScaleTheirOwnKavach_RoundedDown()
        {
            CombatModel model = CreateModel(Deck(_guard, 10));
            model.StartPlayerTurn();
            int[] expected = { 5, 5, 7, 10, 15 };

            for (int i = 0; i < expected.Length; i++)
            {
                int before = model.Player.Block;
                Play(model, _guard, -1);
                Assert.AreEqual(expected[i], model.Player.Block - before, $"Guard {i + 1}");
            }
        }

        [Test]
        public void Model_SpecialArrows_NeitherBuildNorBreakTheChain()
        {
            ArrowDefinitionSO arrow = _assets.Arrow(_assets.Damage(4));
            CombatModel model = CreateModel(Deck(_strike, 10), arrow);
            model.StartPlayerTurn();
            Combatant enemy = model.Enemies[0].Fighter;

            Play(model, _strike, 0);
            Play(model, _strike, 0);
            int beforeArrow = enemy.Health;
            Assert.IsTrue(model.TryFireArrow(0, 0));
            Assert.AreEqual(4, beforeArrow - enemy.Health);
            Assert.AreEqual(2, model.Chain.Count);

            int before = enemy.Health;
            Play(model, _strike, 0);
            Assert.AreEqual(10, before - enemy.Health);
        }

        [Test]
        public void Model_EndOfPlayerTurn_ResetsTheChain()
        {
            CombatModel model = CreateModel(Deck(_strike, 10));
            model.StartPlayerTurn();
            Combatant enemy = model.Enemies[0].Fighter;
            Play(model, _strike, 0);
            Play(model, _strike, 0);

            model.EndPlayerTurn();
            Assert.AreEqual(0, model.Chain.Count);

            model.StartPlayerTurn();
            int before = enemy.Health;
            Play(model, _strike, 0);
            Assert.AreEqual(7, before - enemy.Health);
        }

        [Test]
        public void Model_PreviewAndCardText_ShowChainedNumbers_WithoutTouchingTheChain()
        {
            CombatModel model = CreateModel(Deck(_strike, 10));
            model.StartPlayerTurn();
            Combatant enemy = model.Enemies[0].Fighter;
            Play(model, _strike, 0);
            Play(model, _strike, 0);
            CardInstance next = FirstInHand(model, _strike);

            var outcome = new CombatOutcomePreview();
            model.PreviewPlay(next, 0, outcome);
            Assert.IsTrue(outcome.TryGet(enemy, out CombatantOutcome result));
            Assert.AreEqual(enemy.Health - 10, result.EndHealth);

            var values = new List<CardValue>();
            var text = new CardTextContext(model.Resolver, model.Player, enemy, model.Chain.PeekScale(next.Definition));
            next.Definition.CollectCardValues(text, values);
            Assert.AreEqual(7, values[0].Base);
            Assert.AreEqual(10, values[0].Current);
            Assert.IsTrue(values[0].IsBuffed);

            Assert.AreEqual(2, model.Chain.Count);
            Assert.AreEqual(enemy.Health, result.StartHealth);
        }

        [Test]
        public void Model_ChainedPlay_RaisesModifiersChangedOnce()
        {
            CombatModel model = CreateModel(Deck(_strike, 10));
            model.StartPlayerTurn();
            int changes = 0;
            model.Resolver.ModifiersChanged += () => changes++;

            Play(model, _strike, 0);

            Assert.AreEqual(1, changes);
        }

        private ChainKindSO Kind(bool scalesDamage, bool scalesKavach)
        {
            ChainKindSO kind = _assets.Create<ChainKindSO>();
            using var serializedObject = new SerializedObject(kind);
            serializedObject.FindProperty("_scalesDamage").boolValue = scalesDamage;
            serializedObject.FindProperty("_scalesKavach").boolValue = scalesKavach;
            SerializedProperty ladder = serializedObject.FindProperty("_multiplierPercents");
            ladder.arraySize = Ladder.Length;
            for (int i = 0; i < Ladder.Length; i++)
            {
                ladder.GetArrayElementAtIndex(i).intValue = Ladder[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return kind;
        }

        private CardDefinitionSO Chained(int cost, ChainKindSO kind, params CombatEffectSO[] effects)
        {
            CardDefinitionSO card = _assets.Card(cost, effects);
            SerializedTestUtility.SetObject(card, "_chain", kind);
            return card;
        }

        private static List<CardDefinitionSO> Deck(CardDefinitionSO card, int count)
        {
            var deck = new List<CardDefinitionSO>(count);
            for (int i = 0; i < count; i++)
            {
                deck.Add(card);
            }

            return deck;
        }

        private CombatModel CreateModel(List<CardDefinitionSO> deck, params ArrowDefinitionSO[] arrows)
        {
            var setup = new PlayerSetup("rama", 78, 78, deck, new Quiver(3, arrows));
            return new CombatModel(_rules, setup, _assets.Encounter(_dummy), new CombatRandom(Seed));
        }

        private static CardInstance FirstInHand(CombatModel model, CardDefinitionSO definition)
        {
            for (int i = 0; i < model.Deck.Hand.Count; i++)
            {
                if (model.Deck.Hand[i].Definition == definition)
                {
                    return model.Deck.Hand[i];
                }
            }

            Assert.Fail($"No {definition.name} in hand.");
            return null;
        }

        private static void Play(CombatModel model, CardDefinitionSO definition, int targetIndex)
        {
            CardInstance card = FirstInHand(model, definition);
            Assert.IsTrue(model.TryPlayCard(card.Id, targetIndex), $"Could not play {definition.name}.");
        }
    }
}
