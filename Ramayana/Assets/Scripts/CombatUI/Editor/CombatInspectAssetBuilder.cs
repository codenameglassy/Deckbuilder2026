// Creates the combat inspect style (pile viewer, enemy info panel) next to the Combat HUD style.
// Existing assets are never changed. Runs after the Combat UI builder.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CombatInspectAssetBuilder
    {
        public const string StylePath = CombatUIAssetBuilder.Root + "/CombatInspectStyle.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat Inspect", priority = AssetBuilderConstants.FeatureMenuPriority + 21)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat Inspect", Build);
        }

        [DefaultAssetBuilder(132)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatUIAssetBuilder.Root, report);
            AssetBuilderSerialization.LoadOrCreateConfigured<CombatInspectStyleSO>(StylePath, report, null);
        }
    }
}
