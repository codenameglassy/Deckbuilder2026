// Plays pooled quad effects (impact bursts, slashes, cast mandalas) for every fighter from one tick. It keeps one ComponentPool per look material, built and prewarmed at start from the looks the style lists, so
// a fight never instantiates effects mid-play; when a look's pool has no free instance the oldest playing instance of
// that look is recycled instead. Templates are built in code under an inactive holder. Effects advance on game time
// and freeze with hit-stop. Lives on its own scene object at the origin with no scale, since instances are placed by
// world position and uniform local scale. Assigning a new look to the style or the hit library in Play mode adds its
// pool on the next spawn (an Editor tuning path only). Without a tick service (Play mode not started from Boot) it plays nothing.
using System.Collections.Generic;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;
using UnityEngine.Rendering;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat VFX/Quad VFX Spawner")]
    public sealed class QuadVfxSpawner : MonoBehaviour, ITickable
    {
        private const float GoldenRatioFraction = 0.618034f;

        [SerializeField] private CombatVfxStyleSO _style;

        private readonly Dictionary<Material, ComponentPool<QuadVfx>> _pools = new Dictionary<Material, ComponentPool<QuadVfx>>(8);
        private readonly List<QuadVfx> _active = new List<QuadVfx>(32);
        private readonly List<IQuadVfxLook> _looks = new List<IQuadVfxLook>(32);
        private readonly List<HitVfxProfileSO> _profiles = new List<HitVfxProfileSO>(16);
        private HitVfxLibrarySO _library;
        private Transform _templates;
        private ITickService _ticks;
        private bool _registered;
        private bool _poolsDirty;
        private int _playCounter;

        private void Awake()
        {
            var holder = new GameObject("Templates");
            holder.SetActive(false);
            _templates = holder.transform;
            _templates.SetParent(transform, false);

            if (_style == null)
            {
                Debug.LogError($"{name}: no Combat VFX Style assigned. Run Tools > Journey to Lanka > Wire Selected > Combat VFX.", this);
            }
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _ticks);
            BuildPools();
        }

        private void OnEnable()
        {
            if (_style != null)
            {
                _style.Changed += HandleStyleChanged;
                _library = _style.HitLibrary;
                if (_library != null)
                {
                    _library.Changed += HandleStyleChanged;
                }
            }
        }

        private void OnDisable()
        {
            if (_style != null)
            {
                _style.Changed -= HandleStyleChanged;
            }

            if (_library != null)
            {
                _library.Changed -= HandleStyleChanged;
                _library = null;
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Release(_active[i]);
            }

            _active.Clear();
            SetRegistered(false);
        }

        private void OnDestroy()
        {
            foreach (ComponentPool<QuadVfx> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
        }

        public bool Spawn(IQuadVfxLook look, Vector3 position, in VfxScale scale, Vector3 direction)
        {
            if (look == null || !look.IsEnabled || _ticks == null || !isActiveAndEnabled)
            {
                return false;
            }

            if (_poolsDirty)
            {
                _poolsDirty = false;
                BuildPools();
            }

            Material source = look.Material;
            if (source == null || !_pools.TryGetValue(source, out ComponentPool<QuadVfx> pool))
            {
                return false;
            }

            QuadVfx quad = pool.CountInactive > 0 ? pool.Get() : TakeOldestActive(source);
            if (quad == null)
            {
                return false;
            }

            _playCounter++;
            float seed = Mathf.Repeat(_playCounter * GoldenRatioFraction, 1f);
            look.Apply(quad.Material, new VfxSpawnContext(scale.Intensity, seed, direction));
            quad.Play(position, look.Size * scale.Size, look.Duration * scale.Duration, _playCounter);
            _active.Add(quad);
            SetRegistered(true);
            return true;
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                QuadVfx quad = _active[i];
                if (quad.Advance(deltaTime))
                {
                    _active.RemoveAt(i);
                    Release(quad);
                }
            }

            if (_active.Count == 0)
            {
                SetRegistered(false);
            }
        }

        private void HandleStyleChanged()
        {
            // Raised from OnValidate, where creating objects is not allowed; the pools are rebuilt on the next spawn.
            _poolsDirty = true;
        }

        private void BuildPools()
        {
            if (_style == null || _style.QuadMesh == null)
            {
                return;
            }

            _looks.Clear();
            _style.CollectQuadLooks(_profiles, _looks);
            for (int i = 0; i < _looks.Count; i++)
            {
                IQuadVfxLook look = _looks[i];
                Material material = look != null && look.IsEnabled ? look.Material : null;
                if (material == null || _pools.ContainsKey(material))
                {
                    continue;
                }

                ComponentPool<QuadVfx> pool = CreatePool(material);
                _pools.Add(material, pool);
                pool.Prewarm(_style.QuadPrewarmPerLook);
            }

            _looks.Clear();
        }

        private ComponentPool<QuadVfx> CreatePool(Material material)
        {
            // Parent under the inactive holder before adding components, so the template's Awake never runs and it
            // never makes a material copy of its own; only the pool's instances do.
            var templateObject = new GameObject(material.name);
            templateObject.transform.SetParent(_templates, false);

            var filter = templateObject.AddComponent<MeshFilter>();
            filter.sharedMesh = _style.QuadMesh;

            var meshRenderer = templateObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.allowOcclusionWhenDynamic = false;

            var template = templateObject.AddComponent<QuadVfx>();
            template.Initialize(meshRenderer, material);

            return new ComponentPool<QuadVfx>(template, transform, _style.QuadPrewarmPerLook, _style.QuadMaxPerLook);
        }

        private QuadVfx TakeOldestActive(Material source)
        {
            int oldestIndex = -1;
            int oldestOrder = int.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                QuadVfx quad = _active[i];
                if (quad.SourceMaterial == source && quad.PlayOrder < oldestOrder)
                {
                    oldestOrder = quad.PlayOrder;
                    oldestIndex = i;
                }
            }

            if (oldestIndex < 0)
            {
                return null;
            }

            QuadVfx oldest = _active[oldestIndex];
            _active.RemoveAt(oldestIndex);
            return oldest;
        }

        private void Release(QuadVfx quad)
        {
            if (quad == null)
            {
                return;
            }

            if (quad.SourceMaterial != null && _pools.TryGetValue(quad.SourceMaterial, out ComponentPool<QuadVfx> pool))
            {
                pool.Release(quad);
            }
            else
            {
                quad.gameObject.SetActive(false);
            }
        }

        private void SetRegistered(bool registered)
        {
            if (_ticks == null || _registered == registered)
            {
                return;
            }

            _registered = registered;
            if (registered)
            {
                _ticks.AddTickable(this);
            }
            else
            {
                _ticks.RemoveTickable(this);
            }
        }
    }
}
