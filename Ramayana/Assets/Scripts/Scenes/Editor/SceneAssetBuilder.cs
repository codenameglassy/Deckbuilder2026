// Scene loading assets. The default builder creates the Load Scene channel. "Register Active Scene" (under
// Tools > Journey to Lanka > Scenes) makes the open, saved scene an Addressable and creates its SceneDefinitionSO
// at Data/Scenes/<SceneName>.asset, filling the scene reference only while it is empty. Both are idempotent.
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

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                report.AddSkipped("Addressables settings could not be created");
                return;
            }

            if (settings.FindAssetEntry(guid) != null)
            {
                report.AddExisted($"Addressable entry for {scene.path}");
            }
            else
            {
                settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                report.AddCreated($"Addressable entry for {scene.path}");
            }

            string definitionPath = Root + "/" + scene.name + ".asset";
            SceneDefinitionSO definition = AssetBuilderSerialization.LoadOrCreateConfigured<SceneDefinitionSO>(definitionPath, report,
                so => AssetBuilderSerialization.SetString(so, "_id", scene.name.ToLowerInvariant()));
            if (definition == null)
            {
                return;
            }

            using var serializedObject = new SerializedObject(definition);
            SerializedProperty guidProperty = AssetBuilderSerialization.Require(serializedObject, "_scene.m_AssetGUID");
            if (string.IsNullOrEmpty(guidProperty.stringValue))
            {
                guidProperty.stringValue = guid;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                report.AddWired($"{definitionPath} scene reference");
            }
            else
            {
                report.AddExisted($"{definitionPath} scene reference");
            }
        }
    }
}
