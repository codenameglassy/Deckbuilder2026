// Hand layout: a straight row, centred, with a fixed gap between cards that shrinks (and overlaps) when the hand would
// be wider than its area. A hovered card takes the shared hover pose in its own slot (enlarged, fully above the
// hand's bottom edge), and its neighbours slide outward, nearest most.
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Hand Layout/Flat", fileName = "FlatHandLayout")]
    public sealed class FlatHandLayoutSO : HandLayoutSO
    {
        [SerializeField] private float _spacing = 12f;
        [SerializeField, Min(0f)] private float _hoverSpread = 40f;
        [SerializeField, Range(0f, 1f)] private float _hoverFalloff = 0.5f;

        public override HandCardPose GetPose(in HandLayoutInput input, int index)
        {
            int count = Mathf.Max(1, input.Count);
            float cardWidth = input.CardSize.x;
            float spacing = _spacing;
            if (count > 1)
            {
                spacing = Mathf.Min(spacing, (input.Width - count * cardWidth) / (count - 1));
            }

            float x = (index - (count - 1) * 0.5f) * (cardWidth + spacing);
            if (index == input.HoveredIndex)
            {
                return HoveredPose(input, x);
            }

            x += HoverOffset(index, input.HoveredIndex, _hoverSpread, _hoverFalloff);
            return new HandCardPose(new Vector2(x, input.CardSize.y * 0.5f), 0f, 1f);
        }
    }
}
