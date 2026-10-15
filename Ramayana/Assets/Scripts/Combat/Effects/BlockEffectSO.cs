// Grants Kavach to the actor. In the reference build Kavach always goes to whoever plays the card or makes the move.
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Block", fileName = "BlockEffect")]
    public sealed class BlockEffectSO : CombatEffectSO
    {
        [SerializeField, Min(0)] private int _amount = 5;

        public int Amount => _amount;

        public override void Apply(in EffectContext context)
        {
            ICombatant source = context.Source;
            if (source != null && source.IsAlive)
            {
                context.Actions.GainBlock(source, _amount);
            }
        }
    }
}
