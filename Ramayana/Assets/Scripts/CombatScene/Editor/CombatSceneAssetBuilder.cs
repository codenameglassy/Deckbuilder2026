// Creates the combat scene's assets: the CombatStarted and CombatEnded channels and the default combatant view
// style. Values are set only on assets this run creates, so tuned styles are never overwritten.
// Runs after the Combat and Combat Flow builders.
using JourneyToLanka.Combat;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Core.Events;
using UnityEditor;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class CombatSceneAssetBuilder
    {
        public const string Root = AssetBuilderConstants.DataRoot + "/CombatScene";
        public const string ChannelsFolder = Root + "/Channels";
        public const string CombatStartedPath = ChannelsFolder + "/CombatStarted.asset";
        public const string CombatEndedPath = ChannelsFolder + "/CombatEnded.asset";
        public const string ViewStylePath = Root + "/CombatantViewStyle.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat Scene", priority = AssetBuilderConstants.FeatureMenuPriority + 12)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat Scene", Build);
        }

        [DefaultAssetBuilder(120)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(ChannelsFolder, report);

            AssetBuilderSerialization.LoadOrCreateConfigured<VoidEventChannelSO>(CombatStartedPath, report,
                so => AssetBuilderSerialization.SetString(so, "_description", "Raised once when a fight has been built and is about to begin."));

            AssetBuilderSerialization.LoadOrCreateConfigured<CombatResultEventChannelSO>(CombatEndedPath, report,
                so => AssetBuilderSerialization.SetString(so, "_description", "Raised once when a fight ends, with the result and Rama's remaining health."));

            AssetBuilderSerialization.LoadOrCreateConfigured<CombatantViewStyleSO>(ViewStylePath, report, null);
        }
    }
}
