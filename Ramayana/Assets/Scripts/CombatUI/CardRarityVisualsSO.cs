// How one card rarity looks on a card: the colour of the frame's trim, the name banner's edge and the art window's
// rim, and how strongly a slow band of light sweeps along them (none for everyday cards, a glint for Epic, a clear
// shimmer for Divine). One asset per rarity (Common, Rare, Epic, Divine), listed on the Combat HUD style, so a rarity's
// look is data and never code. A card whose rarity has no asset uses the style's own Frame Trim Color with no
// shimmer. Shared and read-only at runtime. In the Editor, changing a value raises Changed so cards re-apply it live.
using System;
using JourneyToLanka.Combat.Cards;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Card Rarity Visuals", fileName = "CardRarityVisuals")]
    public sealed class CardRarityVisualsSO : ScriptableObject
    {
        [SerializeField] private CardRarity _rarity = CardRarity.Common;
        [SerializeField] private Color _trimColor = new Color(0.72f, 0.5f, 0.3f, 0.9f);
        [Tooltip("Strength of the band of light that sweeps along the trim. Zero for none.")]
        [SerializeField, Range(0f, 1f)] private float _shimmer;

        public event Action Changed;

        public CardRarity Rarity => _rarity;

        public Color TrimColor => _trimColor;

        public float Shimmer => _shimmer;

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
