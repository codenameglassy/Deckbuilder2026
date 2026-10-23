// The cards a reward can offer, authored as a list. A run rolls a few distinct cards from it after a victory.
// Duplicate and empty entries are ignored when rolling.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Cards
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Card Reward Pool", fileName = "CardRewardPool")]
    public sealed class CardRewardPoolSO : ScriptableObject
    {
        [SerializeField] private CardDefinitionSO[] _cards = Array.Empty<CardDefinitionSO>();

        public IReadOnlyList<CardDefinitionSO> Cards => _cards;
    }
}
