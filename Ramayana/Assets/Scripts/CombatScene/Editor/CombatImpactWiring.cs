// Scene wiring for combat impact. On each selected object with a CombatController it adds a CombatImpactDirector and
// a HitStop if missing; it adds a CombatCameraShake to the scene camera (a selected camera, or the first camera in the
// open scenes); then it fills the director's empty style, hit-stop and camera shake fields. Recorded with Undo;
// nothing already added or assigned is replaced.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class CombatImpactWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Combat Impact";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 20)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Combat Impact", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            var style = AssetDatabase.LoadAssetAtPath<CombatImpactStyleSO>(CombatImpactAssetBuilder.ImpactStylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatImpactAssetBuilder.ImpactStylePath} (missing, run Create Default Assets > All first)");
            }

            GameObject[] selection = Selection.gameObjects;
            CombatCameraShake cameraShake = EnsureCameraShake(selection, report);
            bool foundController = false;

            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];
                if (!target.TryGetComponent(out CombatController _))
                {
                    continue;
                }

                foundController = true;
                CombatImpactDirector director = GetOrAdd<CombatImpactDirector>(target, report);
                HitStop hitStop = GetOrAdd<HitStop>(target, report);

                if (style != null)
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(director, "_style", style, report);
                }

                AssetBuilderUtility.WireSceneFieldIfNull(director, "_hitStop", hitStop, report);

                if (cameraShake != null)
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(director, "_cameraShake", cameraShake, report);
                }
            }

            if (!foundController)
            {
                report.AddSkipped("Combat Impact Director (select the object with the Combat Controller)");
            }
        }

        private static CombatCameraShake EnsureCameraShake(GameObject[] selection, AssetBuilderReport report)
        {
            Camera camera = null;
            for (int i = 0; i < selection.Length && camera == null; i++)
            {
                selection[i].TryGetComponent(out camera);
            }

            if (camera == null)
            {
                camera = Object.FindAnyObjectByType<Camera>();
            }

            if (camera == null)
            {
                report.AddSkipped("CombatCameraShake (no camera in the open scenes)");
                return null;
            }

            return GetOrAdd<CombatCameraShake>(camera.gameObject, report);
        }

        private static T GetOrAdd<T>(GameObject target, AssetBuilderReport report) where T : Component
        {
            if (target.TryGetComponent(out T existing))
            {
                report.AddExisted($"{target.name}.{typeof(T).Name}");
                return existing;
            }

            T added = Undo.AddComponent<T>(target);
            report.AddCreated($"{target.name}.{typeof(T).Name}");
            return added;
        }
    }
}
