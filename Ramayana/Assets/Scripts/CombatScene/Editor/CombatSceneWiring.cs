// Scene wiring for the combat scene. Fills only empty serialized fields on the selected objects, with Undo:
// a CombatController gets the combat rules, player, Scout encounter, pacing and both channels; a CombatantView
// gets the default style. Assigned values are never replaced. Run the Create Default Assets builders first.
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Core.Events;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class CombatSceneWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Combat Scene";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 12)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Combat Scene", WireSelection);
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

                if (target.TryGetComponent(out CombatController controller))
                {
                    WireController(controller, report);
                }

                if (target.TryGetComponent(out CombatantView view))
                {
                    Wire(view, "_style", Load<CombatantViewStyleSO>(CombatSceneAssetBuilder.ViewStylePath, report), report);
                }
            }
        }

        private static void WireController(CombatController controller, AssetBuilderReport report)
        {
            Wire(controller, "_rules", Load<CombatRulesSO>(CombatAssetBuilder.CombatRulesPath, report), report);
            Wire(controller, "_player", Load<PlayerDefinitionSO>(CombatAssetBuilder.PlayerPath, report), report);
            Wire(controller, "_encounter", Load<CombatEncounterSO>(CombatAssetBuilder.ScoutEncounterPath, report), report);
            Wire(controller, "_pacing", Load<CombatPacingSO>(CombatFlowAssetBuilder.PacingPath, report), report);
            Wire(controller, "_combatStarted", Load<VoidEventChannelSO>(CombatSceneAssetBuilder.CombatStartedPath, report), report);
            Wire(controller, "_combatEnded", Load<CombatResultEventChannelSO>(CombatSceneAssetBuilder.CombatEndedPath, report), report);
        }

        private static void Wire(Object owner, string propertyPath, Object value, AssetBuilderReport report)
        {
            if (value != null)
            {
                AssetBuilderUtility.WireSceneFieldIfNull(owner, propertyPath, value, report);
            }
        }

        private static T Load<T>(string path, AssetBuilderReport report) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                report.AddSkipped($"{path} (missing, run Create Default Assets > All first)");
            }

            return asset;
        }
    }
}
