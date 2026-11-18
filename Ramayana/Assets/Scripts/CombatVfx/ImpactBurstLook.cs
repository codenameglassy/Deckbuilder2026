// The impact burst a hit plays where it lands: a hot core flash, an expanding shockwave ring and a star of rays, torn
// by noise, drawn by the Impact Burst shader on a camera-facing quad. The material picks the colour family (radiant,
// fire or shadow) by its keyword; Shadow Opacity is read only by the shadow family, where it darkens the smoke core.
// Holds the builder-made material it draws with and every value the shader reads; Apply writes them into an
// instance's own material copy at spawn, scaled by the hit's intensity, with a per-spawn seed that turns the burst and
// varies its rays. Serializable, embedded in HitVfxProfileSO; read-only at runtime.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public sealed class ImpactBurstLook : IQuadVfxLook
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int CoreSizeId = Shader.PropertyToID("_CoreSize");
        private static readonly int CoreFadeId = Shader.PropertyToID("_CoreFade");
        private static readonly int RingStartId = Shader.PropertyToID("_RingStart");
        private static readonly int RingEndId = Shader.PropertyToID("_RingEnd");
        private static readonly int RingWidthId = Shader.PropertyToID("_RingWidth");
        private static readonly int RingFadeId = Shader.PropertyToID("_RingFade");
        private static readonly int RayCountId = Shader.PropertyToID("_RayCount");
        private static readonly int RaySharpnessId = Shader.PropertyToID("_RaySharpness");
        private static readonly int RayLengthId = Shader.PropertyToID("_RayLength");
        private static readonly int RayFadeId = Shader.PropertyToID("_RayFade");
        private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int CameraPushId = Shader.PropertyToID("_CameraPush");
        private static readonly int SoftFadeId = Shader.PropertyToID("_SoftFade");
        private static readonly int ShadowOpacityId = Shader.PropertyToID("_ShadowOpacity");

        [Tooltip("Builder-made material using the Impact Burst shader. Its shader and keywords are never changed at runtime.")]
        [SerializeField] private Material _material;

        [Header("Timing and Size")]
        [Tooltip("Diameter in world units for an ordinary hit.")]
        [SerializeField, Min(0.01f)] private float _size = 1.8f;
        [Tooltip("Seconds for an ordinary hit, on game time (freezes with hit-stop).")]
        [SerializeField, Min(0.01f)] private float _duration = 0.32f;

        [Header("Colour")]
        [SerializeField, ColorUsage(false, true)] private Color _color = new Color(3.2f, 1.5f, 0.45f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color _coreColor = new Color(5f, 4.2f, 3.2f, 1f);
        [SerializeField, Min(0f)] private float _intensity = 1f;

        [Header("Core Flash")]
        [Tooltip("Radius of the flash as a share of the quad's radius.")]
        [SerializeField, Range(0.01f, 1f)] private float _coreSize = 0.3f;
        [Tooltip("How fast the flash dies: higher fades sooner.")]
        [SerializeField, Min(0.1f)] private float _coreFade = 3f;

        [Header("Shockwave Ring")]
        [SerializeField, Range(0f, 1f)] private float _ringStart = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _ringEnd = 0.85f;
        [SerializeField, Range(0.005f, 0.5f)] private float _ringWidth = 0.07f;
        [SerializeField, Min(0.1f)] private float _ringFade = 1.4f;

        [Header("Rays")]
        [Tooltip("Number of rays; 0 hides them.")]
        [SerializeField, Range(0, 16)] private int _rayCount = 7;
        [Tooltip("Higher makes thinner rays.")]
        [SerializeField, Range(1f, 64f)] private float _raySharpness = 18f;
        [SerializeField, Range(0.05f, 1f)] private float _rayLength = 0.95f;
        [SerializeField, Min(0.1f)] private float _rayFade = 2f;

        [Header("Breakup")]
        [SerializeField, Range(0f, 1f)] private float _noiseStrength = 0.35f;
        [SerializeField, Min(0.1f)] private float _noiseScale = 5f;

        [Header("Placement")]
        [Tooltip("World units the burst is drawn toward the camera, so it sits in front of the fighter it hits.")]
        [SerializeField, Min(0f)] private float _cameraPush = 0.35f;
        [Tooltip("World units over which the burst fades where it meets scene geometry. Needs the depth texture.")]
        [SerializeField, Min(0f)] private float _softFade = 0.25f;

        [Header("Shadow Family")]
        [Tooltip("How much the smoke core darkens what is behind it. Read only by shadow family materials.")]
        [SerializeField, Range(0f, 1f)] private float _shadowOpacity = 0.6f;

        public Material Material => _material;

        public bool IsEnabled => _material != null;

        public float Size => _size;

        public float Duration => _duration;

        public void Apply(Material target, in VfxSpawnContext context)
        {
            if (target == null)
            {
                return;
            }

            target.SetColor(ColorId, _color);
            target.SetColor(CoreColorId, _coreColor);
            target.SetFloat(IntensityId, _intensity * context.Intensity);
            target.SetFloat(SeedId, context.Seed);
            target.SetFloat(CoreSizeId, _coreSize);
            target.SetFloat(CoreFadeId, _coreFade);
            target.SetFloat(RingStartId, _ringStart);
            target.SetFloat(RingEndId, _ringEnd);
            target.SetFloat(RingWidthId, _ringWidth);
            target.SetFloat(RingFadeId, _ringFade);
            target.SetFloat(RayCountId, _rayCount);
            target.SetFloat(RaySharpnessId, _raySharpness);
            target.SetFloat(RayLengthId, _rayLength);
            target.SetFloat(RayFadeId, _rayFade);
            target.SetFloat(NoiseStrengthId, _noiseStrength);
            target.SetFloat(NoiseScaleId, _noiseScale);
            target.SetFloat(CameraPushId, _cameraPush);
            target.SetFloat(SoftFadeId, _softFade);
            target.SetFloat(ShadowOpacityId, _shadowOpacity);
        }
    }
}
