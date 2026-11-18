// The player's turn: Kavach clears from turn two, Prana refills and five cards are drawn, then the phase opens
// for input. It waits for PlayCardCommand, FireArrowCommand and EndTurnCommand. A loosed arrow is held here for its
// flight (CombatPacingSO.ArrowFlight): while it flies the turn refuses every other player command, and when the
// flight ends the arrow resolves through the flow. Leaving the turn (the fight stopping) drops an arrow still in the
// air without resolving it. Its only per-frame work is that flight timer.
using JourneyToLanka.Core.StateMachines;

namespace JourneyToLanka.Combat.Flow
{
    internal sealed class PlayerTurnState : ITickableState<CombatFlow>
    {
        private float _flightTimer;
        private int _flyingSlot;
        private int _flyingTarget;

        public bool IsArrowInFlight { get; private set; }

        public void Enter(CombatFlow flow)
        {
            ClearFlight();
            flow.Model.StartPlayerTurn();
            if (!flow.CheckOutcome())
            {
                flow.SetPhase(CombatPhase.PlayerTurn);
            }
        }

        public void Exit(CombatFlow flow)
        {
            ClearFlight();
        }

        public void Tick(CombatFlow flow, float deltaTime)
        {
            if (!IsArrowInFlight)
            {
                return;
            }

            _flightTimer -= deltaTime;
            if (_flightTimer > 0f)
            {
                return;
            }

            int slot = _flyingSlot;
            int target = _flyingTarget;
            ClearFlight();
            flow.ResolveFireArrow(slot, target);
        }

        public void BeginFlight(int slot, int targetIndex, float duration)
        {
            _flyingSlot = slot;
            _flyingTarget = targetIndex;
            _flightTimer = duration;
            IsArrowInFlight = true;
        }

        private void ClearFlight()
        {
            IsArrowInFlight = false;
            _flightTimer = 0f;
            _flyingSlot = -1;
            _flyingTarget = -1;
        }
    }
}
