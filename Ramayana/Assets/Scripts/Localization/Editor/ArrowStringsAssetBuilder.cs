// Seeds the English names and descriptions of the twelve special arrows into the Combat string table, under the keys
// the Special Arrows builder writes into the arrow assets. Descriptions are patterns filled with the arrow's numbers
// in effect order, as card descriptions are (the Arrow of Agni reports damage, hits, then Dahan). Only missing or
// empty entries are filled.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class ArrowStringsAssetBuilder
    {
        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("arrow.agni.name", "Arrow of Agni"),
            new KeyValuePair<string, string>("arrow.agni.description", "Deal {0} damage, then apply {2} Dahan."),
            new KeyValuePair<string, string>("arrow.vajra.name", "Vajra Arrow"),
            new KeyValuePair<string, string>("arrow.vajra.description", "Gain {0} Kavach."),
            new KeyValuePair<string, string>("arrow.vayu.name", "Arrow of Vayu"),
            new KeyValuePair<string, string>("arrow.vayu.description", "Draw {0} cards."),
            new KeyValuePair<string, string>("arrow.surya.name", "Arrow of Surya"),
            new KeyValuePair<string, string>("arrow.surya.description", "Gain {0} Prana."),
            new KeyValuePair<string, string>("arrow.dread.name", "Arrow of Dread"),
            new KeyValuePair<string, string>("arrow.dread.description", "Apply {0} Bhaya."),
            new KeyValuePair<string, string>("arrow.mohana.name", "Mohana Arrow"),
            new KeyValuePair<string, string>("arrow.mohana.description", "Apply {0} Moha to all enemies."),
            new KeyValuePair<string, string>("arrow.indra.name", "Arrow of Indra"),
            new KeyValuePair<string, string>("arrow.indra.description", "Deal {0} damage to all enemies."),
            new KeyValuePair<string, string>("arrow.shakti.name", "Arrow of Shakti"),
            new KeyValuePair<string, string>("arrow.shakti.description", "Gain {0} Focus this turn."),
            new KeyValuePair<string, string>("arrow.sanjivani.name", "Sanjivani Arrow"),
            new KeyValuePair<string, string>("arrow.sanjivani.description", "Heal {0}% of your maximum health."),
            new KeyValuePair<string, string>("arrow.dharma.name", "Arrow of Dharma"),
            new KeyValuePair<string, string>("arrow.dharma.description", "Gain {0} Dharma."),
            new KeyValuePair<string, string>("arrow.garuda.name", "Arrow of Garuda"),
            new KeyValuePair<string, string>("arrow.garuda.description", "Clear your Moha and draw {0} cards."),
            new KeyValuePair<string, string>("arrow.narayana.name", "Arrow of Narayana"),
            new KeyValuePair<string, string>("arrow.narayana.description", "Deal {0} damage."),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Special Arrow Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Special Arrow Strings", Build);
        }

        [DefaultAssetBuilder(115)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
