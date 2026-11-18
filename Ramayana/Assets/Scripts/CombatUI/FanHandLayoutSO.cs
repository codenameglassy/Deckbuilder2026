// Hand layout: cards fanned along the top of a large circle, centred. Centres are spaced by a fraction of the card
// width (squeezed so the outer cards stay inside the hand area), each card tilts with the circle's tangent (scaled and
// capped) and sinks with its curve, so the middle card stands upright and highest. A hovered card rises and can
// straighten, and its neighbours spread outward, nearest most. Symmetric about the centre.
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Hand Layout/Fan", fileName = "FanHandLayout")]
    public sealed class FanHandLayoutSO : HandLayoutSO
    {
        [SerializeField, Range(0.2f, 1.2f)] private float _spacingFactor = 0.78f;
        [SerializeField, Min(100f)] private float _arcRadius = 2000f;
        [SerializeField, Range(0f, 3f)] private float _tiltScale = 1f;
        [SerializeField, Range(0f, 30f)] private float _maxTilt = 12f;
        [SerializeField, Min(0f)] private float _hoverSpread = 60f;
        [SerializeField, Range(0f, 1f)] private float _hoverFalloff = 0.55f;
        [SerializeField] private float _hoverRise = 24f;
        [SerializeField] private bool _straightenHovered = true;

        public override HandCardPose GetPose(in HandLayoutInput input, int index)
        {
            int count = Mathf.Max(1, input.Count);
            float cardWidth = input.CardSize.x;
            float step = cardWidth * _spacingFactor;
            if (count > 1)
            {
                step = Mathf.Min(step, Mathf.Max(0f, input.Width - cardWidth) / (count - 1));
            }

            float x = (index - (count - 1) * 0.5f) * step;
            float onArc = Mathf.Clamp(x, -_arcRadius * 0.99f, _arcRadius * 0.99f);
            float drop = _arcRadius - Mathf.Sqrt(_arcRadius * _arcRadius - onArc * onArc);
            float tilt = Mathf.Clamp(-Mathf.Asin(onArc / _arcRadius) * Mathf.Rad2Deg * _tiltScale, -_maxTilt, _maxTilt);

            bool hovered = index == input.HoveredIndex;
            x += HoverOffset(index, input.HoveredIndex, _hoverSpread, _hoverFalloff);
            float y = input.CardSize.y * 0.5f - drop + (hovered ? _hoverRise : 0f);
            float rotation = hovered && _straightenHovered ? 0f : tilt;
            return new HandCardPose(new Vector2(x, y), rotation, 1f);
        }
    }
}
