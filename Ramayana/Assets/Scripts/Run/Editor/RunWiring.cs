// Scene wiring for runs. With the Boot object selected it adds the Run Service Installer (listed on the
// Bootstrapper after the existing installers) and the Run Director with its CombatEnded, Load Scene and card reward
// channels.
// With the CombatController selected it fills its Run Definition with the slice run. Select both and run it once.
// Recorded with Undo; nothing already assigned is replaced.
using JourneyToLanka.Combat;
using JourneyToLanka.CombatScene;
using JourneyToLanka.CombatScene.EditorTools;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Scenes;
using JourneyToLanka.Scenes.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Run.EditorTools
{
    public static class RunWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Run";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 17)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Run", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            GameObject[] selection = Selection.gameObjects;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];

                if (target.TryGetComponent(out Bootstrapper bootstrapper))
                {
                    WireBoot(target, bootstrapper, report);
                }

                if (target.TryGetComponent(out CombatController controller))
                {
                    Wire(controller, "_run", AssetDatabase.LoadAssetAtPath<RunDefinitionSO>(RunAssetBuilder.SliceRunPath), report);
                }
            }
        }

        private static void WireBoot(GameObject boot, Bootstrapper bootstrapper, AssetBuilderReport report)
        {
            RunServiceInstaller installer = ServiceInstallerWiring.GetOrAddComponent<RunServiceInstaller>(boot, report);
            ServiceInstallerWiring.EnsureInstallerListed(bootstrapper, installer, report);

            RunDirector director = ServiceInstallerWiring.GetOrAddComponent<RunDirector>(boot, report);
            Wire(director, "_combatEnded", AssetDatabase.LoadAssetAtPath<CombatResultEventChannelSO>(CombatSceneAssetBuilder.CombatEndedPath), report);
            Wire(director, "_loadScene", AssetDatabase.LoadAssetAtPath<LoadSceneEventChannelSO>(SceneAssetBuilder.LoadSceneChannelPath), report);
            Wire(director, "_cardRewardOffered", AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>(RunAssetBuilder.CardRewardOfferedPath), report);
            Wire(director, "_cardRewardChosen", AssetDatabase.LoadAssetAtPath<IntEventChannelSO>(RunAssetBuilder.CardRewardChosenPath), report);
        }

        private static void Wire(Object owner, string propertyPath, Object value, AssetBuilderReport report)
        {
            if (value == null)
            {
                report.AddSkipped($"{owner.name}.{propertyPath} (asset missing, run Create Default Assets > All first)");
                return;
            }

            AssetBuilderUtility.WireSceneFieldIfNull(owner, propertyPath, value, report);
        }
    }
}
