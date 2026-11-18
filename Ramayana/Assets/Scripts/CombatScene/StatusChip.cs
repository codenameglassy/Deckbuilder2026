// One status chip under a fighter's health bar: the status's icon, or until it has one the fallback disc in the
// status's colour with its short localized label, and the stack count on a small badge at the corner. Built in code
// by StatusChipRow and reused for whichever status lands in its slot; it only re-styles when the status in its slot
// changes and only rewrites the count when the amount changes, so a refresh allocates nothing. The label follows the
// player's language through one LocalizedString, unsubscribed when the chip is hidden or disposed. While a card is
// previewed a chip can show as a ghost (a status the card would add, faded through its CanvasGroup) or with its count
// in the preview colour (a status whose stacks the card would change).
using System;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace JourneyToLanka.CombatScene
{
    internal sealed class StatusChip : IDisposable
    {
        private readonly RectTransform _root;
        private readonly Image _disc;
        private readonly TextMeshProUGUI _label;
        private readonly Image _countBadge;
        private readonly TextMeshProUGUI _count;
        private readonly CanvasGroup _group;
        private readonly LocalizedString _shortName = new LocalizedString();
        private readonly LocalizedString.ChangeHandler _shortNameChanged;
        private CombatantViewStyleSO _style;
        private StatusDefinitionSO _definition;
        private int _amount = -1;
        private bool _subscribed;
        private bool _ghost;
        private bool _changed;

        private StatusChip(RectTransform root, Image disc, TextMeshProUGUI label, Image countBadge, TextMeshProUGUI count,
            CanvasGroup group)
        {
            _root = root;
            _group = group;
            _disc = disc;
            _label = label;
            _countBadge = countBadge;
            _count = count;
            _shortNameChanged = HandleShortNameChanged;
        }

        public static StatusChip Create(Transform parent)
        {
            var rootObject = new GameObject("Status", typeof(RectTransform));
            var root = (RectTransform)rootObject.transform;
            root.SetParent(parent, false);
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);

            Image disc = rootObject.AddComponent<Image>();
            disc.raycastTarget = false;
            CanvasGroup group = rootObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            TextMeshProUGUI label = CreateText("Label", root);
            Stretch(label.rectTransform);

            var badgeObject = new GameObject("Count", typeof(RectTransform));
            var badgeRect = (RectTransform)badgeObject.transform;
            badgeRect.SetParent(root, false);
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            Image countBadge = badgeObject.AddComponent<Image>();
            countBadge.raycastTarget = false;

            TextMeshProUGUI count = CreateText("Value", badgeRect);
            Stretch(count.rectTransform);

            rootObject.SetActive(false);
            return new StatusChip(root, disc, label, countBadge, count, group);
        }

        public void ApplyStyle(CombatantViewStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            _root.sizeDelta = style.StatusChipSize;
            _countBadge.rectTransform.sizeDelta = style.StatusCountBadgeSize;
            _countBadge.sprite = style.StatusChipSprite;
            _countBadge.color = style.StatusCountBadgeColor;
            StyleText(_label, style.Font, style.StatusLabelFontSize, style.StatusLabelColor);
            StyleText(_count, style.Font, style.StatusCountFontSize, style.StatusCountColor);
            ApplyPreviewLook();

            if (_definition != null)
            {
                ApplyDefinition();
            }
        }

        public void Show(in StatusInstance status, bool ghost, bool changed)
        {
            if (ghost != _ghost || changed != _changed)
            {
                _ghost = ghost;
                _changed = changed;
                ApplyPreviewLook();
            }

            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
            }

            if (status.Definition != _definition)
            {
                _definition = status.Definition;
                ApplyDefinition();
            }

            if (status.Amount != _amount)
            {
                _amount = status.Amount;
                _count.SetText("{0}", (float)_amount);
            }
        }

        public void Hide()
        {
            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }

            _definition = null;
            _amount = -1;
            ClearShortName();
        }

        public void SetPosition(float x)
        {
            _root.anchoredPosition = new Vector2(x, 0f);
        }

        public void Dispose()
        {
            ClearShortName();
        }

        private void ApplyPreviewLook()
        {
            _group.alpha = _ghost && _style != null ? _style.StatusGhostAlpha : 1f;
            if (_style != null)
            {
                _count.color = _ghost || _changed ? _style.PreviewTextColor : _style.StatusCountColor;
            }
        }

        private void ApplyDefinition()
        {
            Sprite icon = _definition.Icon;
            bool hasIcon = icon != null;
            Sprite sprite = hasIcon ? icon : _style != null ? _style.StatusChipSprite : null;
            _disc.sprite = sprite;
            _disc.preserveAspect = sprite != null;
            _disc.color = hasIcon ? Color.white : _definition.Color;
            _label.gameObject.SetActive(!hasIcon);

            if (hasIcon)
            {
                ClearShortName();
            }
            else
            {
                SetShortName(_definition.ShortNameKey);
            }
        }

        private void SetShortName(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                ClearShortName();
                return;
            }

            _shortName.TableReference = LocalizationTableNames.Combat;
            _shortName.TableEntryReference = key;
            if (_subscribed)
            {
                _shortName.RefreshString();
                return;
            }

            _shortName.StringChanged += _shortNameChanged;
            _subscribed = true;
        }

        private void ClearShortName()
        {
            if (_subscribed)
            {
                _shortName.StringChanged -= _shortNameChanged;
                _subscribed = false;
            }

            _label.text = string.Empty;
        }

        private void HandleShortNameChanged(string value)
        {
            _label.text = value;
        }

        private static void StyleText(TextMeshProUGUI text, TMP_FontAsset font, float size, Color color)
        {
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.color = color;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(string textName, Transform parent)
        {
            var textObject = new GameObject(textName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
    }
}
