// Strategy for arranging the hand: given the card count, the hand width, the card size and the hovered card, it returns
// each card's pose (HandCardPose). Layouts are interchangeable ScriptableObject assets: assign one to the Combat HUD
// style's Hand Layout field, even in Play mode, and the hand re-flows. Each layout is pure maths, so it is covered by
// EditMode tests. FindCardAt decides which card the pointer is over from the resting layout (nothing hovered), not
// from the drawn poses, so spreading around a hovered card can never move the pointer onto another card and jitter;
// the hovered card's zone also reaches up over its zoomed copy. Shared, immutable data; no runtime state.
// In the Editor, changing a value raises Changed so the hand re-flows live.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    public abstract class HandLayoutSO : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _hoverZoneExtraTop = 140f;

        public event Action Changed;

        public float HoverZoneExtraTop => _hoverZoneExtraTop;

        public abstract HandCardPose GetPose(in HandLayoutInput input, int index);

        public int FindCardAt(in HandLayoutInput input, Vector2 point, int currentIndex)
        {
            HandLayoutInput resting = input.WithHovered(-1);
            Vector2 half = input.CardSize * 0.5f;
            int best = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < input.Count; i++)
            {
                HandCardPose pose = GetPose(resting, i);
                float scale = Mathf.Max(pose.Scale, 0.0001f);
                Vector2 local = Rotate(point - pose.Position, -pose.Rotation) / scale;
                float top = half.y + (i == currentIndex ? _hoverZoneExtraTop / scale : 0f);

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
