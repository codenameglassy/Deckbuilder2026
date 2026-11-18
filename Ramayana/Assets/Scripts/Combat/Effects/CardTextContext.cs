// What a card needs to work out the live numbers in its description: the damage preview, who plays it, the enemy it
// would hit (null when none is chosen yet, or for cards that hit every enemy), and the combo chain scale it would be
// played with. The default value has no preview and a neutral chain, so every number shows its authored base, as on
// the reward screen and in the pile viewer. WithTarget keeps the chain and WithChain keeps the target, so a hand can
// hold one shared context and give each card its own chain scale.
namespace JourneyToLanka.Combat.Effects
{
    public readonly struct CardTextContext
    {
        public CardTextContext(IDamagePreview preview, ICombatant source, ICombatant target)
            : this(preview, source, target, ChainScale.None)
        {
        }

        public CardTextContext(IDamagePreview preview, ICombatant source, ICombatant target, ChainScale chain)
        {
            Preview = preview;
            Source = source;
            Target = target;
            Chain = chain;
        }

        public IDamagePreview Preview { get; }

        public ICombatant Source { get; }

        public ICombatant Target { get; }

        public ChainScale Chain { get; }

        public CardTextContext WithTarget(ICombatant target)
        {
            return new CardTextContext(Preview, Source, target, Chain);
        }

        public CardTextContext WithChain(ChainScale chain)
        {
            return new CardTextContext(Preview, Source, Target, chain);
        }
    }
}
