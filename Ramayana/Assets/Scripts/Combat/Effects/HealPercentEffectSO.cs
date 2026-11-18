// Restores a share of the actor's maximum health, rounded down, up to the maximum (the Sanjivani Arrow heals 20%).
// On a card or arrow it adds one value, the percentage.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Heal Percent", fileName = "HealPercentEffect")]
    public sealed class HealPercentEffectSO : CombatEffectSO
    {
        [SerializeField, Range(1, 100)] private int _percent = 20;

        public int Percent => _percent;

        public override void AddCardValues(in CardTextContext context, List<CardValue> values)
        {
            values.Add(CardValue.Fixed(_percent));
        }

        public override void Apply(in EffectContext context)
        {
            ICombatant source = context.Source;
            if (source == null || !source.IsAlive)
            {
                return;
            }

            int amount = context.Actions.GetMaxHealth(source) * _percent / 100;
            if (amount > 0)
            {
                context.Actions.Heal(source, amount);
            }
        }
    }
}
