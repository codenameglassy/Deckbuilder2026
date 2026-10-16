// Publishes the CombatController on this GameObject into a CombatControllerAnchorSO while enabled.
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [RequireComponent(typeof(CombatController))]
    [AddComponentMenu("Journey to Lanka/Combat/Combat Controller Anchor Provider")]
    public sealed class CombatControllerAnchorProvider : MonoBehaviour
    {
        [SerializeField] private CombatControllerAnchorSO _anchor;

        private CombatController _controller;

        private void Awake()
        {
            _controller = GetComponent<CombatController>();
        }

        private void OnEnable()
        {
            if (_anchor != null)
            {
                _anchor.Provide(_controller);
            }
        }

        private void OnDisable()
        {
            if (_anchor != null)
            {
                _anchor.Unset(_controller);
            }
        }
    }
}
