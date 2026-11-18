// Keeps status auras on the fighters. Bound by CombatVfxDirector when a fight starts, it listens to every fighter's
// StatusSet and, whenever one changes, asks the style's StatusVfxProfileSO which auras that fighter should show (at
// most the profile's limit, by priority, a shared aura once): auras no longer wanted stop emitting and fade out in
// place, newly wanted ones start from their own pools. A fighter that falls has its statuses cleared, so its auras
// fade with them. Every tick it keeps each live aura on its fighter through IFighterVfxAnchors (so auras follow lunges
// and hit reactions) and returns faded ones to their pools; the tick is registered only while an aura plays.
// Auras keep their own pools, separate from the one-shot accents so a burst of hits can never recycle an aura: one
// pool per aura prefab with the style's Aura Instances Per Prefab copies, built when the fight binds; with every copy
// in use a further aura of that prefab is not shown. Loops run on game time and freeze with hit-stop. Editing the
// profile in Play mode restarts every aura from the new settings on the next tick. Lives on the "Combat VFX" object at
// the origin with no scale. It only presents and never changes combat state.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat VFX/Status Aura Presenter")]
    public sealed class StatusAuraPresenter : MonoBehaviour, ITickable
    {
        [SerializeField] private CombatVfxStyleSO _style;

        private readonly Dictionary<GameObject, ComponentPool<PrefabVfxInstance>> _pools =
            new Dictionary<GameObject, ComponentPool<PrefabVfxInstance>>(8);
        private readonly List<StatusAura> _auras = new List<StatusAura>(8);
        private readonly List<int> _desired = new List<int>(4);
        private Action<StatusSet> _statusesChanged;
        private Combatant[] _fighters = Array.Empty<Combatant>();
        private StatusVfxProfileSO _profile;
        private IFighterVfxAnchors _anchors;
        private Transform _templates;
        private ITickService _ticks;
        private bool _registered;
        private bool _refreshAll;
        private int _playCounter;

        private void Awake()
        {
            _statusesChanged = HandleStatusesChanged;

            var holder = new GameObject("Aura Templates");
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
            if (_ticks == null)
            {
                ServiceLocator.TryGet(out _ticks);
            }
        }

        private void OnDisable()
        {
            UnbindAll();
        }

        private void OnDestroy()
        {
            foreach (ComponentPool<PrefabVfxInstance> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
        }

        public void Bind(CombatModel model, IFighterVfxAnchors anchors)
        {
            UnbindAll();
            if (model == null || anchors == null || _style == null || _style.Statuses == null)
            {
                return;
            }

            if (_ticks == null)
            {
                ServiceLocator.TryGet(out _ticks);
            }

            _profile = _style.Statuses;
            _anchors = anchors;
            _profile.Changed += HandleProfileChanged;
            BuildPools();

            int count = model.Enemies.Count + 1;
            if (_fighters.Length != count)
            {
                _fighters = new Combatant[count];
            }

            _fighters[0] = model.Player;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                _fighters[i + 1] = model.Enemies[i].Fighter;
            }

            for (int i = 0; i < _fighters.Length; i++)
            {
                _fighters[i].Statuses.Changed += _statusesChanged;
                Refresh(i);
            }
        }

        public void UnbindAll()
        {
            for (int i = 0; i < _fighters.Length; i++)
            {
                if (_fighters[i] != null)
                {
                    _fighters[i].Statuses.Changed -= _statusesChanged;
                    _fighters[i] = null;
                }
            }

            if (_profile != null)
            {
                _profile.Changed -= HandleProfileChanged;
                _profile = null;
            }

            _anchors = null;
            _refreshAll = false;
            for (int i = _auras.Count - 1; i >= 0; i--)
            {
                Release(_auras[i].Instance);
            }

            _auras.Clear();
            SetRegistered(false);
        }

        public void Tick(float deltaTime)
        {
            if (_refreshAll)
            {
                _refreshAll = false;
                RefreshAll();
            }

            for (int i = _auras.Count - 1; i >= 0; i--)
            {
                StatusAura aura = _auras[i];
                if (!aura.Stopping && _anchors != null && _profile != null && aura.EntryIndex < _profile.Count
                    && _anchors.TryPlace(aura.Slot, _profile.EntryAt(aura.EntryIndex).Aura, out Vector3 point))
                {
                    aura.Instance.MoveTo(point);
                }

                if (aura.Instance.Advance(deltaTime))
                {
                    _auras.RemoveAt(i);
                    Release(aura.Instance);
                }
            }

            if (_auras.Count == 0 && !_refreshAll)
            {
                SetRegistered(false);
            }
        }

        private void HandleStatusesChanged(StatusSet statuses)
        {
            for (int i = 0; i < _fighters.Length; i++)
            {
                if (_fighters[i] != null && _fighters[i].Statuses == statuses)
                {
                    Refresh(i);
                    return;
                }
            }
        }

        private void HandleProfileChanged()
        {
            // Raised from OnValidate, where creating objects is not allowed; the auras restart on the next tick.
            _refreshAll = true;
            SetRegistered(true);
        }

        private void RefreshAll()
        {
            if (_profile == null)
            {
                return;
            }

            for (int i = 0; i < _auras.Count; i++)
            {
                StopAt(i);
            }

            BuildPools();
            for (int i = 0; i < _fighters.Length; i++)
            {
                Refresh(i);
            }
        }

        // Fighter index is slot + 1: Rama first, then the enemies.
        private void Refresh(int fighterIndex)
        {
            if (_profile == null)
            {
                return;
            }

            Combatant fighter = _fighters[fighterIndex];
            int slot = fighterIndex - 1;
            if (fighter != null && fighter.IsAlive)
            {
                _profile.SelectAuras(fighter.Statuses, _desired);
            }
            else
            {
                _desired.Clear();
            }

            for (int i = 0; i < _auras.Count; i++)
            {
                StatusAura aura = _auras[i];
                if (aura.Slot == slot && !aura.Stopping && !_desired.Contains(aura.EntryIndex))
                {
                    StopAt(i);
                }
            }

            for (int i = 0; i < _desired.Count; i++)
            {
                if (!IsPlaying(slot, _desired[i]))
                {
                    StartAura(slot, _desired[i]);
                }
            }
        }

        private void StartAura(int slot, int entryIndex)
        {
            PrefabVfxLook look = _profile.EntryAt(entryIndex).Aura;
            if (!TryGetPool(look.Prefab, out ComponentPool<PrefabVfxInstance> pool) || pool.CountInactive == 0
                || !_anchors.TryPlace(slot, look, out Vector3 point))
            {
                return;
            }

            PrefabVfxInstance instance = pool.Get();
            _playCounter++;
            instance.Play(point, instance.BaseRotation, look.ScaleFor(VfxScale.One), look.Tint, float.PositiveInfinity,
                _style.AccentLinger, _playCounter);
            _auras.Add(new StatusAura(slot, entryIndex, instance));
            SetRegistered(true);
        }

        private void StopAt(int index)
        {
            StatusAura aura = _auras[index];
            if (aura.Stopping)
            {
                return;
            }

            aura.Instance.StopLoop();
            aura.Stopping = true;
            _auras[index] = aura;
        }

        private bool IsPlaying(int slot, int entryIndex)
        {
            for (int i = 0; i < _auras.Count; i++)
            {
                StatusAura aura = _auras[i];
                if (aura.Slot == slot && aura.EntryIndex == entryIndex && !aura.Stopping)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildPools()
        {
            for (int i = 0; i < _profile.Count; i++)
            {
                StatusVfxEntry entry = _profile.EntryAt(i);
                if (entry != null && entry.Aura != null && entry.Aura.IsEnabled && !_pools.ContainsKey(entry.Aura.Prefab))
                {
                    AddPool(entry.Aura.Prefab);
                }
            }
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
            PrefabVfxInstance template = PrefabVfxTemplates.Create(prefab, _templates, _style.StripAccentScripts,
                _style.DisableAccentLights);
            int perPrefab = _style.AuraInstancesPerPrefab;
            var pool = new ComponentPool<PrefabVfxInstance>(template, transform, perPrefab, perPrefab);
            _pools.Add(prefab, pool);
            pool.Prewarm(perPrefab);
            return pool;
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
