// Editor toggle under Tools > Journey to Lanka > Play From Boot (on by default). When on, pressing Play in any
// scene starts from the Boot scene, and Boot then loads the scene you were working in. It records that scene in
// SessionState for BootSceneFlow. Turn it off to fall back to Unity's normal Play behaviour.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.Scenes.EditorTools
{
    [InitializeOnLoad]
    public static class PlayFromBoot
    {
        private const string MenuPath = "Tools/Journey to Lanka/Play From Boot";
        private const string EnabledKey = "JourneyToLanka.PlayFromBoot.Enabled";

        static PlayFromBoot()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += Apply;
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        [MenuItem(MenuPath, priority = 1)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CoreAssetBuilder.BootScenePath);
            if (bootScene == null)
            {
                Debug.LogWarning($"Play From Boot: no Boot scene at {CoreAssetBuilder.BootScenePath}. Run Create Default Assets > Core.");
            }

            EditorSceneManager.playModeStartScene = bootScene;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            SessionState.EraseString(EditorStartScene.SessionKey);
            if (!Enabled)
            {
                return;
            }

            Apply();
            string startScenePath = FindStartScenePath();
            if (!string.IsNullOrEmpty(startScenePath))
            {
                SessionState.SetString(EditorStartScene.SessionKey, startScenePath);
            }
        }

        private static string FindStartScenePath()
        {
            Scene active = SceneManager.GetActiveScene();
            if (IsGameplayScene(active))
            {
                return active.path;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (IsGameplayScene(scene))
                {
                    return scene.path;
                }
            }

            return string.Empty;
        }

        private static bool IsGameplayScene(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && !string.IsNullOrEmpty(scene.path) && scene.path != CoreAssetBuilder.BootScenePath;
        }
    }
}
