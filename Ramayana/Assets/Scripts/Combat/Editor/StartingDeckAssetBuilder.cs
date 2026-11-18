// Fills Rama's starting deck with the reference build's chapter one starters: 4 Kodanda Strike, 4 Vajra Guard,
// 1 Twin Shafts and 1 Vow of Dharma. The deck asset itself is created by the Combat builder; this builder runs after
// the Reward Cards and Status Cards builders so every starter card already exists. The entries are written only
// while the deck is empty, so a deck that has already been filled or edited is never changed.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class StartingDeckAssetBuilder
    {
        private static readonly string[] CardFiles = { "KodandaStrike", "VajraGuard", "TwinShafts", "VowOfDharma" };
        private static readonly int[] CardCounts = { 4, 4, 1, 1 };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Starting Deck", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Starting Deck", Build);
        }

        [DefaultAssetBuilder(116)]
        public static void Build(AssetBuilderReport report)
        {
            var deck = AssetDatabase.LoadAssetAtPath<StartingDeckSO>(CombatAssetBuilder.StartingDeckPath);
            if (deck == null)
            {
                report.AddSkipped($"{CombatAssetBuilder.StartingDeckPath} (missing, run the Combat builder first)");
                return;
            }

            var cards = new CardDefinitionSO[CardFiles.Length];
            for (int i = 0; i < CardFiles.Length; i++)
            {
                string path = CombatAssetBuilder.CardsFolder + "/" + CardFiles[i] + ".asset";
                cards[i] = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (cards[i] == null)
                {
                    report.AddSkipped($"{CombatAssetBuilder.StartingDeckPath} entries ({path} is missing, run the card builders first)");
                    return;
                }
            }

            AssetBuilderSerialization.FillArrayIfEmpty(deck, "_entries", cards.Length, (element, index) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_card").objectReferenceValue = cards[index];
                AssetBuilderSerialization.RequireRelative(element, "_count").intValue = CardCounts[index];
            }, report);
        }
    }
}
