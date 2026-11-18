// Seeds the English interface text for the combat HUD into the UI string table: Prana, the draw and discard
// piles, End Turn, the turn banners, Victory and Defeat, the threat panel captions, the messages shown when a card
// cannot be played, the pile viewer, the enemy info panel and the lethal tag of the outcome preview. Only missing or empty entries are filled, so translated
// or edited text is never overwritten.
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
            new KeyValuePair<string, string>(CombatUIKeys.YourTurn, "Your Turn"),
            new KeyValuePair<string, string>(CombatUIKeys.Victory, "Victory"),
            new KeyValuePair<string, string>(CombatUIKeys.Defeat, "Defeat"),
            new KeyValuePair<string, string>(CombatUIKeys.ThreatIncoming, "Incoming"),
            new KeyValuePair<string, string>(CombatUIKeys.ThreatKavach, "Kavach"),
            new KeyValuePair<string, string>(CombatUIKeys.ThreatTaken, "You take"),
            new KeyValuePair<string, string>(CombatUIKeys.NotEnoughPrana, "Not enough Prana"),
            new KeyValuePair<string, string>(CombatUIKeys.ChooseTarget, "Choose a target"),
            new KeyValuePair<string, string>(CombatUIKeys.DrawPileTitle, "Draw Pile"),
            new KeyValuePair<string, string>(CombatUIKeys.DiscardPileTitle, "Discard Pile"),
            new KeyValuePair<string, string>(CombatUIKeys.PileEmpty, "No cards here"),
            new KeyValuePair<string, string>(CombatUIKeys.Close, "Close"),
            new KeyValuePair<string, string>(CombatUIKeys.InfoHealth, "Health"),
            new KeyValuePair<string, string>(CombatUIKeys.InfoKavach, "Kavach"),
            new KeyValuePair<string, string>(CombatUIKeys.IntentAttack, "Attack"),
            new KeyValuePair<string, string>(CombatUIKeys.IntentDefend, "Defend"),
            new KeyValuePair<string, string>(CombatUIKeys.Lethal, "Lethal"),
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
