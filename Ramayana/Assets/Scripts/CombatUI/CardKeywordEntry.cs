// One keyword that card descriptions colour: the localization key of the word (in the Combat string table) and the
// colour it is shown in. Serialized inside a CardKeywordsSO; plain data.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [Serializable]
    public struct CardKeywordEntry
    {
        [SerializeField] private string _nameKey;
        [SerializeField] private Color _color;

        public string NameKey => _nameKey;

        public Color Color => _color;
    }
}
