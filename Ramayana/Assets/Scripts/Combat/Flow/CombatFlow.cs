// Runs one fight: a state machine over player turn, enemy turn and the two endings, with every player and enemy
// action going through one CommandProcessor so input, AI and later replays share a single pipeline.
// It is plain C# with no timing source of its own: the scene owner calls Begin once, forwards frames to Tick from
// its ITickable, and calls Stop when the fight is torn down. One flow runs one fight and is never restarted.
// Special arrows fire through the same pipeline (FireArrow), and victory is checked after them as after a card.
// Victory is checked after each card and defeat after each enemy action; if both sides fall at once, defeat takes priority.
// When every enemy has acted the round ends (statuses tick, Dahan burns) and the outcome is checked again before the
// next player turn, since a burn can finish a fight.
// EnemyActionStarted fires when an enemy begins its action, one windup (CombatPacingSO) before the action resolves,
// so views can play an attack that lands exactly when the damage does. Arrows work the same way: a fired arrow is
// loosed (ArrowLoosed, with the arrow still in its quiver slot), flies for the pacing's arrow flight, then resolves
// and ArrowLanded follows its effects, so its damage, hit-stop and hit effects land with it. While it flies the player
// cannot act (CanPlayerAct is false), so nothing changes between loosing and landing. Both are presentation timing
// only: commands are unchanged, and with no flight time an arrow is loosed and lands in the same call.
using System;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Commands;
using JourneyToLanka.Core.StateMachines;

namespace JourneyToLanka.Combat.Flow
{
    public sealed class CombatFlow
    {
        private readonly StateMachine<CombatFlow> _machine;
        private readonly CommandProcessor _commands = new CommandProcessor();
        private readonly PlayerTurnState _playerTurn = new PlayerTurnState();
        private readonly EnemyTurnState _enemyTurn = new EnemyTurnState();
        private readonly CombatOverState _victory = new CombatOverState(CombatPhase.Victory);
        private readonly CombatOverState _defeat = new CombatOverState(CombatPhase.Defeat);
        private readonly EndTurnCommand _endTurnCommand;
        private readonly EnemyActCommand[] _enemyActCommands;
        private bool _hasBegun;

        public CombatFlow(CombatModel model, CombatPacingSO pacing)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (pacing == null)
            {
                throw new ArgumentNullException(nameof(pacing));
            }

            Model = model;
            Pacing = pacing;
            _machine = new StateMachine<CombatFlow>(this);
            _endTurnCommand = new EndTurnCommand(this);

            _enemyActCommands = new EnemyActCommand[model.Enemies.Count];
            for (int i = 0; i < _enemyActCommands.Length; i++)
            {
                _enemyActCommands[i] = new EnemyActCommand(this, i);
            }
        }

        public event Action<CombatPhase> PhaseChanged;

        // Enemy slot index. Raised one windup before that enemy's EnemyActCommand is submitted.
        public event Action<int> EnemyActionStarted;

        // Quiver slot and target index (-1 for untargeted arrows). Raised when an arrow is loosed, one arrow flight
        // before it resolves; the arrow is still in Model.Quiver at that slot.
        public event Action<int, int> ArrowLoosed;

        // The same slot and target index as ArrowLoosed. Raised once the arrow has resolved (after the model's
        // ArrowFired) and before the outcome is checked; the arrow has left the quiver.
        public event Action<int, int> ArrowLanded;

        public CombatModel Model { get; }

        public CombatPacingSO Pacing { get; }

        public CommandProcessor Commands => _commands;

        public CombatPhase Phase { get; private set; }

        public bool IsPlayerTurn => Phase == CombatPhase.PlayerTurn;

        public bool IsEnemyTurn => Phase == CombatPhase.EnemyTurn;

        public bool IsOver => Phase == CombatPhase.Victory || Phase == CombatPhase.Defeat;

        public bool IsArrowInFlight => _playerTurn.IsArrowInFlight;

        // True on the player's turn while no special arrow is in flight: the only time player commands are accepted.
        public bool CanPlayerAct => IsPlayerTurn && !IsArrowInFlight;

        public void Begin()
        {
            if (_hasBegun)
            {
                throw new InvalidOperationException("CombatFlow.Begin can only be called once. Create a new flow for each fight.");
            }

            _hasBegun = true;
            _machine.ChangeState(_playerTurn);
        }

        public void Tick(float deltaTime)
        {
            _machine.Tick(deltaTime);
        }

        public void Stop()
        {
            _commands.ClearPending();
            _machine.Stop();
            SetPhase(CombatPhase.None);
        }

        public CommandSubmitResult PlayCard(int cardInstanceId, int targetIndex)
        {
            return _commands.Submit(new PlayCardCommand(this, cardInstanceId, targetIndex));
        }

        public CommandSubmitResult FireArrow(int slot, int targetIndex)
        {
            return _commands.Submit(new FireArrowCommand(this, slot, targetIndex));
        }

        public CommandSubmitResult EndTurn()
        {
            return _commands.Submit(_endTurnCommand);
        }

        internal void SetPhase(CombatPhase phase)
        {
            if (Phase == phase)
            {
                return;
            }

            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        internal void StartPlayerTurn()
        {
            _machine.ChangeState(_playerTurn);
        }

        internal void AnnounceEnemyAction(int enemyIndex)
        {
            EnemyActionStarted?.Invoke(enemyIndex);
        }

        internal void SubmitEnemyAction(int enemyIndex)
        {
            _commands.Submit(_enemyActCommands[enemyIndex]);
        }

        internal void ResolvePlayCard(int cardInstanceId, int targetIndex)
        {
            Model.TryPlayCard(cardInstanceId, targetIndex);
            CheckOutcome();
        }

        internal void LooseArrow(int slot, int targetIndex)
        {
            float flight = Pacing.ArrowFlight;
            if (flight > 0f)
            {
                // The flight starts before the announcement, so a listener already sees the player unable to act.
                _playerTurn.BeginFlight(slot, targetIndex, flight);
                ArrowLoosed?.Invoke(slot, targetIndex);
                return;
            }

            ArrowLoosed?.Invoke(slot, targetIndex);
            ResolveFireArrow(slot, targetIndex);
        }

        internal void ResolveFireArrow(int slot, int targetIndex)
        {
            Model.TryFireArrow(slot, targetIndex);
            ArrowLanded?.Invoke(slot, targetIndex);
            CheckOutcome();
        }

        internal void ResolveEndTurn()
        {
            Model.EndPlayerTurn();
            _machine.ChangeState(_enemyTurn);
        }

        internal void ResolveEnemyAction(int enemyIndex)
        {
            Model.EnemyTakeTurn(enemyIndex);
            CheckOutcome();
        }

        internal bool ResolveRoundEnd()
        {
            Model.EndRound();
            return CheckOutcome();
        }

        internal bool CheckOutcome()
        {
            if (Model.IsPlayerDefeated)
            {
                _machine.ChangeState(_defeat);
                return true;
            }

            if (Model.AreEnemiesDefeated)
            {
                _machine.ChangeState(_victory);
                return true;
            }

            return false;
        }
    }
}
