// A source of stand-in status sets used while a card is being previewed, so statuses the card would apply earlier in
// its effect list (Bhaya before its hit) change the damage previewed later in the same card. CombatOutcomePreview
// provides it; the status damage modifier reads it only during a preview.
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Statuses
{
    public interface IStatusOverlay
    {
        bool TryGetStatuses(ICombatant combatant, out StatusSet statuses);
    }
}
