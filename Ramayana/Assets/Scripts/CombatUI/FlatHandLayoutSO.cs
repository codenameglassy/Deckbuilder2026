// Hand layout: a straight row, centred, with a fixed gap between cards that shrinks (and overlaps) when the hand would
// be wider than its area. A hovered card can rise, and its neighbours slide outward, nearest most.
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Hand Layout/Flat", fileName = "FlatHandLayout")]
    public sealed class FlatHandLayoutSO : HandLayoutSO
    {
        [SerializeField] private float _spacing = 12f;
        [SerializeField, Min(0f)] private float _hoverSpread = 40f;
        [SerializeField, Range(0f, 1f)] private float _hoverFalloff = 0.5f;
        [SerializeField] private float _hoverRise;

        public override HandCardPose GetPose(in HandLayoutInput input, int index)
        {
            int count = Mathf.Max(1, input.Count);
            float cardWidth = input.CardSize.x;
            float spacing = _spacing;
            if (count > 1)
            {
                spacing = Mathf.Min(spacing, (input.Width - count * cardWidth) / (count - 1));
            }

            bool hovered = index == input.HoveredIndex;
            float x = (index - (count - 1) * 0.5f) * (cardWidth + spacing)
                + HoverOffset(index, input.HoveredIndex, _hoverSpread, _hoverFalloff);
            float y = input.CardSize.y * 0.5f + (hovered ? _hoverRise : 0f);
            return new HandCardPose(new Vector2(x, y), 0f, 1f);
        }
    }
}
