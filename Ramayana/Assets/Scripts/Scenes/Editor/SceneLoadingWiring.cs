// Scene wiring for scene loading, run with the Boot object selected. Adds the Scene Loader Installer and lists it
// on the Bootstrapper after the existing installers, adds Boot Scene Flow, and fills its empty fields: the
// BootCompleted and Load Scene channels, and the first scene when exactly one Scene Definition exists.
// Everything is recorded with Undo; nothing already assigned is replaced.
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Scenes.EditorTools
{
    public static class SceneLoadingWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Scene Loading";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 3)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Scene Loading", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.activeGameObject != null;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            GameObject boot = Selection.activeGameObject;
            if (!boot.TryGetComponent(out Bootstrapper bootstrapper))
            {
                report.AddSkipped($"{boot.name} has no Bootstrapper; select the Boot object in the Boot scene");
                return;
            }

            SceneLoaderInstaller installer = GetOrAdd<SceneLoaderInstaller>(boot, report);
            AppendInstaller(bootstrapper, installer, report);

            BootSceneFlow flow = GetOrAdd<BootSceneFlow>(boot, report);
            Wire(flow, "_bootCompleted", AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>(CoreAssetBuilder.BootCompletedChannelPath), report);
            Wire(flow, "_loadScene", AssetDatabase.LoadAssetAtPath<LoadSceneEventChannelSO>(SceneAssetBuilder.LoadSceneChannelPath), report);
            WireFirstScene(flow, report);
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

        private static void AppendInstaller(Bootstrapper bootstrapper, ServiceInstaller installer, AssetBuilderReport report)
        {
            using var serializedObject = new SerializedObject(bootstrapper);
            SerializedProperty installers = AssetBuilderSerialization.Require(serializedObject, "_installers");

            for (int i = 0; i < installers.arraySize; i++)
            {
                if (installers.GetArrayElementAtIndex(i).objectReferenceValue == installer)
                {
                    report.AddExisted($"{bootstrapper.name} installer list contains {installer.GetType().Name}");
                    return;
                }
            }

            int index = installers.arraySize;
            installers.arraySize = index + 1;
            installers.GetArrayElementAtIndex(index).objectReferenceValue = installer;
            serializedObject.ApplyModifiedProperties();
            report.AddWired($"{bootstrapper.name} installer list += {installer.GetType().Name}");
        }

        private static void WireFirstScene(BootSceneFlow flow, AssetBuilderReport report)
        {
            string[] guids = AssetDatabase.IsValidFolder(SceneAssetBuilder.Root)
                ? AssetDatabase.FindAssets("t:" + nameof(SceneDefinitionSO), new[] { SceneAssetBuilder.Root })
                : new string[0];
            if (guids.Length != 1)
            {
                using var serializedObject = new SerializedObject(flow);
                if (AssetBuilderSerialization.Require(serializedObject, "_firstScene").objectReferenceValue == null)
                {
                    report.AddSkipped($"{flow.name}._firstScene ({guids.Length} Scene Definitions found, assign the first scene by hand)");
                }

                return;
            }

            var definition = AssetDatabase.LoadAssetAtPath<SceneDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
            Wire(flow, "_firstScene", definition, report);
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
