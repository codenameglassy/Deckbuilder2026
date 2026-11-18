// The chance of each rarity when a card reward is rolled, as weights: 60 Common, 33 Rare and 7 Epic after a normal
// fight in the reference build, 45, 42 and 13 after an elite fight. Roll is pure, so a run's seeded random makes every
// reward replayable. With every weight at zero it always answers Common. Shared and immutable.
using System;
using JourneyToLanka.Combat.Model;
using UnityEngine;

namespace JourneyToLanka.Combat.Cards
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Card Reward Odds", fileName = "CardRewardOdds")]
    public sealed class CardRewardOddsSO : ScriptableObject
    {
        [SerializeField, Min(0)] private int _commonWeight = 60;
        [SerializeField, Min(0)] private int _rareWeight = 33;
        [SerializeField, Min(0)] private int _epicWeight = 7;

        public int CommonWeight => _commonWeight;

        public int RareWeight => _rareWeight;

        public int EpicWeight => _epicWeight;

        public CardRarity Roll(CombatRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int total = _commonWeight + _rareWeight + _epicWeight;
            if (total <= 0)
            {
                return CardRarity.Common;
            }

            int roll = random.Range(0, total - 1);
            if (roll < _commonWeight)
            {
                return CardRarity.Common;
            }

            return roll < _commonWeight + _rareWeight ? CardRarity.Rare : CardRarity.Epic;
        }
    }
}
