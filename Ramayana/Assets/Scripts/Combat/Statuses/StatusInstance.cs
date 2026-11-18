// One status on one fighter: which status it is, its amount and, for statuses that count turns separately from their
// amount (Focus), how many of its owner's turns it has left. What the amount means belongs to the status: turns left
// for Bhaya and Moha, extra damage per hit for Focus, burn for Dahan, the count for Dharma. Plain data.
namespace JourneyToLanka.Combat.Statuses
{
    public struct StatusInstance
    {
        public StatusInstance(StatusDefinitionSO definition, int amount, int duration)
        {
            Definition = definition;
            Amount = amount;
            Duration = duration;
        }

        public StatusDefinitionSO Definition { get; }

        public int Amount { get; set; }

        public int Duration { get; set; }

        public bool IsActive => Amount > 0;
    }
}
