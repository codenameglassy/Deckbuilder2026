// The end of a fight, used for both Victory and Defeat. It drops any queued commands and publishes the phase;
// rewards, the defeat screen and returning to the map are handled by whoever listens to PhaseChanged.
using JourneyToLanka.Core.StateMachines;

namespace JourneyToLanka.Combat.Flow
{
    internal sealed class CombatOverState : IState<CombatFlow>
    {
        private readonly CombatPhase _phase;

        public CombatOverState(CombatPhase phase)
        {
            _phase = phase;
        }

        public void Enter(CombatFlow flow)
        {
            flow.Commands.ClearPending();
            flow.SetPhase(_phase);
        }

        public void Exit(CombatFlow flow)
        {
        }
    }
}
