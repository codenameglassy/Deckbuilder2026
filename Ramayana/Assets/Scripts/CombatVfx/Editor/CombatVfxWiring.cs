// Scene wiring for combat VFX. On each selected object with a CombatController it adds a CombatVfxDirector if
// missing; it finds the scene's QuadVfxSpawner or creates one on a new root object named "Combat VFX" at the origin
// in that controller's scene, and adds a SparkBurstSpawner, a KavachShellPresenter, a PrefabVfxSpawner and a
// StatusAuraPresenter beside it if missing; then it fills the director's empty style, spawner, Kavach presenter, accent
// spawner, status aura presenter and combat camera anchor fields and the empty style field of each component on the
// "Combat VFX" object. Every CombatantView in that scene gets a KavachShellTarget if missing;
// an empty target gets one row for the view's own renderer, and rows without a shell mesh get one baked by
// KavachShellMeshAssetBuilder. Recorded with Undo; nothing already added or assigned is replaced. Save the scene
// afterwards: Play From Boot loads it from disk.
using JourneyToLanka.CombatScene;
using JourneyToLanka.CombatUI.EditorTools;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class CombatVfxWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Combat VFX";
        private const string SpawnerObjectName = "Combat VFX";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 30)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Combat VFX", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            var style = AssetDatabase.LoadAssetAtPath<CombatVfxStyleSO>(CombatVfxAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatVfxAssetBuilder.StylePath} (missing, run Create Default Assets > All first)");
            }

            var cameraAnchor = AssetDatabase.LoadAssetAtPath<CombatCameraAnchorSO>(TargetingLineAssetBuilder.CameraAnchorPath);
            if (cameraAnchor == null)
            {
                report.AddSkipped($"{TargetingLineAssetBuilder.CameraAnchorPath} (missing, run Create Default Assets > All first)");
            }

            GameObject[] selection = Selection.gameObjects;
            bool foundController = false;

            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];
                if (!target.TryGetComponent(out CombatController _))
                {
                    continue;
                }

                foundController = true;
                CombatVfxDirector director = GetOrAdd<CombatVfxDirector>(target, report);
                QuadVfxSpawner spawner = FindOrCreateSpawner(target.scene, report);
                SparkBurstSpawner sparks = GetOrAdd<SparkBurstSpawner>(spawner.gameObject, report);
                KavachShellPresenter kavach = GetOrAdd<KavachShellPresenter>(spawner.gameObject, report);
                PrefabVfxSpawner accents = GetOrAdd<PrefabVfxSpawner>(spawner.gameObject, report);
                StatusAuraPresenter auras = GetOrAdd<StatusAuraPresenter>(spawner.gameObject, report);

                if (style != null)
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(director, "_style", style, report);
                    AssetBuilderUtility.WireSceneFieldIfNull(spawner, "_style", style, report);
                    AssetBuilderUtility.WireSceneFieldIfNull(sparks, "_style", style, report);
                    AssetBuilderUtility.WireSceneFieldIfNull(kavach, "_style", style, report);
                    AssetBuilderUtility.WireSceneFieldIfNull(accents, "_style", style, report);
                    AssetBuilderUtility.WireSceneFieldIfNull(auras, "_style", style, report);
                }

                AssetBuilderUtility.WireSceneFieldIfNull(director, "_quads", spawner, report);
                AssetBuilderUtility.WireSceneFieldIfNull(director, "_sparks", sparks, report);
                AssetBuilderUtility.WireSceneFieldIfNull(director, "_kavach", kavach, report);
                AssetBuilderUtility.WireSceneFieldIfNull(director, "_accents", accents, report);
                AssetBuilderUtility.WireSceneFieldIfNull(director, "_auras", auras, report);
                if (cameraAnchor != null)
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(director, "_cameraAnchor", cameraAnchor, report);
                }

                WireShellTargets(target.scene, report);
            }

            if (!foundController)
            {
                report.AddSkipped("Combat VFX Director (select the object with the Combat Controller)");
            }
        }

        private static void WireShellTargets(Scene scene, AssetBuilderReport report)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                CombatantView[] views = roots[r].GetComponentsInChildren<CombatantView>(true);
                for (int v = 0; v < views.Length; v++)
                {
                    KavachShellTarget shellTarget = GetOrAdd<KavachShellTarget>(views[v].gameObject, report);
                    FillOwnRendererIfEmpty(shellTarget, report);
                    KavachShellMeshAssetBuilder.WireTarget(shellTarget, report);
                }
            }
        }

        private static void FillOwnRendererIfEmpty(KavachShellTarget shellTarget, AssetBuilderReport report)
        {
            if (shellTarget.Entries.Count > 0)
            {
                return;
            }

            if (!shellTarget.TryGetComponent(out Renderer body))
            {
                report.AddSkipped($"{shellTarget.name}.{nameof(KavachShellTarget)} (no renderer on the view; list its body renderers by hand)");
                return;
            }

            using var serializedObject = new SerializedObject(shellTarget);
            SerializedProperty entries = serializedObject.FindProperty("_entries");
            entries.arraySize = 1;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("_source").objectReferenceValue = body;
            serializedObject.ApplyModifiedProperties();
            report.AddWired($"{shellTarget.name}.{nameof(KavachShellTarget)} -> {body.name}");
        }

        private static QuadVfxSpawner FindOrCreateSpawner(Scene scene, AssetBuilderReport report)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                QuadVfxSpawner existing = roots[i].GetComponentInChildren<QuadVfxSpawner>(true);
                if (existing != null)
                {
                    report.AddExisted($"{existing.name}.{nameof(QuadVfxSpawner)}");
                    return existing;
                }
            }

            var spawnerObject = new GameObject(SpawnerObjectName);
            SceneManager.MoveGameObjectToScene(spawnerObject, scene);
            Undo.RegisterCreatedObjectUndo(spawnerObject, "Create Combat VFX");
            QuadVfxSpawner spawner = Undo.AddComponent<QuadVfxSpawner>(spawnerObject);
            report.AddCreated($"{SpawnerObjectName} ({scene.name}).{nameof(QuadVfxSpawner)}");
            return spawner;
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
