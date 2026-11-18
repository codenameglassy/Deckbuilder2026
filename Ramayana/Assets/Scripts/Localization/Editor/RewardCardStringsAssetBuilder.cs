// Seeds the English names and descriptions of the reward cards (Twin Shafts, Storm of Arrows, Lakshmana's Watch,
// Sage's Counsel and their upgraded forms) into the Combat string table. Only missing or empty entries are filled.
// Descriptions are patterns filled with the card's live numbers in effect order (damage per hit then hits, Kavach,
// cards drawn). Sage's Counsel Exhausts in both forms, so its description says so. Entries still holding an older
// default (the fixed-number texts, or Sage's Counsel without "Exhausts.") are moved to the current patterns; edited
// entries stay.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class RewardCardStringsAssetBuilder
    {
        private const string TwinDescription = "Deal {0} damage twice.";
        private const string StormDescription = "Deal {0} damage {1} times.";
        private const string WatchDescription = "Gain {0} Kavach. Draw {1}.";
        private const string CounselDescription = "Draw {0} cards. Exhausts.";
        private const string OldCounselDescription = "Draw {0} cards.";

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("card.twin.name", "Twin Shafts"),
            new KeyValuePair<string, string>("card.twin.description", TwinDescription),
            new KeyValuePair<string, string>("card.twin_plus.name", "Twin Shafts+"),
            new KeyValuePair<string, string>("card.twin_plus.description", TwinDescription),
            new KeyValuePair<string, string>("card.storm.name", "Storm of Arrows"),
            new KeyValuePair<string, string>("card.storm.description", StormDescription),
            new KeyValuePair<string, string>("card.storm_plus.name", "Storm of Arrows+"),
            new KeyValuePair<string, string>("card.storm_plus.description", StormDescription),
            new KeyValuePair<string, string>("card.watch.name", "Lakshmana's Watch"),
            new KeyValuePair<string, string>("card.watch.description", WatchDescription),
            new KeyValuePair<string, string>("card.watch_plus.name", "Lakshmana's Watch+"),
            new KeyValuePair<string, string>("card.watch_plus.description", WatchDescription),
            new KeyValuePair<string, string>("card.counsel.name", "Sage's Counsel"),
            new KeyValuePair<string, string>("card.counsel.description", CounselDescription),
            new KeyValuePair<string, string>("card.counsel_plus.name", "Sage's Counsel+"),
            new KeyValuePair<string, string>("card.counsel_plus.description", CounselDescription),
        };

        private static readonly (string Key, string OldValue, string NewValue)[] DescriptionUpdates =
        {
            ("card.twin.description", "Deal 4 damage twice.", TwinDescription),
            ("card.twin_plus.description", "Deal 5 damage twice.", TwinDescription),
            ("card.storm.description", "Deal 3 damage four times.", StormDescription),
            ("card.storm_plus.description", "Deal 4 damage four times.", StormDescription),
            ("card.watch.description", "Gain 8 Kavach. Draw 1.", WatchDescription),
            ("card.watch_plus.description", "Gain 11 Kavach. Draw 1.", WatchDescription),
            ("card.counsel.description", "Draw 2 cards.", CounselDescription),
            ("card.counsel_plus.description", "Draw 3 cards.", CounselDescription),
            ("card.counsel.description", OldCounselDescription, CounselDescription),
            ("card.counsel_plus.description", OldCounselDescription, CounselDescription),
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
            LocalizationBuilderUtility.ReplaceEntriesIfUnchanged(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, DescriptionUpdates, report);
        }
    }
}
