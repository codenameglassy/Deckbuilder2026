// Every shared value for combat VFX: the billboard quad mesh every quad effect draws on, the hit library that picks a
// profile per source and the default profile for anything it leaves out, where on a fighter effects appear (a height
// between the bottom and the top of the fighter's collider: hits at the chest, brought out to the camera-facing
// surface of its bounds and a small offset beyond; the cast mandala at the feet, lifted just off the ground), how
// much bigger, longer and brighter heavy hits and killing blows play, and how many of each look are prewarmed and may
// play at once. What counts as heavy comes from CombatImpactStyleSO, so hit-stop, camera shake and VFX agree. The
// Kavach profile drives every fighter's shield shell, and the status profile its status auras and apply accents (auras
// keep their own pools, Aura Instances Per Prefab copies of each). Arrow missiles launch from Rama at a height (and a little
// toward their target) and rise by an arc on the way. Accents are pooled particle prefabs (PrefabVfxLook): whether
// their scripts are stripped and their lights turned off, how many copies of each prefab are made at start, how many
// may play at once, and how long one may linger after it stops emitting. CollectQuadLooks, CollectSparkLooks and
// CollectAccentLooks list every look the spawners must prewarm, including Kavach's shatter shards and accents. Shared
// flyweight with no runtime state; Changed (Editor only) lets the spawners add pools for newly assigned looks.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.CombatScene;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat VFX/Combat VFX Style", fileName = "CombatVfxStyle")]
    public sealed class CombatVfxStyleSO : ScriptableObject
    {
        private const float FallbackHeavyHitFraction = 0.2f;

        [Header("References")]
        [SerializeField] private Mesh _quadMesh;
        [SerializeField] private HitVfxLibrarySO _hitLibrary;
        [Tooltip("Used for any source the library does not cover.")]
        [SerializeField] private HitVfxProfileSO _defaultHit;
        [Tooltip("Source of the heavy-hit threshold, shared with hit-stop and camera shake.")]
        [SerializeField] private CombatImpactStyleSO _impactStyle;
        [SerializeField] private KavachVfxProfileSO _kavach;
        [SerializeField] private StatusVfxProfileSO _statuses;

        [Header("Placement")]
        [Tooltip("Where a hit lands on a fighter: 0 at the bottom of its collider, 1 at the top.")]
        [SerializeField, Range(0f, 1.5f)] private float _impactHeight = 0.65f;
        [Tooltip("World units hit effects sit in front of the camera-facing surface of the fighter's bounds.")]
        [SerializeField, Min(0f)] private float _surfaceOffset = 0.05f;
        [Tooltip("Where the cast mandala sits: 0 at the bottom of the fighter's collider, 1 at the top.")]
        [SerializeField, Range(0f, 1f)] private float _castHeight = 0f;
        [Tooltip("World units the cast mandala is lifted, so it never fights the ground for depth.")]
        [SerializeField, Min(0f)] private float _castLift = 0.03f;

        [Header("Heavy Hits")]
        [SerializeField, Min(1f)] private float _heavySize = 1.35f;
        [SerializeField, Min(1f)] private float _heavyDuration = 1.2f;
        [SerializeField, Min(1f)] private float _heavyIntensity = 1.4f;

        [Header("Killing Blows")]
        [SerializeField, Min(1f)] private float _killSize = 1.8f;
        [SerializeField, Min(1f)] private float _killDuration = 1.5f;
        [SerializeField, Min(1f)] private float _killIntensity = 2f;

        [Header("Pooling")]
        [Tooltip("Instances of each quad look created before the fight starts.")]
        [SerializeField, Min(1)] private int _quadPrewarmPerLook = 6;
        [Tooltip("Most instances of one quad look playing at once; beyond it the oldest is recycled.")]
        [SerializeField, Min(1)] private int _quadMaxPerLook = 12;
        [Tooltip("Spark bursts of each look that may play at once; beyond it the oldest is recycled. All are made at start.")]
        [SerializeField, Min(1)] private int _sparkBurstsPerLook = 8;
        [Tooltip("Copies of each accent prefab created before the fight starts; beyond them the oldest playing copy is recycled.")]
        [SerializeField, Min(1)] private int _accentInstancesPerPrefab = 3;
        [Tooltip("Most accents playing at once across every prefab; beyond it the oldest is stopped.")]
        [SerializeField, Min(1)] private int _accentMaxActive = 12;
        [Tooltip("Copies of each status aura prefab created when the fight starts; one per fighter that may show it at once.")]
        [SerializeField, Min(1)] private int _auraInstancesPerPrefab = 4;

        [Header("Arrow Flight")]
        [Tooltip("Where missiles leave Rama: 0 at the bottom of his collider, 1 at the top.")]
        [SerializeField, Range(0f, 1.5f)] private float _missileLaunchHeight = 0.7f;
        [Tooltip("World units the launch point sits toward the target, so missiles leave from in front of Rama.")]
        [SerializeField, Min(0f)] private float _missileLaunchForward = 0.4f;
        [Tooltip("World units a missile rises above the straight line at the halfway point.")]
        [SerializeField, Min(0f)] private float _missileArc = 0.6f;

        [Header("Accents")]
        [Tooltip("Remove every script from accent prefabs (Polygon Arsenal's demo helpers and sound spawners) so only their particles play.")]
        [SerializeField] private bool _stripAccentScripts = true;
        [Tooltip("Turn off lights and particle light modules in accent prefabs; realtime lights cost more than the particles.")]
        [SerializeField] private bool _disableAccentLights = true;
        [Tooltip("Seconds an accent may keep particles alive after it stops emitting before it is returned to its pool regardless.")]
        [SerializeField, Min(0.5f)] private float _accentLinger = 3f;

        public event Action Changed;

        public Mesh QuadMesh => _quadMesh;

        public HitVfxLibrarySO HitLibrary => _hitLibrary;

        public HitVfxProfileSO DefaultHit => _defaultHit;

        public KavachVfxProfileSO Kavach => _kavach;

        public StatusVfxProfileSO Statuses => _statuses;

        public int AuraInstancesPerPrefab => _auraInstancesPerPrefab;

        public float ImpactHeight => _impactHeight;

        public float SurfaceOffset => _surfaceOffset;

        public float CastHeight => _castHeight;

        public float CastLift => _castLift;

        public int QuadPrewarmPerLook => _quadPrewarmPerLook;

        public int QuadMaxPerLook => Mathf.Max(_quadMaxPerLook, _quadPrewarmPerLook);

        public int SparkBurstsPerLook => _sparkBurstsPerLook;

        public int AccentInstancesPerPrefab => _accentInstancesPerPrefab;

        public int AccentMaxActive => _accentMaxActive;

        public bool StripAccentScripts => _stripAccentScripts;

        public bool DisableAccentLights => _disableAccentLights;

        public float AccentLinger => _accentLinger;

        public float MissileLaunchHeight => _missileLaunchHeight;

        public float MissileLaunchForward => _missileLaunchForward;

        public float MissileArcHeight => _missileArc;

        public bool IsHeavy(in DamageResult result, int maxHealth)
        {
            if (result.Killed)
            {
                return true;
            }

            float fraction = _impactStyle != null ? _impactStyle.HeavyHitFraction : FallbackHeavyHitFraction;
            return result.HealthLost >= Mathf.CeilToInt(maxHealth * fraction);
        }

        public VfxScale GetHitScale(in DamageResult result, int maxHealth)
        {
            if (result.Killed)
            {
                return new VfxScale(_killSize, _killDuration, _killIntensity);
            }

            return IsHeavy(result, maxHealth)
                ? new VfxScale(_heavySize, _heavyDuration, _heavyIntensity)
                : VfxScale.One;
        }

        // Every hit profile reachable from this style, each once. Used at start to prewarm pools, never per frame.
        public void CollectProfiles(List<HitVfxProfileSO> profiles)
        {
            if (profiles == null)
            {
                return;
            }

            if (_defaultHit != null && !profiles.Contains(_defaultHit))
            {
                profiles.Add(_defaultHit);
            }

            if (_hitLibrary != null)
            {
                _hitLibrary.CollectProfiles(profiles);
            }
        }

        public void CollectQuadLooks(List<HitVfxProfileSO> scratch, List<IQuadVfxLook> looks)
        {
            if (scratch == null || looks == null)
            {
                return;
            }

            scratch.Clear();
            CollectProfiles(scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                HitVfxProfileSO profile = scratch[i];
                looks.Add(profile.Impact);
                looks.Add(profile.Slash);
                looks.Add(profile.Cast);
            }

            scratch.Clear();
        }

        public void CollectSparkLooks(List<HitVfxProfileSO> scratch, List<SparkBurstLook> looks)
        {
            if (scratch == null || looks == null)
            {
                return;
            }

            scratch.Clear();
            CollectProfiles(scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                looks.Add(scratch[i].Sparks);
            }

            if (_kavach != null)
            {
                looks.Add(_kavach.Shards);
            }

            scratch.Clear();
        }

        public void CollectAccentLooks(List<HitVfxProfileSO> scratch, List<PrefabVfxLook> looks)
        {
            if (scratch == null || looks == null)
            {
                return;
            }

            scratch.Clear();
            CollectProfiles(scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                HitVfxProfileSO profile = scratch[i];
                looks.Add(profile.Accent);
                looks.Add(profile.HeavyAccent);
                looks.Add(profile.KillAccent);
                looks.Add(profile.Death);
                looks.Add(profile.LaunchAccent);
                looks.Add(profile.Missile);
                looks.Add(profile.Arrival);
            }

            if (_kavach != null)
            {
                looks.Add(_kavach.GainAccent);
                looks.Add(_kavach.BreakAccent);
            }

            if (_statuses != null)
            {
                _statuses.CollectApplyAccents(looks);
            }

            scratch.Clear();
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
