// A loadable scene as data: a stable id and the scene's Addressables reference. Gameplay asks for scenes by this
// asset, never by name or build index. Shared and read-only; load state lives in the scene loader, not here.
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace JourneyToLanka.Scenes
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Scenes/Scene Definition", fileName = "SceneDefinition")]
    public sealed class SceneDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private AssetReference _scene;

        public string Id => _id;

        public object RuntimeKey => _scene != null ? _scene.RuntimeKey : null;

        public bool IsValid => _scene != null && _scene.RuntimeKeyIsValid();
    }
}
