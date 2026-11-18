// How one card type looks on a card: the accent colour the frame shader uses for its border, title band and glow,
// the colour of the type label, and an optional icon shown beside the label. One asset per card type (Attack,
// Skill, and later Power, Astra, Curse), listed on the Combat HUD style, so a new type is a new asset and never new
// code. Shared and read-only at runtime. In the Editor, changing a value raises Changed so cards re-apply it live.
using System;
using JourneyToLanka.Combat.Cards;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Card Type Visuals", fileName = "CardTypeVisuals")]
    public sealed class CardTypeVisualsSO : ScriptableObject
    {
        [SerializeField] private CardTypeSO _type;
        [SerializeField] private Color _accentColor = new Color(0.7f, 0.55f, 0.3f);
        [SerializeField] private Color _typeLabelColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField] private Sprite _icon;

        public event Action Changed;

        public CardTypeSO Type => _type;

        public Color AccentColor => _accentColor;

        public Color TypeLabelColor => _typeLabelColor;

        public Sprite Icon => _icon;

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
