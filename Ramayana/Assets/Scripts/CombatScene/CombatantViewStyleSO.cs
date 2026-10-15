// Every visual value for a combatant view: label font, size, colours and offsets, and the tint for a fallen
// fighter. Read-only at runtime. In the Editor, changing a value raises Changed so views in Play mode re-apply
// it at once without rebuilding.
using System;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combatant View Style", fileName = "CombatantViewStyle")]
    public sealed class CombatantViewStyleSO : ScriptableObject
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField, Min(0.1f)] private float _fontSize = 5f;
        [SerializeField] private Vector2 _labelSize = new Vector2(4f, 1f);
        [SerializeField] private Color _healthColor = new Color(0.95f, 0.4f, 0.35f);
        [SerializeField] private Vector3 _healthOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private Color _blockColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Vector3 _blockOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private Color _fallenTint = new Color(0.25f, 0.25f, 0.25f);

        public event Action Changed;

        public TMP_FontAsset Font => _font;

        public float FontSize => _fontSize;

        public Vector2 LabelSize => _labelSize;

        public Color HealthColor => _healthColor;

        public Vector3 HealthOffset => _healthOffset;

        public Color BlockColor => _blockColor;

        public Vector3 BlockOffset => _blockOffset;

        public Color FallenTint => _fallenTint;

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
