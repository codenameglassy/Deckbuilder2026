// Creates the card reward screen's style and seeds its English UI text (the title and Take Nothing).
// Existing assets and edited text are never changed. Runs after the Combat UI builders.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Localization;
using JourneyToLanka.Localization.EditorTools;
using UnityEditor;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CardRewardAssetBuilder
    {
        public const string StylePath = CombatUIAssetBuilder.Root + "/CardRewardStyle.asset";

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>(CardRewardKeys.Title, "Choose a card to carry on"),
            new KeyValuePair<string, string>(CardRewardKeys.Skip, "Take nothing"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Card Reward", priority = AssetBuilderConstants.FeatureMenuPriority + 18)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Card Reward", Build);
        }

        [DefaultAssetBuilder(145)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderSerialization.LoadOrCreateConfigured<CardRewardStyleSO>(StylePath, report, null);
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.UI, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
