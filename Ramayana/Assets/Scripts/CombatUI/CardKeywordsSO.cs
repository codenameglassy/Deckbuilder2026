// The keywords card descriptions colour (Kavach, Prana, Exhaust and the status names) and the colour of each. A word is
// found by its localization key, so it is coloured in every language without touching the string tables. Shared and
// read-only at runtime; referenced by the Combat HUD style. In the Editor, changing a value raises Changed so card
// text re-colours live.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Card Keywords", fileName = "CardKeywords")]
    public sealed class CardKeywordsSO : ScriptableObject
    {
        [SerializeField] private CardKeywordEntry[] _entries = Array.Empty<CardKeywordEntry>();

        public event Action Changed;

        public int Count => _entries != null ? _entries.Length : 0;

        public CardKeywordEntry Get(int index)
        {
            return _entries[index];
        }

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
