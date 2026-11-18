// Publishes the Camera on this GameObject into a CombatCameraAnchorSO while enabled.
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Journey to Lanka/Combat/Combat Camera Anchor Provider")]
    public sealed class CombatCameraAnchorProvider : MonoBehaviour
    {
        [SerializeField] private CombatCameraAnchorSO _anchor;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (_anchor != null)
            {
                _anchor.Provide(_camera);
            }
        }

        private void OnDisable()
        {
            if (_anchor != null)
            {
                _anchor.Unset(_camera);
            }
        }
    }
}
