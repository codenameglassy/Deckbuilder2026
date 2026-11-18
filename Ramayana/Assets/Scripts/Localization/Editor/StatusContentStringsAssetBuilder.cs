// Seeds the English text for the status content into the Combat string table: the Dharma, Invocation and Astra card
// types, the status cards with their upgraded forms, and the Corrupted Yaksha and Maya-Marich. Card descriptions are
// patterns filled with the card's numbers in effect order ({0} is the first value its effects report: the status
// amount before the hit for Terror of the Bow, Break the Illusion and Agneyastra; Piercing Vow reports damage, hits,
// the Dharma needed and the bonus; Brahmastra its base damage and damage per Dharma). Only missing or empty entries
// are filled.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class StatusContentStringsAssetBuilder
    {
        private const string FearDescription = "Deal {1} damage. Apply {0} Bhaya.";
        private const string IllusionDescription = "Apply {0} Moha. Gain {1} Kavach.";
        private const string AgneyastraDescription = "Deal {1} damage. Apply {0} Dahan.";
        private const string HerbDescription = "Heal {0} health. Exhausts.";
        private const string TapasyaDescription = "Gain {0} Focus this turn.";
        private const string VowDescription = "Gain {0} Dharma.";
        private const string MeditationDescription = "Gain {0} Dharma. Draw {1}.";
        private const string PierceDescription = "Deal {0} damage. With {2} or more Dharma, deal {3} more.";
        private const string EndureDescription = "Gain {0} Kavach. It is not cleared next turn.";

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("card.type.dharma", "Dharma"),
            new KeyValuePair<string, string>("card.type.invocation", "Invocation"),
            new KeyValuePair<string, string>("card.type.astra", "Astra"),
            new KeyValuePair<string, string>("card.fear.name", "Terror of the Bow"),
            new KeyValuePair<string, string>("card.fear.description", FearDescription),
            new KeyValuePair<string, string>("card.fear_plus.name", "Terror of the Bow+"),
            new KeyValuePair<string, string>("card.fear_plus.description", FearDescription),
            new KeyValuePair<string, string>("card.breakil.name", "Break the Illusion"),
            new KeyValuePair<string, string>("card.breakil.description", IllusionDescription),
            new KeyValuePair<string, string>("card.breakil_plus.name", "Break the Illusion+"),
            new KeyValuePair<string, string>("card.breakil_plus.description", IllusionDescription),
            new KeyValuePair<string, string>("card.agneya.name", "Agneyastra"),
            new KeyValuePair<string, string>("card.agneya.description", AgneyastraDescription),
            new KeyValuePair<string, string>("card.agneya_plus.name", "Agneyastra+"),
            new KeyValuePair<string, string>("card.agneya_plus.description", AgneyastraDescription),
            new KeyValuePair<string, string>("card.herb.name", "Sanjeevani Leaf"),
            new KeyValuePair<string, string>("card.herb.description", HerbDescription),
            new KeyValuePair<string, string>("card.herb_plus.name", "Sanjeevani Leaf+"),
            new KeyValuePair<string, string>("card.herb_plus.description", HerbDescription),
            new KeyValuePair<string, string>("card.tapasya.name", "Tapasya"),
            new KeyValuePair<string, string>("card.tapasya.description", TapasyaDescription),
            new KeyValuePair<string, string>("card.tapasya_plus.name", "Tapasya+"),
            new KeyValuePair<string, string>("card.tapasya_plus.description", TapasyaDescription),
            new KeyValuePair<string, string>("card.vow.name", "Vow of Dharma"),
            new KeyValuePair<string, string>("card.vow.description", VowDescription),
            new KeyValuePair<string, string>("card.vow_plus.name", "Vow of Dharma+"),
            new KeyValuePair<string, string>("card.vow_plus.description", VowDescription),
            new KeyValuePair<string, string>("card.medit.name", "Meditation"),
            new KeyValuePair<string, string>("card.medit.description", MeditationDescription),
            new KeyValuePair<string, string>("card.medit_plus.name", "Meditation+"),
            new KeyValuePair<string, string>("card.medit_plus.description", MeditationDescription),
            new KeyValuePair<string, string>("card.pierce.name", "Piercing Vow"),
            new KeyValuePair<string, string>("card.pierce.description", PierceDescription),
            new KeyValuePair<string, string>("card.pierce_plus.name", "Piercing Vow+"),
            new KeyValuePair<string, string>("card.pierce_plus.description", PierceDescription),
            new KeyValuePair<string, string>("card.endure.name", "Ancient Endurance"),
            new KeyValuePair<string, string>("card.endure.description", EndureDescription),
            new KeyValuePair<string, string>("card.endure_plus.name", "Ancient Endurance+"),
            new KeyValuePair<string, string>("card.endure_plus.description", EndureDescription),
            new KeyValuePair<string, string>("card.brahma.name", "Brahmastra"),
            new KeyValuePair<string, string>("card.brahma.description",
                "Spend all Dharma. Deal {0} damage, plus {1} per Dharma spent. Exhausts."),
            new KeyValuePair<string, string>("enemy.yaksha.name", "Corrupted Yaksha"),
            new KeyValuePair<string, string>("enemy.maya.name", "Maya-Marich"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Status Content Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Status Content Strings", Build);
        }

        [DefaultAssetBuilder(109)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
