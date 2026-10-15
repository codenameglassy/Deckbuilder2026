// The deck a run or chapter starts with, authored as card and count pairs.
// Entries with no card or a count below one are ignored, so a half-edited list never breaks combat.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Cards
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Starting Deck", fileName = "StartingDeck")]
    public sealed class StartingDeckSO : ScriptableObject
    {
        [SerializeField] private DeckEntry[] _entries = Array.Empty<DeckEntry>();

        public IReadOnlyList<DeckEntry> Entries => _entries;

        public int TotalCount
        {
            get
            {
                int total = 0;
                if (_entries == null)
                {
                    return total;
                }

                for (int i = 0; i < _entries.Length; i++)
                {
                    DeckEntry entry = _entries[i];
                    if (entry.Card != null && entry.Count > 0)
                    {
                        total += entry.Count;
                    }
                }

                return total;
            }
        }
    }
}
