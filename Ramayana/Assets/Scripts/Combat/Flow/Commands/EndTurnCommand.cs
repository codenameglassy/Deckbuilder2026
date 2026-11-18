// Ends the player's turn: the hand is discarded and the enemy turn begins. Valid only while the player can act, so a
// special arrow in flight always lands before the turn ends. It carries no data, so the flow keeps one instance and
// reuses it.
using JourneyToLanka.Core.Commands;

namespace JourneyToLanka.Combat.Flow
{
    public sealed class EndTurnCommand : ICommand
    {
        private readonly CombatFlow _flow;

        public EndTurnCommand(CombatFlow flow)
        {
            _flow = flow;
        }

        public bool CanExecute()
        {
            return _flow.CanPlayerAct;
        }

        public void Execute()
        {
            _flow.ResolveEndTurn();
        }
    }
}
