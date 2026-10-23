// Draws cards for the actor, through ICardActions when the actions in the context provide them (the combat model
// does; enemy moves have no deck, so the effect does nothing there). Drawing follows the deck rules: reshuffle
// the discard pile when the draw pile runs out, stop at the hand limit.
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Draw Cards", fileName = "DrawCardsEffect")]
    public sealed class DrawCardsEffectSO : CombatEffectSO
    {
        [SerializeField, Min(1)] private int _count = 1;

        public int Count => _count;

        public override void Apply(in EffectContext context)
        {
            if (context.Actions is ICardActions cards)
            {
                cards.DrawCards(_count);
            }
        }
    }
}
