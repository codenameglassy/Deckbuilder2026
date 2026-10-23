// Moves the run forward between fights. When a fight ends it applies the result to the run and waits long enough for
// the result banner to be seen. Then, if the run rolled a card reward, it raises CardRewardOffered and waits for
// CardRewardChosen (the chosen index, or -1 to take nothing) before loading the next fight. Otherwise it loads the
// next fight, or, when the run is won or lost, ends it and reloads the combat scene so a fresh run starts.
// Lives on the Boot object; the delay runs on the tick service. The reward screen must be in the combat scene.
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using JourneyToLanka.Scenes;
using UnityEngine;

namespace JourneyToLanka.Run
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Core/Run Director")]
    public sealed class RunDirector : MonoBehaviour, ITickable
    {
        [SerializeField] private CombatResultEventChannelSO _combatEnded;
        [SerializeField] private LoadSceneEventChannelSO _loadScene;
        [SerializeField] private VoidEventChannelSO _cardRewardOffered;
        [SerializeField] private IntEventChannelSO _cardRewardChosen;
        [SerializeField, Min(0f)] private float _resultDelay = 2.5f;

        private IRunService _runs;
        private ITickService _ticks;
        private SceneDefinitionSO _nextScene;
        private float _remaining;
        private bool _waiting;
        private bool _offerRewardWhenDone;
        private bool _awaitingRewardChoice;

        private void OnEnable()
        {
            if (_combatEnded != null)
            {
                _combatEnded.Register(HandleCombatEnded);
            }

            if (_cardRewardChosen != null)
            {
                _cardRewardChosen.Register(HandleRewardChosen);
            }
        }

        private void OnDisable()
        {
            if (_combatEnded != null)
            {
                _combatEnded.Unregister(HandleCombatEnded);
            }

            if (_cardRewardChosen != null)
            {
                _cardRewardChosen.Unregister(HandleRewardChosen);
            }

            StopTimer();
            _awaitingRewardChoice = false;
        }

        public void Tick(float deltaTime)
        {
            _remaining -= deltaTime;
            if (_remaining > 0f)
            {
                return;
            }

            StopTimer();

            if (_offerRewardWhenDone)
            {
                _offerRewardWhenDone = false;
                _awaitingRewardChoice = true;
                _cardRewardOffered.Raise();
                return;
            }

            LoadNextScene();
        }

        private void HandleCombatEnded(CombatResult result)
        {
            if (!ServiceLocator.TryGet(out _runs) || !_runs.HasActiveRun || !ServiceLocator.TryGet(out _ticks))
            {
                return;
            }

            RunState run = _runs.Current;
            run.ApplyCombatResult(result);
            _nextScene = run.Definition.CombatScene;
            _offerRewardWhenDone = !run.IsFinished && run.HasPendingReward && _cardRewardOffered != null && _cardRewardChosen != null;
            _awaitingRewardChoice = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{name}: fight ended ({(result.Victory ? "victory" : "defeat")}), run {run.Outcome}, encounter {run.EncounterIndex + 1}/{run.EncounterCount}, health {run.Health}/{run.MaxHealth}, reward offered: {_offerRewardWhenDone}.", this);
#endif

            if (run.IsFinished)
            {
                _runs.EndRun();
            }

            if (_nextScene == null)
            {
                Debug.LogError($"{name}: {run.Definition.name} has no combat scene, cannot continue.", this);
                return;
            }

            StartTimer();
        }

        private void HandleRewardChosen(int choiceIndex)
        {
            if (!_awaitingRewardChoice || _runs == null || !_runs.HasActiveRun)
            {
                return;
            }

            _awaitingRewardChoice = false;
            CardDefinitionSO chosen = _runs.Current.ResolveReward(choiceIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{name}: reward {(chosen != null ? chosen.name + " added, deck " + _runs.Current.Deck.Count : "skipped")}.", this);
#endif
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            SceneDefinitionSO scene = _nextScene;
            _nextScene = null;
            if (scene != null && _loadScene != null)
            {
                _loadScene.Raise(scene);
            }
        }

        private void StartTimer()
        {
            _remaining = _resultDelay;
            if (!_waiting)
            {
                _waiting = true;
                _ticks.AddTickable(this);
            }
        }

        private void StopTimer()
        {
            if (_waiting)
            {
                _waiting = false;
                _ticks?.RemoveTickable(this);
            }
        }
    }
}
