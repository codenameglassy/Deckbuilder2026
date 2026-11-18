// Seeds the English names, descriptions and short chip labels of the five statuses into the Combat string table,
// under the keys the Statuses builder writes into the status assets. Descriptions state the reference build's rules
// in player terms. Only missing or empty entries are filled.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class StatusStringsAssetBuilder
    {
        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("status.focus.name", "Focus"),
            new KeyValuePair<string, string>("status.focus.short", "FOC"),
            new KeyValuePair<string, string>("status.focus.description",
                "Each hit deals extra damage equal to Focus. Ends after the holder's turn."),
            new KeyValuePair<string, string>("status.moha.name", "Moha"),
            new KeyValuePair<string, string>("status.moha.short", "MOH"),
            new KeyValuePair<string, string>("status.moha.description",
                "Deals 25% less damage. Lasts one round per stack, up to 3."),
            new KeyValuePair<string, string>("status.bhaya.name", "Bhaya"),
            new KeyValuePair<string, string>("status.bhaya.short", "BHY"),
            new KeyValuePair<string, string>("status.bhaya.description",
                "Takes 25% more damage. Lasts one round per stack, up to 3."),
            new KeyValuePair<string, string>("status.dahan.name", "Dahan"),
            new KeyValuePair<string, string>("status.dahan.short", "DAH"),
            new KeyValuePair<string, string>("status.dahan.description",
                "At the end of each round, loses health equal to Dahan, ignoring Kavach. Then Dahan halves."),
            new KeyValuePair<string, string>("status.dharma.name", "Dharma"),
            new KeyValuePair<string, string>("status.dharma.short", "DHR"),
            new KeyValuePair<string, string>("status.dharma.description",
                "Builds up over the fight and does not fade. Some cards grow stronger with it or spend it."),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Status Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Status Strings", Build);
        }

        [DefaultAssetBuilder(108)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
