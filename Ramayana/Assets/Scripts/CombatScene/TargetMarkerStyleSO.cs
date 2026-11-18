// Visual values for the target marker shown under an enemy while the player picks a target: the material the disc
// renders with (a URP material asset, so it is in the build; a runtime primitive's default material is not), its
// colour, local offset and scale. Read-only at runtime; in the Editor, Changed lets markers re-apply it live.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Target Marker Style", fileName = "TargetMarkerStyle")]
    public sealed class TargetMarkerStyleSO : ScriptableObject
    {
        [SerializeField] private Material _material;
        [SerializeField] private Color _color = new Color(1f, 0.8f, 0.3f);
        [SerializeField] private Vector3 _offset = new Vector3(0f, -0.49f, 0f);
        [SerializeField] private Vector3 _scale = new Vector3(1.6f, 0.02f, 1.6f);

        public event Action Changed;

        public Material Material => _material;

        public Color Color => _color;

        public Vector3 Offset => _offset;

        public Vector3 Scale => _scale;

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
