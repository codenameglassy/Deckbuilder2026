// Shared definition of one card: stable id, localization keys, type, Prana cost and its effect strategies.
// The upgraded version is a separate definition asset, so upgrading swaps the reference and never mutates data.
// Runtime state (which pile, instance id) lives on the combat model's card instance, not here.
using System;
using JourneyToLanka.Combat.Effects;
using UnityEngine;

namespace JourneyToLanka.Combat.Cards
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Card", fileName = "Card")]
    public sealed class CardDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [SerializeField] private CardTypeSO _type;
        [SerializeField, Min(0)] private int _cost = 1;
        [SerializeField] private CombatEffectSO[] _effects = Array.Empty<CombatEffectSO>();
        [SerializeField] private CardDefinitionSO _upgradedForm;

        public string Id => _id;

        public string NameKey => _nameKey;

        public string DescriptionKey => _descriptionKey;

        public CardTypeSO Type => _type;

        public int Cost => _cost;

        public int EffectCount => _effects != null ? _effects.Length : 0;

        public CardDefinitionSO UpgradedForm => _upgradedForm;

        public bool CanUpgrade => _upgradedForm != null;

        public bool RequiresChosenTarget => CombatEffectSO.AnyRequiresChosenTarget(_effects);

        public CombatEffectSO GetEffect(int index)
        {
            return _effects[index];
        }

        public void ApplyEffects(in EffectContext context)
        {
            CombatEffectSO.ApplyAll(_effects, context);
        }
    }
}
