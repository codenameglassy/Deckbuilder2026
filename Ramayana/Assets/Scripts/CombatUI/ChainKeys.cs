// Localization keys for the combo chain bar's interface text, in the UI string table: the multiplier pattern under
// each pip ("x{0}"), the Finisher tag and the broken-chain message ("Broken at {0} in a row"). The chain's own name
// and hint line come from its ChainKindSO, in the Combat table. Shared by the chain bar and the builder that seeds
// the English text, so a key is typed in one place only.
namespace JourneyToLanka.CombatUI
{
    public static class ChainKeys
    {
        public const string Multiplier = "ui.chain.multiplier";
        public const string Finisher = "ui.chain.finisher";
        public const string Broken = "ui.chain.broken";
    }
}
