// Event channel requesting a scene change, carrying the SceneDefinitionSO to load. The Boot scene's flow
// listens and hands the request to the scene loader, so gameplay never touches the loader directly.
using JourneyToLanka.Core.Events;
using UnityEngine;

namespace JourneyToLanka.Scenes
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Events/Load Scene Event Channel", fileName = "LoadSceneEventChannel")]
    public sealed class LoadSceneEventChannelSO : EventChannelSO<SceneDefinitionSO>
    {
    }
}
