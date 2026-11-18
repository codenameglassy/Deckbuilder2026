// Scene wiring for the targeting line. Fills the Targeting Line style and the combat camera anchor into the empty
// fields of every selected CombatHudView, adds a CombatCameraAnchorProvider to the scene camera (a selected camera, or
// the first camera in the open scenes) and fills its anchor. Recorded with Undo; nothing already assigned is replaced.
using JourneyToLanka.CombatScene;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class TargetingLineWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Targeting Line";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 22)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Targeting Line", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            var style = AssetDatabase.LoadAssetAtPath<TargetingLineStyleSO>(TargetingLineAssetBuilder.StylePath);
            var anchor = AssetDatabase.LoadAssetAtPath<CombatCameraAnchorSO>(TargetingLineAssetBuilder.CameraAnchorPath);
            if (style == null || anchor == null)
            {
                report.AddSkipped("Targeting Line assets (missing, run Create Default Assets > All first)");
                return;
            }

            GameObject[] selection = Selection.gameObjects;
            bool foundHud = false;
            Camera camera = null;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];
                if (target.TryGetComponent(out CombatHudView hud))
                {
                    foundHud = true;
                    AssetBuilderUtility.WireSceneFieldIfNull(hud, "_targetingLineStyle", style, report);
                    AssetBuilderUtility.WireSceneFieldIfNull(hud, "_cameraAnchor", anchor, report);
                }

                if (camera == null)
                {
                    target.TryGetComponent(out camera);
                }
            }

            if (!foundHud)
            {
                report.AddSkipped("Combat HUD fields (select the object with the Combat HUD view)");
            }

            if (camera == null)
            {
                camera = Object.FindAnyObjectByType<Camera>();
            }

            if (camera == null)
            {
                report.AddSkipped("CombatCameraAnchorProvider (no camera in the open scenes)");
                return;
            }

            if (!camera.TryGetComponent(out CombatCameraAnchorProvider provider))
            {
                provider = Undo.AddComponent<CombatCameraAnchorProvider>(camera.gameObject);
                report.AddCreated($"{camera.name}.CombatCameraAnchorProvider");
            }
            else
            {
                report.AddExisted($"{camera.name}.CombatCameraAnchorProvider");
            }

            AssetBuilderUtility.WireSceneFieldIfNull(provider, "_anchor", anchor, report);
        }
    }
}
