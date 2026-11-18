// At the end of every round the status burns its owner for its full amount, straight through Kavach and untouched by
// damage modifiers, then keeps only a share of itself (Dahan keeps half, rounded down), so a stack burns for about
// twice its size in total.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Decay/Burn", fileName = "BurnDecay")]
    public sealed class BurnDecaySO : StatusDecaySO
    {
        [SerializeField, Range(0, 100)] private int _keepPercent = 50;

        public int KeepPercent => _keepPercent;

        public override void OnRoundEnd(ref StatusInstance status, in StatusTickContext context)
        {
            if (status.Amount <= 0)
            {
                return;
            }

            if (context.Actions != null && context.Owner != null && context.Owner.IsAlive)
            {
                context.Actions.LoseHealth(context.Owner, status.Amount);
            }

            status.Amount = status.Amount * _keepPercent / 100;
        }
    }
}
