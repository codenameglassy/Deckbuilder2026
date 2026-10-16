// What an effect needs to describe itself as an intent: the damage preview, who acts, and whom it would target.
namespace JourneyToLanka.Combat.Effects
{
    public readonly struct IntentContext
    {
        public IntentContext(IDamagePreview preview, ICombatant source, ICombatant target)
        {
            Preview = preview;
            Source = source;
            Target = target;
        }

        public IDamagePreview Preview { get; }

        public ICombatant Source { get; }

        public ICombatant Target { get; }
    }
}
