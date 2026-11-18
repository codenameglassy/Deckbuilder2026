// Shared definition of one card: stable id, localization keys, type, Prana cost and its effect strategies.
// The upgraded version is a separate definition asset, so upgrading swaps the reference and never mutates data.
// Runtime state (which pile, instance id) lives on the combat model's card instance, not here.
// CollectCardValues gathers the numbers its description pattern shows, from every effect in order.
// Exhausts sends the card to the exhaust pile once played, out of the deck for the rest of the fight. Rarity decides
// how often a reward roll offers it.
using System;
using System.Collections.Generic;
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
        [Tooltip("How often reward rolls offer this card. None means not set yet; Divine cards are never rolled.")]
        [SerializeField] private CardRarity _rarity = CardRarity.None;
        [SerializeField] private CombatEffectSO[] _effects = Array.Empty<CombatEffectSO>();
        [SerializeField] private CardDefinitionSO _upgradedForm;
        [Tooltip("Once played, the card goes to the exhaust pile for the rest of the fight instead of the discard pile.")]
        [SerializeField] private bool _exhausts;

        public string Id => _id;

        public string NameKey => _nameKey;

        public string DescriptionKey => _descriptionKey;

        public CardTypeSO Type => _type;

        public int Cost => _cost;
        public CardRarity Rarity => _rarity;

        public int EffectCount => _effects != null ? _effects.Length : 0;

        public CardDefinitionSO UpgradedForm => _upgradedForm;

        public bool CanUpgrade => _upgradedForm != null;

        public bool Exhausts => _exhausts;

        public bool RequiresChosenTarget => CombatEffectSO.AnyRequiresChosenTarget(_effects);

        public CombatEffectSO GetEffect(int index)
        {
            return _effects[index];
        }

        public void CollectCardValues(in CardTextContext context, List<CardValue> values)
        {
            CombatEffectSO.AddAllCardValues(_effects, context, values);
        }

        public void ApplyEffects(in EffectContext context)
        {
            CombatEffectSO.ApplyAll(_effects, context);
        }
    }
}
