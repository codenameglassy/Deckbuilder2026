// Scene wiring for the combat inspect widgets. Fills the Combat Inspect style into the empty field of every selected
// CombatHudView, with Undo. Assigned values are never replaced.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CombatInspectWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Combat Inspect";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 21)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Combat Inspect", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            var style = AssetDatabase.LoadAssetAtPath<CombatInspectStyleSO>(CombatInspectAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatInspectAssetBuilder.StylePath} (missing, run Create Default Assets > All first)");
                return;
            }

            bool foundHud = false;
            GameObject[] selection = Selection.gameObjects;
            for (int i = 0; i < selection.Length; i++)
            {
                if (selection[i].TryGetComponent(out CombatHudView hud))
                {
                    foundHud = true;
                    AssetBuilderUtility.WireSceneFieldIfNull(hud, "_inspectStyle", style, report);
                }
            }

            if (!foundHud)
            {
                report.AddSkipped("Combat Inspect Style (select the object with the Combat HUD view)");
            }
        }
    }
}
