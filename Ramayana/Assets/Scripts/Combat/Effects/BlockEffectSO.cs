// Grants Kavach to the actor. In the reference build Kavach always goes to whoever plays the card or makes the move.
// As an intent it shows the Kavach the actor will gain. On a card it adds one value, the Kavach gained.
// Marked Retain, the actor's Kavach is not cleared at its next turn start (Ancient Endurance), once.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Block", fileName = "BlockEffect")]
    public sealed class BlockEffectSO : CombatEffectSO
    {
        [SerializeField, Min(0)] private int _amount = 5;
        [Tooltip("Keep the actor's Kavach through its next turn start instead of clearing it, once.")]
        [SerializeField] private bool _retain;

        public int Amount => _amount;

        public bool Retain => _retain;

        public override void AddToIntent(in IntentContext context, ref MoveIntent intent)
        {
            intent.AddBlock(_amount);
        }

        public override void AddCardValues(in CardTextContext context, List<CardValue> values)
        {
            values.Add(CardValue.Fixed(_amount));
        }

        public override void Apply(in EffectContext context)
        {
            ICombatant source = context.Source;
            if (source != null && source.IsAlive)
            {
                context.Actions.GainBlock(source, _amount);
                if (_retain)
                {
                    context.Actions.RetainBlock(source);
                }
            }
        }
    }
}
