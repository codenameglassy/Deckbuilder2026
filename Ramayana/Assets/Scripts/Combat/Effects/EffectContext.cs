// Everything an effect needs to resolve: the actions it may perform, who is acting, the opponent the actor chose
// (null when nothing was chosen), and every opponent on the field for multi-target rules.
// For a card, the source is the player and the opponents are the enemies; for an enemy move, the reverse.
// Chain is the combo chain's scale for the card being played (Bow Chain damage, Ring Chain Kavach). Only the combat
// model sets it, for player cards; every other context (enemy moves, arrows, tests) keeps the neutral default.
using System.Collections.Generic;

namespace JourneyToLanka.Combat.Effects
{
    public readonly struct EffectContext
    {
        public EffectContext(ICombatActions actions, ICombatant source, ICombatant chosenTarget, IReadOnlyList<ICombatant> opponents)
            : this(actions, source, chosenTarget, opponents, ChainScale.None)
        {
        }

        public EffectContext(ICombatActions actions, ICombatant source, ICombatant chosenTarget,
            IReadOnlyList<ICombatant> opponents, ChainScale chain)
        {
            Actions = actions;
            Source = source;
            ChosenTarget = chosenTarget;
            Opponents = opponents;
            Chain = chain;
        }

        public ICombatActions Actions { get; }

        public ICombatant Source { get; }

        public ICombatant ChosenTarget { get; }

        public IReadOnlyList<ICombatant> Opponents { get; }

        public ChainScale Chain { get; }
    }
}
