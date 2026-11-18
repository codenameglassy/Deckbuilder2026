// The health bar over a combatant: UGUI on its own small world-space canvas, so it shares a look with the HUD and
// a change here never rebuilds anything else. The fill shows current health; behind it a trail drains after any
// loss so the size of the loss reads, while a gain snaps both at once; the number sits on top. A Kavach badge at
// the bar's left shows current Kavach, and while the fighter has any the fill and border switch to the Kavach
// colours, so protection reads at a glance.
// The fill and trail are sized by anchors, so the images need no sprite. Built in code by CombatantView; every value
// comes from CombatantViewStyleSO. Tweens are DOTween core on game time and are killed on Dispose.
// The bar keeps a uniform world scale even when its fighter's stand-in is scaled unevenly (a tall cube), so its text
// is never stretched; its offset stays relative to the stand-in so it still sits just above it.
// ShowPreview draws a card's outcome before it is played: the health it would lose as a pulsing ghost over the fill,
// the number it would leave and the Kavach it would end with in the preview colours, and, when the blow is lethal,
// the border and ghost in the lethal colour with a pulsing tag above the bar. ClearPreview restores the live view.
// The pulse is one looping DOTween core tween on unscaled time, run only while a preview shows.
// Root is exposed so the fighter's status chips can sit under the bar on the same small canvas.
using System;
using DG.Tweening;
using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatScene
{
    internal sealed class CombatantHealthBar : IDisposable
    {
        private const int PunchVibrato = 6;
        private const float PunchElasticity = 0.5f;

        private readonly RectTransform _root;
        private readonly Image _border;
        private readonly Image _background;
        private readonly Image _trail;
        private readonly Image _fill;
        private readonly TextMeshProUGUI _value;
        private readonly Image _badge;
        private readonly TextMeshProUGUI _badgeValue;
        private readonly Image _ghost;
        private readonly TextMeshProUGUI _lethalTag;
        private readonly DOGetter<float> _getPulse;
        private readonly DOSetter<float> _setPulse;
        private readonly DOGetter<float> _getTrail;
        private readonly DOSetter<float> _setTrail;
        private readonly TweenCallback _clearTrailTween;
        private readonly TweenCallback _clearBadgePunch;
        private CombatantViewStyleSO _style;
        private Tween _trailTween;
        private Tween _badgePunch;
        private Tween _pulseTween;
        private float _pulse;
        private int _health;
        private int _maxHealth;
        private int _block;
        private int _previewHealth;
        private int _previewBlock;
        private bool _previewLethal;
        private bool _previewing;
        private float _fillRatio = 1f;
        private float _trailRatio = 1f;
        private int _lastBlock;
        private bool _shielded;
        private bool _hasValues;

        private CombatantHealthBar(RectTransform root, Image border, Image background, Image trail, Image fill,
            TextMeshProUGUI value, Image badge, TextMeshProUGUI badgeValue, Image ghost, TextMeshProUGUI lethalTag)
        {
            _root = root;
            _border = border;
            _background = background;
            _trail = trail;
            _fill = fill;
            _value = value;
            _badge = badge;
            _badgeValue = badgeValue;
            _ghost = ghost;
            _lethalTag = lethalTag;
            _getPulse = () => _pulse;
            _setPulse = SetPulse;
            _getTrail = () => _trailRatio;
            _setTrail = SetTrail;
            _clearTrailTween = () => _trailTween = null;
            _clearBadgePunch = () => _badgePunch = null;
        }

        public RectTransform Root => _root;

        public static CombatantHealthBar Create(Transform parent)
        {
            var rootObject = new GameObject("HealthBar", typeof(RectTransform));
            var root = (RectTransform)rootObject.transform;
            root.SetParent(parent, false);

            var canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Image border = CreateImage("Border", root);
            Image background = CreateImage("Background", root);
            Image trail = CreateImage("Trail", root);
            Image fill = CreateImage("Fill", root);
            Image ghost = CreateImage("PreviewGhost", root);
            TextMeshProUGUI value = CreateText("Value", root);
            TextMeshProUGUI lethalTag = CreateText("LethalTag", root);
            Image badge = CreateImage("Kavach", root);
            TextMeshProUGUI badgeValue = CreateText("Value", badge.transform);

            Stretch(background.rectTransform, 0f);
            Stretch(trail.rectTransform, 0f);
            Stretch(fill.rectTransform, 0f);
            Stretch(value.rectTransform, 0f);
            Stretch(badgeValue.rectTransform, 0f);
            badge.gameObject.SetActive(false);
            ghost.rectTransform.anchorMin = Vector2.zero;
            ghost.rectTransform.anchorMax = Vector2.zero;
            ghost.rectTransform.offsetMin = Vector2.zero;
            ghost.rectTransform.offsetMax = Vector2.zero;
            ghost.enabled = false;
            lethalTag.gameObject.SetActive(false);

            return new CombatantHealthBar(root, border, background, trail, fill, value, badge, badgeValue, ghost, lethalTag);
        }

        public void ApplyStyle(CombatantViewStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            _root.localPosition = style.BarOffset;
            _root.localScale = UniformWorldScale(_root.parent, style.BarWorldScale);
            _root.sizeDelta = style.BarSize;

            Stretch(_border.rectTransform, style.BarBorder);
            _background.color = style.BarBackgroundColor;
            _trail.color = style.BarTrailColor;
            StyleText(_value, style.Font, style.BarFontSize, style.BarTextColor);

            RectTransform badgeRect = _badge.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-(style.BarBorder + style.KavachBadgeGap), 0f);
            badgeRect.sizeDelta = style.KavachBadgeSize;
            _badge.sprite = style.KavachBadgeSprite;
            _badge.preserveAspect = style.KavachBadgeSprite != null;
            _badge.color = style.KavachBadgeColor;
            StyleText(_badgeValue, style.Font, style.KavachFontSize, style.KavachTextColor);

            RectTransform tagRect = _lethalTag.rectTransform;
            tagRect.anchorMin = new Vector2(0.5f, 1f);
            tagRect.anchorMax = new Vector2(0.5f, 1f);
            tagRect.pivot = new Vector2(0.5f, 0f);
            tagRect.anchoredPosition = style.LethalTagOffset;
            tagRect.sizeDelta = new Vector2(style.BarSize.x, style.LethalTagFontSize * 1.4f);
            StyleText(_lethalTag, style.Font, style.LethalTagFontSize, style.LethalColor);

            ApplyColours();
            if (_previewing)
            {
                ApplyPreview();
            }
        }

        public void SetVisible(bool visible)
        {
            _root.gameObject.SetActive(visible);
        }

        public void SetValues(int health, int maxHealth, int block)
        {
            _health = health;
            _maxHealth = maxHealth;
            _block = block;
            float ratio = maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;
            _value.SetText("{0}/{1}", (float)health, (float)maxHealth);

            bool gainedBlock = _hasValues && block > _lastBlock;
            _lastBlock = block;
            _shielded = block > 0;
            _badge.gameObject.SetActive(_shielded);
            if (_shielded)
            {
                _badgeValue.SetText("{0}", (float)block);
            }

            ApplyColours();

            if (_hasValues && ratio < _fillRatio && _style != null)
            {
                SetFill(ratio);
                DrainTrailTo(ratio);
            }
            else if (!_hasValues || ratio != _fillRatio)
            {
                _trailTween?.Kill();
                SetFill(ratio);
                SetTrail(ratio);
            }

            if (gainedBlock)
            {
                PunchBadge();
            }

            _hasValues = true;
            if (_previewing)
            {
                ApplyPreview();
            }
        }

        public void ShowPreview(int endHealth, int endBlock, bool lethal, string lethalLabel)
        {
            _previewHealth = endHealth;
            _previewBlock = endBlock;
            _previewLethal = lethal;
            _lethalTag.text = lethalLabel ?? string.Empty;
            _previewing = true;
            ApplyPreview();

            if (_pulseTween == null && _style != null)
            {
                _pulse = 0f;
                _pulseTween = DOTween.To(_getPulse, _setPulse, 1f, _style.PreviewPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        public void ClearPreview()
        {
            if (!_previewing)
            {
                return;
            }

            _previewing = false;
            StopPulse();
            _ghost.enabled = false;
            _lethalTag.gameObject.SetActive(false);
            _value.SetText("{0}/{1}", (float)_health, (float)_maxHealth);
            _badge.gameObject.SetActive(_shielded);
            if (_shielded)
            {
                _badgeValue.SetText("{0}", (float)_block);
            }

            ApplyColours();
            if (_style != null)
            {
                _value.color = _style.BarTextColor;
                _badgeValue.color = _style.KavachTextColor;
            }
        }

        public void Dispose()
        {
            _trailTween?.Kill();
            _badgePunch?.Kill();
            StopPulse();
        }

        private void ApplyPreview()
        {
            if (_style == null)
            {
                return;
            }

            float afterRatio = _maxHealth > 0 ? Mathf.Clamp01((float)_previewHealth / _maxHealth) : 0f;
            bool losesHealth = afterRatio < _fillRatio;
            _ghost.enabled = losesHealth;
            if (losesHealth)
            {
                RectTransform ghost = _ghost.rectTransform;
                ghost.anchorMin = new Vector2(afterRatio, 0f);
                ghost.anchorMax = new Vector2(_fillRatio, 1f);
            }

            _value.SetText("{0}/{1}", (float)_previewHealth, (float)_maxHealth);
            _value.color = _previewLethal ? _style.LethalColor
                : _previewHealth != _health ? _style.PreviewTextColor : _style.BarTextColor;

            bool showBadge = _previewBlock > 0 || _block > 0;
            _badge.gameObject.SetActive(showBadge);
            if (showBadge)
            {
                _badgeValue.SetText("{0}", (float)_previewBlock);
                _badgeValue.color = _previewBlock != _block ? _style.PreviewKavachTextColor : _style.KavachTextColor;
            }

            _lethalTag.gameObject.SetActive(_previewLethal);
            SetPulse(_pulse);
        }

        private void SetPulse(float pulse)
        {
            _pulse = pulse;
            if (!_previewing || _style == null)
            {
                return;
            }

            Color loss = _previewLethal ? _style.LethalColor : _style.PreviewLossColor;
            float dim = loss.a * _style.PreviewLossDimAlpha;
            loss.a = Mathf.Lerp(dim, loss.a, pulse);
            _ghost.color = loss;

            Color border = _shielded ? _style.KavachBorderColor : _style.BarBorderColor;
            _border.color = _previewLethal ? Color.Lerp(border, _style.LethalColor, pulse) : border;

            if (_previewLethal)
            {
                Color tag = _style.LethalColor;
                tag.a = Mathf.Lerp(0.55f, 1f, pulse);
                _lethalTag.color = tag;
                float scale = 1f + 0.08f * pulse;
                _lethalTag.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void StopPulse()
        {
            if (_pulseTween == null)
            {
                return;
            }

            Tween pulse = _pulseTween;
            _pulseTween = null;
            pulse.Kill();
        }

        private void DrainTrailTo(float ratio)
        {
            _trailTween?.Kill();
            _trailTween = DOTween.To(_getTrail, _setTrail, ratio, _style.TrailDuration)
                .SetDelay(_style.TrailDelay)
                .SetEase(Ease.OutQuad)
                .OnKill(_clearTrailTween);
        }

        private void PunchBadge()
        {
            if (_style == null || _style.KavachPunchScale <= 0f)
            {
                return;
            }

            Transform badge = _badge.transform;
            _badgePunch?.Kill(true);
            badge.localScale = Vector3.one;
            _badgePunch = badge.DOPunchScale(Vector3.one * _style.KavachPunchScale, _style.KavachPunchDuration, PunchVibrato, PunchElasticity)
                .OnKill(_clearBadgePunch);
        }

        private void ApplyColours()
        {
            if (_style == null)
            {
                return;
            }

            _fill.color = _shielded ? _style.KavachFillColor : _style.BarFillColor;
            _border.color = _shielded ? _style.KavachBorderColor : _style.BarBorderColor;
        }

        private void SetFill(float ratio)
        {
            _fillRatio = ratio;
            _fill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        }

        private void SetTrail(float ratio)
        {
            _trailRatio = ratio;
            _trail.rectTransform.anchorMax = new Vector2(ratio, 1f);
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

        internal static Vector3 UniformWorldScale(Transform parent, float worldScale)
        {
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(Divide(worldScale, parentScale.x), Divide(worldScale, parentScale.y), Divide(worldScale, parentScale.z));
        }

        private static float Divide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

        private static void Stretch(RectTransform rect, float outset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(-outset, -outset);
            rect.offsetMax = new Vector2(outset, outset);
        }

        private static Image CreateImage(string imageName, Transform parent)
        {
            var imageObject = new GameObject(imageName, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
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
