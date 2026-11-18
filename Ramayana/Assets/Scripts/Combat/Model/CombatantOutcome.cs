// What a previewed card would do to one fighter: health and Kavach before and after, and the Kavach it would gain,
// from which the health lost, the Kavach broken and whether the blow is lethal follow. Plain data produced by
// CombatOutcomePreview; views read it to draw ghost bars.
namespace JourneyToLanka.Combat.Model
{
    public readonly struct CombatantOutcome
    {
        public CombatantOutcome(Combatant combatant, int startHealth, int endHealth, int startBlock, int endBlock, int blockGained)
        {
            Combatant = combatant;
            StartHealth = startHealth;
            EndHealth = endHealth;
            StartBlock = startBlock;
            EndBlock = endBlock;
            BlockGained = blockGained;
        }

        public Combatant Combatant { get; }

        public int StartHealth { get; }

        public int EndHealth { get; }

        public int StartBlock { get; }

        public int EndBlock { get; }

        public int BlockGained { get; }

        public int HealthLost => StartHealth - EndHealth;

        public int BlockLost => StartBlock + BlockGained - EndBlock;

        public bool IsLethal => StartHealth > 0 && EndHealth <= 0;

        public bool HasChange => EndHealth != StartHealth || EndBlock != StartBlock;
    }
}
