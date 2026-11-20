// Creates the Card Keywords asset that colours keywords in card descriptions, fills it (only while it is still empty)
// with Kavach (sky blue), Prana (pale gold), Exhaust (grey) and the five statuses (Focus amber, Moha violet, Bhaya red,
// Dahan orange, Dharma jade) using each status's own name key, seeds the English words for Kavach, Prana and Exhaust
// into the Combat string table, and assigns the asset to the Combat HUD style if it has none. Statuses whose asset is
// missing are left out and reported. Existing assets, entries, colours, assigned references and edited text are never
// changed. Runs after the Status, Status Strings and Combat UI builders.
using System.Collections.Generic;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Localization;
using JourneyToLanka.Localization.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CardKeywordsAssetBuilder
    {
        public const string KeywordsPath = CombatUIAssetBuilder.Root + "/CardKeywords.asset";

        private static readonly Color KavachColor = new Color(0.5f, 0.8f, 1f);
        private static readonly Color PranaColor = new Color(1f, 0.88f, 0.5f);
        private static readonly Color ExhaustColor = new Color(0.68f, 0.68f, 0.68f);
        private static readonly Color FocusColor = new Color(1f, 0.7f, 0.28f);
        private static readonly Color MohaColor = new Color(0.76f, 0.6f, 1f);
        private static readonly Color BhayaColor = new Color(1f, 0.42f, 0.45f);
        private static readonly Color DahanColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color DharmaColor = new Color(0.55f, 0.95f, 0.72f);

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>(CardKeywordKeys.Kavach, "Kavach"),
            new KeyValuePair<string, string>(CardKeywordKeys.Prana, "Prana"),
            new KeyValuePair<string, string>(CardKeywordKeys.Exhaust, "Exhaust"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Card Keywords", priority = AssetBuilderConstants.FeatureMenuPriority + 27)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Card Keywords", Build);
        }

        [DefaultAssetBuilder(139)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatUIAssetBuilder.Root, report);
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);

            CardKeywordsSO keywords =
                AssetBuilderSerialization.LoadOrCreateConfigured<CardKeywordsSO>(KeywordsPath, report, null);
            if (keywords == null)
            {
                return;
            }

            var entries = new List<(string Key, Color Color)>
            {
                (CardKeywordKeys.Kavach, KavachColor),
                (CardKeywordKeys.Prana, PranaColor),
                (CardKeywordKeys.Exhaust, ExhaustColor),
            };
            AddStatus(entries, StatusAssetBuilder.FocusPath, FocusColor, report);
            AddStatus(entries, StatusAssetBuilder.MohaPath, MohaColor, report);
            AddStatus(entries, StatusAssetBuilder.BhayaPath, BhayaColor, report);
            AddStatus(entries, StatusAssetBuilder.DahanPath, DahanColor, report);
            AddStatus(entries, StatusAssetBuilder.DharmaPath, DharmaColor, report);

            AssetBuilderSerialization.FillArrayIfEmpty(keywords, "_entries", entries.Count, (element, i) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_nameKey").stringValue = entries[i].Key;
                AssetBuilderSerialization.RequireRelative(element, "_color").colorValue = entries[i].Color;
            }, report);

            var style = AssetDatabase.LoadAssetAtPath<CombatHudStyleSO>(CombatUIAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped(
                    $"{CombatUIAssetBuilder.StylePath} (missing, run Create Default Assets > Combat UI first)");
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(style, "_cardKeywords", keywords, report);
        }

        private static void AddStatus(List<(string Key, Color Color)> entries, string path, Color color,
            AssetBuilderReport report)
        {
            var status = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(path);
            if (status == null || string.IsNullOrEmpty(status.NameKey))
            {
                report.AddSkipped(
                    $"{path} keyword (status or its name key missing, run Create Default Assets > Status first)");
                return;
            }

            entries.Add((status.NameKey, color));
        }
    }
}
