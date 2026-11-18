// Creates the effects test deck, Decks/StartingDeck_DevStatuses.asset: one or two copies of every card whose effect
// has its own look, and no plain strikes or guards, so each status, buff and power shows up within a few turns.
// Debuffs: Terror of the Bow (Bhaya), Break the Illusion (Moha), Agneyastra (Dahan). Buffs: Tapasya (Focus), Vow of
// Dharma and Meditation (Dharma), Brahmastra (spends Dharma, so its aura can be seen leaving). Powers: Vow of the
// Exile, Devotion, Blessing of Agni. Others: Ancient Endurance (retained Kavach), Sanjeevani Leaf (heal), Vayavyastra
// (strips Kavach). It is a test deck only: the builder never assigns it. To use it, set Dev Starting Deck on
// Player.asset (read in the Editor and Development builds only); clear that field to go back to the real deck. The
// entries are written only while the deck is empty, so an edited test deck is never changed; to rebuild it, set its
// Entries size to 0 and run the builder again. Runs after the Starting Deck builder (116), when every card it uses
// already exists.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class DevStartingDeckAssetBuilder
    {
        public const string DeckPath = CombatAssetBuilder.DecksFolder + "/StartingDeck_DevStatuses.asset";

        private static readonly (string File, int Count)[] Entries =
        {
            ("TerrorOfTheBow", 2),
            ("BreakTheIllusion", 2),
            ("Agneyastra", 1),
            ("Tapasya", 2),
            ("VowOfDharma", 2),
            ("Meditation", 1),
            ("Brahmastra", 1),
            ("VowOfTheExile", 1),
            ("Devotion", 1),
            ("BlessingOfAgni", 1),
            ("AncientEndurance", 1),
            ("SanjeevaniLeaf", 1),
            ("Vayavyastra", 1),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Dev Starting Deck", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Dev Starting Deck", Build);
        }

        [DefaultAssetBuilder(117)]
        public static void Build(AssetBuilderReport report)
        {
            var cards = new CardDefinitionSO[Entries.Length];
            for (int i = 0; i < Entries.Length; i++)
            {
                string path = CombatAssetBuilder.CardsFolder + "/" + Entries[i].File + ".asset";
                cards[i] = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (cards[i] == null)
                {
                    report.AddSkipped($"{DeckPath} ({path} is missing, run the card builders first)");
                    return;
                }
            }

            StartingDeckSO deck = AssetBuilderSerialization.LoadOrCreateConfigured<StartingDeckSO>(DeckPath, report, null);
            AssetBuilderSerialization.FillArrayIfEmpty(deck, "_entries", cards.Length, (element, index) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_card").objectReferenceValue = cards[index];
                AssetBuilderSerialization.RequireRelative(element, "_count").intValue = Entries[index].Count;
            }, report);
        }
    }
}
