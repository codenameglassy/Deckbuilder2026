// Plays pooled accent particle prefabs (PrefabVfxLook) for every fighter from one tick: in place (Spawn) or flying
// along an arc from a launch point to a landing point over a set time (Launch, used for arrow missiles, which stop
// emitting on arrival). At start it builds one pool per prefab the style lists (hit profile accents, arrow launches,
// missiles and arrivals, enemy deaths, Kavach gain and break) and prewarms it, so a fight
// never instantiates accents mid-play. Each pool's template comes from PrefabVfxTemplates (scripts stripped and lights
// off as the style says). Status auras are not played here: StatusAuraPresenter keeps its own pools of loops. When a
// prefab has no free copy the oldest playing copy of it is recycled, and when the style's cap on accents playing at
// once is reached the oldest accent of any kind is stopped. Accents advance on game time and freeze with hit-stop.
// Lives on the "Combat VFX" object at the origin with no scale, since copies are placed by world position. Assigning
// a new accent in Play mode adds its pool on the next spawn (an Editor tuning path only; a player build plays only
// the accents listed at start). Without a tick service (Play mode not started from Boot) it plays nothing.
using System.Collections.Generic;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat VFX/Prefab VFX Spawner")]
    public sealed class PrefabVfxSpawner : MonoBehaviour, ITickable
    {
        [SerializeField] private CombatVfxStyleSO _style;

        private readonly Dictionary<GameObject, ComponentPool<PrefabVfxInstance>> _pools =
            new Dictionary<GameObject, ComponentPool<PrefabVfxInstance>>(16);
        private readonly List<PrefabVfxInstance> _active = new List<PrefabVfxInstance>(16);
        private readonly List<PrefabVfxLook> _looks = new List<PrefabVfxLook>(32);
        private readonly List<HitVfxProfileSO> _profiles = new List<HitVfxProfileSO>(16);
        private HitVfxLibrarySO _library;
        private Transform _templates;
        private ITickService _ticks;
        private bool _registered;
        private bool _poolsDirty;
        private int _playCounter;

        private void Awake()
        {
            var holder = new GameObject("Accent Templates");
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
            foreach (ComponentPool<PrefabVfxInstance> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
        }

        // Plays the look's prefab at a world position. Direction is the blow (attacker to target, zero when unknown),
        // used only by looks that align to it; scale multiplies the prefab's own scale.
        public bool Spawn(PrefabVfxLook look, Vector3 position, Vector3 direction, float scale)
        {
            PrefabVfxInstance instance = Acquire(look);
            if (instance == null)
            {
                return false;
            }

            Quaternion rotation = look.AlignToBlow && direction.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(direction.normalized)
                : instance.BaseRotation;

            _playCounter++;
            instance.Play(position, rotation, scale, look.Tint, look.MaxDuration, _style.AccentLinger, _playCounter);
            Activate(instance);
            return true;
        }

        // Flies the look's prefab from one world point to another over the duration, rising by arc at the halfway
        // point, pointing along its path; it stops emitting on arrival. The look's own placement and max duration are
        // not used (the caller places both ends); its tint is.
        public bool Launch(PrefabVfxLook look, Vector3 from, Vector3 to, float duration, float arc, float scale)
        {
            PrefabVfxInstance instance = Acquire(look);
            if (instance == null)
            {
                return false;
            }

            _playCounter++;
            instance.PlayFlight(from, to, arc, duration, scale, look.Tint, _style.AccentLinger, _playCounter);
            Activate(instance);
            return true;
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PrefabVfxInstance instance = _active[i];
                if (instance.Advance(deltaTime))
                {
                    _active.RemoveAt(i);
                    Release(instance);
                }
            }

            if (_active.Count == 0)
            {
                SetRegistered(false);
            }
        }

        private PrefabVfxInstance Acquire(PrefabVfxLook look)
        {
            if (look == null || !look.IsEnabled || _style == null || _ticks == null || !isActiveAndEnabled)
            {
                return null;
            }

            if (_poolsDirty)
            {
                _poolsDirty = false;
                BuildPools();
            }

            GameObject prefab = look.Prefab;
            if (!TryGetPool(prefab, out ComponentPool<PrefabVfxInstance> pool))
            {
                return null;
            }

            PrefabVfxInstance instance = pool.CountInactive > 0 ? pool.Get() : TakeOldestActive(prefab);
            if (instance == null)
            {
                return null;
            }

            int maxActive = _style.AccentMaxActive;
            while (_active.Count >= maxActive && _active.Count > 0)
            {
                ReleaseOldest();
            }

            return instance;
        }

        private void Activate(PrefabVfxInstance instance)
        {
            _active.Add(instance);
            SetRegistered(true);
        }

        private void HandleStyleChanged()
        {
            // Raised from OnValidate, where creating objects is not allowed; the pools are rebuilt on the next spawn.
            _poolsDirty = true;
        }

        private void BuildPools()
        {
            if (_style == null)
            {
                return;
            }

            _looks.Clear();
            _style.CollectAccentLooks(_profiles, _looks);
            for (int i = 0; i < _looks.Count; i++)
            {
                PrefabVfxLook look = _looks[i];
                if (look != null && look.IsEnabled && !_pools.ContainsKey(look.Prefab))
                {
                    AddPool(look.Prefab);
                }
            }

            _looks.Clear();
        }

        private bool TryGetPool(GameObject prefab, out ComponentPool<PrefabVfxInstance> pool)
        {
            if (_pools.TryGetValue(prefab, out pool))
            {
                return true;
            }

#if UNITY_EDITOR
            pool = AddPool(prefab);
            return true;
#else
            return false;
#endif
        }

        private ComponentPool<PrefabVfxInstance> AddPool(GameObject prefab)
        {
            ComponentPool<PrefabVfxInstance> pool = CreatePool(prefab);
            _pools.Add(prefab, pool);
            pool.Prewarm(_style.AccentInstancesPerPrefab);
            return pool;
        }

        private ComponentPool<PrefabVfxInstance> CreatePool(GameObject prefab)
        {
            PrefabVfxInstance template = PrefabVfxTemplates.Create(prefab, _templates, _style.StripAccentScripts,
                _style.DisableAccentLights);
            int perPrefab = _style.AccentInstancesPerPrefab;
            return new ComponentPool<PrefabVfxInstance>(template, transform, perPrefab, perPrefab);
        }

        private PrefabVfxInstance TakeOldestActive(GameObject prefab)
        {
            int oldestIndex = -1;
            int oldestOrder = int.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                PrefabVfxInstance instance = _active[i];
                if (instance.Source == prefab && instance.PlayOrder < oldestOrder)
                {
                    oldestOrder = instance.PlayOrder;
                    oldestIndex = i;
                }
            }

            if (oldestIndex < 0)
            {
                return null;
            }

            PrefabVfxInstance oldest = _active[oldestIndex];
            _active.RemoveAt(oldestIndex);
            return oldest;
        }

        private void ReleaseOldest()
        {
            int oldestIndex = 0;
            for (int i = 1; i < _active.Count; i++)
            {
                if (_active[i].PlayOrder < _active[oldestIndex].PlayOrder)
                {
                    oldestIndex = i;
                }
            }

            PrefabVfxInstance oldest = _active[oldestIndex];
            _active.RemoveAt(oldestIndex);
            Release(oldest);
        }

        private void Release(PrefabVfxInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.Source != null && _pools.TryGetValue(instance.Source, out ComponentPool<PrefabVfxInstance> pool))
            {
                pool.Release(instance);
            }
            else
            {
                instance.gameObject.SetActive(false);
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
