// One step of an enemy's pattern, such as "attack 7" or "defend 6", built from the same effect strategies as cards.
// The enemy's chosen target is the player, so damage effects use the Chosen Target rule.
using System;
using JourneyToLanka.Combat.Effects;
using UnityEngine;

namespace JourneyToLanka.Combat.Enemies
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Enemy Move", fileName = "EnemyMove")]
    public sealed class EnemyMoveSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private CombatEffectSO[] _effects = Array.Empty<CombatEffectSO>();

        public string Id => _id;

        public int EffectCount => _effects != null ? _effects.Length : 0;

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
