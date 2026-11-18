// Restores health to the actor, up to its maximum (Sanjeevani Leaf). On a card it adds one value, the health restored.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Heal", fileName = "HealEffect")]
    public sealed class HealEffectSO : CombatEffectSO
    {
        [SerializeField, Min(1)] private int _amount = 8;

        public int Amount => _amount;

        public override void AddCardValues(in CardTextContext context, List<CardValue> values)
        {
            values.Add(CardValue.Fixed(_amount));
        }

        public override void Apply(in EffectContext context)
        {
            ICombatant source = context.Source;
            if (source != null && source.IsAlive)
            {
                context.Actions.Heal(source, _amount);
            }
        }
    }
}
