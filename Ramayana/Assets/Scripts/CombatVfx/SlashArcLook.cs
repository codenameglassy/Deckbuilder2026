// The slash a blow leaves across the fighter it hits: a curved blade of light swept from the attacker's side to the
// far side, with a hot core, a tapering tail and tapered ends, drawn by the Slash Arc shader on a camera-facing quad.
// The shader turns the arc to follow the blow's direction on screen (attacker to target), jittered a little per spawn
// so repeated hits never line up. Plays only for hits with an attacker; a profile can turn it off (burns). Holds the
// builder-made material and every value the shader reads; Apply writes them into an instance's own material copy at
// spawn. Serializable, embedded in HitVfxProfileSO; read-only at runtime.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public sealed class SlashArcLook : IQuadVfxLook
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        private static readonly int WidthId = Shader.PropertyToID("_Width");
        private static readonly int CoreShareId = Shader.PropertyToID("_CoreShare");
        private static readonly int BowId = Shader.PropertyToID("_Bow");
        private static readonly int SweepPortionId = Shader.PropertyToID("_SweepPortion");
        private static readonly int TailLengthId = Shader.PropertyToID("_TailLength");
        private static readonly int FadePowerId = Shader.PropertyToID("_FadePower");
        private static readonly int AngleJitterId = Shader.PropertyToID("_AngleJitter");
        private static readonly int CameraPushId = Shader.PropertyToID("_CameraPush");
        private static readonly int SoftFadeId = Shader.PropertyToID("_SoftFade");

        [SerializeField] private bool _enabled = true;
        [Tooltip("Builder-made material using the Slash Arc shader. Its shader is never changed at runtime.")]
        [SerializeField] private Material _material;

        [Header("Timing and Size")]
        [Tooltip("Length of the slash in world units for an ordinary hit.")]
        [SerializeField, Min(0.01f)] private float _size = 2.2f;
        [Tooltip("Seconds for an ordinary hit, on game time (freezes with hit-stop).")]
        [SerializeField, Min(0.01f)] private float _duration = 0.28f;
        [Tooltip("Share of the duration spent sweeping; the rest fades.")]
        [SerializeField, Range(0.05f, 1f)] private float _sweepPortion = 0.45f;

        [Header("Colour")]
        [SerializeField, ColorUsage(false, true)] private Color _color = new Color(3f, 1.3f, 0.4f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color _coreColor = new Color(5f, 4.5f, 3.8f, 1f);
        [SerializeField, Min(0f)] private float _intensity = 1f;

        [Header("Shape")]
        [Tooltip("Half thickness of the blade as a share of the quad's radius.")]
        [SerializeField, Range(0.005f, 0.4f)] private float _width = 0.09f;
        [Tooltip("Share of the blade's thickness that is the hot core.")]
        [SerializeField, Range(0.05f, 1f)] private float _coreShare = 0.35f;
        [Tooltip("How far the arc bows away from a straight cut, as a share of the quad's radius.")]
        [SerializeField, Range(0.05f, 0.9f)] private float _bow = 0.35f;
        [Tooltip("Length of the fading tail behind the sweeping head, in quad radii.")]
        [SerializeField, Range(0.1f, 2f)] private float _tailLength = 0.9f;
        [Tooltip("Higher fades the slash out faster after it sweeps.")]
        [SerializeField, Min(0.1f)] private float _fadePower = 1.5f;
        [Tooltip("Largest random turn in degrees, either side of the blow's direction.")]
        [SerializeField, Range(0f, 90f)] private float _angleJitter = 25f;

        [Header("Placement")]
        [Tooltip("World units the slash is drawn toward the camera, in front of the impact burst.")]
        [SerializeField, Min(0f)] private float _cameraPush = 0.5f;
        [Tooltip("World units over which the slash fades where it meets scene geometry. Needs the depth texture.")]
        [SerializeField, Min(0f)] private float _softFade = 0.25f;

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
            target.SetColor(CoreColorId, _coreColor);
            target.SetFloat(IntensityId, _intensity * context.Intensity);
            target.SetFloat(SeedId, context.Seed);
            target.SetVector(DirectionId, context.ShaderDirection);
            target.SetFloat(WidthId, _width);
            target.SetFloat(CoreShareId, _coreShare);
            target.SetFloat(BowId, _bow);
            target.SetFloat(SweepPortionId, _sweepPortion);
            target.SetFloat(TailLengthId, _tailLength);
            target.SetFloat(FadePowerId, _fadePower);
            target.SetFloat(AngleJitterId, _angleJitter * Mathf.Deg2Rad);
            target.SetFloat(CameraPushId, _cameraPush);
            target.SetFloat(SoftFadeId, _softFade);
        }
    }
}
