// Creates the combat pacing profile next to the other combat data. Values are set only when this run creates
// the asset, so tuned timings are never overwritten. Runs after the Combat builder.
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class CombatFlowAssetBuilder
    {
        public const string PacingPath = CombatAssetBuilder.Root + "/CombatPacing.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat Flow", priority = AssetBuilderConstants.FeatureMenuPriority + 11)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat Flow", Build);
        }

        [DefaultAssetBuilder(110)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderSerialization.LoadOrCreateConfigured<CombatPacingSO>(PacingPath, report, so =>
            {
                AssetBuilderSerialization.Require(so, "_enemyTurnStartDelay").floatValue = 0.5f;
                AssetBuilderSerialization.Require(so, "_enemyActionInterval").floatValue = 0.8f;
                AssetBuilderSerialization.Require(so, "_enemyTurnEndDelay").floatValue = 0.5f;
            });
        }
    }
}
