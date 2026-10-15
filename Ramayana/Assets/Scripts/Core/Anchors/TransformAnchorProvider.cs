// Publishes this GameObject's Transform into a TransformAnchorSO while enabled.
using UnityEngine;

namespace JourneyToLanka.Core.Anchors
{
    [AddComponentMenu("Journey to Lanka/Core/Transform Anchor Provider")]
    public sealed class TransformAnchorProvider : MonoBehaviour
    {
        [SerializeField] private TransformAnchorSO _anchor;

        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
        }

        private void OnEnable()
        {
            if (_anchor != null)
            {
                _anchor.Provide(_transform);
            }
        }

        private void OnDisable()
        {
            if (_anchor != null)
            {
                _anchor.Unset(_transform);
            }
        }
    }
}
