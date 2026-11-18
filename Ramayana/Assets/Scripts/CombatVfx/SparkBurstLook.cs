// The sparks a hit throws: a burst of glowing streaks flung out of the far side of the struck fighter, along the
// blow's direction within a cone (in every direction when nothing struck), slowed by drag and pulled down by gravity.
// Each spark's path is a closed-form function of time and a seed, evaluated in the Spark Burst shader, so a burst is
// one procedural draw with no CPU simulation and nothing uploaded per frame. Heavy hits and kills throw more sparks
// (count scales with the hit's size, up to Max Count). Holds the builder-made material and every value the shader
// reads; Apply writes them into a burst's own material copy at spawn. Serializable, embedded in HitVfxProfileSO;
// read-only at runtime.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public sealed class SparkBurstLook
    {
        private static readonly int OriginId = Shader.PropertyToID("_Origin");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        private static readonly int AgeId = Shader.PropertyToID("_Age");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int CountId = Shader.PropertyToID("_Count");
        private static readonly int HotColorId = Shader.PropertyToID("_HotColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int LifetimeId = Shader.PropertyToID("_Lifetime");
        private static readonly int LifetimeJitterId = Shader.PropertyToID("_LifetimeJitter");
        private static readonly int SpeedMinId = Shader.PropertyToID("_SpeedMin");
        private static readonly int SpeedMaxId = Shader.PropertyToID("_SpeedMax");
        private static readonly int DragId = Shader.PropertyToID("_Drag");
        private static readonly int GravityId = Shader.PropertyToID("_Gravity");
        private static readonly int SpreadId = Shader.PropertyToID("_Spread");
        private static readonly int UpBiasId = Shader.PropertyToID("_UpBias");
        private static readonly int SparkSizeId = Shader.PropertyToID("_SparkSize");
        private static readonly int StretchId = Shader.PropertyToID("_Stretch");
        private static readonly int FadePowerId = Shader.PropertyToID("_FadePower");
        private static readonly int CameraPushId = Shader.PropertyToID("_CameraPush");

        [Tooltip("Builder-made material using the Spark Burst shader. Its shader is never changed at runtime.")]
        [SerializeField] private Material _material;

        [Header("Count")]
        [Tooltip("Sparks for an ordinary hit; 0 turns sparks off.")]
        [SerializeField, Range(0, 128)] private int _count = 22;
        [Tooltip("Most sparks one burst may throw after heavy and kill scaling.")]
        [SerializeField, Range(1, 256)] private int _maxCount = 96;

        [Header("Motion")]
        [Tooltip("Seconds a spark lives at most, on game time (freezes with hit-stop).")]
        [SerializeField, Min(0.05f)] private float _lifetime = 0.5f;
        [Tooltip("Share by which a spark's life may be shorter than the maximum.")]
        [SerializeField, Range(0f, 0.95f)] private float _lifetimeJitter = 0.5f;
        [SerializeField, Min(0f)] private float _speedMin = 3f;
        [SerializeField, Min(0f)] private float _speedMax = 8f;
        [Tooltip("How quickly sparks slow down, per second.")]
        [SerializeField, Min(0.01f)] private float _drag = 3.5f;
        [Tooltip("Downward pull in world units per second squared.")]
        [SerializeField] private float _gravity = 9f;
        [Tooltip("Half angle in degrees of the cone around the blow's direction.")]
        [SerializeField, Range(0f, 180f)] private float _spread = 65f;
        [Tooltip("How much the cone leans upward.")]
        [SerializeField, Range(0f, 2f)] private float _upBias = 0.35f;

        [Header("Look")]
        [SerializeField, ColorUsage(false, true)] private Color _hotColor = new Color(6f, 5f, 3.5f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color _color = new Color(3.5f, 1.4f, 0.35f, 1f);
        [SerializeField, Min(0f)] private float _intensity = 1f;
        [Tooltip("Spark thickness in world units.")]
        [SerializeField, Min(0.001f)] private float _sparkSize = 0.05f;
        [Tooltip("Streak length in world units per unit of speed.")]
        [SerializeField, Min(0f)] private float _stretch = 0.035f;
        [Tooltip("Higher fades each spark out sooner.")]
        [SerializeField, Min(0.1f)] private float _fadePower = 1.4f;
        [Tooltip("World units the burst is drawn toward the camera, so it starts in front of the fighter.")]
        [SerializeField, Min(0f)] private float _cameraPush = 0.4f;

        public Material Material => _material;

        public bool IsEnabled => _material != null && _count > 0;

        public float Lifetime => _lifetime;

        public int CountFor(in VfxScale scale)
        {
            return Mathf.Clamp(Mathf.RoundToInt(_count * scale.Size), 1, _maxCount);
        }

        // A box sure to hold every spark of a burst from this origin: drag caps how far a spark flies, gravity adds the
        // fall over its life, and the camera push and spark size pad it.
        public Bounds GetBounds(Vector3 origin)
        {
            float flight = _speedMax / Mathf.Max(0.01f, _drag);
            float fall = Mathf.Abs(_gravity) * _lifetime * _lifetime;
            float reach = flight + fall + _cameraPush + _sparkSize + _stretch * _speedMax;
            return new Bounds(origin, Vector3.one * (reach * 2f));
        }

        public void Apply(Material target, Vector3 origin, int count, in VfxSpawnContext context)
        {
            if (target == null)
            {
                return;
            }

            target.SetVector(OriginId, new Vector4(origin.x, origin.y, origin.z, 1f));
            target.SetVector(DirectionId, context.ShaderDirection);
            target.SetFloat(AgeId, 0f);
            target.SetFloat(SeedId, context.Seed);
            target.SetFloat(CountId, count);
            target.SetColor(HotColorId, _hotColor);
            target.SetColor(ColorId, _color);
            target.SetFloat(IntensityId, _intensity * context.Intensity);
            target.SetFloat(LifetimeId, _lifetime);
            target.SetFloat(LifetimeJitterId, _lifetimeJitter);
            target.SetFloat(SpeedMinId, Mathf.Min(_speedMin, _speedMax));
            target.SetFloat(SpeedMaxId, Mathf.Max(_speedMin, _speedMax));
            target.SetFloat(DragId, _drag);
            target.SetFloat(GravityId, _gravity);
            target.SetFloat(SpreadId, _spread * Mathf.Deg2Rad);
            target.SetFloat(UpBiasId, _upBias);
            target.SetFloat(SparkSizeId, _sparkSize);
            target.SetFloat(StretchId, _stretch);
            target.SetFloat(FadePowerId, _fadePower);
            target.SetFloat(CameraPushId, _cameraPush);
        }
    }
}
