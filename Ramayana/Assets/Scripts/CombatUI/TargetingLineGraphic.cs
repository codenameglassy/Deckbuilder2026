// The targeting line's mesh as a UGUI graphic: a strip of segments along t in [0, 1] plus one arrowhead quad, built
// once (and again only if the segment count changes). The vertices carry only uv0 codes; the Targeting Line shader
// places them along a Bezier curve from material values, so moving the line never rebuilds this mesh or its canvas.
// The placeholder positions cover the graphic's whole rect with real area (each strip vertex sits on the rect's top
// or bottom edge by side, the arrowhead quad on its corners), so the canvas never sees a flat, zero-height mesh it
// could skip; the shader replaces every position anyway. Never a raycast target.
// The arrowhead quad reaches HeadMargin head lengths behind the base and beyond the tip. Head vertices are coded
// HeadCodeOffset + position, so even the rearmost (HeadCodeOffset - HeadMargin = 3.4) stays far above the ribbon's
// codes (0 to 1); the shader splits them at HEAD_CODE_THRESHOLD (2). HeadCodeOffset and HeadMargin must match the
// shader's HEAD_CODE_OFFSET and HEAD_MARGIN.
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Journey to Lanka/UI/Targeting Line Graphic")]
    public sealed class TargetingLineGraphic : Graphic
    {
        private const int MinSegments = 8;
        private const int MaxSegments = 128;
        private const float HeadCodeOffset = 4f;
        private const float HeadMargin = 0.6f;
        private const float HeadBack = -HeadMargin;
        private const float HeadFront = 1f + HeadMargin;

        [SerializeField, Range(MinSegments, MaxSegments)] private int _segments = 48;

        public void SetSegments(int segments)
        {
            int clamped = Mathf.Clamp(segments, MinSegments, MaxSegments);
            if (clamped == _segments)
            {
                return;
            }

            _segments = clamped;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = rectTransform.rect;
            Color32 tint = color;

            for (int i = 0; i <= _segments; i++)
            {
                float t = (float)i / _segments;
                float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                vertices.AddVert(new Vector3(x, rect.yMin, 0f), tint, new Vector4(t, -1f, 0f, 0f));
                vertices.AddVert(new Vector3(x, rect.yMax, 0f), tint, new Vector4(t, 1f, 0f, 0f));
            }

            for (int i = 0; i < _segments; i++)
            {
                int a = i * 2;
                vertices.AddTriangle(a, a + 1, a + 3);
                vertices.AddTriangle(a, a + 3, a + 2);
            }

            int head = (_segments + 1) * 2;
            vertices.AddVert(new Vector3(rect.xMin, rect.yMin, 0f), tint, new Vector4(HeadCodeOffset + HeadBack, -1f, 0f, 0f));
            vertices.AddVert(new Vector3(rect.xMin, rect.yMax, 0f), tint, new Vector4(HeadCodeOffset + HeadBack, 1f, 0f, 0f));
            vertices.AddVert(new Vector3(rect.xMax, rect.yMin, 0f), tint, new Vector4(HeadCodeOffset + HeadFront, -1f, 0f, 0f));
            vertices.AddVert(new Vector3(rect.xMax, rect.yMax, 0f), tint, new Vector4(HeadCodeOffset + HeadFront, 1f, 0f, 0f));
            vertices.AddTriangle(head, head + 1, head + 3);
            vertices.AddTriangle(head, head + 3, head + 2);
        }
    }
}
