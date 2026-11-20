// Prana on the combat HUD: a small medallion (a gold ring around a dark face, both drawn by the Card Art shader's round
// material) showing "current/per turn", with the Prana orbs on an arc around it (PranaArcLayout), pointing toward the
// hand by default. Spending drains orbs from the bottom of the arc up and refilling lights them from the top down, each
// staggered with a flash; bonus Prana adds orbs up to the style's cap, extending the arc around the medallion. While a
// card is focused, dragged or being targeted, the orbs it would spend pulse and any it lacks glow red (SetPreviewCost).
// A card rejected for lack of Prana shakes the medallion and orbs and flashes the missing orbs red. Orbs are created
// once up to the cap and reused. The HUD places the medallion's centre (PlaceAt) in its left column, using
// ColumnHalfHeight: how far the per-turn orbs reach above and below the centre, at least the medallion's radius. Bonus
// orbs may reach past it for a while instead of pushing the column around; ColumnChanged fires when the per-turn amount
// or the style changes it. Everything else comes from the Prana Orbs style. It only presents.
using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class PranaPanel
    {
        private const int ShakeVibrato = 10;
        private const float ShakeElasticity = 0.5f;

        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        private readonly RectTransform _rect;
        private readonly Image _ring;
        private readonly Image _face;
        private readonly TextMeshProUGUI _value;
        private readonly RectTransform _row;
        private readonly List<PranaOrb> _orbs = new List<PranaOrb>(10);
        private readonly TweenCallback _clearShake;
        private CombatHudStyleSO _hudStyle;
        private PranaOrbsStyleSO _style;
        private Vector2 _centre;
        private float _columnHalfHeight;
        private Tween _shake;
        private int _current;
        private int _perTurn;
        private int _count = -1;
        private int _previewCost = -1;

        private PranaPanel(RectTransform rect, Image ring, Image face, TextMeshProUGUI value, RectTransform row)
        {
            _rect = rect;
            _ring = ring;
            _face = face;
            _value = value;
            _row = row;
            _clearShake = () => _shake = null;
        }

        public event Action ColumnChanged;

        public RectTransform Rect => _rect;

        public float ColumnHalfHeight => _columnHalfHeight;

        public static PranaPanel Create(Transform parent)
        {
            RectTransform root = UIBuildUtility.CreateRect("Prana", parent);
            Image ring = UIBuildUtility.CreateImage("MedallionRing", root, false);
            ring.gameObject.AddComponent<CardArtMeshEffect>();
            Image face = UIBuildUtility.CreateImage("MedallionFace", root, false);
            face.gameObject.AddComponent<CardArtMeshEffect>();
            TextMeshProUGUI value = UIBuildUtility.CreateText("Value", face.transform, TextAlignmentOptions.Center);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            UIBuildUtility.Stretch(value.rectTransform);
            RectTransform row = UIBuildUtility.CreateRect("Orbs", root);
            UIBuildUtility.Stretch(row);
            return new PranaPanel(root, ring, face, value, row);
        }

        public void ApplyStyle(CombatHudStyleSO hudStyle, PranaOrbsStyleSO style)
        {
            _hudStyle = hudStyle;
            _style = style;

            bool styled = hudStyle != null && style != null;
            _ring.enabled = styled;
            _face.enabled = styled;
            if (styled)
            {
                Material round = CardFrameMaterials.GetRound(hudStyle);
                UIBuildUtility.StyleImage(_ring, null, style.MedallionRingColor);
                _ring.material = round;
                UIBuildUtility.StyleImage(_face, null, style.MedallionFaceColor);
                _face.material = round;
                UIBuildUtility.StyleText(_value, hudStyle.Font, style.ValueFontSize, style.ValueColor);
            }

            for (int i = 0; i < _orbs.Count; i++)
            {
                _orbs[i].ApplyStyle(style);
            }

            _count = -1;
            Show(_current, _perTurn, false);
        }

        public void PlaceAt(Vector2 centre)
        {
            _centre = centre;
            Layout();
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

            RefreshColumnHeight();
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
            ColumnChanged = null;
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
            if (_style == null)
            {
                return;
            }

            float diameter = _style.OrbDiameter;
            float medallion = _style.MedallionDiameter;
            float half = Mathf.Max(_style.ArcRadius + diameter * 0.5f, medallion * 0.5f);
            var centre = new Vector2(half, half);
            float face = Mathf.Max(0f, medallion - _style.MedallionRingWidth * 2f);

            StopShake();
            UIBuildUtility.Place(_rect, Vector2.zero, Centre, _centre, new Vector2(half * 2f, half * 2f));
            UIBuildUtility.Place(_ring.rectTransform, Vector2.zero, Centre, centre, new Vector2(medallion, medallion));
            UIBuildUtility.Place(_face.rectTransform, Vector2.zero, Centre, centre, new Vector2(face, face));

            float quad = _style.OrbQuadSize;
            for (int i = 0; i < _count; i++)
            {
                RectTransform orb = _orbs[i].Rect;
                orb.sizeDelta = new Vector2(quad, quad);
                orb.anchoredPosition = centre + PranaArcLayout.Position(i, _count, _style.ArcRadius,
                    _style.ArcStepDegrees, _style.ArcCentreDegrees);
            }
        }

        private void RefreshColumnHeight()
        {
            float half = 0f;
            if (_style != null)
            {
                int baseCount = PranaOrbMath.OrbCount(_perTurn, 0, _style.MaxOrbs);
                half = Mathf.Max(_style.MedallionDiameter * 0.5f, PranaArcLayout.HalfHeight(baseCount, _style.ArcRadius,
                    _style.ArcStepDegrees, _style.ArcCentreDegrees, _style.OrbDiameter));
            }

            if (Mathf.Approximately(half, _columnHalfHeight))
            {
                return;
            }

            _columnHalfHeight = half;
            ColumnChanged?.Invoke();
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
