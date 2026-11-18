// Gives the actor extra Prana this turn, through ICardActions when the actions provide them (the combat model does;
// previews and enemy moves do not, so it does nothing there). Prana can go above the per-turn amount, as the Arrow of
// Surya does in the reference build. On a card or arrow it adds one value, the Prana gained.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Gain Prana", fileName = "GainPranaEffect")]
    public sealed class GainPranaEffectSO : CombatEffectSO
    {
        [SerializeField, Min(1)] private int _amount = 2;

        public int Amount => _amount;

        public override void AddCardValues(in CardTextContext context, List<CardValue> values)
        {
            values.Add(CardValue.Fixed(_amount));
        }

        public override void Apply(in EffectContext context)
        {
            if (context.Actions is ICardActions cards)
            {
                cards.GainPrana(_amount);
            }
        }
    }
}
