// What a card needs to work out the live numbers in its description: the damage preview, who plays it, and the
// enemy it would hit (null when none is chosen yet, or for cards that hit every enemy). The default value has no
// preview, so every number shows its authored base, as on the reward screen and in the pile viewer.
namespace JourneyToLanka.Combat.Effects
{
    public readonly struct CardTextContext
    {
        public CardTextContext(IDamagePreview preview, ICombatant source, ICombatant target)
        {
            Preview = preview;
            Source = source;
            Target = target;
        }

        public IDamagePreview Preview { get; }

        public ICombatant Source { get; }

        public ICombatant Target { get; }

        public CardTextContext WithTarget(ICombatant target)
        {
            return new CardTextContext(Preview, Source, target);
        }
    }
}
