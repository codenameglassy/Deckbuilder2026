// The final multipliers a combo chain puts on one card, as whole percents: one for its damage, one for the Kavach
// it gains itself. 100 is no change. Damage is scaled after every damage modifier (Focus, statuses, relics) and
// Kavach is scaled on the card's own GainBlock only; both round down. The default value is neutral (100 and 100),
// so an EffectContext or CardTextContext built without a chain, as enemy moves, arrows and statuses are, scales
// nothing. Plain immutable data.
using System;

namespace JourneyToLanka.Combat.Effects
{
    public readonly struct ChainScale
    {
        public const int NeutralPercent = 100;

        // Stored as offsets from 100 so that default(ChainScale) is neutral.
        private readonly int _damageOffset;
        private readonly int _kavachOffset;

        public ChainScale(int damagePercent, int kavachPercent)
        {
            _damageOffset = Math.Max(0, damagePercent) - NeutralPercent;
            _kavachOffset = Math.Max(0, kavachPercent) - NeutralPercent;
        }

        public static ChainScale None => default;

        public int DamagePercent => NeutralPercent + _damageOffset;

        public int KavachPercent => NeutralPercent + _kavachOffset;

        public bool IsNeutral => _damageOffset == 0 && _kavachOffset == 0;

        public int ScaleDamage(int amount)
        {
            return Apply(amount, DamagePercent);
        }

        public int ScaleKavach(int amount)
        {
            return Apply(amount, KavachPercent);
        }

        // floor(amount x percent / 100) for non-negative amounts; zero or negative amounts pass through unchanged.
        public static int Apply(int amount, int percent)
        {
            if (percent == NeutralPercent || amount <= 0)
            {
                return amount;
            }

            if (percent <= 0)
            {
                return 0;
            }

            long scaled = (long)amount * percent / NeutralPercent;
            return scaled > int.MaxValue ? int.MaxValue : (int)scaled;
        }
    }
}
