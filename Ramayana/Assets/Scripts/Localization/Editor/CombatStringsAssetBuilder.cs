// Seeds the English text for the combat slice into the Combat string table: Rama, the Attack and Skill card
// types, Kodanda Strike and Vajra Guard with their upgraded forms, and the Nishachar Scout. Keys follow the
// convention the Combat builder writes into the data assets. Only missing or empty entries are filled.
// Card descriptions are patterns filled with the card's live numbers ({0} is the first value its effects report:
// damage per hit then hits for damage, Kavach for block). Entries still holding the old fixed-number defaults are
// moved to the patterns; edited entries are left alone.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class CombatStringsAssetBuilder
    {
        private const string StrikeDescription = "Deal {0} damage.";
        private const string GuardDescription = "Gain {0} Kavach.";

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("character.rama.name", "Rama"),
            new KeyValuePair<string, string>("card.type.attack", "Attack"),
            new KeyValuePair<string, string>("card.type.skill", "Skill"),
            new KeyValuePair<string, string>("card.strike.name", "Kodanda Strike"),
            new KeyValuePair<string, string>("card.strike.description", StrikeDescription),
            new KeyValuePair<string, string>("card.strike_plus.name", "Kodanda Strike+"),
            new KeyValuePair<string, string>("card.strike_plus.description", StrikeDescription),
            new KeyValuePair<string, string>("card.guard.name", "Vajra Guard"),
            new KeyValuePair<string, string>("card.guard.description", GuardDescription),
            new KeyValuePair<string, string>("card.guard_plus.name", "Vajra Guard+"),
            new KeyValuePair<string, string>("card.guard_plus.description", GuardDescription),
            new KeyValuePair<string, string>("enemy.scout.name", "Nishachar Scout"),
        };

        private static readonly (string Key, string OldValue, string NewValue)[] DescriptionUpdates =
        {
            ("card.strike.description", "Deal 6 damage.", StrikeDescription),
            ("card.strike_plus.description", "Deal 8 damage.", StrikeDescription),
            ("card.guard.description", "Gain 5 Kavach.", GuardDescription),
            ("card.guard_plus.description", "Gain 8 Kavach.", GuardDescription),
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
            LocalizationBuilderUtility.ReplaceEntriesIfUnchanged(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, DescriptionUpdates, report);
        }
    }
}
