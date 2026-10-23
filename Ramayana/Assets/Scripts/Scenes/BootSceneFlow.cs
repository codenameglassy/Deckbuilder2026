// Starts the game once Boot has installed its services: loads the scene recorded by Play from Boot (Editor only),
// or adopts a gameplay scene that is already open next to Boot, or loads the first scene. Afterwards it forwards
// every Load Scene channel request to the scene loader. Lives on the Boot object.
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.Scenes
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Core/Boot Scene Flow")]
    public sealed class BootSceneFlow : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO _bootCompleted;
        [SerializeField] private LoadSceneEventChannelSO _loadScene;
        [SerializeField] private SceneDefinitionSO _firstScene;

        private ISceneLoader _loader;

        private void OnEnable()
        {
            if (_bootCompleted != null)
            {
                _bootCompleted.Register(HandleBootCompleted);
            }

            if (_loadScene != null)
            {
                _loadScene.Register(HandleLoadScene);
            }
        }

        private void OnDisable()
        {
            if (_bootCompleted != null)
            {
                _bootCompleted.Unregister(HandleBootCompleted);
            }

            if (_loadScene != null)
            {
                _loadScene.Unregister(HandleLoadScene);
            }
        }

        private void HandleBootCompleted()
        {
            if (!TryGetLoader())
            {
                return;
            }

#if UNITY_EDITOR
            if (EditorStartScene.TryConsume(out string editorScenePath))
            {
                _loader.LoadEditorScene(editorScenePath);
                return;
            }
#endif

            Scene alreadyLoaded = FindLoadedGameplayScene();
            if (alreadyLoaded.IsValid())
            {
                _loader.AdoptLoadedScene(alreadyLoaded);
                return;
            }

            if (_firstScene != null)
            {
                _loader.Load(_firstScene);
            }
            else
            {
                Debug.LogWarning($"{name}: no First Scene assigned, nothing loaded after boot.", this);
            }
        }

        private void HandleLoadScene(SceneDefinitionSO scene)
        {
            if (TryGetLoader())
            {
                _loader.Load(scene);
            }
        }

        private bool TryGetLoader()
        {
            if (_loader != null || ServiceLocator.TryGet(out _loader))
            {
                return true;
            }

            Debug.LogError($"{name}: no ISceneLoader service. Add the Scene Loader Installer to the Bootstrapper's installers.", this);
            return false;
        }

        private Scene FindLoadedGameplayScene()
        {
            Scene bootScene = gameObject.scene;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != bootScene && scene.isLoaded)
                {
                    return scene;
                }
            }

            return default;
        }
    }
}
