// The damage a fighter is about to take, marked on its own health bar: a striped segment over the end of the fill for
// the health the coming attacks will remove, and a small tag hanging under that segment ("-11", with a pointer up to
// it). Both turn the lethal colours when the loss would kill. While the player considers a card the number takes the
// preview colour, so a preview never reads as the live state (lethal still wins). The stripes breathe gently while
// shown and the tag punches when the loss changes, so the effect of a Kavach card reads at a glance. Composed into
// CombatantHealthBar, which creates it right after its fill (so the stripes draw over the fill and under the preview
// ghost and the number) and calls Refresh whenever its health changes; the tag is moved to the end of the bar so it
// draws over the status chips. Every visual value comes from CombatantViewStyleSO. The breathing and the punch are
// DOTween core tweens on unscaled time, run only while the mark is shown and killed on Clear and Dispose.
using System;
using DG.Tweening;
using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatScene
{
    internal sealed class HealthBarThreat : IDisposable
    {
        private const int PunchVibrato = 6;
        private const float PunchElasticity = 0.5f;

        private readonly Image _stripes;
        private readonly Image _tag;
        private readonly Image _pointer;
        private readonly TextMeshProUGUI _text;
        private readonly DOGetter<float> _getBreath;
        private readonly DOSetter<float> _setBreath;
        private readonly TweenCallback _clearPunch;
        private CombatantViewStyleSO _style;
        private Tween _breathTween;
        private Tween _punch;
        private float _breath = 1f;
        private int _loss;
        private bool _lethal;
        private bool _previewing;
        private bool _shown;
        private int _health;
        private int _maxHealth;

        private HealthBarThreat(Image stripes, Image tag, Image pointer, TextMeshProUGUI text)
        {
            _stripes = stripes;
            _tag = tag;
            _pointer = pointer;
            _text = text;
            _getBreath = () => _breath;
            _setBreath = SetBreath;
            _clearPunch = () => _punch = null;
        }

        public static HealthBarThreat Create(RectTransform bar)
        {
            Image stripes = CreateImage("ThreatStripes", bar);
            stripes.enabled = false;

            Image tag = CreateImage("ThreatTag", bar);
            Image pointer = CreateImage("Pointer", tag.transform);
            var textObject = new GameObject("Value", typeof(RectTransform));
            textObject.transform.SetParent(tag.transform, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            tag.gameObject.SetActive(false);

            return new HealthBarThreat(stripes, tag, pointer, text);
        }

        public void ApplyStyle(CombatantViewStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            Sprite stripes = style.ThreatStripeSprite;
            _stripes.sprite = stripes;
            _stripes.type = stripes != null ? Image.Type.Tiled : Image.Type.Simple;

            Sprite tag = style.ThreatTagSprite;
            _tag.sprite = tag;
            _tag.type = tag != null && tag.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            _pointer.sprite = style.ThreatPointerSprite;
            _pointer.enabled = style.ThreatPointerSprite != null;

            if (style.Font != null)
            {
                _text.font = style.Font;
            }

            _text.fontSize = style.ThreatTagFontSize;
            _tag.transform.SetAsLastSibling();
            Refresh(_health, _maxHealth);
        }

        public void Show(int loss, bool lethal, bool previewing)
        {
            bool changed = _shown && loss != _loss;
            _loss = loss;
            _lethal = lethal;
            _previewing = previewing;
            _shown = loss > 0;
            Refresh(_health, _maxHealth);

            if (_shown)
            {
                StartBreathing();
                if (changed)
                {
                    Punch();
                }
            }
            else
            {
                StopTweens();
            }
        }

        public void Clear()
        {
            _shown = false;
            _loss = 0;
            StopTweens();
            Refresh(_health, _maxHealth);
        }

        // Places the stripes and the tag for the current health. Runs when health, the loss or the style changes.
        public void Refresh(int health, int maxHealth)
        {
            _health = health;
            _maxHealth = maxHealth;
            bool visible = _shown && _style != null && maxHealth > 0 && health > 0;
            _stripes.enabled = visible;
            _tag.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            float end = Mathf.Clamp01((float)health / maxHealth);
            float start = Mathf.Clamp01((float)(health - _loss) / maxHealth);
            RectTransform stripes = _stripes.rectTransform;
            stripes.anchorMin = new Vector2(start, 0f);
            stripes.anchorMax = new Vector2(end, 1f);
            stripes.offsetMin = Vector2.zero;
            stripes.offsetMax = Vector2.zero;

            _text.SetText("-{0}", (float)_loss);
            _text.color = _lethal ? _style.ThreatLethalTextColor
                : _previewing ? _style.PreviewTextColor : _style.ThreatTextColor;
            Color tagColour = _lethal ? _style.ThreatLethalTagColor : _style.ThreatTagColor;
            _tag.color = tagColour;
            _pointer.color = tagColour;

            _text.ForceMeshUpdate();
            float textWidth = _text.GetPreferredValues().x + _style.ThreatTagPadding * 2f;
            float width = Mathf.Max(textWidth, _style.ThreatTagHeight);
            RectTransform tag = _tag.rectTransform;
            float middle = (start + end) * 0.5f;
            tag.anchorMin = new Vector2(middle, 0f);
            tag.anchorMax = new Vector2(middle, 0f);
            tag.pivot = new Vector2(0.5f, 1f);
            tag.anchoredPosition = _style.ThreatTagOffset;
            tag.sizeDelta = new Vector2(width, _style.ThreatTagHeight);

            RectTransform pointer = _pointer.rectTransform;
            pointer.anchorMin = new Vector2(0.5f, 1f);
            pointer.anchorMax = new Vector2(0.5f, 1f);
            pointer.pivot = new Vector2(0.5f, 0f);
            pointer.anchoredPosition = Vector2.zero;
            pointer.sizeDelta = _style.ThreatPointerSize;

            SetBreath(_breath);
        }

        public void Dispose()
        {
            StopTweens();
        }

        private void StartBreathing()
        {
            if (_breathTween != null || _style == null || _style.ThreatPulseDuration <= 0f)
            {
                return;
            }

            _breath = 1f;
            _breathTween = DOTween.To(_getBreath, _setBreath, 0f, _style.ThreatPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void SetBreath(float breath)
        {
            _breath = breath;
            if (_style == null)
            {
                return;
            }

            Color stripes = _lethal ? _style.ThreatLethalStripeColor : _style.ThreatStripeColor;
            stripes.a *= Mathf.Lerp(_style.ThreatStripeDimAlpha, 1f, breath);
            _stripes.color = stripes;
        }

        private void Punch()
        {
            if (_style == null || _style.ThreatPunchScale <= 0f)
            {
                return;
            }

            Transform tag = _tag.transform;
            _punch?.Kill(true);
            tag.localScale = Vector3.one;
            _punch = tag.DOPunchScale(Vector3.one * _style.ThreatPunchScale, _style.ThreatPunchDuration, PunchVibrato,
                    PunchElasticity)
                .SetUpdate(true)
                .OnKill(_clearPunch);
        }

        private void StopTweens()
        {
            _punch?.Kill(true);
            if (_breathTween != null)
            {
                Tween breath = _breathTween;
                _breathTween = null;
                breath.Kill();
            }

            _breath = 1f;
        }

        private static Image CreateImage(string imageName, Transform parent)
        {
            var imageObject = new GameObject(imageName, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}
