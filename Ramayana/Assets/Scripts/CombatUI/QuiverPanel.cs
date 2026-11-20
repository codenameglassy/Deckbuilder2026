// Rama's quiver on the combat HUD: a slim carved rail (a gold-trimmed pill drawn by the Card Art shader's round
// material) holding one round socket per quiver place, each showing its special arrow (the arrow's icon, or the
// fallback disc in its colour) or an empty frame, with a thin rim and a small 1 to 3 key hint. The sockets are taller
// than the rail, so they read as set into it. The rail is pinned to a screen corner from the style (top-left by
// default), a small localized caption ("Special arrow") sits above it, and each key hint sits centred under its socket.
// A slot is pressed with the mouse, with the submit button once selected (gamepad and keyboard reach the slots by
// moving up from the hand), or by the HUD's number keys; every way raises SlotActivated with the slot, and the HUD
// decides how to fire it. The arrow being considered (the targeted arrow while one waits for its target, else the slot
// under the pointer, else the selected slot; none while a card is being targeted or the quiver is not usable) gets a
// highlight ring, a punch and a tooltip beside the rail (below it at the top of the screen) with its localized name in
// the arrow's colour and its description with live numbers (the same CardTextContext cards use, with the highlighted
// enemy while the arrow is being targeted), refreshed when damage modifiers change. ConsideredSlotChanged reports it
// (-1 for none) so the HUD can preview its outcome. Slots chain left and right among the filled ones and lead down to
// the neighbour the HUD gives (SetDownNeighbour, the hand); FirstSelectable is where the hand leads up to, and
// NavigationChanged fires when that may have changed. SelectionLost fires when the selected slot empties, so the HUD
// can move the selection. It refreshes only when the quiver changes, never per frame, and only presents.
using System;
using System.Collections.Generic;
using DG.Tweening;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class QuiverPanel : IDisposable
    {
        private const int PunchVibrato = 6;
        private const float KeyLineHeight = 1.3f;
        private const float PunchElasticity = 0.5f;

        private readonly RectTransform _rect;
        private readonly Image _railTrim;
        private readonly Image _rail;
        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _caption;
        private readonly LocalizedText _captionText;
        private readonly RectTransform _row;
        private readonly List<Slot> _slots = new List<Slot>(3);
        private readonly RectTransform _tooltip;
        private readonly Image _tooltipBackground;
        private readonly VerticalLayoutGroup _tooltipLayout;
        private readonly TextMeshProUGUI _tooltipName;
        private readonly TextMeshProUGUI _tooltipDescription;
        private readonly LocalizedText _tooltipNameText;
        private readonly LocalizedPatternText _tooltipDescriptionText;
        private readonly List<CardValue> _values = new List<CardValue>(4);
        private readonly Action<Quiver> _quiverChanged;
        private readonly Action _modifiersChanged;
        private readonly Action<bool> _targetingActiveChanged;
        private readonly Action<int> _targetChanged;
        private CombatHudStyleSO _hudStyle;
        private QuiverStyleSO _style;
        private CombatModel _model;
        private TargetingPresenter _targeting;
        private Selectable _downNeighbour;
        private ArrowDefinitionSO _consideredArrow;
        private bool _interactable;
        private int _hovered = -1;
        private int _selected = -1;
        private int _considered = -1;
        private int _targetIndex = -1;

        private QuiverPanel(RectTransform rect, Image railTrim, Image rail, CanvasGroup group, TextMeshProUGUI caption,
            RectTransform row,
            RectTransform tooltip, Image tooltipBackground, VerticalLayoutGroup tooltipLayout, TextMeshProUGUI tooltipName,
            TextMeshProUGUI tooltipDescription)
        {
            _rect = rect;
            _railTrim = railTrim;
            _rail = rail;
            _group = group;
            _caption = caption;
            _row = row;
            _tooltip = tooltip;
            _tooltipBackground = tooltipBackground;
            _tooltipLayout = tooltipLayout;
            _tooltipName = tooltipName;
            _tooltipDescription = tooltipDescription;
            _captionText = new LocalizedText(caption);
            _captionText.Set(LocalizationTableNames.UI, QuiverKeys.Caption);
            _tooltipNameText = new LocalizedText(tooltipName);
            _tooltipDescriptionText = new LocalizedPatternText(tooltipDescription);
            _quiverChanged = HandleQuiverChanged;
            _modifiersChanged = RefreshTooltipValues;
            _targetingActiveChanged = _ => RefreshConsidered();
            _targetChanged = HandleTargetChanged;
            _tooltip.gameObject.SetActive(false);
        }

        public event Action<int> SlotActivated;

        public event Action<int> ConsideredSlotChanged;

        public event Action NavigationChanged;

        public event Action SelectionLost;

        public int ConsideredSlot => _considered;

        public Selectable FirstSelectable
        {
            get
            {
                if (!_interactable || _model == null || !_rect.gameObject.activeInHierarchy)
                {
                    return null;
                }

                return _model.Quiver.HasArrow(0) && _slots.Count > 0 ? _slots[0].Button : null;
            }
        }

        public static QuiverPanel Create(Transform parent)
        {
            RectTransform root = UIBuildUtility.CreateRect("Quiver", parent);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            Image railTrim = UIBuildUtility.CreateImage("RailTrim", root, false);
            railTrim.gameObject.AddComponent<CardArtMeshEffect>();
            Image rail = UIBuildUtility.CreateImage("Rail", root, false);
            rail.gameObject.AddComponent<CardArtMeshEffect>();
            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", root, TextAlignmentOptions.BottomLeft);
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform row = UIBuildUtility.CreateRect("Slots", root);

            Image tooltipBackground = UIBuildUtility.CreateImage("Tooltip", root, false);
            RectTransform tooltip = tooltipBackground.rectTransform;
            VerticalLayoutGroup layout = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = tooltip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI name = UIBuildUtility.CreateText("Name", tooltip, TextAlignmentOptions.TopLeft);
            TextMeshProUGUI description = UIBuildUtility.CreateText("Description", tooltip, TextAlignmentOptions.TopLeft);

            return new QuiverPanel(root, railTrim, rail, group, caption, row, tooltip, tooltipBackground, layout, name, description);
        }

        public RectTransform GetSlotRect(int slot)
        {
            return slot >= 0 && slot < _slots.Count ? _slots[slot].Rect : null;
        }

        public void ApplyStyle(CombatHudStyleSO hudStyle, QuiverStyleSO style)
        {
            _hudStyle = hudStyle;
            _style = style;
            if (_hudStyle == null || _style == null)
            {
                return;
            }

            Material round = CardFrameMaterials.GetRound(hudStyle);
            UIBuildUtility.StyleImage(_railTrim, null, style.RailTrimColor);
            _railTrim.material = round;
            UIBuildUtility.StyleImage(_rail, null, style.RailColor);
            _rail.material = round;
            UIBuildUtility.StyleText(_caption, hudStyle.Font, style.CaptionFontSize, style.CaptionColor);
            _caption.gameObject.SetActive(style.ShowCaption);

            UIBuildUtility.StyleImage(_tooltipBackground, hudStyle.PanelSprite, hudStyle.PanelColor);
            int padding = Mathf.RoundToInt(style.TooltipPadding);
            _tooltipLayout.padding = new RectOffset(padding, padding, padding, padding);
            _tooltipLayout.spacing = style.TooltipSpacing;
            UIBuildUtility.StyleText(_tooltipName, hudStyle.Font, style.TooltipNameFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_tooltipDescription, hudStyle.Font, style.TooltipDescriptionFontSize, hudStyle.HudTextColor);
            _tooltipDescriptionText.SetColors(hudStyle.CardValueBuffedColor, hudStyle.CardValueWeakenedColor);
            Vector2 tooltipSize = new Vector2(style.TooltipWidth, 0f);
            if (style.Anchor.y >= 0.5f)
            {
                float hints = style.ShowKeyHints ? style.KeyHintGap + style.KeyHintFontSize * KeyLineHeight : 0f;
                UIBuildUtility.Place(_tooltip, Vector2.zero, new Vector2(0f, 1f),
                    new Vector2(style.TooltipOffset.x, -(style.TooltipOffset.y + hints)), tooltipSize);
            }
            else
            {
                UIBuildUtility.Place(_tooltip, new Vector2(0f, 1f), Vector2.zero, style.TooltipOffset, tooltipSize);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].ApplyStyle(hudStyle, style);
            }

            Layout();
            RefreshSlots();
            ApplyConsidered(false);
        }

        public void Attach(CombatModel model, TargetingPresenter targeting)
        {
            Detach();
            _model = model;
            _targeting = targeting;
            _model.Quiver.Changed += _quiverChanged;
            _model.Resolver.ModifiersChanged += _modifiersChanged;
            if (_targeting != null)
            {
                _targeting.ActiveChanged += _targetingActiveChanged;
                _targeting.TargetChanged += _targetChanged;
            }

            EnsureSlots(_model.Quiver.Capacity);
            Layout();
            RefreshSlots();
            RefreshConsidered();
            NavigationChanged?.Invoke();
        }

        public void Detach()
        {
            if (_model == null)
            {
                return;
            }

            _model.Quiver.Changed -= _quiverChanged;
            _model.Resolver.ModifiersChanged -= _modifiersChanged;
            if (_targeting != null)
            {
                _targeting.ActiveChanged -= _targetingActiveChanged;
                _targeting.TargetChanged -= _targetChanged;
            }

            _model = null;
            _targeting = null;
            _hovered = -1;
            _selected = -1;
            _targetIndex = -1;
            RefreshSlots();
            RefreshConsidered();
        }

        public void Dispose()
        {
            Detach();
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Dispose();
            }

            _captionText.Clear();
            _tooltipNameText.Clear();
            _tooltipDescriptionText.Clear();
            SlotActivated = null;
            ConsideredSlotChanged = null;
            NavigationChanged = null;
            SelectionLost = null;
        }

        public void SetActive(bool active)
        {
            bool show = active && _model != null && _model.Quiver.Capacity > 0;
            if (_rect.gameObject.activeSelf == show)
            {
                return;
            }

            _rect.gameObject.SetActive(show);
            if (!show)
            {
                _hovered = -1;
                _selected = -1;
            }

            RefreshConsidered();
            NavigationChanged?.Invoke();
        }

        public void SetInteractable(bool interactable)
        {
            if (_interactable == interactable)
            {
                return;
            }

            _interactable = interactable;
            RefreshSlots();
            RefreshConsidered();
            NavigationChanged?.Invoke();
        }

        public void SetDownNeighbour(Selectable neighbour)
        {
            _downNeighbour = neighbour;
            RefreshNavigation();
        }

        public void Select(int slot)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || _model == null || !_interactable || !_model.Quiver.HasArrow(slot) || slot >= _slots.Count)
            {
                return;
            }

            GameObject target = _slots[slot].Button.gameObject;
            if (target.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(target);
            }
        }

        private void EnsureSlots(int count)
        {
            while (_slots.Count < count)
            {
                var slot = new Slot(_row, _slots.Count);
                slot.View.PointerEntered += HandleSlotPointerEntered;
                slot.View.PointerExited += HandleSlotPointerExited;
                slot.View.Selected += HandleSlotSelected;
                slot.View.Deselected += HandleSlotDeselected;
                int index = _slots.Count;
                slot.Button.onClick.AddListener(() => SlotActivated?.Invoke(index));
                if (_hudStyle != null && _style != null)
                {
                    slot.ApplyStyle(_hudStyle, _style);
                }

                _slots.Add(slot);
            }
        }

        private void Layout()
        {
            if (_hudStyle == null || _style == null)
            {
                return;
            }

            int count = _model != null ? _model.Quiver.Capacity : _slots.Count;
            float size = _style.SlotSize;
            float padding = _style.Padding;
            float rowWidth = count * size + Mathf.Max(0, count - 1) * _style.SlotSpacing;
            float width = rowWidth + padding * 2f;
            float halfRail = Mathf.Min(_style.RailHeight, size) * 0.5f;
            float trim = Mathf.Min(_style.RailTrimWidth, halfRail);
            float middle = size * 0.5f;
            float captionHeight = _style.ShowCaption ? _style.CaptionFontSize * KeyLineHeight : 0f;
            float captionBand = _style.ShowCaption ? captionHeight + _style.CaptionGap : 0f;

            UIBuildUtility.Place(_rect, _style.Anchor, _style.Anchor, _style.Offset, new Vector2(width, size + captionBand));
            UIBuildUtility.Place(_caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding, 0f),
                new Vector2(Mathf.Max(0f, width - padding * 2f), captionHeight));
            UIBuildUtility.SetOffsets(_railTrim.rectTransform, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0f, middle - halfRail), new Vector2(0f, middle + halfRail));
            UIBuildUtility.SetOffsets(_rail.rectTransform, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(trim, middle - halfRail + trim), new Vector2(-trim, middle + halfRail - trim));
            UIBuildUtility.Place(_row, Vector2.zero, Vector2.zero, new Vector2(padding, 0f), new Vector2(rowWidth, size));

            for (int i = 0; i < _slots.Count; i++)
            {
                bool visible = i < count;
                _slots[i].SetVisible(visible);
                if (visible)
                {
                    UIBuildUtility.Place(_slots[i].Rect, Vector2.zero, Vector2.zero,
                        new Vector2(i * (size + _style.SlotSpacing), 0f), new Vector2(size, size));
                }
            }
        }

        private void RefreshSlots()
        {
            _group.alpha = _interactable || _style == null ? 1f : _style.InactiveAlpha;
            _group.interactable = _interactable;
            _group.blocksRaycasts = _interactable;
            if (_style == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                ArrowDefinitionSO arrow = _model != null && _model.Quiver.HasArrow(i) ? _model.Quiver[i] : null;
                _slots[i].Show(arrow, _style, _interactable);
            }

            RefreshNavigation();
        }

        private void RefreshNavigation()
        {
            int filled = _model != null && _interactable ? Mathf.Min(_model.Quiver.Count, _slots.Count) : 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i >= filled)
                {
                    _slots[i].Button.navigation = new Navigation { mode = Navigation.Mode.None };
                    continue;
                }

                _slots[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _slots[i - 1].Button : null,
                    selectOnRight = i < filled - 1 ? _slots[i + 1].Button : null,
                    selectOnDown = _downNeighbour,
                };
            }
        }

        private void HandleQuiverChanged(Quiver quiver)
        {
            bool selectedEmptied = _selected >= 0 && !quiver.HasArrow(_selected) && IsSlotSelected(_selected);
            RefreshSlots();
            RefreshConsidered();
            NavigationChanged?.Invoke();

            if (!selectedEmptied)
            {
                return;
            }

            if (quiver.Count > 0)
            {
                Select(quiver.Count - 1);
            }
            else
            {
                SelectionLost?.Invoke();
            }
        }

        private void HandleTargetChanged(int targetIndex)
        {
            _targetIndex = targetIndex;
            RefreshTooltipValues();
        }

        private void HandleSlotPointerEntered(QuiverSlotView view)
        {
            _hovered = view.Index;
            RefreshConsidered();
        }

        private void HandleSlotPointerExited(QuiverSlotView view)
        {
            if (_hovered == view.Index)
            {
                _hovered = -1;
                RefreshConsidered();
            }
        }

        private void HandleSlotSelected(QuiverSlotView view)
        {
            _selected = view.Index;
            RefreshConsidered();
        }

        private void HandleSlotDeselected(QuiverSlotView view)
        {
            if (_selected == view.Index)
            {
                _selected = -1;
                RefreshConsidered();
            }
        }

        private bool IsSlotSelected(int slot)
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && slot < _slots.Count
                && eventSystem.currentSelectedGameObject == _slots[slot].Button.gameObject;
        }

        private void RefreshConsidered()
        {
            int considered = ComputeConsidered();
            ArrowDefinitionSO arrow = considered >= 0 ? _model.Quiver[considered] : null;
            if (considered == _considered && arrow == _consideredArrow)
            {
                return;
            }

            bool slotChanged = considered != _considered;
            _considered = considered;
            _consideredArrow = arrow;
            ApplyConsidered(slotChanged);
            ConsideredSlotChanged?.Invoke(considered);
        }

        private int ComputeConsidered()
        {
            if (_model == null || !_rect.gameObject.activeInHierarchy)
            {
                return -1;
            }

            if (_targeting != null && _targeting.IsActive)
            {
                return _targeting.IsArrowMode && _model.Quiver.HasArrow(_targeting.ArrowSlot) ? _targeting.ArrowSlot : -1;
            }

            if (!_interactable)
            {
                return -1;
            }

            int slot = _hovered >= 0 ? _hovered : _selected;
            return _model.Quiver.HasArrow(slot) ? slot : -1;
        }

        private void ApplyConsidered(bool punch)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetConsidered(i == _considered, _style, punch);
            }

            if (_consideredArrow == null || _style == null)
            {
                _tooltip.gameObject.SetActive(false);
                _tooltipNameText.Clear();
                _tooltipDescriptionText.Clear();
                return;
            }

            _tooltip.gameObject.SetActive(true);
            _tooltipName.color = _consideredArrow.Color;
            _tooltipNameText.Set(LocalizationTableNames.Combat, _consideredArrow.NameKey);
            _tooltipDescriptionText.Set(LocalizationTableNames.Combat, _consideredArrow.DescriptionKey);
            RefreshTooltipValues();
        }

        private void RefreshTooltipValues()
        {
            if (_consideredArrow == null || _model == null)
            {
                return;
            }

            ICombatant target = null;
            IReadOnlyList<ICombatant> enemies = _model.EnemyFighters;
            if (_targeting != null && _targeting.IsArrowMode && _targetIndex >= 0 && _targetIndex < enemies.Count)
            {
                target = enemies[_targetIndex];
            }

            _values.Clear();
            _consideredArrow.CollectValues(new CardTextContext(_model.Resolver, _model.Player, target), _values);
            _tooltipDescriptionText.SetValues(_values);
        }

        private sealed class Slot : IDisposable
        {
            private readonly Image _ring;
            private readonly Image _frame;
            private readonly Image _arrow;
            private readonly TextMeshProUGUI _key;
            private Tween _punch;

            public Slot(Transform parent, int index)
            {
                _ring = UIBuildUtility.CreateImage("Slot" + (index + 1), parent, true);
                _ring.color = Color.clear;
                Rect = _ring.rectTransform;
                Button = _ring.gameObject.AddComponent<Button>();
                Button.transition = Selectable.Transition.None;
                Button.targetGraphic = _ring;
                View = _ring.gameObject.AddComponent<QuiverSlotView>();
                View.Initialize(index);

                _frame = UIBuildUtility.CreateImage("Frame", Rect, false);
                _arrow = UIBuildUtility.CreateImage("Arrow", _frame.transform, false);
                _arrow.preserveAspect = true;
                UIBuildUtility.Place(_arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                _key = UIBuildUtility.CreateText("Key", Rect, TextAlignmentOptions.Top);
                _key.SetText("{0}", index + 1);
            }

            public RectTransform Rect { get; }

            public Button Button { get; }

            public QuiverSlotView View { get; }

            public void ApplyStyle(CombatHudStyleSO hudStyle, QuiverStyleSO style)
            {
                float thickness = style.HighlightThickness;
                UIBuildUtility.SetOffsets(_frame.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(thickness, thickness), new Vector2(-thickness, -thickness));
                float iconSize = Mathf.Max(0f, style.SlotSize - thickness * 2f) * style.ArrowIconFraction;
                _arrow.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                _ring.sprite = style.SlotSprite;
                _frame.sprite = style.SlotSprite;

                UIBuildUtility.StyleText(_key, hudStyle.Font, style.KeyHintFontSize, style.KeyHintColor);
                UIBuildUtility.Place(_key.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -style.KeyHintGap), new Vector2(style.SlotSize, style.KeyHintFontSize * KeyLineHeight));
                _key.gameObject.SetActive(style.ShowKeyHints);
            }

            public void SetVisible(bool visible)
            {
                if (Rect.gameObject.activeSelf != visible)
                {
                    Rect.gameObject.SetActive(visible);
                }
            }

            public void Show(ArrowDefinitionSO arrow, QuiverStyleSO style, bool interactable)
            {
                bool filled = arrow != null;
                _frame.color = filled ? style.SlotColor : style.EmptySlotColor;
                _arrow.gameObject.SetActive(filled);
                Button.interactable = interactable && filled;
                if (!filled)
                {
                    return;
                }

                bool hasIcon = arrow.Icon != null;
                _arrow.sprite = hasIcon ? arrow.Icon : style.FallbackArrowSprite;
                _arrow.color = hasIcon ? Color.white : arrow.Color;
            }

            public void SetConsidered(bool considered, QuiverStyleSO style, bool punch)
            {
                if (style == null)
                {
                    _ring.color = Color.clear;
                    return;
                }

                _ring.color = considered ? style.HighlightColor : style.SocketRimColor;
                if (!considered || !punch || style.PunchScale <= 0f)
                {
                    return;
                }

                _punch?.Kill(true);
                Rect.localScale = Vector3.one;
                _punch = Rect.DOPunchScale(Vector3.one * style.PunchScale, style.PunchDuration, PunchVibrato, PunchElasticity);
            }

            public void Dispose()
            {
                _punch?.Kill();
                _punch = null;
                Button.onClick.RemoveAllListeners();
            }
        }
    }
}
