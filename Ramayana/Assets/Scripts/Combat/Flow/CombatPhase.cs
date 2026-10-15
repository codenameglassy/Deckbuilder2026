// The phase of a fight as views see it: whose turn it is, or how the fight ended. It is a read-only label the
// flow publishes for presentation (enable the hand, show the end screen). Behaviour lives in the state classes.
namespace JourneyToLanka.Combat.Flow
{
    public enum CombatPhase
    {
        None,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat,
    }
}
