// Loads gameplay scenes additively through Addressables and unloads the previous one first, so Boot is never
// replaced and only one gameplay scene is loaded at a time. It can also adopt a scene that was already loaded
// (scenes opened together in the Editor) and, in the Editor only, load any scene by path for Play from Boot.
// Failures are logged and leave the loader ready for the next request.
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.Scenes
{
    public sealed class SceneLoader : ISceneLoader
    {
        private AsyncOperationHandle<SceneInstance> _currentHandle;
        private bool _hasCurrentHandle;
        private Scene _currentPlainScene;

        public event Action<SceneDefinitionSO> LoadStarted;

        public event Action<SceneDefinitionSO> LoadCompleted;

        public bool IsLoading { get; private set; }

        public SceneDefinitionSO CurrentScene { get; private set; }

        public void Load(SceneDefinitionSO scene)
        {
            if (scene == null || !scene.IsValid)
            {
                Debug.LogError($"SceneLoader: cannot load '{(scene != null ? scene.name : "null")}', it has no valid scene reference.");
                return;
            }

            if (!BeginLoad(scene))
            {
                return;
            }

            UnloadCurrent(() => LoadAddressable(scene));
        }

        public void AdoptLoadedScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            _currentPlainScene = scene;
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
            if (_hasCurrentHandle)
            {
                AsyncOperationHandle<SceneInstance> handle = _currentHandle;
                _hasCurrentHandle = false;
                _currentHandle = default;
                Addressables.UnloadSceneAsync(handle).Completed += _ => next();
                return;
            }

            if (_currentPlainScene.IsValid() && _currentPlainScene.isLoaded)
            {
                AsyncOperation operation = SceneManager.UnloadSceneAsync(_currentPlainScene);
                _currentPlainScene = default;
                if (operation != null)
                {
                    operation.completed += _ => next();
                    return;
                }
            }

            _currentPlainScene = default;
            next();
        }

        private void LoadAddressable(SceneDefinitionSO scene)
        {
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(scene.RuntimeKey, LoadSceneMode.Additive);
            handle.Completed += completed =>
            {
                if (completed.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentHandle = completed;
                    _hasCurrentHandle = true;
                    SceneManager.SetActiveScene(completed.Result.Scene);
                    FinishLoad(scene);
                    return;
                }

                Debug.LogError($"SceneLoader: loading '{scene.name}' failed. {completed.OperationException}");
                FinishLoad(null);
            };
        }
    }
}
