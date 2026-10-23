// EditMode tests for the draw effect: a played draw card pulls cards through the combat model's deck while the
// card itself waits in play, and the effect does nothing when the actions have no deck (enemy moves).
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class DrawCardsEffectTests
    {
        private CombatTestAssets _assets;

        [SetUp]
        public void SetUp()
        {
            _assets = new CombatTestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void PlayingADrawCard_DrawsThroughTheDeck()
        {
            DrawCardsEffectSO drawTwo = _assets.Create<DrawCardsEffectSO>();
            SerializedTestUtility.SetInt(drawTwo, "_count", 2);
            CardDefinitionSO counsel = _assets.Card(0, drawTwo);
            PlayerDefinitionSO player = _assets.Player(78, _assets.Deck(new[] { counsel }, new[] { 10 }));
            CombatEncounterSO encounter = _assets.Encounter(_assets.Enemy(30, 30, 0, _assets.Move(_assets.Damage(7))));
            var model = new CombatModel(_assets.Rules(3, 5, 10), player, 78, encounter, new CombatRandom(5));
            model.StartPlayerTurn();

            Assert.IsTrue(model.TryPlayCard(model.Deck.Hand[0].Id, -1));

            Assert.AreEqual(6, model.Deck.Hand.Count);
            Assert.AreEqual(3, model.Deck.DrawPile.Count);
            Assert.AreEqual(1, model.Deck.DiscardPile.Count);
            Assert.AreEqual(3, model.Prana.Current);
        }

        [Test]
        public void DrawEffect_WithoutCardActions_DoesNothing()
        {
            DrawCardsEffectSO drawTwo = _assets.Create<DrawCardsEffectSO>();
            var actions = new FakeCombatActions();
            var hero = new FakeCombatant("hero", 30);

            Assert.DoesNotThrow(() => drawTwo.Apply(new EffectContext(actions, hero, null, new ICombatant[0])));
            CollectionAssert.IsEmpty(actions.Log);
        }
    }
}
