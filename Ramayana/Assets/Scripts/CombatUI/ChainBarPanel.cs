// The combo chain bead string at the top centre of the screen: the running chain's name in its colour, one bead per
// ladder step threaded on a thin line (lit in the chain's colour for each card played in a row, the next bead half
// lit, the finisher bead larger, a pale ring behind the bead just played), and the current multiplier under the
// string (x1, x1.5, x2...). It fades in when a chain starts and out when the chain breaks or resets. Each card that
// builds the chain punches its bead and the multiplier; a step with a multiplier also flashes a halo in the chain's
// colour behind that bead, bigger and brighter for bigger multipliers. The finisher pops a Finisher tag under the
// multiplier, holds the full string for a moment, then fades it out. A chain of the style's minimum length or more
// that another card breaks raises a short "Broken at N in a row" message that drifts and fades. It listens only to
// the ComboChain's events and never changes combat state. It lives on its own child canvas, so its fades and punches
// never rebuild the static HUD panels. Tweens are DOTween core on unscaled time, killed on Detach and Dispose. Every
// visual value comes from ChainBarStyleSO and the Combat HUD style; text comes from the chain kind's name key
// (Combat table) and ChainKeys (UI table).
using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using DG.Tweening.Core;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class ChainBarPanel : IDisposable
    {
        private const float LineHeight = 1.25f;
        private const int PunchVibrato = 6;
        private const float PunchElasticity = 0.5f;
        private const int LabelCapacity = 16;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _backing;
        private readonly TextMeshProUGUI _name;
        private readonly RectTransform _row;
        private readonly Image _thread;
        private readonly Image _flash;
        private readonly Image _ring;
        private readonly TextMeshProUGUI _multiplier;
        private readonly TextMeshProUGUI _finisher;
        private readonly RectTransform _brokenRect;
        private readonly TextMeshProUGUI _broken;
        private readonly LocalizedText _nameText;
        private readonly LocalizedText _finisherText;
        private readonly LocalizedPatternText _brokenText;
        private readonly List<Bead> _beads = new List<Bead>(5);
        private readonly List<float> _beadX = new List<float>(5);
        private readonly List<float> _beadSizes = new List<float>(5);
        private readonly List<CardValue> _brokenValues = new List<CardValue>(1);
        private readonly StringBuilder _labelBuilder = new StringBuilder(LabelCapacity);
        private readonly LocalizedString _multiplierString = new LocalizedString(LocalizationTableNames.UI, ChainKeys.Multiplier);
        private readonly LocalizedString.ChangeHandler _multiplierChanged;
        private readonly Action<ChainKindSO, int, int> _advanced;
        private readonly Action<ChainKindSO, int> _brokenHandler;
        private readonly Action _changed;
        private readonly DOGetter<float> _getAlpha;
        private readonly DOSetter<float> _setAlpha;
        private readonly DOGetter<float> _getFlash;
        private readonly DOSetter<float> _setFlash;
        private readonly DOGetter<float> _getFinisher;
        private readonly DOSetter<float> _setFinisher;
        private readonly DOGetter<float> _getBroken;
        private readonly DOSetter<float> _setBroken;
        private readonly TweenCallback _hideRoot;
        private readonly TweenCallback _finisherDone;
        private readonly TweenCallback _hideBroken;
        private CombatHudStyleSO _hudStyle;
        private ChainBarStyleSO _style;
        private ComboChain _chain;
        private ChainKindSO _shownKind;
        private string _multiplierPattern;
        private int _shownCount;
        private bool _allowed;
        private bool _shown;
        private bool _holdingFinisher;
        private float _flashAlpha;
        private float _finisherAlpha;
        private float _brokenProgress;
        private Tween _fade;
        private Tween _multiplierPunch;
        private Tween _flashTween;
        private Sequence _finisherSequence;
        private Sequence _brokenSequence;

        private ChainBarPanel(RectTransform root, CanvasGroup group, Image backing, TextMeshProUGUI name,
            RectTransform row, Image thread, Image flash, Image ring, TextMeshProUGUI multiplier,
            TextMeshProUGUI finisher, TextMeshProUGUI broken)
        {
            _root = root;
            _group = group;
            _backing = backing;
            _name = name;
            _row = row;
            _thread = thread;
            _flash = flash;
            _ring = ring;
            _multiplier = multiplier;
            _finisher = finisher;
            _broken = broken;
            _brokenRect = broken.rectTransform;
            _nameText = new LocalizedText(name);
            _finisherText = new LocalizedText(finisher);
            _finisherText.Set(LocalizationTableNames.UI, ChainKeys.Finisher);
            _brokenText = new LocalizedPatternText(broken);
            _brokenText.Set(LocalizationTableNames.UI, ChainKeys.Broken);

            _advanced = HandleAdvanced;
            _brokenHandler = HandleBroken;
            _changed = HandleChanged;
            _getAlpha = () => _group.alpha;
            _setAlpha = value => _group.alpha = value;
            _getFlash = () => _flashAlpha;
            _setFlash = SetFlashAlpha;
            _getFinisher = () => _finisherAlpha;
            _setFinisher = SetFinisherAlpha;
            _getBroken = () => _brokenProgress;
            _setBroken = SetBrokenProgress;
            _hideRoot = HandleHideComplete;
            _finisherDone = HandleFinisherDone;
            _hideBroken = () => _brokenRect.gameObject.SetActive(false);
            _multiplierChanged = HandleMultiplierChanged;
            _multiplierString.StringChanged += _multiplierChanged;

            _group.alpha = 0f;
            _root.gameObject.SetActive(false);
            _ring.gameObject.SetActive(false);
            SetFlashAlpha(0f);
            SetFinisherAlpha(0f);
            _brokenRect.gameObject.SetActive(false);
        }

        public static ChainBarPanel Create(Transform parent)
        {
            RectTransform root = UIBuildUtility.CreateRect("ChainBar", parent);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            Image backing = UIBuildUtility.CreateImage("Backing", root, false);
            UIBuildUtility.Stretch(backing.rectTransform);
            TextMeshProUGUI name = UIBuildUtility.CreateText("Name", root, TextAlignmentOptions.Center);
            RectTransform row = UIBuildUtility.CreateRect("Beads", root);
            Image thread = UIBuildUtility.CreateImage("Thread", row, false);
            Image flash = UIBuildUtility.CreateImage("Flash", row, false);
            Image ring = UIBuildUtility.CreateImage("Ring", row, false);
            TextMeshProUGUI multiplier = UIBuildUtility.CreateText("Multiplier", root, TextAlignmentOptions.Center);
            TextMeshProUGUI finisher = UIBuildUtility.CreateText("Finisher", root, TextAlignmentOptions.Top);
            TextMeshProUGUI broken = UIBuildUtility.CreateText("Broken", parent, TextAlignmentOptions.Center);

            return new ChainBarPanel(root, group, backing, name, row, thread, flash, ring, multiplier, finisher, broken);
        }

        public void ApplyStyle(CombatHudStyleSO hudStyle, ChainBarStyleSO style)
        {
            _hudStyle = hudStyle;
            _style = style;
            if (_hudStyle == null || _style == null)
            {
                HideNow();
                return;
            }

            UIBuildUtility.StyleImage(_backing, style.BackingSprite, style.BackingColor);
            _backing.enabled = style.BackingColor.a > 0f;
            UIBuildUtility.StyleImage(_thread, null, style.ThreadColor);
            StyleDisc(_ring, style.BeadSprite, style.CurrentRingColor);
            StyleDisc(_flash, style.HaloSprite, Color.clear);
            SetFlashAlpha(_flashAlpha);
            UIBuildUtility.StyleText(_name, hudStyle.Font, style.NameFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_multiplier, hudStyle.Font, style.MultiplierFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_finisher, hudStyle.Font, style.FinisherFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_broken, hudStyle.Font, style.BrokenFontSize, style.BrokenColor);
            UIBuildUtility.Place(_brokenRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), style.BrokenTopOffset,
                style.BrokenSize);
            SetBrokenProgress(_brokenProgress);

            for (int i = 0; i < _beads.Count; i++)
            {
                _beads[i].ApplyStyle(style);
            }

            Render();
        }

        public void Attach(ComboChain chain)
        {
            Detach();
            if (chain == null)
            {
                return;
            }

            _chain = chain;
            _chain.Advanced += _advanced;
            _chain.Broken += _brokenHandler;
            _chain.Changed += _changed;
            HandleChanged();
        }

        public void Detach()
        {
            if (_chain != null)
            {
                _chain.Advanced -= _advanced;
                _chain.Broken -= _brokenHandler;
                _chain.Changed -= _changed;
                _chain = null;
            }

            HideNow();
        }

        public void SetActive(bool allowed)
        {
            if (_allowed == allowed)
            {
                return;
            }

            _allowed = allowed;
            if (!allowed)
            {
                HideNow();
                return;
            }

            HandleChanged();
        }

        public void Dispose()
        {
            Detach();
            _multiplierString.StringChanged -= _multiplierChanged;
            for (int i = 0; i < _beads.Count; i++)
            {
                _beads[i].Dispose();
            }

            _nameText.Clear();
            _finisherText.Clear();
            _brokenText.Clear();
        }

        private void HandleAdvanced(ChainKindSO kind, int step, int percent)
        {
            if (!CanShow() || kind == null)
            {
                return;
            }

            KillFinisher();
            _shownKind = kind;
            _shownCount = step;
            _holdingFinisher = kind.IsFinisher(step);
            Render();
            Show();
            PlayTier(kind, step, percent);
            if (_holdingFinisher)
            {
                PlayFinisher(kind);
            }
        }

        private void HandleBroken(ChainKindSO kind, int length)
        {
            if (!CanShow() || length < _style.BrokenMinLength)
            {
                return;
            }

            ShowBrokenMessage(length);
        }

        private void HandleChanged()
        {
            if (_chain == null || !CanShow())
            {
                return;
            }

            if (_chain.Kind != null && _chain.Count > 0)
            {
                if (_chain.Kind != _shownKind || _chain.Count != _shownCount)
                {
                    KillFinisher();
                    _shownKind = _chain.Kind;
                    _shownCount = _chain.Count;
                    Render();
                }

                Show();
                return;
            }

            if (!_holdingFinisher)
            {
                Hide();
            }
        }

        private void HandleMultiplierChanged(string value)
        {
            _multiplierPattern = value;
            Render();
        }

        private bool CanShow()
        {
            return _allowed && _style != null && _hudStyle != null;
        }

        private void Render()
        {
            if (_style == null || _hudStyle == null || _shownKind == null)
            {
                return;
            }

            ChainKindSO kind = _shownKind;
            _name.color = kind.Color;
            _nameText.Set(LocalizationTableNames.Combat, kind.NameKey);
            ChainMultiplierFormat.Format(_multiplierPattern, kind.GetPercent(_shownCount), _labelBuilder);
            _multiplier.SetText(_labelBuilder);

            EnsureBeads(kind.Length);
            Layout(kind.Length);

            for (int i = 0; i < _beads.Count; i++)
            {
                bool visible = i < kind.Length;
                _beads[i].SetVisible(visible);
                if (!visible)
                {
                    continue;
                }

                if (i < _shownCount)
                {
                    _beads[i].SetColor(kind.Color);
                }
                else if (i == _shownCount)
                {
                    Color next = kind.Color;
                    next.a *= _style.NextBeadAlpha;
                    _beads[i].SetColor(next);
                }
                else
                {
                    _beads[i].SetColor(_style.BeadEmptyColor);
                }
            }

            int current = Mathf.Min(_shownCount, kind.Length) - 1;
            bool hasCurrent = current >= 0 && current < _beadX.Count;
            _ring.gameObject.SetActive(hasCurrent);
            if (hasCurrent)
            {
                PlaceAtBead(_ring.rectTransform, current, _beadSizes[current] * _style.CurrentRingScale);
            }
        }

        private void EnsureBeads(int count)
        {
            while (_beads.Count < count)
            {
                var bead = new Bead(_row, _beads.Count);
                if (_style != null)
                {
                    bead.ApplyStyle(_style);
                }

                _beads.Add(bead);
            }
        }

        private void Layout(int beadCount)
        {
            ChainBarStyleSO style = _style;
            float padding = style.BackingPadding;
            float width = style.Width;
            float nameHeight = style.NameFontSize * LineHeight;
            float maxBead = style.BeadSize * style.FinisherBeadScale;
            float multiplierHeight = style.MultiplierFontSize * LineHeight;
            float height = padding * 2f + nameHeight + style.NameGap + maxBead + style.MultiplierGap + multiplierHeight;

            UIBuildUtility.Place(_root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), style.TopOffset,
                new Vector2(width + padding * 2f, height));

            float y = -padding;
            UIBuildUtility.Place(_name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y),
                new Vector2(width, nameHeight));
            y -= nameHeight + style.NameGap;
            UIBuildUtility.Place(_row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y),
                new Vector2(width, maxBead));
            y -= maxBead + style.MultiplierGap;
            UIBuildUtility.Place(_multiplier.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, y), new Vector2(width, multiplierHeight));
            UIBuildUtility.Place(_finisher.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, -style.FinisherGap), new Vector2(width, style.FinisherFontSize * LineHeight));

            _beadX.Clear();
            _beadSizes.Clear();
            float usable = Mathf.Max(0f, width - maxBead);
            for (int i = 0; i < beadCount; i++)
            {
                float x = maxBead * 0.5f + (beadCount > 1 ? usable * i / (beadCount - 1) : usable * 0.5f);
                float size = i == beadCount - 1 ? maxBead : style.BeadSize;
                _beadX.Add(x);
                _beadSizes.Add(size);
                if (i < _beads.Count)
                {
                    PlaceAtBead(_beads[i].Rect, i, size);
                }
            }

            if (beadCount > 0)
            {
                float start = _beadX[0];
                float length = _beadX[beadCount - 1] - start;
                UIBuildUtility.Place(_thread.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(start, 0f), new Vector2(length, style.ThreadThickness));
            }
        }

        private void PlaceAtBead(RectTransform rect, int index, float size)
        {
            UIBuildUtility.Place(rect, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(_beadX[index], 0f),
                new Vector2(size, size));
        }

        private void Show()
        {
            if (_shown)
            {
                return;
            }

            _shown = true;
            KillTween(ref _fade);
            _root.gameObject.SetActive(true);
            _fade = DOTween.To(_getAlpha, _setAlpha, 1f, _style.FadeIn).SetUpdate(true);
        }

        private void Hide()
        {
            if (!_shown)
            {
                return;
            }

            _shown = false;
            KillTween(ref _fade);
            _fade = DOTween.To(_getAlpha, _setAlpha, 0f, _style.FadeOut).SetUpdate(true).OnComplete(_hideRoot);
        }

        private void HideNow()
        {
            KillTween(ref _fade);
            KillTween(ref _multiplierPunch);
            KillTween(ref _flashTween);
            KillFinisher();
            KillBroken();
            for (int i = 0; i < _beads.Count; i++)
            {
                _beads[i].StopPunch();
            }

            _shown = false;
            _shownKind = null;
            _shownCount = 0;
            _group.alpha = 0f;
            _multiplier.rectTransform.localScale = Vector3.one;
            SetFlashAlpha(0f);
            _root.gameObject.SetActive(false);
        }

        private void HandleHideComplete()
        {
            _fade = null;
            if (_shown)
            {
                return;
            }

            _root.gameObject.SetActive(false);
            _shownKind = null;
            _shownCount = 0;
        }

        private void PlayTier(ChainKindSO kind, int step, int percent)
        {
            float strength = Mathf.Max(0, percent - ChainScale.NeutralPercent) / (float)ChainScale.NeutralPercent;
            int bead = step - 1;
            if (bead >= 0 && bead < _beads.Count)
            {
                _beads[bead].Punch(_style.BeadPunchBase + _style.BeadPunchPerMultiplier * strength, _style.PunchDuration);
            }

            KillTween(ref _multiplierPunch);
            Transform multiplier = _multiplier.rectTransform;
            multiplier.localScale = Vector3.one;
            float punch = _style.MultiplierPunchBase + _style.MultiplierPunchPerMultiplier * strength;
            if (punch > 0f)
            {
                _multiplierPunch = multiplier.DOPunchScale(Vector3.one * punch, _style.PunchDuration, PunchVibrato,
                    PunchElasticity).SetUpdate(true);
            }

            KillTween(ref _flashTween);
            float flash = Mathf.Min(_style.FlashMaxAlpha, _style.FlashAlphaPerMultiplier * strength);
            if (flash <= 0f || bead < 0 || bead >= _beadX.Count)
            {
                SetFlashAlpha(0f);
                return;
            }

            float size = _beadSizes[bead] * (_style.HaloScale + _style.HaloScalePerMultiplier * strength);
            PlaceAtBead(_flash.rectTransform, bead, size);
            _flash.color = new Color(kind.Color.r, kind.Color.g, kind.Color.b, 0f);
            SetFlashAlpha(flash);
            _flashTween = DOTween.To(_getFlash, _setFlash, 0f, _style.FlashDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private void PlayFinisher(ChainKindSO kind)
        {
            KillFinisher();
            _holdingFinisher = true;
            _finisher.color = new Color(kind.Color.r, kind.Color.g, kind.Color.b, 0f);
            SetFinisherAlpha(0f);
            Transform label = _finisher.rectTransform;
            label.localScale = Vector3.one * _style.FinisherStartScale;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(_getFinisher, _setFinisher, 1f, _style.FinisherPopDuration));
            sequence.Join(label.DOScale(1f, _style.FinisherPopDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(_style.FinisherHold);
            sequence.AppendCallback(_finisherDone);
            sequence.Append(DOTween.To(_getFinisher, _setFinisher, 0f, _style.FadeOut));
            sequence.SetUpdate(true);
            _finisherSequence = sequence;
        }

        private void HandleFinisherDone()
        {
            _holdingFinisher = false;
            if (_chain == null || _chain.Kind == null || _chain.Count == 0)
            {
                Hide();
            }
        }

        private void ShowBrokenMessage(int length)
        {
            KillBroken();
            _brokenValues.Clear();
            _brokenValues.Add(CardValue.Fixed(length));
            _brokenText.SetValues(_brokenValues);
            _brokenRect.gameObject.SetActive(true);
            _brokenRect.SetAsLastSibling();
            SetBrokenProgress(0f);

            float total = _style.BrokenFadeIn + _style.BrokenHold + _style.BrokenFadeOut;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(_getBroken, _setBroken, 1f, total).SetEase(Ease.Linear));
            sequence.SetUpdate(true);
            sequence.OnComplete(_hideBroken);
            _brokenSequence = sequence;
        }

        // Progress 0 to 1 over the whole message: it fades in, holds, fades out, and drifts by BrokenRise the whole time.
        private void SetBrokenProgress(float progress)
        {
            _brokenProgress = progress;
            if (_style == null)
            {
                return;
            }

            float time = progress * (_style.BrokenFadeIn + _style.BrokenHold + _style.BrokenFadeOut);
            float alpha;
            if (time < _style.BrokenFadeIn)
            {
                alpha = time / _style.BrokenFadeIn;
            }
            else if (time < _style.BrokenFadeIn + _style.BrokenHold)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 1f - (time - _style.BrokenFadeIn - _style.BrokenHold) / _style.BrokenFadeOut;
            }

            _broken.alpha = Mathf.Clamp01(alpha);
            _brokenRect.anchoredPosition = _style.BrokenTopOffset + new Vector2(0f, _style.BrokenRise * progress);
        }

        private void SetFlashAlpha(float alpha)
        {
            _flashAlpha = alpha;
            Color color = _flash.color;
            color.a = alpha;
            _flash.color = color;
            _flash.enabled = alpha > 0f;
        }

        private void SetFinisherAlpha(float alpha)
        {
            _finisherAlpha = alpha;
            _finisher.alpha = alpha;
        }

        private void KillFinisher()
        {
            _holdingFinisher = false;
            if (_finisherSequence != null)
            {
                Sequence sequence = _finisherSequence;
                _finisherSequence = null;
                sequence.Kill();
            }

            SetFinisherAlpha(0f);
        }

        private void KillBroken()
        {
            if (_brokenSequence != null)
            {
                Sequence sequence = _brokenSequence;
                _brokenSequence = null;
                sequence.Kill();
            }

            _brokenRect.gameObject.SetActive(false);
        }

        private static void StyleDisc(Image image, Sprite sprite, Color color)
        {
            UIBuildUtility.StyleImage(image, sprite, color);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        private static void KillTween(ref Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            Tween active = tween;
            tween = null;
            active.Kill();
        }

        private sealed class Bead : IDisposable
        {
            private readonly Image _image;
            private Tween _punch;

            public Bead(Transform parent, int index)
            {
                _image = UIBuildUtility.CreateImage("Bead" + (index + 1), parent, false);
            }

            public RectTransform Rect => _image.rectTransform;

            public void ApplyStyle(ChainBarStyleSO style)
            {
                StyleDisc(_image, style.BeadSprite, _image.color);
            }

            public void SetVisible(bool visible)
            {
                if (_image.gameObject.activeSelf != visible)
                {
                    _image.gameObject.SetActive(visible);
                }
            }

            public void SetColor(Color color)
            {
                _image.color = color;
            }

            public void Punch(float scale, float duration)
            {
                StopPunch();
                if (scale <= 0f)
                {
                    return;
                }

                _punch = _image.rectTransform.DOPunchScale(Vector3.one * scale, duration, PunchVibrato, PunchElasticity)
                    .SetUpdate(true);
            }

            public void StopPunch()
            {
                if (_punch != null)
                {
                    Tween punch = _punch;
                    _punch = null;
                    punch.Kill();
                }

                _image.rectTransform.localScale = Vector3.one;
            }

            public void Dispose()
            {
                StopPunch();
            }
        }
    }
}
