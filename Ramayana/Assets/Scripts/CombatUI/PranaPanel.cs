// The Prana panel on the combat HUD: a localized caption and a "current/per turn" number over a row of Prana orbs.
// Spending drains orbs right to left and refilling lights them left to right, each staggered with a flash; bonus
// Prana adds orbs up to the style's cap. While a card is focused, dragged or being targeted, the orbs it would spend
// pulse and any it lacks glow red (SetPreviewCost). A card rejected for lack of Prana shakes the panel and flashes the
// missing orbs red. Orbs are created once up to the cap and reused. Values come from the Combat HUD style (panel,
// caption, placement) and its Prana Orbs style (everything about the orbs). It only presents.
using System.Collections.Generic;
using DG.Tweening;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class PranaPanel
    {
        private const int ShakeVibrato = 10;
        private const float ShakeElasticity = 0.5f;
        private const float CaptionLineHeight = 1.3f;

        private readonly RectTransform _rect;
        private readonly Image _background;
        private readonly TextMeshProUGUI _caption;
        private readonly TextMeshProUGUI _value;
        private readonly RectTransform _row;
        private readonly LocalizedText _captionText;
        private readonly List<PranaOrb> _orbs = new List<PranaOrb>(10);
        private readonly TweenCallback _clearShake;
        private CombatHudStyleSO _hudStyle;
        private PranaOrbsStyleSO _style;
        private Vector2 _offset;
        private Tween _shake;
        private int _current;
        private int _perTurn;
        private int _count = -1;
        private int _previewCost = -1;

        private PranaPanel(RectTransform rect, Image background, TextMeshProUGUI caption, TextMeshProUGUI value, RectTransform row)
        {
            _rect = rect;
            _background = background;
            _caption = caption;
            _value = value;
            _row = row;
            _captionText = new LocalizedText(caption);
            _captionText.Set(LocalizationTableNames.UI, CombatUIKeys.Prana);
            _clearShake = () => _shake = null;
        }

        public RectTransform Rect => _rect;

        public static PranaPanel Create(Transform parent)
        {
            Image background = UIBuildUtility.CreateImage("Prana", parent, false);
            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", background.transform, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI value = UIBuildUtility.CreateText("Value", background.transform, TextAlignmentOptions.MidlineRight);
            RectTransform row = UIBuildUtility.CreateRect("Orbs", background.transform);
            return new PranaPanel(background.rectTransform, background, caption, value, row);
        }

        public void ApplyStyle(CombatHudStyleSO hudStyle, PranaOrbsStyleSO style, Vector2 offset)
        {
            _hudStyle = hudStyle;
            _style = style;
            _offset = offset;
            UIBuildUtility.StyleImage(_background, hudStyle.PanelSprite, hudStyle.PanelColor);
            UIBuildUtility.StyleText(_caption, hudStyle.Font, hudStyle.CaptionFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_value, hudStyle.Font, style != null ? style.ValueFontSize : hudStyle.ValueFontSize,
                hudStyle.HudTextColor);

            for (int i = 0; i < _orbs.Count; i++)
            {
                _orbs[i].ApplyStyle(style);
            }

            _count = -1;
            Show(_current, _perTurn, false);
        }

        public void SetActive(bool active)
        {
            _rect.gameObject.SetActive(active);
        }

        public void Show(int current, int perTurn, bool animate)
        {
            int previous = _current;
            _current = current;
            _perTurn = perTurn;
            _value.SetText("{0}/{1}", (float)current, (float)perTurn);
            if (_hudStyle == null)
            {
                return;
            }

            int count = _style != null ? PranaOrbMath.OrbCount(perTurn, current, _style.MaxOrbs) : 0;
            if (count != _count)
            {
                _count = count;
                EnsureOrbs(count);
                Layout();
            }

            for (int i = 0; i < count; i++)
            {
                bool filled = i < current;
                float delay = 0f;
                if (animate && filled && i >= previous)
                {
                    delay = (i - previous) * _style.RefillStagger;
                }
                else if (animate && !filled && i < previous)
                {
                    delay = (previous - 1 - i) * _style.DrainStagger;
                }

                _orbs[i].SetFilled(filled, _style, animate, delay);
            }

            RefreshPreview();
        }

        public void SetPreviewCost(int cost)
        {
            if (cost == _previewCost)
            {
                return;
            }

            _previewCost = cost;
            RefreshPreview();
        }

        public void PlayRejected()
        {
            if (_style == null || !_rect.gameObject.activeInHierarchy)
            {
                return;
            }

            StopShake();
            _shake = _rect.DOPunchPosition(new Vector3(_style.RejectShakeStrength, 0f, 0f), _style.RejectShakeDuration,
                    ShakeVibrato, ShakeElasticity)
                .SetUpdate(true)
                .OnComplete(_clearShake);

            for (int i = 0; i < _count; i++)
            {
                bool missing = _previewCost > _current
                    ? PranaOrbMath.Preview(i, _current, _previewCost) == PranaOrbPreview.Deficit
                    : i >= _current;
                if (missing)
                {
                    _orbs[i].FlashDeficit(_style.DeficitFlashDuration);
                }
            }
        }

        public void Dispose()
        {
            StopShake();
            _captionText.Clear();
            for (int i = 0; i < _orbs.Count; i++)
            {
                _orbs[i].Dispose();
            }

            _orbs.Clear();
        }

        private void EnsureOrbs(int count)
        {
            while (_orbs.Count < count)
            {
                var orb = new PranaOrb(_row, _orbs.Count);
                orb.ApplyStyle(_style);
                orb.SetFilled(false, _style, false, 0f);
                _orbs.Add(orb);
            }

            for (int i = 0; i < _orbs.Count; i++)
            {
                _orbs[i].SetActive(i < count);
            }
        }

        private void Layout()
        {
            float padding = _style != null ? _style.PanelPadding : 0f;
            float captionHeight = _hudStyle.CaptionFontSize * CaptionLineHeight;
            float diameter = _style != null ? _style.OrbDiameter : 0f;
            float gap = _style != null ? _style.OrbGap : 0f;
            float rowWidth = _count > 0 ? _count * diameter + (_count - 1) * gap : 0f;
            Vector2 statSize = _hudStyle.StatPanelSize;
            float width = Mathf.Max(statSize.x, rowWidth + padding * 2f);
            float height = _count > 0 ? padding * 2f + captionHeight + diameter + gap : statSize.y;

            StopShake();
            UIBuildUtility.Place(_rect, Vector2.zero, Vector2.zero, _offset, new Vector2(width, height));
            UIBuildUtility.SetOffsets(_caption.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(padding, -(padding + captionHeight)), new Vector2(-padding, -padding));
            UIBuildUtility.SetOffsets(_value.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(padding, -(padding + captionHeight)), new Vector2(-padding, -padding));
            UIBuildUtility.SetOffsets(_row, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(padding, padding), new Vector2(-padding, padding + diameter));

            if (_style == null)
            {
                return;
            }

            float quad = _style.OrbQuadSize;
            for (int i = 0; i < _count; i++)
            {
                RectTransform orb = _orbs[i].Rect;
                orb.sizeDelta = new Vector2(quad, quad);
                orb.anchoredPosition = new Vector2(diameter * 0.5f + i * (diameter + gap), diameter * 0.5f);
            }
        }

        private void RefreshPreview()
        {
            if (_style == null)
            {
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                _orbs[i].SetPreview(PranaOrbMath.Preview(i, _current, _previewCost), _style.PreviewFadeDuration);
            }
        }

        private void StopShake()
        {
            if (_shake == null)
            {
                return;
            }

            Tween shake = _shake;
            _shake = null;
            shake.Kill(true);
        }
    }
}
