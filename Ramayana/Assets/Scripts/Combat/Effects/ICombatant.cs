// The view of a fighter that effects and targeting rules need. The combat model's combatant implements it;
// effects never touch health or Kavach directly, they go through ICombatActions.
namespace JourneyToLanka.Combat.Effects
{
    public interface ICombatant
    {
        bool IsAlive { get; }
    }
}
