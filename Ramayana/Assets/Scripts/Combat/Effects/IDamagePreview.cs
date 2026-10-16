// Read-only damage calculation for previews: what a hit of baseAmount from source to target would deal after
// every damage modifier, before Kavach. Enemy intents and card text use it so a shown number matches what lands.
namespace JourneyToLanka.Combat.Effects
{
    public interface IDamagePreview
    {
        int PreviewDamage(ICombatant source, ICombatant target, int baseAmount);
    }
}
