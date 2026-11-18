// Everything a hand layout strategy needs to place the cards: how many there are, the width the hand may use, the
// size of one card, and which card is hovered (-1 for none). Plain data, so layouts are pure and testable.
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    public readonly struct HandLayoutInput
    {
        public HandLayoutInput(int count, float width, Vector2 cardSize, int hoveredIndex)
        {
            Count = count;
            Width = width;
            CardSize = cardSize;
            HoveredIndex = hoveredIndex;
        }

        public int Count { get; }

        public float Width { get; }

        public Vector2 CardSize { get; }

        public int HoveredIndex { get; }

        public HandLayoutInput WithHovered(int hoveredIndex)
        {
            return new HandLayoutInput(Count, Width, CardSize, hoveredIndex);
        }
    }
}
