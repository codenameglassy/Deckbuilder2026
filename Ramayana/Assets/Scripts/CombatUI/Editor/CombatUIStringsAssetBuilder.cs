// Seeds the English interface text for the combat HUD into the UI string table: Prana, the draw and discard
// piles, End Turn, Enemy Turn, Victory and Defeat. Only missing or empty entries are filled.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Localization;
using JourneyToLanka.Localization.EditorTools;
using UnityEditor;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CombatUIStringsAssetBuilder
    {
        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>(CombatUIKeys.Prana, "Prana"),
            new KeyValuePair<string, string>(CombatUIKeys.DrawPile, "Draw"),
            new KeyValuePair<string, string>(CombatUIKeys.DiscardPile, "Discard"),
            new KeyValuePair<string, string>(CombatUIKeys.EndTurn, "End Turn"),
            new KeyValuePair<string, string>(CombatUIKeys.EnemyTurn, "Enemy Turn"),
            new KeyValuePair<string, string>(CombatUIKeys.Victory, "Victory"),
            new KeyValuePair<string, string>(CombatUIKeys.Defeat, "Defeat"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat UI Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 15)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat UI Strings", Build);
        }

        [DefaultAssetBuilder(131)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.UI, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
