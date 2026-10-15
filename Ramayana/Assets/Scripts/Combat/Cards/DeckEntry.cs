// One line of a deck list: a card definition and how many copies of it.
using System;
using UnityEngine;

namespace JourneyToLanka.Combat.Cards
{
    [Serializable]
    public struct DeckEntry
    {
        [SerializeField] private CardDefinitionSO _card;
        [SerializeField, Min(1)] private int _count;

        public DeckEntry(CardDefinitionSO card, int count)
        {
            _card = card;
            _count = count;
        }

        public CardDefinitionSO Card => _card;

        public int Count => _count;
    }
}
