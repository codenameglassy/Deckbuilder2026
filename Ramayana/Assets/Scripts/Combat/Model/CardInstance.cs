// One physical card in a fight: a unique id for commands, replays and views, plus the shared definition it uses.
// Two Kodanda Strikes share one definition asset but are two instances with different ids.
using System;
using JourneyToLanka.Combat.Cards;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CardInstance
    {
        public CardInstance(int id, CardDefinitionSO definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            Id = id;
            Definition = definition;
        }

        public int Id { get; }

        public CardDefinitionSO Definition { get; }

        public int Cost => Definition.Cost;

        public bool RequiresChosenTarget => Definition.RequiresChosenTarget;
    }
}
