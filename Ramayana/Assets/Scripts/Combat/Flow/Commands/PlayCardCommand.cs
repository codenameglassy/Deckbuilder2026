// Plays one card from the hand at an enemy slot (ignored by untargeted cards). The card instance id and target
// index are the whole command, so a recorded list of them replays a fight exactly from the same seed.
// Valid only on the player's turn and only when CombatModel.CheckPlay says the card is playable right now.
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Commands;

namespace JourneyToLanka.Combat.Flow
{
    public sealed class PlayCardCommand : ICommand
    {
        private readonly CombatFlow _flow;

        public PlayCardCommand(CombatFlow flow, int cardInstanceId, int targetIndex)
        {
            _flow = flow;
            CardInstanceId = cardInstanceId;
            TargetIndex = targetIndex;
        }

        public int CardInstanceId { get; }

        public int TargetIndex { get; }

        public bool CanExecute()
        {
            return _flow.IsPlayerTurn && _flow.Model.CheckPlay(CardInstanceId, TargetIndex) == CardPlayCheck.Playable;
        }

        public void Execute()
        {
            _flow.ResolvePlayCard(CardInstanceId, TargetIndex);
        }
    }
}
