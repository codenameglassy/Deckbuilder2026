// A small HUD panel with a localized caption over a number, used for Prana and the draw and discard piles.
// Numbers update through TMP's numeric SetText, which does not allocate.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class StatPanel
    {
        private const float CaptionShare = 0.38f;

        private readonly RectTransform _rect;
        private readonly Image _background;
        private readonly TextMeshProUGUI _caption;
        private readonly TextMeshProUGUI _value;
        private readonly LocalizedText _captionText;

        private StatPanel(RectTransform rect, Image background, TextMeshProUGUI caption, TextMeshProUGUI value)
        {
            _rect = rect;
            _background = background;
            _caption = caption;
            _value = value;
            _captionText = new LocalizedText(caption);
        }

        public static StatPanel Create(string name, Transform parent, string captionTable, string captionKey)
        {
            Image background = UIBuildUtility.CreateImage(name, parent, false);
            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", background.transform, TextAlignmentOptions.Center);
            TextMeshProUGUI value = UIBuildUtility.CreateText("Value", background.transform, TextAlignmentOptions.Center);

            UIBuildUtility.SetOffsets(caption.rectTransform, new Vector2(0f, 1f - CaptionShare), Vector2.one, Vector2.zero, Vector2.zero);
            UIBuildUtility.SetOffsets(value.rectTransform, Vector2.zero, new Vector2(1f, 1f - CaptionShare), Vector2.zero, Vector2.zero);

            var panel = new StatPanel(background.rectTransform, background, caption, value);
            panel._captionText.Set(captionTable, captionKey);
            return panel;
        }

        public void ApplyStyle(CombatHudStyleSO style, Vector2 corner, Vector2 offset)
        {
            UIBuildUtility.Place(_rect, corner, corner, offset, style.StatPanelSize);
            UIBuildUtility.StyleImage(_background, style.PanelSprite, style.PanelColor);
            UIBuildUtility.StyleText(_caption, style.Font, style.CaptionFontSize, style.HudTextColor);
            UIBuildUtility.StyleText(_value, style.Font, style.ValueFontSize, style.HudTextColor);
        }

        public void SetActive(bool active)
        {
            _rect.gameObject.SetActive(active);
        }

        public void SetValue(int value)
        {
            _value.SetText("{0}", (float)value);
        }

        public void SetValue(int value, int max)
        {
            _value.SetText("{0}/{1}", (float)value, (float)max);
        }

        public void Dispose()
        {
            _captionText.Clear();
        }
    }
}
