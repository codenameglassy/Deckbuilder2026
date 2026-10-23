// The application-lifetime scene loader. One gameplay scene is loaded at a time, additively next to the persistent
// Boot scene: Load unloads the current scene first, then loads the requested one and makes it active.
// A request made while a load is running is ignored with a warning.
using System;
using UnityEngine.SceneManagement;

namespace JourneyToLanka.Scenes
{
    public interface ISceneLoader
    {
        event Action<SceneDefinitionSO> LoadStarted;

        event Action<SceneDefinitionSO> LoadCompleted;

        bool IsLoading { get; }

        SceneDefinitionSO CurrentScene { get; }

        void Load(SceneDefinitionSO scene);

        void AdoptLoadedScene(Scene scene);

#if UNITY_EDITOR
        void LoadEditorScene(string scenePath);
#endif
    }
}
