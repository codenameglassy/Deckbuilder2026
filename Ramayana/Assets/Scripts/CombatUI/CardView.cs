// One card in the hand: frame, Prana cost badge, and localized name, type and description. It presents a
// CardInstance and reports clicks through Clicked; it never plays cards itself. Pooled: OnTakenFromPool resets
// it, OnReturnedToPool drops every listener, localization binding and tween. Built by CardViewFactory, styled here.
// Its text bindings are created on first use, so a view can be bound while its parent is still inactive
// (before Awake has run), as the reward screen does. A hand card can be hidden while its drawn copy is still
// flying in (SetRevealed), hidden behind its zoomed copy while it keeps receiving the pointer (SetZoomHidden), and
// wiggles when a click is rejected (PlayRejected, a DOTween core rotation punch). It reports pointer hover and UI
// selection through FocusChanged, and selection alone through Selected, so the hand can zoom it and the pile viewer
// can scroll to it. In the hand, hover is decided by the hand itself (SetExternalHover) instead of this card's own
// pointer events, because fanned cards overlap. Lifting for targeting is state (IsLifted, LiftScale, LiftChanged)
// that the hand's layout animator applies; the view never moves its own transform. SetCostAffordable switches the
// cost badge to the style's unaffordable colours when the player lacks the Prana for the card. The description is a
// localized pattern filled with the card's live numbers: SetTextContext tells the card how to preview them (the
// hand passes the combat resolver, Rama and the chosen enemy), and values that differ from the authored base show in
// the style's buffed or weakened colour. Without a context (reward screen, pile viewer) it shows the base values.
// The frame is drawn by the Card Frame shader in its card type's accent (CardTypeVisualsSO via the style), with the
// type label in the type's colour, an optional type icon, and a panel behind the description for readability. A Card
// Sprite on the style replaces the shader frame. SetGlow fades a soft halo in or out behind the card (the hand turns
// it on for cards that can be played now); every card shares the glow material and fades only its own vertex alpha.
using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour, IPoolable, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private const int RejectVibrato = 8;
        private const float RejectElasticity = 0.6f;

        [SerializeField] private Image _frame;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private Image _costBadge;
        [SerializeField] private TextMeshProUGUI _cost;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _type;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private Image _typeIcon;
        [SerializeField] private Image _descriptionPanel;
        [SerializeField] private Image _glow;

        private LocalizedText _nameText;
        private LocalizedText _typeText;
        private LocalizedPatternText _descriptionText;
        private readonly List<CardValue> _values = new List<CardValue>(4);
        private readonly List<CardValue> _collected = new List<CardValue>(4);
        private CardTextContext _textContext;
        private CombatHudStyleSO _style;
        private DOGetter<float> _getGlow;
        private DOSetter<float> _setGlow;
        private TweenCallback _clearGlowTween;
        private Tween _glowTween;
        private float _glowAlpha;
        private bool _glowOn;
        private TweenCallback _clearReject;
        private Tween _reject;
        private float _playableAlpha = 1f;
        private bool _revealed = true;
        private bool _zoomHidden;
        private bool _externalHover;
        private bool _costAffordable = true;
        private bool _costStyled;
        private Color _costBadgeColor;
        private Color _costTextColor;
        private Color _costUnaffordableBadgeColor;
        private Color _costUnaffordableTextColor;

        public event Action<CardView> Clicked;

        public event Action<CardView> LiftChanged;

        public event Action<CardView> FocusChanged;

        public event Action<CardView> Selected;

        public CardInstance Card { get; private set; }

        public bool IsPointerInside { get; private set; }

        public bool IsSelected { get; private set; }

        public bool IsLifted { get; private set; }

        public float LiftScale { get; private set; } = 1f;

        public int CardId { get; private set; } = -1;

        public Button Button => _button;

        public bool IsRevealed => _revealed;

        public bool IsCostAffordable => _costAffordable;

        public CardTextContext TextContext => _textContext;

        public bool IsGlowing => _glowOn;

        internal CanvasGroup Group => _group;

        internal void Initialize(Image frame, Button button, CanvasGroup group, LayoutElement layoutElement,
            Image costBadge, TextMeshProUGUI cost, TextMeshProUGUI cardName, TextMeshProUGUI type, TextMeshProUGUI description,
            Image typeIcon, Image descriptionPanel, Image glow)
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
            _typeIcon = typeIcon;
            _descriptionPanel = descriptionPanel;
            _glow = glow;
        }

        private void Awake()
        {
            EnsureTexts();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }

            StopReject();
            StopGlow();
            ClearTexts();
        }

        public void Bind(CardInstance card)
        {
            EnsureTexts();
            Card = card;
            CardId = card.Id;
            CardDefinitionSO definition = card.Definition;

            _cost.SetText("{0}", (float)card.Cost);
            _nameText.Set(LocalizationTableNames.Combat, definition.NameKey);
            _textContext = default;
            _values.Clear();
            CollectValues();
            _descriptionText.SetValues(_values);
            _descriptionText.Set(LocalizationTableNames.Combat, definition.DescriptionKey);

            if (definition.Type != null)
            {
                _typeText.Set(LocalizationTableNames.Combat, definition.Type.NameKey);
            }
            else
            {
                _typeText.Clear();
            }

            RefreshFrame();
        }

        public void SetLifted(bool lifted, float liftedScale)
        {
            float scale = lifted ? liftedScale : 1f;
            if (IsLifted == lifted && Mathf.Approximately(LiftScale, scale))
            {
                return;
            }

            IsLifted = lifted;
            LiftScale = scale;
            LiftChanged?.Invoke(this);
        }

        public void UseExternalHover(bool external)
        {
            _externalHover = external;
        }

        public void SetExternalHover(bool hovered)
        {
            if (!_externalHover || IsPointerInside == hovered)
            {
                return;
            }

            IsPointerInside = hovered;
            FocusChanged?.Invoke(this);
        }

        public void SetPlayable(bool playable, float unplayableAlpha)
        {
            _playableAlpha = playable ? 1f : unplayableAlpha;
            ApplyAlpha();
        }

        public void SetTextContext(in CardTextContext context)
        {
            _textContext = context;
            if (Card == null || !CollectValues())
            {
                return;
            }

            _descriptionText.SetValues(_values);
        }

        public void SetGlow(bool on)
        {
            if (_glowOn == on && _glowTween == null)
            {
                return;
            }

            _glowOn = on;
            StopGlow();
            float target = on ? 1f : 0f;
            float duration = _style != null ? _style.CardGlowFadeDuration : 0f;
            if (duration <= 0f || !isActiveAndEnabled)
            {
                SetGlowAlpha(target);
                return;
            }

            _getGlow ??= () => _glowAlpha;
            _setGlow ??= SetGlowAlpha;
            _clearGlowTween ??= () => _glowTween = null;
            _glowTween = DOTween.To(_getGlow, _setGlow, target, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(_clearGlowTween);
        }

        public void SetCostAffordable(bool affordable)
        {
            if (_costAffordable == affordable)
            {
                return;
            }

            _costAffordable = affordable;
            ApplyCostColors();
        }

        public void SetZoomHidden(bool hidden)
        {
            _zoomHidden = hidden;
            ApplyAlpha();
        }

        public void SetRevealed(bool revealed)
        {
            _revealed = revealed;
            _group.blocksRaycasts = revealed;
            ApplyAlpha();
        }

        public void PlayRejected(float angle, float duration)
        {
            StopReject();
            if (angle <= 0f || duration <= 0f)
            {
                return;
            }

            _clearReject ??= () => _reject = null;
            _reject = transform.DOPunchRotation(new Vector3(0f, 0f, angle), duration, RejectVibrato, RejectElasticity)
                .SetUpdate(true)
                .OnComplete(_clearReject);
        }

        public void ApplyStyle(CombatHudStyleSO style)
        {
            EnsureTexts();
            _style = style;

            Material glowMaterial = CardFrameMaterials.GetGlow(style);
            float spread = style.CardGlowSpread;
            UIBuildUtility.SetOffsets(_glow.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(-spread, -spread), new Vector2(spread, spread));
            _glow.material = glowMaterial;
            _glow.enabled = glowMaterial != null;
            SetGlowAlpha(_glowAlpha);
            _descriptionText.SetColors(style.CardValueBuffedColor, style.CardValueWeakenedColor);
            float padding = style.CardPadding;
            float badge = style.CostBadgeSize;
            float typeHeight = style.CardTypeFontSize * 1.4f;

            _layoutElement.preferredWidth = style.CardSize.x;
            _layoutElement.preferredHeight = style.CardSize.y;
            ((RectTransform)transform).sizeDelta = style.CardSize;

            _button.colors = CreateTint(style);

            UIBuildUtility.StyleImage(_costBadge, style.CostBadgeSprite, style.CostBadgeColor);
            UIBuildUtility.Place(_costBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(padding, -padding), new Vector2(badge, badge));
            UIBuildUtility.StyleText(_cost, style.Font, style.CostFontSize, style.CostTextColor);
            _costBadgeColor = style.CostBadgeColor;
            _costTextColor = style.CostTextColor;
            _costUnaffordableBadgeColor = style.CostUnaffordableBadgeColor;
            _costUnaffordableTextColor = style.CostUnaffordableTextColor;
            _costStyled = true;
            ApplyCostColors();

            UIBuildUtility.StyleText(_name, style.Font, style.CardNameFontSize, style.CardTextColor);
            UIBuildUtility.SetOffsets(_name.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(badge + padding * 2f, -(padding + badge)), new Vector2(-padding, -padding));

            UIBuildUtility.StyleText(_type, style.Font, style.CardTypeFontSize, style.CardTextColor);
            UIBuildUtility.SetOffsets(_type.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(padding, -(padding * 2f + badge + typeHeight)), new Vector2(-padding, -(padding * 2f + badge)));

            float iconSize = style.TypeIconSize;
            UIBuildUtility.Place(_typeIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(padding, -(padding * 2f + badge + typeHeight * 0.5f)), new Vector2(iconSize, iconSize));

            UIBuildUtility.StyleText(_description, style.Font, style.CardDescriptionFontSize, style.CardTextColor);
            Vector2 descriptionMin = new Vector2(padding, padding);
            Vector2 descriptionMax = new Vector2(-padding, -(padding * 3f + badge + typeHeight));
            UIBuildUtility.SetOffsets(_description.rectTransform, Vector2.zero, Vector2.one, descriptionMin, descriptionMax);

            float panelPadding = style.DescriptionPanelPadding;
            UIBuildUtility.StyleImage(_descriptionPanel, style.DescriptionPanelSprite, style.DescriptionPanelColor);
            UIBuildUtility.SetOffsets(_descriptionPanel.rectTransform, Vector2.zero, Vector2.one,
                descriptionMin - new Vector2(panelPadding, panelPadding), descriptionMax + new Vector2(panelPadding, panelPadding));
            _descriptionPanel.enabled = style.DescriptionPanelColor.a > 0f;

            RefreshFrame();
        }

        public void OnTakenFromPool()
        {
            StopReject();
            StopGlow();
            _glowOn = false;
            SetGlowAlpha(0f);
            Card = null;
            CardId = -1;
            _textContext = default;
            _values.Clear();
            _playableAlpha = 1f;
            _revealed = true;
            _zoomHidden = false;
            _externalHover = false;
            _costAffordable = true;
            ApplyCostColors();
            IsPointerInside = false;
            IsSelected = false;
            IsLifted = false;
            LiftScale = 1f;
            _group.blocksRaycasts = true;
            ApplyAlpha();
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        public void OnReturnedToPool()
        {
            StopReject();
            StopGlow();
            _glowOn = false;
            SetGlowAlpha(0f);
            Clicked = null;
            FocusChanged = null;
            Selected = null;
            LiftChanged = null;
            Card = null;
            CardId = -1;
            _textContext = default;
            _values.Clear();
            _zoomHidden = false;
            _externalHover = false;
            IsPointerInside = false;
            IsSelected = false;
            IsLifted = false;
            LiftScale = 1f;
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_externalHover)
            {
                return;
            }

            IsPointerInside = true;
            FocusChanged?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_externalHover)
            {
                return;
            }

            IsPointerInside = false;
            FocusChanged?.Invoke(this);
        }

        public void OnSelect(BaseEventData eventData)
        {
            IsSelected = true;
            FocusChanged?.Invoke(this);
            Selected?.Invoke(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            IsSelected = false;
            FocusChanged?.Invoke(this);
        }

        private void RefreshFrame()
        {
            if (_style == null)
            {
                return;
            }

            CardTypeVisualsSO visuals = _style.FindCardTypeVisuals(Card != null ? Card.Definition.Type : null);
            Material frameMaterial = _style.CardSprite == null ? CardFrameMaterials.Get(_style, visuals) : null;
            if (frameMaterial != null)
            {
                _frame.sprite = null;
                _frame.type = Image.Type.Simple;
                _frame.color = Color.white;
                _frame.material = frameMaterial;
            }
            else
            {
                _frame.material = null;
                UIBuildUtility.StyleImage(_frame, _style.CardSprite, _style.CardColor);
            }

            Color labelColor = visuals != null ? visuals.TypeLabelColor : _style.CardTextColor;
            _type.color = labelColor;
            Sprite icon = visuals != null ? visuals.Icon : null;
            _typeIcon.sprite = icon;
            _typeIcon.color = labelColor;
            _typeIcon.enabled = icon != null;
        }

        private void ApplyCostColors()
        {
            if (!_costStyled)
            {
                return;
            }

            _costBadge.color = _costAffordable ? _costBadgeColor : _costUnaffordableBadgeColor;
            _cost.color = _costAffordable ? _costTextColor : _costUnaffordableTextColor;
        }

        private void ApplyAlpha()
        {
            _group.alpha = _revealed && !_zoomHidden ? _playableAlpha : 0f;
        }

        private void SetGlowAlpha(float alpha)
        {
            _glowAlpha = alpha;
            if (_glow != null)
            {
                _glow.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void StopGlow()
        {
            if (_glowTween == null)
            {
                return;
            }

            Tween glow = _glowTween;
            _glowTween = null;
            glow.Kill();
        }

        private void StopReject()
        {
            if (_reject == null)
            {
                return;
            }

            Tween reject = _reject;
            _reject = null;
            reject.Kill(true);
        }

        private void EnsureTexts()
        {
            _nameText ??= new LocalizedText(_name);
            _typeText ??= new LocalizedText(_type);
            _descriptionText ??= new LocalizedPatternText(_description);
        }

        private bool CollectValues()
        {
            _collected.Clear();
            Card.Definition.CollectCardValues(_textContext, _collected);

            bool changed = _collected.Count != _values.Count;
            for (int i = 0; !changed && i < _collected.Count; i++)
            {
                changed = _collected[i].Base != _values[i].Base || _collected[i].Current != _values[i].Current;
            }

            if (!changed)
            {
                return false;
            }

            _values.Clear();
            _values.AddRange(_collected);
            return true;
        }

        private void ClearTexts()
        {
            _nameText?.Clear();
            _typeText?.Clear();
            _descriptionText?.Clear();
        }
    }
}
