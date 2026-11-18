// Scene wiring for card targeting. Fills the marker style on every selected EnemyTargetView, and adds a
// PhysicsRaycaster to the scene camera (a selected camera, or the first camera in the open scenes) so pointer clicks
// reach the enemies' colliders. Recorded with Undo; nothing already assigned or added is replaced.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class TargetingWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Targeting";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 19)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Targeting", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            var style = AssetDatabase.LoadAssetAtPath<TargetMarkerStyleSO>(TargetingAssetBuilder.MarkerStylePath);
            Camera camera = null;

            GameObject[] selection = Selection.gameObjects;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];

                if (target.TryGetComponent(out EnemyTargetView view))
                {
                    if (style != null)
                    {
                        AssetBuilderUtility.WireSceneFieldIfNull(view, "_style", style, report);
                    }
                    else
                    {
                        report.AddSkipped($"{TargetingAssetBuilder.MarkerStylePath} (missing, run Create Default Assets > All first)");
                    }
                }

                if (camera == null)
                {
                    target.TryGetComponent(out camera);
                }
            }

            if (camera == null)
            {
                camera = Object.FindAnyObjectByType<Camera>();
            }

            if (camera == null)
            {
                report.AddSkipped("PhysicsRaycaster (no camera in the open scenes)");
                return;
            }

            if (camera.TryGetComponent(out PhysicsRaycaster _))
            {
                report.AddExisted($"{camera.name}.PhysicsRaycaster");
                return;
            }

            Undo.AddComponent<PhysicsRaycaster>(camera.gameObject);
            report.AddCreated($"{camera.name}.PhysicsRaycaster");
        }
    }
}
