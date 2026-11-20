// One card in the hand: frame, Prana cost orb, art window, and localized name, type and description. It presents a
// CardInstance and reports clicks through Clicked; it never plays cards itself. Pooled: OnTakenFromPool resets it,
// OnReturnedToPool drops every listener, localization binding and tween. Built by CardViewFactory, styled here. Its
// text bindings are created on first use, so a view can be bound while its parent is still inactive (before Awake has
// run), as the reward screen does. A hand card can be hidden while its drawn copy is still flying in (SetRevealed), and
// wiggles when a click is rejected (PlayRejected, a DOTween core rotation punch). It reports pointer hover and UI
// selection through FocusChanged, and selection alone through Selected, so the hand can raise it and the pile viewer
// can scroll to it. In the hand, hover is decided by the hand itself (SetExternalHover) instead of this card's own
// pointer events, because fanned cards overlap. Lifting for targeting is state (IsLifted, LiftScale, LiftChanged) that
// the hand's layout animator applies; the view never moves its own transform. SetCostAffordable switches the cost badge
// to the style's unaffordable colours when the player lacks the Prana for the card. The description is a localized
// pattern filled with the card's live numbers: SetTextContext tells the card how to preview them (the hand passes the
// combat resolver, Rama and the chosen enemy), and values that differ from the authored base show in the style's buffed
// or weakened colour. Without a context (reward screen, pile viewer) it shows the base values. The face follows
// CardFaceLayout: a name banner (the name shrinks to stay on one line), an art window (the card's painting, or the
// type's accent times the style's placeholder tint until it has one), a type ribbon in the type's accent with the label
// (one line, shrinking to fit) in the type's label colour and an optional icon, and a description box whose text
// shrinks to fit, over a readability panel. The frame, banner and art socket are drawn by the Card Frame shader in the
// type's accent (CardTypeVisualsSO via the style); the art window, ribbon and cost orb by the Card Art shader. The
// trim, banner edge and art rim take the card's rarity colour (CardRarityVisualsSO via the style), with a slow shimmer
// on rarer cards. Keywords in the description (Kavach, the status names and others on the style's Card Keywords) show
// in their own colours. A Card Sprite on the style replaces the shader frame, and a Cost Badge Sprite replaces the
// round orb. SetGlow fades a soft halo in or out behind the card (the hand turns it on only for the playable card the
// player is considering); every card shares the glow material and fades only its own vertex alpha. IsPlayable remembers
// the last SetPlayable call so the hand can decide the glow without asking the combat model again.
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
        [SerializeField] private Image _art;
        [SerializeField] private Image _typeRibbon;

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

        public bool IsPlayable { get; private set; }

        internal CanvasGroup Group => _group;

        internal void Initialize(Image frame, Button button, CanvasGroup group, LayoutElement layoutElement,
            Image costBadge, TextMeshProUGUI cost, TextMeshProUGUI cardName, TextMeshProUGUI type, TextMeshProUGUI description,
            Image typeIcon, Image descriptionPanel, Image glow, Image art, Image typeRibbon)
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
            _art = art;
            _typeRibbon = typeRibbon;
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
            IsPlayable = playable;
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
            _descriptionText.SetKeywords(CardKeywordSet.For(style.CardKeywords));

            _layoutElement.preferredWidth = style.CardSize.x;
            _layoutElement.preferredHeight = style.CardSize.y;
            ((RectTransform)transform).sizeDelta = style.CardSize;

            _button.colors = CreateTint(style);

            CardFaceLayout layout = CardFaceLayout.From(style);
            Material round = CardFrameMaterials.GetRound(style);

            float badge = style.CostBadgeSize;
            bool roundBadge = style.CostBadgeSprite == null;
            UIBuildUtility.StyleImage(_costBadge, style.CostBadgeSprite, style.CostBadgeColor);
            _costBadge.material = roundBadge ? round : null;
            UIBuildUtility.Place(_costBadge.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), layout.CostCentre,
                new Vector2(badge, badge));
            UIBuildUtility.StyleText(_cost, style.Font, style.CostFontSize, style.CostTextColor);
            _costBadgeColor = style.CostBadgeColor;
            _costTextColor = style.CostTextColor;
            _costUnaffordableBadgeColor = style.CostUnaffordableBadgeColor;
            _costUnaffordableTextColor = style.CostUnaffordableTextColor;
            _costStyled = true;
            ApplyCostColors();

            UIBuildUtility.StyleText(_name, style.Font, style.CardNameFontSize, style.CardTextColor);
            SetFitting(_name, style.CardNameMinFontSize, style.CardNameFontSize, TextWrappingModes.NoWrap);
            PlaceRect(_name.rectTransform, layout.Name);

            _art.material = CardFrameMaterials.GetArt(style);
            PlaceRect(_art.rectTransform, layout.Art);

            _typeRibbon.material = round;
            PlaceRect(_typeRibbon.rectTransform, layout.Ribbon);
            UIBuildUtility.StyleText(_type, style.Font, style.CardTypeFontSize, style.CardTextColor);
            SetFitting(_type, style.CardTypeMinFontSize, style.CardTypeFontSize, TextWrappingModes.NoWrap);
            float iconSize = style.TypeIconSize;
            float iconInset = layout.Ribbon.height * 0.25f;
            UIBuildUtility.Place(_typeIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(iconInset, 0f), new Vector2(iconSize, iconSize));

            UIBuildUtility.StyleText(_description, style.Font, style.CardDescriptionFontSize, style.CardTextColor);
            SetFitting(_description, style.CardDescriptionMinFontSize, style.CardDescriptionFontSize,
                TextWrappingModes.Normal);
            PlaceRect(_description.rectTransform, layout.Description);

            float panelPadding = style.DescriptionPanelPadding;
            Rect panel = layout.Description;
            panel.xMin -= panelPadding;
            panel.yMin -= panelPadding;
            panel.xMax += panelPadding;
            panel.yMax += panelPadding;
            UIBuildUtility.StyleImage(_descriptionPanel, style.DescriptionPanelSprite, style.DescriptionPanelColor);
            PlaceRect(_descriptionPanel.rectTransform, panel);
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
            IsPlayable = false;
            _revealed = true;
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
            IsPlayable = false;
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
            CardRarityVisualsSO rarity = Card != null ? _style.FindCardRarityVisuals(Card.Definition.Rarity) : null;
            Material frameMaterial = _style.CardSprite == null ? CardFrameMaterials.Get(_style, visuals, rarity) : null;
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

            Color accent = visuals != null ? visuals.AccentColor : _style.CardColor;
            _typeRibbon.color = accent;

            Sprite art = Card != null ? Card.Definition.Art : null;
            _art.sprite = art;
            _art.color = art != null ? Color.white : accent * _style.CardArtPlaceholderTint;

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
            _group.alpha = _revealed ? _playableAlpha : 0f;
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

        private static void PlaceRect(RectTransform rect, Rect area)
        {
            UIBuildUtility.Place(rect, Vector2.zero, Vector2.zero, area.position, area.size);
        }

        private static void SetFitting(TMP_Text text, float minSize, float maxSize, TextWrappingModes wrapping)
        {
            text.textWrappingMode = wrapping;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.enableAutoSizing = true;
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
