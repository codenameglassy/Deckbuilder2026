// Seeds the English text for the combat slice into the Combat string table: Rama, the Attack and Skill card
// types, Kodanda Strike and Vajra Guard with their upgraded forms, and the Nishachar Scout. Keys follow the
// convention the Combat builder writes into the data assets. Only missing or empty entries are filled.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class CombatStringsAssetBuilder
    {
        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("character.rama.name", "Rama"),
            new KeyValuePair<string, string>("card.type.attack", "Attack"),
            new KeyValuePair<string, string>("card.type.skill", "Skill"),
            new KeyValuePair<string, string>("card.strike.name", "Kodanda Strike"),
            new KeyValuePair<string, string>("card.strike.description", "Deal 6 damage."),
            new KeyValuePair<string, string>("card.strike_plus.name", "Kodanda Strike+"),
            new KeyValuePair<string, string>("card.strike_plus.description", "Deal 8 damage."),
            new KeyValuePair<string, string>("card.guard.name", "Vajra Guard"),
            new KeyValuePair<string, string>("card.guard.description", "Gain 5 Kavach."),
            new KeyValuePair<string, string>("card.guard_plus.name", "Vajra Guard+"),
            new KeyValuePair<string, string>("card.guard_plus.description", "Gain 8 Kavach."),
            new KeyValuePair<string, string>("enemy.scout.name", "Nishachar Scout"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat Strings", Build);
        }

        [DefaultAssetBuilder(105)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
