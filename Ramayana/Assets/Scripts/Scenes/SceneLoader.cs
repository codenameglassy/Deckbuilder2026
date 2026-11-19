// Loads gameplay scenes additively from the build's scene list and unloads the previous one first, so Boot is never
// replaced and only one gameplay scene is loaded at a time. Scenes are not loaded through Addressables, because a
// ScriptableObject referenced by both Boot and an Addressable scene is duplicated in a player build, which splits
// event channels into two unconnected copies. It can also adopt a scene that was already loaded (scenes opened
// together in the Editor) and, in the Editor only, load any scene by path for Play from Boot.
// Failures are logged and leave the loader ready for the next request.
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.Scenes
{
    public sealed class SceneLoader : ISceneLoader
    {
        private Scene _currentScene;

        public event Action<SceneDefinitionSO> LoadStarted;

        public event Action<SceneDefinitionSO> LoadCompleted;

        public bool IsLoading { get; private set; }

        public SceneDefinitionSO CurrentScene { get; private set; }

        public void Load(SceneDefinitionSO scene)
        {
            if (scene == null || !scene.IsValid)
            {
                Debug.LogError($"SceneLoader: cannot load '{(scene != null ? scene.name : "null")}', it has no scene path. " +
                               "Open the scene and run Tools > Journey to Lanka > Scenes > Register Active Scene.");
                return;
            }

            if (SceneUtility.GetBuildIndexByScenePath(scene.ScenePath) < 0)
            {
                Debug.LogError($"SceneLoader: '{scene.ScenePath}' is not in the build's scene list. " +
                               "Open the scene and run Tools > Journey to Lanka > Scenes > Register Active Scene.");
                return;
            }

            if (!BeginLoad(scene))
            {
                return;
            }

            UnloadCurrent(() => LoadFromBuildList(scene));
        }

        public void AdoptLoadedScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            _currentScene = scene;
            SceneManager.SetActiveScene(scene);
        }

#if UNITY_EDITOR
        public void LoadEditorScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath) || !BeginLoad(null))
            {
                return;
            }

            UnloadCurrent(() =>
            {
                var parameters = new LoadSceneParameters(LoadSceneMode.Additive);
                AsyncOperation operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, parameters);
                if (operation == null)
                {
                    Debug.LogError($"SceneLoader: could not load '{scenePath}' in Play mode.");
                    FinishLoad(null);
                    return;
                }

                operation.completed += _ =>
                {
                    AdoptLoadedScene(SceneManager.GetSceneByPath(scenePath));
                    FinishLoad(null);
                };
            });
        }
#endif

        private bool BeginLoad(SceneDefinitionSO scene)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"SceneLoader: a scene is already loading, request for '{(scene != null ? scene.name : "editor scene")}' ignored.");
                return false;
            }

            IsLoading = true;
            LoadStarted?.Invoke(scene);
            return true;
        }

        private void FinishLoad(SceneDefinitionSO scene)
        {
            CurrentScene = scene;
            IsLoading = false;
            LoadCompleted?.Invoke(scene);
        }

        private void UnloadCurrent(Action next)
        {
            Scene current = _currentScene;
            _currentScene = default;

            if (current.IsValid() && current.isLoaded)
            {
                AsyncOperation operation = SceneManager.UnloadSceneAsync(current);
                if (operation != null)
                {
                    operation.completed += _ => next();
                    return;
                }
            }

            next();
        }

        private void LoadFromBuildList(SceneDefinitionSO scene)
        {
            string path = scene.ScenePath;
            AsyncOperation operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"SceneLoader: loading '{path}' could not start.");
                FinishLoad(null);
                return;
            }

            operation.completed += _ =>
            {
                Scene loaded = SceneManager.GetSceneByPath(path);
                if (!loaded.IsValid() || !loaded.isLoaded)
                {
                    Debug.LogError($"SceneLoader: loading '{path}' finished but the scene is not loaded.");
                    FinishLoad(null);
                    return;
                }

                AdoptLoadedScene(loaded);
                FinishLoad(scene);
            };
        }
    }
}
