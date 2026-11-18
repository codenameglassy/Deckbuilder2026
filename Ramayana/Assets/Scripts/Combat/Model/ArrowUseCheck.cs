// Why a special arrow can or cannot be fired right now. The quiver UI uses it to dim slots and pick feedback, and
// the fire command uses it to reject bad input without throwing.
namespace JourneyToLanka.Combat.Model
{
    public enum ArrowUseCheck
    {
        Usable,
        CombatOver,
        NoArrow,
        InvalidTarget,
    }
}
