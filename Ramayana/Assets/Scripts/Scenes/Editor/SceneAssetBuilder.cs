// Scene loading assets. The default builder creates the Load Scene channel. "Register Active Scene" (under
// Tools > Journey to Lanka > Scenes) adds the open, saved scene to the build's scene list, removes any Addressable
// entry it has (gameplay scenes must not be Addressable, or ScriptableObjects shared with Boot are duplicated in
// player builds), and creates its SceneDefinitionSO at Data/Scenes/<SceneName>.asset, filling the scene GUID only
// while it is empty and keeping the stored path in sync with that GUID. Both are idempotent.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.Scenes.EditorTools
{
    public static class SceneAssetBuilder
    {
        public const string Root = AssetBuilderConstants.DataRoot + "/Scenes";
        public const string ChannelsFolder = Root + "/Channels";
        public const string LoadSceneChannelPath = ChannelsFolder + "/LoadScene.asset";

        private const string RegisterMenuPath = "Tools/Journey to Lanka/Scenes/Register Active Scene";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Scenes", priority = AssetBuilderConstants.FeatureMenuPriority + 3)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Scenes", Build);
        }

        [DefaultAssetBuilder(30)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderSerialization.LoadOrCreateConfigured<LoadSceneEventChannelSO>(LoadSceneChannelPath, report,
                so => AssetBuilderSerialization.SetString(so, "_description", "Request to load a gameplay scene; Boot's scene flow handles it."));
        }

        [MenuItem(RegisterMenuPath, priority = 20)]
        private static void RegisterActiveScene()
        {
            AssetBuilderRunner.Run("Register Active Scene", Register);
        }

        [MenuItem(RegisterMenuPath, true)]
        private static bool CanRegisterActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            return scene.IsValid() && !string.IsNullOrEmpty(scene.path) && scene.path != CoreAssetBuilder.BootScenePath;
        }

        private static void Register(AssetBuilderReport report)
        {
            Scene scene = SceneManager.GetActiveScene();
            string guid = AssetDatabase.AssetPathToGUID(scene.path);
            if (string.IsNullOrEmpty(guid))
            {
                report.AddSkipped("Active scene is not saved as an asset");
                return;
            }

            AddToBuildScenes(scene.path, report);
            RemoveAddressableEntry(guid, scene.path, report);

            string definitionPath = Root + "/" + scene.name + ".asset";
            SceneDefinitionSO definition = AssetBuilderSerialization.LoadOrCreateConfigured<SceneDefinitionSO>(definitionPath, report,
                so => AssetBuilderSerialization.SetString(so, "_id", scene.name.ToLowerInvariant()));
            if (definition == null)
            {
                return;
            }

            using var serializedObject = new SerializedObject(definition);
            SerializedProperty guidProperty = AssetBuilderSerialization.Require(serializedObject, "_sceneGuid");
            SerializedProperty pathProperty = AssetBuilderSerialization.Require(serializedObject, "_scenePath");

            if (string.IsNullOrEmpty(guidProperty.stringValue))
            {
                guidProperty.stringValue = guid;
                report.AddWired($"{definitionPath} scene reference");
            }
            else if (guidProperty.stringValue != guid)
            {
                report.AddSkipped($"{definitionPath} already points at another scene");
                return;
            }
            else
            {
                report.AddExisted($"{definitionPath} scene reference");
            }

            if (pathProperty.stringValue != scene.path)
            {
                pathProperty.stringValue = scene.path;
                report.AddWired($"{definitionPath} scene path");
            }

            if (serializedObject.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(definition);
            }
        }

        private static void AddToBuildScenes(string scenePath, AssetBuilderReport report)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path != scenePath)
                {
                    continue;
                }

                if (scenes[i].enabled)
                {
                    report.AddExisted($"Build scene list entry for {scenePath}");
                    return;
                }

                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes;
                report.AddWired($"Enabled build scene list entry for {scenePath}");
                return;
            }

            var list = new List<EditorBuildSettingsScene>(scenes) { new EditorBuildSettingsScene(scenePath, true) };
            EditorBuildSettings.scenes = list.ToArray();
            report.AddCreated($"Build scene list entry for {scenePath}");
        }

        private static void RemoveAddressableEntry(string guid, string scenePath, AssetBuilderReport report)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || settings.FindAssetEntry(guid) == null)
            {
                return;
            }

            settings.RemoveAssetEntry(guid);
            report.AddWired($"Removed Addressable entry for {scenePath}");
        }
    }
}
