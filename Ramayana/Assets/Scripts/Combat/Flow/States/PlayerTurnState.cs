// The player's turn: Kavach clears from turn two, Prana refills and five cards are drawn, then the phase opens
// for input. It waits for PlayCardCommand and EndTurnCommand; it has no per-frame work.
using JourneyToLanka.Core.StateMachines;

namespace JourneyToLanka.Combat.Flow
{
    internal sealed class PlayerTurnState : IState<CombatFlow>
    {
        public void Enter(CombatFlow flow)
        {
            flow.Model.StartPlayerTurn();
            if (!flow.CheckOutcome())
            {
                flow.SetPhase(CombatPhase.PlayerTurn);
            }
        }

        public void Exit(CombatFlow flow)
        {
        }
    }
}
