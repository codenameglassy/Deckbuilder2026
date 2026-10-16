// Starts and owns one fight in the scene: builds the CombatModel and CombatFlow from data assets, binds the
// combatant views, forwards frames from the tick service, and announces the fight on the CombatStarted and
// CombatEnded channels. Services are resolved in Start because the Bootstrapper installs them in its Awake,
// and every Awake finishes before any Start, whichever scene loads first.
// Enemy views that also carry an EnemyIntentView get it bound to their enemy here.
// Same-object helpers listen to CombatBegan, a plain C# event.
using System;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Combat Controller")]
    public sealed class CombatController : MonoBehaviour, ITickable
    {
        [Header("Fight")]
        [SerializeField] private CombatRulesSO _rules;
        [SerializeField] private PlayerDefinitionSO _player;
        [SerializeField] private CombatEncounterSO _encounter;
        [SerializeField] private CombatPacingSO _pacing;
        [SerializeField] private bool _randomizeSeed = true;
        [SerializeField] private int _seed = 2026;

        [Header("Views")]
        [SerializeField] private CombatantView _playerView;
        [SerializeField] private CombatantView[] _enemyViews = Array.Empty<CombatantView>();

        [Header("Channels")]
        [SerializeField] private VoidEventChannelSO _combatStarted;
        [SerializeField] private CombatResultEventChannelSO _combatEnded;

        private ITickService _ticks;
        private CombatFlow _flow;

        public event Action<CombatFlow> CombatBegan;

        public CombatFlow Flow => _flow;

        public int Seed { get; private set; }

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _ticks))
            {
                Debug.LogError($"{name}: no tick service. Enter Play mode from the Boot scene with this scene loaded additively.", this);
                enabled = false;
                return;
            }

            if (!HasRequiredData())
            {
                enabled = false;
                return;
            }

            Seed = _randomizeSeed ? Environment.TickCount : _seed;
            var model = new CombatModel(_rules, _player, _player.MaxHealth, _encounter, new CombatRandom(Seed));
            _flow = new CombatFlow(model, _pacing);
            _flow.PhaseChanged += HandlePhaseChanged;

            BindViews(model);
            _ticks.AddTickable(this);

            CombatBegan?.Invoke(_flow);
            if (_combatStarted != null)
            {
                _combatStarted.Raise();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{name}: combat started with seed {Seed}.", this);
#endif
            _flow.Begin();
        }

        public void Tick(float deltaTime)
        {
            _flow.Tick(deltaTime);
        }

        private void OnDestroy()
        {
            _ticks?.RemoveTickable(this);

            if (_flow == null)
            {
                return;
            }

            _flow.PhaseChanged -= HandlePhaseChanged;
            _flow.Stop();
            _flow = null;
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase != CombatPhase.Victory && phase != CombatPhase.Defeat)
            {
                return;
            }

            _ticks?.RemoveTickable(this);

            if (_combatEnded != null)
            {
                CombatModel model = _flow.Model;
                _combatEnded.Raise(new CombatResult(phase == CombatPhase.Victory, model.TurnNumber, model.Player.Health));
            }
        }

        private void BindViews(CombatModel model)
        {
            if (_playerView != null)
            {
                _playerView.Bind(model.Player);
            }

            var enemies = model.Enemies;
            for (int i = 0; i < _enemyViews.Length; i++)
            {
                CombatantView view = _enemyViews[i];
                if (view == null)
                {
                    continue;
                }

                if (i < enemies.Count)
                {
                    view.Bind(enemies[i].Fighter);
                    if (view.TryGetComponent(out EnemyIntentView intent))
                    {
                        intent.Bind(enemies[i], model);
                    }
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }

            if (enemies.Count > _enemyViews.Length)
            {
                Debug.LogWarning($"{name}: {enemies.Count} enemies but only {_enemyViews.Length} enemy views. Extra enemies are not shown.", this);
            }
        }

        private bool HasRequiredData()
        {
            bool valid = true;
            valid &= Require(_rules, nameof(_rules));
            valid &= Require(_player, nameof(_player));
            valid &= Require(_encounter, nameof(_encounter));
            valid &= Require(_pacing, nameof(_pacing));
            return valid;
        }

        private bool Require(UnityEngine.Object value, string fieldName)
        {
            if (value != null)
            {
                return true;
            }

            Debug.LogError($"{name}: {fieldName} is not assigned. Run Tools > Journey to Lanka > Wire Selected > Combat Scene.", this);
            return false;
        }
    }
}
