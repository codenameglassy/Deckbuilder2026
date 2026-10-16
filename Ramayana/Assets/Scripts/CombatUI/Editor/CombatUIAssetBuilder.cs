// Creates the combat UI's assets: the Combat HUD style with its default look, and the CombatController anchor the
// HUD uses to find the running fight. Existing assets are never changed. Runs after the Combat Scene builder.
using JourneyToLanka.CombatScene;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CombatUIAssetBuilder
    {
        public const string Root = AssetBuilderConstants.DataRoot + "/CombatUI";
        public const string StylePath = Root + "/CombatHudStyle.asset";
        public const string ControllerAnchorPath = Root + "/CombatControllerAnchor.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat UI", priority = AssetBuilderConstants.FeatureMenuPriority + 14)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat UI", Build);
        }

        [DefaultAssetBuilder(130)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(Root, report);
            AssetBuilderSerialization.LoadOrCreateConfigured<CombatHudStyleSO>(StylePath, report, null);
            AssetBuilderSerialization.LoadOrCreateConfigured<CombatControllerAnchorSO>(ControllerAnchorPath, report,
                so => AssetBuilderSerialization.SetString(so, "_description", "The running fight's CombatController, provided by the controller's anchor provider."));
        }
    }
}
