// Creates the combat feedback style (damage numbers and enemy intents) next to the other combat scene assets.
// Existing assets are never changed. Runs after the Combat Scene builder.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class CombatFeedbackAssetBuilder
    {
        public const string FeedbackStylePath = CombatSceneAssetBuilder.Root + "/CombatFeedbackStyle.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat Feedback", priority = AssetBuilderConstants.FeatureMenuPriority + 16)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat Feedback", Build);
        }

        [DefaultAssetBuilder(125)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderSerialization.LoadOrCreateConfigured<CombatFeedbackStyleSO>(FeedbackStylePath, report, null);
        }
    }
}
