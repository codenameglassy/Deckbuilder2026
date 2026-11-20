// Localization keys for card keywords that are not statuses, all in the Combat string table. Shared by the Card
// Keywords builder that seeds the English words and by anything else that needs to name them, so a key is typed in one
// place only. Status keywords (Focus, Moha, Bhaya, Dahan, Dharma) reuse each status's own name key.
namespace JourneyToLanka.CombatUI
{
    public static class CardKeywordKeys
    {
        public const string Kavach = "keyword.kavach";
        public const string Prana = "keyword.prana";
        public const string Exhaust = "keyword.exhaust";
    }
}
