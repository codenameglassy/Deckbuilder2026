// One special arrow as a Type Object: stable id, localization keys, tier, colour and icon for the quiver, and the
// same effect strategies cards use, run in order when it is fired (the Arrow of Agni hits, then burns). Firing an
// arrow costs no Prana; it needs a chosen target when any of its effects does. CollectValues gathers the numbers its
// description pattern shows, as cards do. Shared and immutable.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using UnityEngine;

namespace JourneyToLanka.Combat.Arrows
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Special Arrow", fileName = "Arrow")]
    public sealed class ArrowDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [SerializeField] private ArrowTier _tier;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Sprite _icon;
        [SerializeField] private CombatEffectSO[] _effects = Array.Empty<CombatEffectSO>();

        public string Id => _id;

        public string NameKey => _nameKey;

        public string DescriptionKey => _descriptionKey;

        public ArrowTier Tier => _tier;

        public Color Color => _color;

        public Sprite Icon => _icon;

        public int EffectCount => _effects != null ? _effects.Length : 0;

        public bool RequiresChosenTarget => CombatEffectSO.AnyRequiresChosenTarget(_effects);

        public CombatEffectSO GetEffect(int index)
        {
            return _effects[index];
        }

        public void CollectValues(in CardTextContext context, List<CardValue> values)
        {
            CombatEffectSO.AddAllCardValues(_effects, context, values);
        }

        public void ApplyEffects(in EffectContext context)
        {
            CombatEffectSO.ApplyAll(_effects, context);
        }
    }
}
