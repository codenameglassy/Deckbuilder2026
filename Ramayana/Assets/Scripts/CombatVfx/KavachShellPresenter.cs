// Shows Kavach on every fighter. When a fight attaches, the VFX director binds each fighter with its view's
// KavachShellTarget; the presenter builds that target's hull once (fight setup, reused in later fights since views
// persist) and listens to the fighter's BlockChanged to flash, swell or fade it. The director reports each hit Kavach
// absorbed through Absorb: a ripple when Kavach survives, a shatter when it reaches zero (Absorb returns true so the
// director throws the shard burst). Kavach that wears off at turn start only fades. It ticks on game time only while
// some hull is moving, so a shown hull costs nothing on the CPU and the shatter freezes with hit-stop. Editing the
// Kavach profile in Play mode re-applies its look to every hull. Lives on the "Combat VFX" object. Without a tick
// service it shows hulls but never animates them.
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat VFX/Kavach Shell Presenter")]
    public sealed class KavachShellPresenter : MonoBehaviour, ITickable
    {
        [SerializeField] private CombatVfxStyleSO _style;

        private readonly List<KavachShell> _built = new List<KavachShell>(8);
        private readonly List<KavachShell> _bound = new List<KavachShell>(8);
        private KavachVfxProfileSO _profile;
        private ITickService _ticks;
        private bool _registered;
        private bool _staticDirty;

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
        }

        private void OnEnable()
        {
            _profile = _style != null ? _style.Kavach : null;
            if (_profile != null)
            {
                _profile.Changed += HandleProfileChanged;
            }
        }

        private void OnDisable()
        {
            if (_profile != null)
            {
                _profile.Changed -= HandleProfileChanged;
            }

            UnbindAll();
            SetRegistered(false);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _built.Count; i++)
            {
                _built[i].Destroy();
            }

            _built.Clear();
        }

        public void Bind(Combatant fighter, KavachShellTarget target)
        {
            if (fighter == null || target == null || _profile == null || !_profile.IsShellEnabled)
            {
                return;
            }

            if (_ticks == null)
            {
                ServiceLocator.TryGet(out _ticks);
            }

            KavachShell shell = FindBuilt(target);
            if (shell == null)
            {
                shell = new KavachShell(target, _profile.ShellMaterial, gameObject.layer);
                _built.Add(shell);
            }

            if (!shell.HasLayers || shell.Fighter != null)
            {
                return;
            }

            shell.Bind(fighter, _profile);
            _bound.Add(shell);
            fighter.BlockChanged += HandleBlockChanged;
        }

        public void UnbindAll()
        {
            for (int i = 0; i < _bound.Count; i++)
            {
                KavachShell shell = _bound[i];
                if (shell.Fighter != null)
                {
                    shell.Fighter.BlockChanged -= HandleBlockChanged;
                }

                shell.Unbind();
            }

            _bound.Clear();
        }

        // Shows a hit Kavach absorbed, from the blow's direction (attacker to target, zero when unknown). Returns true
        // when this hit broke the fighter's Kavach and the hull shatters; a second report for a hull already breaking
        // returns false, so one break throws one shard burst.
        public bool Absorb(Combatant fighter, int blocked, Vector3 blowDirection)
        {
            KavachShell shell = FindBound(fighter);
            if (shell == null || blocked <= 0)
            {
                return false;
            }

            SetRegistered(true);
            if (fighter.Block <= 0)
            {
                if (shell.IsBreaking)
                {
                    return false;
                }

                shell.Shatter();
                return true;
            }

            shell.Ripple(-blowDirection, _profile.RippleStrengthFor(blocked));
            return false;
        }

        public void Tick(float deltaTime)
        {
            if (_profile == null)
            {
                SetRegistered(false);
                return;
            }

            bool applyStatic = _staticDirty;
            _staticDirty = false;

            bool moving = false;
            for (int i = 0; i < _bound.Count; i++)
            {
                KavachShell shell = _bound[i];
                if (applyStatic)
                {
                    shell.ApplyStatic(_profile);
                }

                moving |= shell.Tick(deltaTime, _profile);
            }

            if (!moving)
            {
                SetRegistered(false);
            }
        }

        private void HandleBlockChanged(Combatant fighter)
        {
            KavachShell shell = FindBound(fighter);
            if (shell == null)
            {
                return;
            }

            shell.Refresh(_profile);
            SetRegistered(true);
        }

        private void HandleProfileChanged()
        {
            _staticDirty = true;
            SetRegistered(true);
        }

        private KavachShell FindBuilt(KavachShellTarget target)
        {
            for (int i = 0; i < _built.Count; i++)
            {
                if (_built[i].Target == target)
                {
                    return _built[i];
                }
            }

            return null;
        }

        private KavachShell FindBound(Combatant fighter)
        {
            for (int i = 0; i < _bound.Count; i++)
            {
                if (_bound[i].Fighter == fighter)
                {
                    return _bound[i];
                }
            }

            return null;
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
