// Creates the Core data folders, the BootCompleted channel and the Boot scene (Bootstrapper + TickRunner, wired),
// and adds the Boot scene first in Build Settings. Never modifies an existing scene or asset.
using System.Collections.Generic;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.Core.EditorTools
{
    public static class CoreAssetBuilder
    {
        public const string CoreDataRoot = AssetBuilderConstants.DataRoot + "/Core";
        public const string ChannelsFolder = CoreDataRoot + "/Channels";
        public const string AnchorsFolder = CoreDataRoot + "/Anchors";
        public const string BootCompletedChannelPath = ChannelsFolder + "/BootCompleted.asset";
        public const string BootScenePath = AssetBuilderConstants.ScenesRoot + "/Boot.unity";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Core", priority = AssetBuilderConstants.FeatureMenuPriority)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Core", Build);
        }

        [DefaultAssetBuilder(0)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(ChannelsFolder, report);
            AssetBuilderUtility.EnsureFolder(AnchorsFolder, report);
            AssetBuilderUtility.EnsureFolder(AssetBuilderConstants.ScenesRoot, report);

            VoidEventChannelSO bootCompleted = AssetBuilderUtility.LoadOrCreate<VoidEventChannelSO>(BootCompletedChannelPath, report);
            EnsureBootScene(bootCompleted, report);
            EnsureBootSceneInBuildSettings(report);
        }

        private static void EnsureBootScene(VoidEventChannelSO bootCompleted, AssetBuilderReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null)
            {
                report.AddExisted(BootScenePath);
                return;
            }

            if (HasUntitledOpenScene())
            {
                report.AddSkipped($"{BootScenePath} (save or close the untitled open scene, then run again)");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            var bootObject = new GameObject("Boot");
            SceneManager.MoveGameObjectToScene(bootObject, scene);
            Bootstrapper bootstrapper = bootObject.AddComponent<Bootstrapper>();

            var tickObject = new GameObject("TickRunner");
            SceneManager.MoveGameObjectToScene(tickObject, scene);
            tickObject.transform.SetParent(bootObject.transform, false);
            TickRunner tickRunner = tickObject.AddComponent<TickRunner>();

            using (var serializedBootstrapper = new SerializedObject(bootstrapper))
            {
                SerializedProperty installers = serializedBootstrapper.FindProperty("_installers");
                installers.arraySize = 1;
                installers.GetArrayElementAtIndex(0).objectReferenceValue = tickRunner;
                serializedBootstrapper.FindProperty("_bootCompleted").objectReferenceValue = bootCompleted;
                serializedBootstrapper.ApplyModifiedPropertiesWithoutUndo();
            }

            bool saved = EditorSceneManager.SaveScene(scene, BootScenePath);
            EditorSceneManager.CloseScene(scene, true);

            if (!saved)
            {
                report.AddSkipped($"{BootScenePath} (scene save failed)");
                return;
            }

            report.AddCreated(BootScenePath);
            report.AddWired("Bootstrapper._installers -> TickRunner");
            if (bootCompleted != null)
            {
                report.AddWired("Bootstrapper._bootCompleted -> BootCompleted");
            }
        }

        private static void EnsureBootSceneInBuildSettings(AssetBuilderReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) == null)
            {
                return;
            }

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path != BootScenePath)
                {
                    continue;
                }

                if (i != 0)
                {
                    report.AddSkipped("Build Settings: Boot scene is listed but not first; move it to index 0");
                }
                else if (!scenes[i].enabled)
                {
                    report.AddSkipped("Build Settings: Boot scene is listed but disabled; enable it");
                }

                return;
            }

            var updated = new List<EditorBuildSettingsScene>(scenes.Length + 1)
            {
                new EditorBuildSettingsScene(BootScenePath, true)
            };
            updated.AddRange(scenes);
            EditorBuildSettings.scenes = updated.ToArray();
            report.AddWired("Build Settings -> Boot scene at index 0");
        }

        private static bool HasUntitledOpenScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
