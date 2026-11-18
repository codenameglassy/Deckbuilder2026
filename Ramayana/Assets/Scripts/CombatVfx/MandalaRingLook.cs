// The cast mandala that blooms on the ground under the one who acts: two rings, a ring of lotus petals and a ring of
// dots that bloom outward, turn slowly and fade, drawn by the Mandala Ring shader on a ground-aligned quad. Played at
// Rama's feet when he plays a card or fires an arrow, in the colour of that card type or arrow. Holds the builder-made
// material and every value the shader reads; Apply writes them into an instance's own material copy at spawn, with a
// seed that sets its starting turn. Serializable, embedded in HitVfxProfileSO; read-only at runtime.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public sealed class MandalaRingLook : IQuadVfxLook
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int PetalCountId = Shader.PropertyToID("_PetalCount");
        private static readonly int PetalDepthId = Shader.PropertyToID("_PetalDepth");
        private static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
        private static readonly int BloomPortionId = Shader.PropertyToID("_BloomPortion");
        private static readonly int SpinId = Shader.PropertyToID("_Spin");
        private static readonly int FadePowerId = Shader.PropertyToID("_FadePower");
        private static readonly int SoftFadeId = Shader.PropertyToID("_SoftFade");

        [SerializeField] private bool _enabled = true;
        [Tooltip("Builder-made material using the Mandala Ring shader. Its shader is never changed at runtime.")]
        [SerializeField] private Material _material;

        [Header("Timing and Size")]
        [Tooltip("Diameter in world units.")]
        [SerializeField, Min(0.01f)] private float _size = 2.6f;
        [Tooltip("Seconds on game time.")]
        [SerializeField, Min(0.01f)] private float _duration = 0.6f;
        [Tooltip("Share of the duration spent blooming outward.")]
        [SerializeField, Range(0.05f, 1f)] private float _bloomPortion = 0.35f;

        [Header("Colour")]
        [SerializeField, ColorUsage(false, true)] private Color _color = new Color(3f, 1.8f, 0.6f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color _glowColor = new Color(0.8f, 0.4f, 0.12f, 1f);
        [SerializeField, Min(0f)] private float _intensity = 1f;

        [Header("Pattern")]
        [SerializeField, Range(3, 16)] private int _petalCount = 8;
        [Tooltip("How far the petals reach between the inner and outer rings, as a share of the radius.")]
        [SerializeField, Range(0.05f, 0.5f)] private float _petalDepth = 0.28f;
        [Tooltip("Line thickness as a share of the radius.")]
        [SerializeField, Range(0.005f, 0.1f)] private float _lineWidth = 0.025f;
        [Tooltip("Turns over the whole duration.")]
        [SerializeField, Range(-1f, 1f)] private float _spin = 0.15f;
        [Tooltip("Higher fades the mandala out sooner.")]
        [SerializeField, Min(0.1f)] private float _fadePower = 1.6f;

        [Header("Placement")]
        [Tooltip("World units over which the mandala fades where it meets the ground. Needs the depth texture.")]
        [SerializeField, Min(0f)] private float _softFade = 0.2f;

        public Material Material => _material;

        public bool IsEnabled => _enabled && _material != null;

        public float Size => _size;

        public float Duration => _duration;

        public void Apply(Material target, in VfxSpawnContext context)
        {
            if (target == null)
            {
                return;
            }

            target.SetColor(ColorId, _color);
            target.SetColor(GlowColorId, _glowColor);
            target.SetFloat(IntensityId, _intensity * context.Intensity);
            target.SetFloat(SeedId, context.Seed);
            target.SetFloat(PetalCountId, _petalCount);
            target.SetFloat(PetalDepthId, _petalDepth);
            target.SetFloat(LineWidthId, _lineWidth);
            target.SetFloat(BloomPortionId, _bloomPortion);
            target.SetFloat(SpinId, _spin);
            target.SetFloat(FadePowerId, _fadePower);
            target.SetFloat(SoftFadeId, _softFade);
        }
    }
}
