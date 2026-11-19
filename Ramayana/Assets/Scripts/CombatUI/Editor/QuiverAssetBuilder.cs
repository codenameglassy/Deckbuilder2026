// Creates the quiver's assets: the Quiver style, with the soft disc sprite (made by the Status Visuals builder) wired
// in as its slot shape and as the fallback for arrows without an icon, then assigns the style to the Combat HUD style
// if it has none, and seeds the quiver caption into the UI string table. Existing assets, values, assigned references
// and edited text are never changed. Runs after the Status Visuals and Combat UI builders.
using System.Collections.Generic;
using JourneyToLanka.CombatScene.EditorTools;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Localization;
using JourneyToLanka.Localization.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class QuiverAssetBuilder
    {
        public const string StylePath = CombatUIAssetBuilder.Root + "/QuiverStyle.asset";

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>(QuiverKeys.Caption, "Special arrow"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Quiver", priority = AssetBuilderConstants.FeatureMenuPriority + 26)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Quiver", Build);
        }

        [DefaultAssetBuilder(137)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatUIAssetBuilder.Root, report);
            QuiverStyleSO style = AssetBuilderSerialization.LoadOrCreateConfigured<QuiverStyleSO>(StylePath, report, null);
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.UI, LocalizationAssetBuilder.English, EnglishEntries, report);
            if (style == null)
            {
                return;
            }

            var disc = AssetDatabase.LoadAssetAtPath<Sprite>(StatusVisualsAssetBuilder.DiscPath);
            if (disc != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_slotSprite", disc, report);
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_fallbackArrowSprite", disc, report);
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

            AssetBuilderUtility.WireAssetFieldIfNull(hudStyle, "_quiver", style, report);
        }
    }
}
