// What a status needs when it ticks at the end of a round: the actions it may perform (Dahan burns through them) and
// the fighter that holds it.
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Statuses
{
    public readonly struct StatusTickContext
    {
        public StatusTickContext(ICombatActions actions, ICombatant owner)
        {
            Actions = actions;
            Owner = owner;
        }

        public ICombatActions Actions { get; }

        public ICombatant Owner { get; }
    }
}
