// Looses the special arrow in one quiver slot at an enemy slot (ignored by untargeted arrows). The slot and target
// index are the whole command, so a recorded list of commands replays a fight exactly from the same seed (a replay
// ticks the flow between commands, as live play does, so each arrow's flight ends before the next command).
// Valid only while the player can act and only when CombatModel.CheckArrow says the arrow can be fired right now.
// Executing it looses the arrow; the flow resolves it after the pacing's arrow flight.
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Commands;

namespace JourneyToLanka.Combat.Flow
{
    public sealed class FireArrowCommand : ICommand
    {
        private readonly CombatFlow _flow;

        public FireArrowCommand(CombatFlow flow, int slot, int targetIndex)
        {
            _flow = flow;
            Slot = slot;
            TargetIndex = targetIndex;
        }

        public int Slot { get; }

        public int TargetIndex { get; }

        public bool CanExecute()
        {
            return _flow.CanPlayerAct && _flow.Model.CheckArrow(Slot, TargetIndex) == ArrowUseCheck.Usable;
        }

        public void Execute()
        {
            _flow.LooseArrow(Slot, TargetIndex);
        }
    }
}
