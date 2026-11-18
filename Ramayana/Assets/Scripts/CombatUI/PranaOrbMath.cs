// The rules behind the Prana orb row, as plain C# so they are EditMode tested: how many orbs to show (the per-turn
// amount, more while bonus Prana is held, never above the style's cap) and what each orb previews for a card's cost.
// A card spends the rightmost lit orbs; when it costs more than the player has, every lit orb is marked to spend and
// the empty orbs it still needs are marked as the deficit.
using System;

namespace JourneyToLanka.CombatUI
{
    public static class PranaOrbMath
    {
        public static int OrbCount(int perTurn, int current, int maxOrbs)
        {
            return Math.Clamp(Math.Max(perTurn, current), 0, Math.Max(maxOrbs, 0));
        }

        public static PranaOrbPreview Preview(int index, int current, int cost)
        {
            if (cost <= 0 || index < 0)
            {
                return PranaOrbPreview.None;
            }

            if (cost <= current)
            {
                return index >= current - cost && index < current ? PranaOrbPreview.Spend : PranaOrbPreview.None;
            }

            if (index < current)
            {
                return PranaOrbPreview.Spend;
            }

            return index < cost ? PranaOrbPreview.Deficit : PranaOrbPreview.None;
        }
    }
}
