// How a fight ended, published on the CombatEnded channel: who won, how many turns it took, and Rama's health
// afterwards so the run can carry it into the next fight.
namespace JourneyToLanka.Combat
{
    public readonly struct CombatResult
    {
        public CombatResult(bool victory, int turns, int playerHealth)
        {
            Victory = victory;
            Turns = turns;
            PlayerHealth = playerHealth;
        }

        public bool Victory { get; }

        public int Turns { get; }

        public int PlayerHealth { get; }
    }
}
