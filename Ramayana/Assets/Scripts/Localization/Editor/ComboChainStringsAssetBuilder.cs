// Seeds the English names and hint lines of the combo chain kinds (Bow Chain, Ring Chain) into the Combat string
// table, under the keys the Combo Chains builder writes on the chain assets. Only missing or empty entries are
// filled, so text edited in the Localization Tables window is never overwritten. Runs after the Localization builder.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class ComboChainStringsAssetBuilder
    {
        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("chain.bow.name", "Bow Chain"),
            new KeyValuePair<string, string>("chain.bow.hint", "Play Kodanda Strikes in a row to multiply their damage."),
            new KeyValuePair<string, string>("chain.ring.name", "Ring Chain"),
            new KeyValuePair<string, string>("chain.ring.hint", "Play Vajra Guards in a row to multiply their Kavach."),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combo Chain Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combo Chain Strings", Build);
        }

        [DefaultAssetBuilder(119)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
