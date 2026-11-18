// Seeds the English text for the powers and the remaining chapter one cards into the Combat string table: the Power
// card type, the three power statuses (names, descriptions and short chip labels), and Vow of the Exile, Devotion,
// Blessing of Agni, Vayavyastra, Gale Arrow and Windforce Volley with their upgraded forms. Card descriptions are
// patterns filled with the card's numbers in effect order. It also moves Sage's Counsel to the reference balance pass
// text, which exhausts, but only while its description still holds the earlier default. Only missing or empty
// entries are filled; edited text is never touched.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class PowerCardStringsAssetBuilder
    {
        private const string StripDescription = "Remove the enemy's Kavach, then deal {0} damage.";
        private const string WindforceDescription = "Deal {0} damage {1} times.";
        private const string OldCounselDescription = "Draw {0} cards.";
        private const string CounselDescription = "Draw {0} cards. Exhausts.";

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("card.type.power", "Power"),
            new KeyValuePair<string, string>("status.exile.name", "Vow of the Exile"),
            new KeyValuePair<string, string>("status.exile.short", "EXL"),
            new KeyValuePair<string, string>("status.exile.description",
                "At the start of each of your turns, gain 1 Dharma. Lasts the whole fight."),
            new KeyValuePair<string, string>("status.devotion.name", "Devotion"),
            new KeyValuePair<string, string>("status.devotion.short", "DEV"),
            new KeyValuePair<string, string>("status.devotion.description",
                "Whenever you gain Dharma, gain 2 Kavach for each Dharma gained. Lasts the whole fight."),
            new KeyValuePair<string, string>("status.agni.name", "Blessing of Agni"),
            new KeyValuePair<string, string>("status.agni.short", "AGN"),
            new KeyValuePair<string, string>("status.agni.description",
                "Each of your hits deals 2 extra damage. Lasts the whole fight."),
            new KeyValuePair<string, string>("card.exile.name", "Vow of the Exile"),
            new KeyValuePair<string, string>("card.exile.description", "Each turn, gain 1 Dharma."),
            new KeyValuePair<string, string>("card.devotion.name", "Devotion"),
            new KeyValuePair<string, string>("card.devotion.description", "Whenever you gain Dharma, gain 2 Kavach for each one."),
            new KeyValuePair<string, string>("card.agnibless.name", "Blessing of Agni"),
            new KeyValuePair<string, string>("card.agnibless.description", "Your attacks deal {0} extra damage."),
            new KeyValuePair<string, string>("card.vayu.name", "Vayavyastra"),
            new KeyValuePair<string, string>("card.vayu.description", StripDescription),
            new KeyValuePair<string, string>("card.vayu_plus.name", "Vayavyastra+"),
            new KeyValuePair<string, string>("card.vayu_plus.description", StripDescription),
            new KeyValuePair<string, string>("card.gale.name", "Gale Arrow"),
            new KeyValuePair<string, string>("card.gale.description", StripDescription),
            new KeyValuePair<string, string>("card.gale_plus.name", "Gale Arrow+"),
            new KeyValuePair<string, string>("card.gale_plus.description", StripDescription),
            new KeyValuePair<string, string>("card.windforce.name", "Windforce Volley"),
            new KeyValuePair<string, string>("card.windforce.description", WindforceDescription),
            new KeyValuePair<string, string>("card.windforce_plus.name", "Windforce Volley+"),
            new KeyValuePair<string, string>("card.windforce_plus.description", WindforceDescription),
        };

        private static readonly (string Key, string OldValue, string NewValue)[] DescriptionUpdates =
        {
            ("card.counsel.description", OldCounselDescription, CounselDescription),
            ("card.counsel_plus.description", OldCounselDescription, CounselDescription),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Power Card Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Power Card Strings", Build);
        }

        [DefaultAssetBuilder(113)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
            LocalizationBuilderUtility.ReplaceEntriesIfUnchanged(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, DescriptionUpdates, report);
        }
    }
}
