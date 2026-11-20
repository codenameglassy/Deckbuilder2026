// How many cards a draw or discard stack shows, as plain C# so it is EditMode tested. An empty pile shows none (the
// HUD draws an empty outline instead); otherwise one card for the first few, one more for every further batch of
// cards, never more than the style's cap, so a stack thickens as the pile grows without counting every card.
using System;

namespace JourneyToLanka.CombatUI
{
    public static class PileStackMath
    {
        public static int LayerCount(int cards, int maxLayers, int cardsPerLayer)
        {
            if (cards <= 0 || maxLayers <= 0)
            {
                return 0;
            }

            int perLayer = Math.Max(cardsPerLayer, 1);
            return Math.Min(maxLayers, 1 + (cards - 1) / perLayer);
        }
    }
}
