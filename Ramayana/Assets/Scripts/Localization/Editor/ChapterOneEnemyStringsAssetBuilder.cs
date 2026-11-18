// Seeds the English names of the chapter one enemies added after the slice's first fight into the Combat string
// table. Only missing or empty entries are filled.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class ChapterOneEnemyStringsAssetBuilder
    {
        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new KeyValuePair<string, string>("enemy.hound.name", "Cursed Hound"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Chapter One Enemy Strings", priority = AssetBuilderConstants.FeatureMenuPriority + 13)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Chapter One Enemy Strings", Build);
        }

        [DefaultAssetBuilder(107)]
        public static void Build(AssetBuilderReport report)
        {
            LocalizationBuilderUtility.AddEntriesIfMissing(
                LocalizationTableNames.Combat, LocalizationAssetBuilder.English, EnglishEntries, report);
        }
    }
}
