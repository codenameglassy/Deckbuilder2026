// Creates the combat impact style (hit-stop, heavy-hit threshold, camera shake) next to the other combat scene
// assets. Existing assets are never changed. Runs after the Combat Scene builder.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class CombatImpactAssetBuilder
    {
        public const string ImpactStylePath = CombatSceneAssetBuilder.Root + "/CombatImpactStyle.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat Impact", priority = AssetBuilderConstants.FeatureMenuPriority + 20)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat Impact", Build);
        }

        [DefaultAssetBuilder(127)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderSerialization.LoadOrCreateConfigured<CombatImpactStyleSO>(ImpactStylePath, report, null);
        }
    }
}
