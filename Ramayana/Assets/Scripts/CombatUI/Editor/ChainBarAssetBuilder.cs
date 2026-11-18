// Creates the combo chain bar's assets: the Chain Bar style, with the soft disc sprite (made by the Status Visuals
// builder) wired in as its pip shape and as the fallback icon for chain kinds without one, then assigns the style to
// the Combat HUD style if it has none, and seeds the chain bar's interface text (the multiplier pattern, Finisher and
// the broken-chain message) into the UI string table. Existing assets, values, assigned references and edited text
// are never changed. Runs after the Status Visuals, Combat UI and Quiver builders.
using System.Collections.Generic;
using JourneyToLanka.CombatScene.EditorTools;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Localization;
using JourneyToLanka.Localization.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class ChainBarAssetBuilder
    {
        public const string StylePath = CombatUIAssetBuilder.Root + "/ChainBarStyle.asset";

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>(ChainKeys.Multiplier, "x{0}"),
            new KeyValuePair<string, string>(ChainKeys.Finisher, "Finisher"),
            new KeyValuePair<string, string>(ChainKeys.Broken, "Broken at {0} in a row"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Chain Bar", priority = AssetBuilderConstants.FeatureMenuPriority + 26)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Chain Bar", Build);
        }

        [DefaultAssetBuilder(138)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatUIAssetBuilder.Root, report);
            ChainBarStyleSO style = AssetBuilderSerialization.LoadOrCreateConfigured<ChainBarStyleSO>(StylePath, report, null);
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.UI, LocalizationAssetBuilder.English, EnglishEntries, report);
            if (style == null)
            {
                return;
            }

            var disc = AssetDatabase.LoadAssetAtPath<Sprite>(StatusVisualsAssetBuilder.DiscPath);
            if (disc != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_pipSprite", disc, report);
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_fallbackIconSprite", disc, report);
            }
            else
            {
                report.AddSkipped($"{StatusVisualsAssetBuilder.DiscPath} (missing, run Create Default Assets > Status Visuals first)");
            }

            var hudStyle = AssetDatabase.LoadAssetAtPath<CombatHudStyleSO>(CombatUIAssetBuilder.StylePath);
            if (hudStyle == null)
            {
                report.AddSkipped($"{CombatUIAssetBuilder.StylePath} (missing, run Create Default Assets > Combat UI first)");
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(hudStyle, "_chainBar", style, report);
        }
    }
}
