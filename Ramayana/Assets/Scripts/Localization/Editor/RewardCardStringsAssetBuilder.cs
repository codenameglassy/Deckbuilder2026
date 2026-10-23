// Seeds the English names and descriptions of the reward cards (Twin Shafts, Storm of Arrows, Lakshmana's Watch,
// Sage's Counsel and their upgraded forms) into the Combat string table. Only missing or empty entries are filled.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class RewardCardStringsAssetBuilder
    {
        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("card.twin.name", "Twin Shafts"),
            new KeyValuePair<string, string>("card.twin.description", "Deal 4 damage twice."),
            new KeyValuePair<string, string>("card.twin_plus.name", "Twin Shafts+"),
            new KeyValuePair<string, string>("card.twin_plus.description", "Deal 5 damage twice."),
            new KeyValuePair<string, string>("card.storm.name", "Storm of Arrows"),
            new KeyValuePair<string, string>("card.storm.description", "Deal 3 damage four times."),
            new KeyValuePair<string, string>("card.storm_plus.name", "Storm of Arrows+"),
            new KeyValuePair<string, string>("card.storm_plus.description", "Deal 4 damage four times."),
            new KeyValuePair<string, string>("card.watch.name", "Lakshmana's Watch"),
            new KeyValuePair<string, string>("card.watch.description", "Gain 8 Kavach. Draw 1."),
            new KeyValuePair<string, string>("card.watch_plus.name", "Lakshmana's Watch+"),
            new KeyValuePair<string, string>("card.watch_plus.description", "Gain 11 Kavach. Draw 1."),
            new KeyValuePair<string, string>("card.counsel.name", "Sage's Counsel"),
            new KeyValuePair<string, string>("card.counsel.description", "Draw 2 cards."),
            new KeyValuePair<string, string>("card.counsel_plus.name", "Sage's Counsel+"),
            new KeyValuePair<string, string>("card.counsel_plus.description", "Draw 3 cards."),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Reward Card Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Reward Card Strings", Build);
        }

        [DefaultAssetBuilder(106)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
