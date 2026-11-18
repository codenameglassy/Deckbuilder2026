// The pile viewer: a full-screen overlay listing every card in one pile as a scrolling grid, with a localized title,
// the card count, an empty message and a Close button. The caller chooses the order by passing a fill strategy (the
// draw pile is shown sorted so it never reveals what comes next). Clicking the backdrop or Close, Escape or the
// east face button closes it; the grid has explicit keyboard and gamepad navigation, and the view scrolls to the
// selected card. Refresh rebuilds it while open, for piles that change underneath. It restores the previous
// selection on close and reports OpenChanged so the HUD can block the hand. Cards are pooled CardViews styled by the
// Combat HUD style; everything else comes from CombatInspectStyleSO. It only presents and never moves cards.
using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class PileViewer : IDisposable
    {
        private const int PoolCapacity = 12;
        private const int PoolMax = 64;
        private const int GridVerticalPadding = 8;

        private readonly RectTransform _root;
        private readonly CanvasGroup _rootGroup;
        private readonly Image _backdrop;
        private readonly Button _backdropButton;
        private readonly Image _panel;
        private readonly TextMeshProUGUI _title;
        private readonly LocalizedText _titleText;
        private readonly TextMeshProUGUI _count;
        private readonly RectTransform _viewport;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _content;
        private readonly GridLayoutGroup _grid;
        private readonly TextMeshProUGUI _empty;
        private readonly LocalizedText _emptyText;
        private readonly Image _closeImage;
        private readonly Button _close;
        private readonly TextMeshProUGUI _closeLabel;
        private readonly LocalizedText _closeText;
        private readonly ComponentPool<CardView> _pool;
        private readonly List<CardView> _shown = new List<CardView>(32);
        private readonly List<CardInstance> _cards = new List<CardInstance>(32);
        private readonly InputAction _cancel;
        private readonly UnityAction _closeClicked;
        private readonly Action<CardView> _cardSelected;
        private readonly DOGetter<float> _getAlpha;
        private readonly DOSetter<float> _setAlpha;
        private readonly TweenCallback _clearFade;
        private CombatHudStyleSO _cardStyle;
        private CombatInspectStyleSO _style;
        private CardPile _pile;
        private Action<CardPile, List<CardInstance>> _fill;
        private GameObject _previousSelection;
        private Tween _fade;

        private PileViewer(RectTransform root, CanvasGroup rootGroup, Image backdrop, Button backdropButton, Image panel,
            TextMeshProUGUI title, TextMeshProUGUI count, RectTransform viewport, ScrollRect scroll, RectTransform content,
            GridLayoutGroup grid, TextMeshProUGUI empty, Image closeImage, Button close, TextMeshProUGUI closeLabel,
            CardView template)
        {
            _root = root;
            _rootGroup = rootGroup;
            _backdrop = backdrop;
            _backdropButton = backdropButton;
            _panel = panel;
            _title = title;
            _count = count;
            _viewport = viewport;
            _scroll = scroll;
            _content = content;
            _grid = grid;
            _empty = empty;
            _closeImage = closeImage;
            _close = close;
            _closeLabel = closeLabel;

            _titleText = new LocalizedText(title);
            _emptyText = new LocalizedText(empty);
            _emptyText.Set(LocalizationTableNames.UI, CombatUIKeys.PileEmpty);
            _closeText = new LocalizedText(closeLabel);
            _closeText.Set(LocalizationTableNames.UI, CombatUIKeys.Close);

            _pool = new ComponentPool<CardView>(template, content, PoolCapacity, PoolMax);
            _closeClicked = Close;
            _cardSelected = ScrollTo;
            _getAlpha = () => _rootGroup.alpha;
            _setAlpha = value => _rootGroup.alpha = value;
            _clearFade = () => _fade = null;

            _close.onClick.AddListener(_closeClicked);
            _backdropButton.onClick.AddListener(_closeClicked);

            _cancel = new InputAction("PileViewerClose", InputActionType.Button, "<Keyboard>/escape");
            _cancel.AddBinding("<Gamepad>/buttonEast");
            _cancel.performed += _ => Close();
        }

        public event Action<bool> OpenChanged;

        public bool IsOpen { get; private set; }

        public CardPile Pile => _pile;

        public static PileViewer Create(Transform parent, CardView template)
        {
            RectTransform root = UIBuildUtility.CreateRect("PileViewer", parent);
            UIBuildUtility.Stretch(root);
            CanvasGroup rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            Image backdrop = UIBuildUtility.CreateImage("Backdrop", root, true);
            UIBuildUtility.Stretch(backdrop.rectTransform);
            Button backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.navigation = new Navigation { mode = Navigation.Mode.None };

            Image panel = UIBuildUtility.CreateImage("Panel", root, true);
            TextMeshProUGUI title = UIBuildUtility.CreateText("Title", panel.transform, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI count = UIBuildUtility.CreateText("Count", panel.transform, TextAlignmentOptions.MidlineRight);

            Image viewportImage = UIBuildUtility.CreateImage("Viewport", panel.transform, true);
            viewportImage.color = Color.clear;
            RectTransform viewport = viewportImage.rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = UIBuildUtility.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.padding = new RectOffset(0, 0, GridVerticalPadding, GridVerticalPadding);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;

            TextMeshProUGUI empty = UIBuildUtility.CreateText("Empty", panel.transform, TextAlignmentOptions.Center);

            Image closeImage = UIBuildUtility.CreateImage("Close", panel.transform, true);
            Button close = closeImage.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImage;
            close.transition = Selectable.Transition.ColorTint;
            TextMeshProUGUI closeLabel = UIBuildUtility.CreateText("Label", closeImage.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(closeLabel.rectTransform);

            root.gameObject.SetActive(false);
            return new PileViewer(root, rootGroup, backdrop, backdropButton, panel, title, count, viewport, scroll, content,
                grid, empty, closeImage, close, closeLabel, template);
        }

        public void ApplyStyle(CombatHudStyleSO cardStyle, CombatInspectStyleSO style)
        {
            _cardStyle = cardStyle;
            _style = style;
            if (_cardStyle == null || _style == null)
            {
                return;
            }

            float padding = style.ViewerPadding;
            float header = style.ViewerHeaderHeight;
            float closeHeight = style.ViewerCloseSize.y;

            _backdrop.color = style.ViewerBackdropColor;
            UIBuildUtility.Place(_panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, style.ViewerPanelSize);
            UIBuildUtility.StyleImage(_panel, style.ViewerPanelSprite, style.ViewerPanelColor);

            Vector2 headerMin = new Vector2(padding, -(padding + header));
            Vector2 headerMax = new Vector2(-padding, -padding);
            UIBuildUtility.SetOffsets(_title.rectTransform, new Vector2(0f, 1f), Vector2.one, headerMin, headerMax);
            UIBuildUtility.SetOffsets(_count.rectTransform, new Vector2(0f, 1f), Vector2.one, headerMin, headerMax);
            UIBuildUtility.StyleText(_title, cardStyle.Font, style.ViewerTitleFontSize, style.ViewerTitleColor);
            UIBuildUtility.StyleText(_count, cardStyle.Font, style.ViewerCountFontSize, style.ViewerCountColor);

            Vector2 bodyMin = new Vector2(padding, padding * 2f + closeHeight);
            Vector2 bodyMax = new Vector2(-padding, -(padding * 2f + header));
            UIBuildUtility.SetOffsets(_viewport, Vector2.zero, Vector2.one, bodyMin, bodyMax);
            UIBuildUtility.SetOffsets(_empty.rectTransform, Vector2.zero, Vector2.one, bodyMin, bodyMax);
            UIBuildUtility.StyleText(_empty, cardStyle.Font, style.ViewerEmptyFontSize, style.ViewerEmptyColor);

            _grid.cellSize = cardStyle.CardSize;
            _grid.spacing = style.ViewerSpacing;
            _grid.constraintCount = style.ViewerColumns;
            _scroll.scrollSensitivity = style.ViewerScrollSensitivity;

            UIBuildUtility.Place(_closeImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, padding), style.ViewerCloseSize);
            UIBuildUtility.StyleImage(_closeImage, style.ViewerCloseSprite, style.ViewerCloseColor);
            _close.colors = CardView.CreateTint(cardStyle);
            UIBuildUtility.StyleText(_closeLabel, cardStyle.Font, style.ViewerCloseFontSize, style.ViewerCloseTextColor);

            for (int i = 0; i < _shown.Count; i++)
            {
                _shown[i].ApplyStyle(cardStyle);
            }

            if (IsOpen)
            {
                WireNavigation();
            }
        }

        public void Open(CardPile pile, string titleKey, Action<CardPile, List<CardInstance>> fill)
        {
            if (pile == null || fill == null || _cardStyle == null || _style == null)
            {
                return;
            }

            bool wasOpen = IsOpen;
            if (!wasOpen)
            {
                EventSystem eventSystem = EventSystem.current;
                _previousSelection = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            }

            _pile = pile;
            _fill = fill;
            _titleText.Set(LocalizationTableNames.UI, titleKey);
            _root.gameObject.SetActive(true);
            Populate(0);
            _cancel.Enable();

            if (wasOpen)
            {
                return;
            }

            IsOpen = true;
            StopFade();
            _rootGroup.alpha = 0f;
            _fade = DOTween.To(_getAlpha, _setAlpha, 1f, _style.ViewerFadeDuration).SetUpdate(true).OnComplete(_clearFade);
            OpenChanged?.Invoke(true);
        }

        public void Refresh()
        {
            if (!IsOpen)
            {
                return;
            }

            int selected = IndexOfSelected();
            Populate(selected >= 0 ? selected : 0);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            StopFade();
            ReleaseShown();
            _cancel.Disable();
            _root.gameObject.SetActive(false);
            _pile = null;
            _fill = null;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                GameObject restore = _previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null;
                eventSystem.SetSelectedGameObject(restore);
            }

            _previousSelection = null;
            OpenChanged?.Invoke(false);
        }

        public void Dispose()
        {
            OpenChanged = null;
            StopFade();
            ReleaseShown();
            _cancel.Dispose();
            _close.onClick.RemoveListener(_closeClicked);
            _backdropButton.onClick.RemoveListener(_closeClicked);
            _titleText.Clear();
            _emptyText.Clear();
            _closeText.Clear();
            _pool.Dispose();
        }

        private void Populate(int selectIndex)
        {
            ReleaseShown();
            _cards.Clear();
            _fill(_pile, _cards);

            for (int i = 0; i < _cards.Count; i++)
            {
                CardView view = _pool.Get();
                view.ApplyStyle(_cardStyle);
                view.Bind(_cards[i]);
                view.SetPlayable(true, 1f);
                view.transform.SetSiblingIndex(i);
                view.Selected += _cardSelected;
                _shown.Add(view);
            }

            _count.SetText("{0}", (float)_cards.Count);
            _empty.gameObject.SetActive(_cards.Count == 0);
            WireNavigation();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scroll.StopMovement();
            _content.anchoredPosition = Vector2.zero;
            Select(Mathf.Min(selectIndex, _shown.Count - 1));
        }

        private void ReleaseShown()
        {
            for (int i = _shown.Count - 1; i >= 0; i--)
            {
                CardView view = _shown[i];
                view.Selected -= _cardSelected;
                _pool.Release(view);
            }

            _shown.Clear();
        }

        private void WireNavigation()
        {
            int count = _shown.Count;
            int columns = Mathf.Max(1, _style.ViewerColumns);
            for (int i = 0; i < count; i++)
            {
                int column = i % columns;
                _shown[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = column > 0 ? _shown[i - 1].Button : null,
                    selectOnRight = column < columns - 1 && i + 1 < count ? _shown[i + 1].Button : null,
                    selectOnUp = i - columns >= 0 ? _shown[i - columns].Button : null,
                    selectOnDown = i + columns < count ? _shown[i + columns].Button : (Selectable)_close,
                };
            }

            _close.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = count > 0 ? _shown[count - 1].Button : null,
            };
        }

        private void Select(int index)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            GameObject target = index >= 0 && index < _shown.Count ? _shown[index].gameObject : _close.gameObject;
            eventSystem.SetSelectedGameObject(target);
        }

        private int IndexOfSelected()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null)
            {
                return -1;
            }

            for (int i = 0; i < _shown.Count; i++)
            {
                if (_shown[i].gameObject == selected)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ScrollTo(CardView view)
        {
            var item = (RectTransform)view.transform;
            float viewportHeight = _viewport.rect.height;
            float contentHeight = _content.rect.height;
            Vector2 position = _content.anchoredPosition;

            float top = item.localPosition.y + item.rect.yMax;
            float bottom = item.localPosition.y + item.rect.yMin;
            if (top > -position.y)
            {
                position.y = -top;
            }
            else if (bottom < -position.y - viewportHeight)
            {
                position.y = -bottom - viewportHeight;
            }

            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, contentHeight - viewportHeight));
            _scroll.StopMovement();
            _content.anchoredPosition = position;
        }

        private void StopFade()
        {
            if (_fade == null)
            {
                return;
            }

            Tween fade = _fade;
            _fade = null;
            fade.Kill();
        }
    }
}
