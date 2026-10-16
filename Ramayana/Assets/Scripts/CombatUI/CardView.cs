// One card in the hand: frame, Prana cost badge, and localized name, type and description. It presents a
// CardInstance and reports clicks through Clicked; it never plays cards itself. Pooled: OnTakenFromPool resets
// it, OnReturnedToPool drops every listener and localization binding. Built by CardViewFactory, styled here.
using System;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour, IPoolable
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private Image _costBadge;
        [SerializeField] private TextMeshProUGUI _cost;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _type;
        [SerializeField] private TextMeshProUGUI _description;

        private LocalizedText _nameText;
        private LocalizedText _typeText;
        private LocalizedText _descriptionText;

        public event Action<CardView> Clicked;

        public int CardId { get; private set; } = -1;

        public Button Button => _button;

        internal void Initialize(Image frame, Button button, CanvasGroup group, LayoutElement layoutElement,
            Image costBadge, TextMeshProUGUI cost, TextMeshProUGUI cardName, TextMeshProUGUI type, TextMeshProUGUI description)
        {
            _frame = frame;
            _button = button;
            _group = group;
            _layoutElement = layoutElement;
            _costBadge = costBadge;
            _cost = cost;
            _name = cardName;
            _type = type;
            _description = description;
        }

        private void Awake()
        {
            _nameText = new LocalizedText(_name);
            _typeText = new LocalizedText(_type);
            _descriptionText = new LocalizedText(_description);
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }

            ClearTexts();
        }

        public void Bind(CardInstance card)
        {
            CardId = card.Id;
            CardDefinitionSO definition = card.Definition;

            _cost.SetText("{0}", (float)card.Cost);
            _nameText.Set(LocalizationTableNames.Combat, definition.NameKey);
            _descriptionText.Set(LocalizationTableNames.Combat, definition.DescriptionKey);

            if (definition.Type != null)
            {
                _typeText.Set(LocalizationTableNames.Combat, definition.Type.NameKey);
            }
            else
            {
                _typeText.Clear();
            }
        }

        public void SetPlayable(bool playable, float unplayableAlpha)
        {
            _group.alpha = playable ? 1f : unplayableAlpha;
        }

        public void ApplyStyle(CombatHudStyleSO style)
        {
            float padding = style.CardPadding;
            float badge = style.CostBadgeSize;
            float typeHeight = style.CardTypeFontSize * 1.4f;

            _layoutElement.preferredWidth = style.CardSize.x;
            _layoutElement.preferredHeight = style.CardSize.y;
            ((RectTransform)transform).sizeDelta = style.CardSize;

            UIBuildUtility.StyleImage(_frame, style.CardSprite, style.CardColor);
            _button.colors = CreateTint(style);

            UIBuildUtility.StyleImage(_costBadge, style.CostBadgeSprite, style.CostBadgeColor);
            UIBuildUtility.Place(_costBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(padding, -padding), new Vector2(badge, badge));
            UIBuildUtility.StyleText(_cost, style.Font, style.CostFontSize, style.CostTextColor);

            UIBuildUtility.StyleText(_name, style.Font, style.CardNameFontSize, style.CardTextColor);
            UIBuildUtility.SetOffsets(_name.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(badge + padding * 2f, -(padding + badge)), new Vector2(-padding, -padding));

            UIBuildUtility.StyleText(_type, style.Font, style.CardTypeFontSize, style.CardTextColor);
            UIBuildUtility.SetOffsets(_type.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(padding, -(padding * 2f + badge + typeHeight)), new Vector2(-padding, -(padding * 2f + badge)));

            UIBuildUtility.StyleText(_description, style.Font, style.CardDescriptionFontSize, style.CardTextColor);
            UIBuildUtility.SetOffsets(_description.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(padding, padding), new Vector2(-padding, -(padding * 3f + badge + typeHeight)));
        }

        public void OnTakenFromPool()
        {
            CardId = -1;
            _group.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        public void OnReturnedToPool()
        {
            Clicked = null;
            CardId = -1;
            ClearTexts();
        }

        internal static ColorBlock CreateTint(CombatHudStyleSO style)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = style.HighlightedTint;
            colors.pressedColor = style.PressedTint;
            colors.selectedColor = style.SelectedTint;
            colors.disabledColor = style.DisabledTint;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = style.TintFadeDuration;
            return colors;
        }

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }

        private void ClearTexts()
        {
            _nameText?.Clear();
            _typeText?.Clear();
            _descriptionText?.Clear();
        }
    }
}
