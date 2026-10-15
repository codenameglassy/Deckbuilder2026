// Why a card can or cannot be played right now. The hand UI uses it to grey out cards and pick feedback text,
// and the play command uses it to reject bad input without throwing.
namespace JourneyToLanka.Combat.Model
{
    public enum CardPlayCheck
    {
        Playable,
        CombatOver,
        NotInHand,
        NotEnoughPrana,
        InvalidTarget,
    }
}
