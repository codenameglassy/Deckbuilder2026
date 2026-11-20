// Strategy for arranging the hand: given the card count, the hand width, the card size and the hovered card, it returns
// each card's pose (HandCardPose). Layouts are interchangeable ScriptableObject assets: assign one to the Combat HUD
// style's Hand Layout field, even in Play mode, and the hand re-flows. Each layout is pure maths, so it is covered by
// EditMode tests. Every layout raises a hovered card the same way (HoveredPose): it straightens, grows by Hover Scale
// and lifts so its bottom edge sits Hover Bottom above the hand area's bottom, staying in its own slot, so the whole
// card is readable. FindCardAt decides which card the pointer is over from the resting layout (nothing hovered), not
// from the drawn poses, so spreading around a hovered card can never move the pointer onto another card and jitter;
// the hovered card's zone also reaches up to the top of its raised pose. Shared, immutable data; no runtime state.
// In the Editor, changing a value raises Changed so the hand re-flows live.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    public abstract class HandLayoutSO : ScriptableObject
    {
        [SerializeField, Range(1f, 2f)] private float _hoverScale = 1.35f;
        [SerializeField] private float _hoverBottom;

        public event Action Changed;

        public float HoverScale => _hoverScale;

        public float HoverBottom => _hoverBottom;

        public abstract HandCardPose GetPose(in HandLayoutInput input, int index);

        public int FindCardAt(in HandLayoutInput input, Vector2 point, int currentIndex)
        {
            HandLayoutInput resting = input.WithHovered(-1);
            HandLayoutInput raised = input.WithHovered(currentIndex);
            Vector2 half = input.CardSize * 0.5f;
            int best = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < input.Count; i++)
            {
                HandCardPose pose = GetPose(resting, i);
                float scale = Mathf.Max(pose.Scale, 0.0001f);
                Vector2 local = Rotate(point - pose.Position, -pose.Rotation) / scale;
                float top = half.y;
                if (i == currentIndex)
                {
                    HandCardPose hovered = GetPose(raised, i);
                    Vector2 hoveredTop = hovered.Position + new Vector2(0f, half.y * hovered.Scale);
                    top = Mathf.Max(top, Rotate(hoveredTop - pose.Position, -pose.Rotation).y / scale);
                }

                if (Mathf.Abs(local.x) > half.x || local.y < -half.y || local.y > top)
                {
                    continue;
                }

                float distance = Mathf.Abs(local.x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        protected HandCardPose HoveredPose(in HandLayoutInput input, float x)
        {
            float y = input.CardSize.y * 0.5f * _hoverScale + _hoverBottom;
            return new HandCardPose(new Vector2(x, y), 0f, _hoverScale);
        }

        protected static float HoverOffset(int index, int hoveredIndex, float spread, float falloff)
        {
            if (hoveredIndex < 0 || index == hoveredIndex)
            {
                return 0f;
            }

            int distance = Mathf.Abs(index - hoveredIndex);
            return Mathf.Sign(index - hoveredIndex) * spread * Mathf.Pow(falloff, distance - 1);
        }

        protected static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        protected virtual void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
