// How Kavach looks on a fighter. The shell is a hull: each listed body mesh drawn again by the Kavach Shell shader,
// pushed out along baked smooth normals by Thickness (world units), so a cube gets a slightly larger cube and a
// character a glowing skin that follows its silhouette and animation. The look is a force field: a fresnel rim, a
// honeycomb laid out in the mesh's own space (so it sticks to the body) that shows mostly toward the edges, a faint
// fill, and a shimmer rising through it.
// Its strength follows the fighter's Kavach (Min Strength at 1, full at Full Block) and eases between values by Fade
// Speed; gaining Kavach flashes it and swells it outward (Gain Swell, settling by Swell Decay). A hit Kavach absorbs
// sends a ripple out across the hull from the side the blow came from (Ripple Band wide, travelling Ripple Reach),
// stronger the more it absorbed. A hit that breaks Kavach to zero dissolves the hull in cells and throws Shards, a
// spark burst drawn with the Kavach Shards material (same property contract as Spark Burst). Kavach that wears off
// at turn start just fades. Two particle prefab accents (such as Polygon Arsenal effects) play over the hull: Gain
// Accent every time a fighter gains Kavach and Break Accent where a hit breaks it. Shared flyweight with no runtime
// state; ApplyStatic writes the values that do not change per frame into a hull's own material copies, and Changed
// (Editor only) lets hulls re-apply them live.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat VFX/Kavach VFX Profile", fileName = "KavachVfxProfile")]
    public sealed class KavachVfxProfileSO : ScriptableObject
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimPowerId = Shader.PropertyToID("_RimPower");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int PatternDensityId = Shader.PropertyToID("_PatternDensity");
        private static readonly int PatternWidthId = Shader.PropertyToID("_PatternWidth");
        private static readonly int PatternStrengthId = Shader.PropertyToID("_PatternStrength");
        private static readonly int ShimmerSpeedId = Shader.PropertyToID("_ShimmerSpeed");
        private static readonly int SoftFadeId = Shader.PropertyToID("_SoftFade");
        private static readonly int RippleColorId = Shader.PropertyToID("_RippleColor");
        private static readonly int RippleBandId = Shader.PropertyToID("_RippleBand");
        private static readonly int RippleReachId = Shader.PropertyToID("_RippleReach");
        private static readonly int BreakColorId = Shader.PropertyToID("_BreakColor");
        private static readonly int ShardDensityId = Shader.PropertyToID("_ShardDensity");

        [Header("Hull")]
        [Tooltip("Builder-made material using the Kavach Shell shader. Its shader is never changed at runtime.")]
        [SerializeField] private Material _shellMaterial;
        [Tooltip("How far the hull stands off the body, in world units.")]
        [SerializeField, Range(0.005f, 0.5f)] private float _thickness = 0.07f;
        [Tooltip("Colour of the honeycomb lines and the faint fill.")]
        [SerializeField, ColorUsage(false, true)] private Color _color = new Color(0.25f, 0.95f, 1.5f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color _rimColor = new Color(0.6f, 1.9f, 2.5f, 1f);
        [Tooltip("Higher keeps the rim glow closer to the hull's silhouette.")]
        [SerializeField, Range(0.5f, 8f)] private float _rimPower = 2.5f;
        [Tooltip("Faint glow across the hull, strongest toward its edges. Keep low so the fighter stays clear.")]
        [SerializeField, Range(0f, 0.5f)] private float _fill = 0.06f;
        [Tooltip("Honeycomb cells per world unit.")]
        [SerializeField, Range(0.25f, 12f)] private float _patternDensity = 4f;
        [Tooltip("Honeycomb line width as a share of a cell.")]
        [SerializeField, Range(0.005f, 0.2f)] private float _patternWidth = 0.04f;
        [Tooltip("Honeycomb brightness at the hull's edges; it fades toward the middle.")]
        [SerializeField, Range(0f, 2f)] private float _patternStrength = 0.6f;
        [Tooltip("How fast the shimmer rises through the honeycomb, on game time.")]
        [SerializeField, Min(0f)] private float _shimmerSpeed = 0.6f;
        [Tooltip("World units over which the hull fades where it cuts into the ground. Keep below Thickness. Needs the depth texture.")]
        [SerializeField, Min(0f)] private float _softFade = 0.03f;

        [Header("Strength")]
        [Tooltip("Kavach at which the hull is at full strength.")]
        [SerializeField, Min(1)] private int _fullBlock = 20;
        [Tooltip("Strength at 1 Kavach.")]
        [SerializeField, Range(0.05f, 1f)] private float _minStrength = 0.4f;
        [Tooltip("Strength change per second when Kavach rises, falls or wears off.")]
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 6f;

        [Header("Gain")]
        [Tooltip("Extra brightness when Kavach is gained.")]
        [SerializeField, Range(0f, 4f)] private float _gainFlash = 1f;
        [Tooltip("Flash lost per second.")]
        [SerializeField, Min(0.1f)] private float _flashDecay = 4f;
        [Tooltip("Extra thickness when Kavach is gained, as a multiple of Thickness.")]
        [SerializeField, Range(0f, 6f)] private float _gainSwell = 1.5f;
        [Tooltip("Swell lost per second.")]
        [SerializeField, Min(0.1f)] private float _swellDecay = 5f;

        [Header("Absorb Ripple")]
        [SerializeField, ColorUsage(false, true)] private Color _rippleColor = new Color(2f, 4f, 5f, 1f);
        [SerializeField, Min(0.05f)] private float _rippleDuration = 0.45f;
        [Tooltip("Width of the ripple ring in world units.")]
        [SerializeField, Range(0.05f, 2f)] private float _rippleBand = 0.3f;
        [Tooltip("How far the ripple travels from its origin over its duration, in world units.")]
        [SerializeField, Range(0.2f, 6f)] private float _rippleReach = 2.2f;
        [Tooltip("Ripple strength per point of Kavach absorbed.")]
        [SerializeField, Min(0f)] private float _rippleStrengthPerBlock = 0.12f;
        [Tooltip("Weakest ripple, for a hit that absorbs 1.")]
        [SerializeField, Range(0f, 1f)] private float _rippleMinStrength = 0.4f;

        [Header("Shatter")]
        [Tooltip("Seconds for the hull to dissolve when Kavach breaks, on game time.")]
        [SerializeField, Min(0.05f)] private float _shatterDuration = 0.35f;
        [Tooltip("Cells per world unit that dissolve one by one.")]
        [SerializeField, Range(0.5f, 12f)] private float _shardDensity = 4f;
        [SerializeField, ColorUsage(false, true)] private Color _breakColor = new Color(2.5f, 4.5f, 5f, 1f);
        [SerializeField] private SparkBurstLook _shards = new SparkBurstLook();

        [Header("Accents")]
        [Tooltip("Played every time a fighter gains Kavach.")]
        [SerializeField] private PrefabVfxLook _gainAccent = new PrefabVfxLook();
        [Tooltip("Played where a hit breaks a fighter's Kavach.")]
        [SerializeField] private PrefabVfxLook _breakAccent = new PrefabVfxLook();

        public event Action Changed;

        public Material ShellMaterial => _shellMaterial;

        public bool IsShellEnabled => _shellMaterial != null;

        public float FadeSpeed => _fadeSpeed;

        public float GainFlash => _gainFlash;

        public float FlashDecay => _flashDecay;

        public float GainSwell => _gainSwell;

        public float SwellDecay => _swellDecay;

        public float RippleDuration => _rippleDuration;

        public float ShatterDuration => _shatterDuration;

        public SparkBurstLook Shards => _shards;

        public PrefabVfxLook GainAccent => _gainAccent;

        public PrefabVfxLook BreakAccent => _breakAccent;

        public float StrengthFor(int block)
        {
            if (block <= 0)
            {
                return 0f;
            }

            return Mathf.Lerp(_minStrength, 1f, Mathf.Clamp01((block - 1f) / Mathf.Max(1f, _fullBlock - 1f)));
        }

        public float RippleStrengthFor(int blocked)
        {
            if (blocked <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.Max(_rippleMinStrength, blocked * _rippleStrengthPerBlock));
        }

        public void ApplyStatic(Material target)
        {
            if (target == null)
            {
                return;
            }

            target.SetColor(ColorId, _color);
            target.SetFloat(FillId, _fill);
            target.SetColor(RimColorId, _rimColor);
            target.SetFloat(RimPowerId, _rimPower);
            target.SetFloat(ThicknessId, _thickness);
            target.SetFloat(PatternDensityId, _patternDensity);
            target.SetFloat(PatternWidthId, _patternWidth);
            target.SetFloat(PatternStrengthId, _patternStrength);
            target.SetFloat(ShimmerSpeedId, _shimmerSpeed);
            target.SetFloat(SoftFadeId, _softFade);
            target.SetColor(RippleColorId, _rippleColor);
            target.SetFloat(RippleBandId, _rippleBand);
            target.SetFloat(RippleReachId, _rippleReach);
            target.SetColor(BreakColorId, _breakColor);
            target.SetFloat(ShardDensityId, _shardDensity);
        }

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
