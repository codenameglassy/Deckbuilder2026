// Creates the hand layout strategies (Flat and Fan) and, if the Combat HUD style has no Hand Layout yet, assigns the
// Fan. To swap layouts, drag either asset (or any new HandLayoutSO) into the style's Hand Layout field, even in Play
// mode. Existing assets and an assigned layout are never changed. Runs after the Combat UI builder.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class HandLayoutAssetBuilder
    {
        public const string Folder = CombatUIAssetBuilder.Root + "/HandLayouts";
        public const string FlatPath = Folder + "/FlatHandLayout.asset";
        public const string FanPath = Folder + "/FanHandLayout.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Hand Layouts", priority = AssetBuilderConstants.FeatureMenuPriority + 23)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Hand Layouts", Build);
        }

        [DefaultAssetBuilder(134)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(Folder, report);
            AssetBuilderSerialization.LoadOrCreateConfigured<FlatHandLayoutSO>(FlatPath, report, null);
            FanHandLayoutSO fan = AssetBuilderSerialization.LoadOrCreateConfigured<FanHandLayoutSO>(FanPath, report, null);

            var style = AssetDatabase.LoadAssetAtPath<CombatHudStyleSO>(CombatUIAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatUIAssetBuilder.StylePath} (missing, run Create Default Assets > Combat UI first)");
                return;
            }

            if (fan != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_handLayout", fan, report);
            }
        }
    }
}
