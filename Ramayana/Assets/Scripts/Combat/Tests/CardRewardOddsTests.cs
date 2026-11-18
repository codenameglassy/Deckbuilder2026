// EditMode tests for card reward odds, against the reference build: a rarity is picked by weight, zero weights fall
// back to Common, the same seed rolls the same rarities, and the normal odds land near 60, 33 and 7 percent.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;
using UnityEditor;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CardRewardOddsTests
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
        public void Roll_PicksTheOnlyWeightedRarity()
        {
            CardRewardOddsSO epicOnly = Odds(0, 0, 1);
            CardRewardOddsSO rareOnly = Odds(0, 1, 0);
            var random = new CombatRandom(5);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(CardRarity.Epic, epicOnly.Roll(random));
                Assert.AreEqual(CardRarity.Rare, rareOnly.Roll(random));
            }
        }

        [Test]
        public void Roll_WithNoWeight_IsCommon()
        {
            Assert.AreEqual(CardRarity.Common, Odds(0, 0, 0).Roll(new CombatRandom(5)));
        }

        [Test]
        public void Roll_IsSeeded()
        {
            CardRewardOddsSO odds = Odds(60, 33, 7);
            var first = new CombatRandom(77);
            var second = new CombatRandom(77);

            for (int i = 0; i < 30; i++)
            {
                Assert.AreEqual(odds.Roll(first), odds.Roll(second));
            }
        }

        [Test]
        public void Roll_NormalOdds_MatchTheReferenceShares()
        {
            CardRewardOddsSO odds = Odds(60, 33, 7);
            var random = new CombatRandom(2026);
            int common = 0;
            int rare = 0;
            int epic = 0;
            const int rolls = 10000;

            for (int i = 0; i < rolls; i++)
            {
                switch (odds.Roll(random))
                {
                    case CardRarity.Common: common++; break;
                    case CardRarity.Rare: rare++; break;
                    case CardRarity.Epic: epic++; break;
                }
            }

            Assert.AreEqual(0.60, common / (double)rolls, 0.02);
            Assert.AreEqual(0.33, rare / (double)rolls, 0.02);
            Assert.AreEqual(0.07, epic / (double)rolls, 0.02);
        }

        private CardRewardOddsSO Odds(int common, int rare, int epic)
        {
            CardRewardOddsSO odds = _assets.Create<CardRewardOddsSO>();
            using var serializedObject = new SerializedObject(odds);
            serializedObject.FindProperty("_commonWeight").intValue = common;
            serializedObject.FindProperty("_rareWeight").intValue = rare;
            serializedObject.FindProperty("_epicWeight").intValue = epic;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return odds;
        }
    }
}
