// One combo chain as a Type Object (Bow Chain, Ring Chain): stable id, localization keys for its name and hint
// line, colour and icon for the chain bar, which of a card's numbers it scales, and its multiplier ladder in whole
// percents, one entry per card in a row (100, 100, 150, 200, 300: the 3rd card is x1.5, the 4th x2, the 5th x3).
// The ladder's last step is the finisher: the chain starts again at 1 after it. Cards opt in through
// CardDefinitionSO's chain reference; ComboChain holds the runtime count, never this asset. Shared and immutable.
using System;
using JourneyToLanka.Combat.Effects;
using UnityEngine;

namespace JourneyToLanka.Combat.Cards
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Chain Kind", fileName = "ChainKind")]
    public sealed class ChainKindSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _hintKey;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Sprite _icon;
        [Tooltip("The chain multiplies the damage of the cards that build it.")]
        [SerializeField] private bool _scalesDamage;
        [Tooltip("The chain multiplies the Kavach the cards that build it grant themselves.")]
        [SerializeField] private bool _scalesKavach;
        [Tooltip("Multiplier in percent for the 1st, 2nd, 3rd... card in a row. The last entry is the finisher.")]
        [SerializeField] private int[] _multiplierPercents = { 100, 100, 150, 200, 300 };

        public string Id => _id;

        public string NameKey => _nameKey;

        public string HintKey => _hintKey;

        public Color Color => _color;

        public Sprite Icon => _icon;

        public bool ScalesDamage => _scalesDamage;

        public bool ScalesKavach => _scalesKavach;

        // Number of steps on the ladder; the step equal to it is the finisher.
        public int Length => _multiplierPercents != null ? _multiplierPercents.Length : 0;

        // Percent for a 1-based step. Steps past the ladder use the last entry; an empty ladder or a step below 1
        // is neutral.
        public int GetPercent(int step)
        {
            int length = Length;
            if (length == 0 || step < 1)
            {
                return ChainScale.NeutralPercent;
            }

            return _multiplierPercents[Math.Min(step, length) - 1];
        }

        public bool IsFinisher(int step)
        {
            return Length > 0 && step >= Length;
        }

        public ChainScale GetScale(int step)
        {
            int percent = GetPercent(step);
            return new ChainScale(
                _scalesDamage ? percent : ChainScale.NeutralPercent,
                _scalesKavach ? percent : ChainScale.NeutralPercent);
        }

        private void OnValidate()
        {
            if (_multiplierPercents == null)
            {
                _multiplierPercents = Array.Empty<int>();
                return;
            }

            for (int i = 0; i < _multiplierPercents.Length; i++)
            {
                if (_multiplierPercents[i] < 0)
                {
                    _multiplierPercents[i] = 0;
                }
            }
        }
    }
}
