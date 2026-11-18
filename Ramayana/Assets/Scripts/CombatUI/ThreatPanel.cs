// The threat panel on Rama's side of the HUD: what the enemies' next moves will deal ("Incoming"), how much of it
// his current Kavach absorbs, and the health he will lose ("You take"), shown in the lethal colour when it would
// kill him. It presents an IncomingThreat; the HUD decides when to show it. The "You take" value gives a small punch
// when it changes, so the effect of a Kavach card reads at a glance. Numbers go through TMP's numeric SetText,
// which does not allocate. The punch is a DOTween core tween on unscaled time, killed on Hide and Dispose.
// While the player considers a card the HUD shows the threat as it would be after that card, and the numbers switch
// to the preview colour (the lethal colour still wins) so a preview never reads as the live state.
using System;
using DG.Tweening;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class ThreatPanel : IDisposable
    {
        private const int IncomingRow = 0;
        private const int KavachRow = 1;
        private const int TakenRow = 2;
        private const int RowCount = 3;
        private const int PunchVibrato = 6;
        private const float PunchElasticity = 0.5f;

        private readonly RectTransform _rect;
        private readonly Image _background;
        private readonly RectTransform _rows;
        private readonly TextMeshProUGUI[] _captions = new TextMeshProUGUI[RowCount];
        private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[RowCount];
        private readonly LocalizedText[] _captionTexts = new LocalizedText[RowCount];
        private readonly TweenCallback _clearPunch;
        private CombatHudStyleSO _style;
        private Tween _punch;
        private bool _shown;
        private int _shownTaken;
        private bool _shownLethal;
        private bool _previewing;

        private ThreatPanel(RectTransform rect, Image background, RectTransform rows)
        {
            _rect = rect;
            _background = background;
            _rows = rows;
            _clearPunch = () => _punch = null;
        }

        public static ThreatPanel Create(Transform parent)
        {
            Image background = UIBuildUtility.CreateImage("ThreatPanel", parent, false);
            RectTransform rows = UIBuildUtility.CreateRect("Rows", background.transform);
            var panel = new ThreatPanel(background.rectTransform, background, rows);

            panel.CreateRow(IncomingRow, "Incoming", CombatUIKeys.ThreatIncoming);
            panel.CreateRow(KavachRow, "Kavach", CombatUIKeys.ThreatKavach);
            panel.CreateRow(TakenRow, "Taken", CombatUIKeys.ThreatTaken);

            background.gameObject.SetActive(false);
            return panel;
        }

        public void ApplyStyle(CombatHudStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            float padding = style.ThreatPadding;
            UIBuildUtility.Place(_rect, Vector2.zero, Vector2.zero, style.ThreatPanelOffset, style.ThreatPanelSize);
            UIBuildUtility.StyleImage(_background, style.PanelSprite, style.PanelColor);
            UIBuildUtility.SetOffsets(_rows, Vector2.zero, Vector2.one, new Vector2(padding, padding), new Vector2(-padding, -padding));

            for (int i = 0; i < RowCount; i++)
            {
                UIBuildUtility.StyleText(_captions[i], style.Font, style.ThreatCaptionFontSize, style.HudTextColor);
                UIBuildUtility.StyleText(_values[i], style.Font, style.ThreatValueFontSize, style.HudTextColor);
            }

            ApplyValueColours();
        }

        public void Show(in IncomingThreat threat, bool previewing)
        {
            bool changed = _shown && threat.Taken != _shownTaken;

            _values[IncomingRow].SetText("{0}", (float)threat.Incoming);
            _values[KavachRow].SetText("{0}", (float)threat.Blocked);
            _values[TakenRow].SetText("{0}", (float)threat.Taken);

            _shownTaken = threat.Taken;
            _shownLethal = threat.IsLethal;
            _previewing = previewing;
            ApplyValueColours();

            if (!_shown)
            {
                _shown = true;
                _rect.gameObject.SetActive(true);
            }

            if (changed)
            {
                PunchTaken();
            }
        }

        public void Hide()
        {
            _punch?.Kill(true);
            _shown = false;
            _rect.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            _punch?.Kill();
            for (int i = 0; i < RowCount; i++)
            {
                _captionTexts[i]?.Clear();
            }
        }

        private void ApplyValueColours()
        {
            if (_style == null)
            {
                return;
            }

            Color preview = _style.ThreatPreviewColor;
            _values[IncomingRow].color = _previewing ? preview : _style.ThreatIncomingColor;
            _values[KavachRow].color = _previewing ? preview : _style.ThreatKavachColor;
            _values[TakenRow].color = _shownLethal ? _style.ThreatLethalColor : _previewing ? preview : _style.ThreatTakenColor;
        }

        private void PunchTaken()
        {
            if (_style == null || _style.ThreatPunchScale <= 0f)
            {
                return;
            }

            RectTransform target = _values[TakenRow].rectTransform;
            _punch?.Kill(true);
            target.localScale = Vector3.one;
            _punch = target.DOPunchScale(Vector3.one * _style.ThreatPunchScale, _style.ThreatPunchDuration, PunchVibrato, PunchElasticity)
                .SetUpdate(true)
                .OnKill(_clearPunch);
        }

        private void CreateRow(int index, string rowName, string captionKey)
        {
            RectTransform row = UIBuildUtility.CreateRect(rowName, _rows);
            float top = 1f - (float)index / RowCount;
            float bottom = 1f - (float)(index + 1) / RowCount;
            UIBuildUtility.SetOffsets(row, new Vector2(0f, bottom), new Vector2(1f, top), Vector2.zero, Vector2.zero);

            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", row, TextAlignmentOptions.MidlineLeft);
            UIBuildUtility.Stretch(caption.rectTransform);
            TextMeshProUGUI value = UIBuildUtility.CreateText("Value", row, TextAlignmentOptions.MidlineRight);
            UIBuildUtility.Stretch(value.rectTransform);

            _captions[index] = caption;
            _values[index] = value;
            _captionTexts[index] = new LocalizedText(caption);
            _captionTexts[index].Set(LocalizationTableNames.UI, captionKey);
        }
    }
}
