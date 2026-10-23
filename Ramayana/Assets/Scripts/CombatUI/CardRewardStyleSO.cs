// Visual values for the card reward screen: draw order over the combat HUD, the dimmed backdrop, the title,
// card spacing and the Take Nothing button. Cards themselves use the Combat HUD style so they match the hand.
// Read-only at runtime; in the Editor, Changed lets the screen re-apply it live.
using System;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Card Reward Style", fileName = "CardRewardStyle")]
    public sealed class CardRewardStyleSO : ScriptableObject
    {
        [SerializeField] private int _sortingOrder = 50;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Color _backdropColor = new Color(0.02f, 0.02f, 0.03f, 0.8f);

        [Header("Title")]
        [SerializeField, Min(1f)] private float _titleFontSize = 48f;
        [SerializeField] private Color _titleColor = new Color(0.98f, 0.86f, 0.55f);
        [SerializeField] private Vector2 _titleOffset = new Vector2(0f, 260f);
        [SerializeField] private Vector2 _titleSize = new Vector2(1200f, 80f);

        [Header("Cards")]
        [SerializeField] private float _cardSpacing = 48f;
        [SerializeField] private Vector2 _rowOffset = new Vector2(0f, 10f);
        [SerializeField, Min(0.1f)] private float _cardScale = 1.2f;

        [Header("Take Nothing")]
        [SerializeField] private Vector2 _skipSize = new Vector2(300f, 72f);
        [SerializeField] private Vector2 _skipOffset = new Vector2(0f, -270f);
        [SerializeField] private Sprite _skipSprite;
        [SerializeField] private Color _skipColor = new Color(0.2f, 0.17f, 0.14f);
        [SerializeField] private Color _skipTextColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField, Min(1f)] private float _skipFontSize = 28f;

        public event Action Changed;

        public int SortingOrder => _sortingOrder;
        public TMP_FontAsset Font => _font;
        public Color BackdropColor => _backdropColor;
        public float TitleFontSize => _titleFontSize;
        public Color TitleColor => _titleColor;
        public Vector2 TitleOffset => _titleOffset;
        public Vector2 TitleSize => _titleSize;
        public float CardSpacing => _cardSpacing;
        public Vector2 RowOffset => _rowOffset;
        public float CardScale => _cardScale;
        public Vector2 SkipSize => _skipSize;
        public Vector2 SkipOffset => _skipOffset;
        public Sprite SkipSprite => _skipSprite;
        public Color SkipColor => _skipColor;
        public Color SkipTextColor => _skipTextColor;
        public float SkipFontSize => _skipFontSize;

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
