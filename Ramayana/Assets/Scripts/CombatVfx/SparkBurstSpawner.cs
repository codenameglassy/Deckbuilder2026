// Plays spark bursts for every fighter from one tick. Each spark look's material gets a fixed set of burst slots,
// each with its own material copy, all made at start from the looks the style lists, so a fight never creates
// anything mid-play; when every slot of a look is busy the oldest burst is recycled. Each frame it ages every active
// burst on game time (so sparks freeze with hit-stop) and submits one procedural draw per burst on this object's
// layer, so cameras that cull that layer (a UI camera) never draw sparks. Lives on the "Combat VFX" object beside
// QuadVfxSpawner. Assigning a new spark look to the style or the hit library in Play mode adds its slots on the next
// spawn (an Editor tuning path only). Without a tick service (Play mode not started from Boot) it plays nothing.
using System.Collections.Generic;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat VFX/Spark Burst Spawner")]
    public sealed class SparkBurstSpawner : MonoBehaviour, ITickable
    {
        private const float SeedStep = 0.7548777f;

        [SerializeField] private CombatVfxStyleSO _style;

        private readonly List<SparkBurst> _bursts = new List<SparkBurst>(32);
        private readonly List<SparkBurst> _active = new List<SparkBurst>(16);
        private readonly List<SparkBurstLook> _looks = new List<SparkBurstLook>(16);
        private readonly List<HitVfxProfileSO> _profiles = new List<HitVfxProfileSO>(16);
        private readonly HashSet<Material> _builtMaterials = new HashSet<Material>();
        private HitVfxLibrarySO _library;
        private ITickService _ticks;
        private bool _registered;
        private bool _slotsDirty;
        private int _playCounter;

        private void Awake()
        {
            if (_style == null)
            {
                Debug.LogError($"{name}: no Combat VFX Style assigned. Run Tools > Journey to Lanka > Wire Selected > Combat VFX.", this);
            }
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _ticks);
            BuildSlots();
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

            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].Stop();
            }

            _active.Clear();
            SetRegistered(false);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _bursts.Count; i++)
            {
                _bursts[i].Dispose();
            }

            _bursts.Clear();
            _builtMaterials.Clear();
        }

        public bool Spawn(SparkBurstLook look, Vector3 origin, in VfxScale scale, Vector3 direction)
        {
            if (look == null || !look.IsEnabled || _ticks == null || !isActiveAndEnabled)
            {
                return false;
            }

            if (_slotsDirty)
            {
                _slotsDirty = false;
                BuildSlots();
            }

            SparkBurst burst = TakeFreeOrOldest(look.Material);
            if (burst == null)
            {
                return false;
            }

            _playCounter++;
            float seed = Mathf.Repeat(_playCounter * SeedStep, 1f);
            burst.Play(look, origin, look.CountFor(scale), new VfxSpawnContext(scale.Intensity, seed, direction), _playCounter);
            // Spawns arrive from the director's LateTick, after this frame's Tick, so the first frame is drawn here.
            burst.Render();
            _active.Add(burst);
            SetRegistered(true);
            return true;
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                SparkBurst burst = _active[i];
                if (burst.Advance(deltaTime))
                {
                    _active.RemoveAt(i);
                    continue;
                }

                burst.Render();
            }

            if (_active.Count == 0)
            {
                SetRegistered(false);
            }
        }

        private void HandleStyleChanged()
        {
            // Raised from OnValidate, where creating objects is not allowed; slots are built on the next spawn.
            _slotsDirty = true;
        }

        private void BuildSlots()
        {
            if (_style == null)
            {
                return;
            }

            _looks.Clear();
            _style.CollectSparkLooks(_profiles, _looks);
            int layer = gameObject.layer;
            for (int i = 0; i < _looks.Count; i++)
            {
                SparkBurstLook look = _looks[i];
                Material material = look != null && look.IsEnabled ? look.Material : null;
                if (material == null || !_builtMaterials.Add(material))
                {
                    continue;
                }

                for (int slot = 0; slot < _style.SparkBurstsPerLook; slot++)
                {
                    _bursts.Add(new SparkBurst(material, layer));
                }
            }

            _looks.Clear();
        }

        private SparkBurst TakeFreeOrOldest(Material source)
        {
            SparkBurst oldest = null;
            for (int i = 0; i < _bursts.Count; i++)
            {
                SparkBurst burst = _bursts[i];
                if (burst.SourceMaterial != source)
                {
                    continue;
                }

                if (!burst.IsActive)
                {
                    return burst;
                }

                if (oldest == null || burst.PlayOrder < oldest.PlayOrder)
                {
                    oldest = burst;
                }
            }

            if (oldest != null)
            {
                _active.Remove(oldest);
                oldest.Stop();
            }

            return oldest;
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
