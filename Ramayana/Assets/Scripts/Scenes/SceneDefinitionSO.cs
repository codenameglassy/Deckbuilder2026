// A loadable scene as data: a stable id and the scene's project path. Gameplay scenes load from the player build's
// scene list, so they share every ScriptableObject instance with Boot. Gameplay asks for scenes by this asset, never
// by name or build index. Shared and read-only at runtime; load state lives in the scene loader, not here.
// In the Editor the path is re-derived from the scene's GUID, so moving or renaming the scene does not break it.
using UnityEngine;

namespace JourneyToLanka.Scenes
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Scenes/Scene Definition", fileName = "SceneDefinition")]
    public sealed class SceneDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _sceneGuid;
        [SerializeField] private string _scenePath;

        public string Id => _id;

        public string ScenePath => _scenePath;

        public bool IsValid => !string.IsNullOrEmpty(_scenePath);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_sceneGuid))
            {
                return;
            }

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(_sceneGuid);
            if (!string.IsNullOrEmpty(path) && path != _scenePath)
            {
                _scenePath = path;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
