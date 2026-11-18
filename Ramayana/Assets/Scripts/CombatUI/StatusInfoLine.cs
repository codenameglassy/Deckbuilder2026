// One status in the info panel: a line with the status's localized name and its stacks in the status's colour, and
// under it the status's localized description in smaller text that wraps to the panel's width. Built in code by the
// info panel, which creates one the first time a fighter shows that many statuses and reuses it after; it only
// re-binds its texts when the status in its slot changes and only rewrites the count when the amount changes.
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class StatusInfoLine
    {
        private readonly RectTransform _row;
        private readonly LayoutElement _rowElement;
        private readonly TextMeshProUGUI _caption;
        private readonly TextMeshProUGUI _value;
        private readonly TextMeshProUGUI _description;
        private readonly LocalizedText _captionText;
        private readonly LocalizedText _descriptionText;
        private StatusDefinitionSO _definition;
        private int _amount = -1;

        private StatusInfoLine(RectTransform row, LayoutElement rowElement, TextMeshProUGUI caption, TextMeshProUGUI value,
            TextMeshProUGUI description)
        {
            _row = row;
            _rowElement = rowElement;
            _caption = caption;
            _value = value;
            _description = description;
            _captionText = new LocalizedText(caption);
            _descriptionText = new LocalizedText(description);
        }

        public static StatusInfoLine Create(RectTransform panel)
        {
            RectTransform row = UIBuildUtility.CreateRect("Status", panel);
            LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", row, TextAlignmentOptions.MidlineLeft);
            UIBuildUtility.Stretch(caption.rectTransform);
            TextMeshProUGUI value = UIBuildUtility.CreateText("Value", row, TextAlignmentOptions.MidlineRight);
            UIBuildUtility.Stretch(value.rectTransform);
            TextMeshProUGUI description = UIBuildUtility.CreateText("Description", panel, TextAlignmentOptions.TopLeft);

            var line = new StatusInfoLine(row, rowElement, caption, value, description);
            line.SetVisible(false);
            return line;
        }

        public void ApplyStyle(CombatHudStyleSO hudStyle, CombatInspectStyleSO style)
        {
            _rowElement.preferredHeight = style.InfoRowHeight;
            UIBuildUtility.StyleText(_caption, hudStyle.Font, style.InfoRowFontSize, style.InfoTextColor);
            UIBuildUtility.StyleText(_value, hudStyle.Font, style.InfoRowFontSize, style.InfoTextColor);
            UIBuildUtility.StyleText(_description, hudStyle.Font, style.InfoStatusDescriptionFontSize, style.InfoStatusDescriptionColor);
            if (_definition != null)
            {
                ApplyColor();
            }
        }

        public void Show(in StatusInstance status)
        {
            SetVisible(true);
            if (status.Definition != _definition)
            {
                _definition = status.Definition;
                _captionText.Set(LocalizationTableNames.Combat, _definition.NameKey);
                _descriptionText.Set(LocalizationTableNames.Combat, _definition.DescriptionKey);
                ApplyColor();
            }

            if (status.Amount != _amount)
            {
                _amount = status.Amount;
                _value.SetText("{0}", (float)_amount);
            }
        }

        public void Hide()
        {
            SetVisible(false);
            if (_definition == null)
            {
                return;
            }

            _definition = null;
            _amount = -1;
            _captionText.Clear();
            _descriptionText.Clear();
        }

        public void Dispose()
        {
            _captionText.Clear();
            _descriptionText.Clear();
        }

        private void ApplyColor()
        {
            _caption.color = _definition.Color;
            _value.color = _definition.Color;
        }

        private void SetVisible(bool visible)
        {
            if (_row.gameObject.activeSelf != visible)
            {
                _row.gameObject.SetActive(visible);
                _description.gameObject.SetActive(visible);
            }
        }
    }
}
