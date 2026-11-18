// The combo chain bar above the hand: a panel with the running chain's icon, its name in the chain's colour and the
// count of its cards played in a row ("Bow Chain 3"), its hint line, and one pip per ladder step with the step's
// multiplier under it (x1, x1, x1.5, x2, x3). Lit pips show the cards played, the next pip is half lit, and the last
// (the finisher) is drawn larger. It fades in when a chain starts and out when the chain breaks or resets. Each card
// that builds the chain punches the panel and lights its pip; a step with a multiplier also flashes the panel in the
// chain's colour, harder for bigger multipliers. The finisher pops a Finisher tag, holds the full bar for a moment,
// then fades it out. A chain of the style's minimum length or more that another card breaks raises a short "Broken
// at N in a row" message that rises and fades. It listens only to the ComboChain's events and never changes combat
// state. It lives on its own child canvas, so its fades and punches never rebuild the static HUD panels. Tweens are
// DOTween core on unscaled time, killed on Detach and Dispose. Every visual value comes from ChainBarStyleSO and the
// Combat HUD style; text comes from the chain kind's keys (Combat table) and ChainKeys (UI table).
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
        private readonly RectTransform _panel;
        private readonly Image _background;
        private readonly Image _flash;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _count;
        private readonly TextMeshProUGUI _hint;
        private readonly RectTransform _pipRow;
        private readonly TextMeshProUGUI _finisher;
        private readonly RectTransform _brokenRect;
        private readonly TextMeshProUGUI _broken;
        private readonly LocalizedText _nameText;
        private readonly LocalizedText _hintText;
        private readonly LocalizedText _finisherText;
        private readonly LocalizedPatternText _brokenText;
        private readonly List<Pip> _pips = new List<Pip>(5);
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
        private ChainKindSO _labelledKind;
        private string _multiplierPattern;
        private int _shownCount;
        private bool _allowed;
        private bool _shown;
        private bool _holdingFinisher;
        private float _flashAlpha;
        private float _finisherAlpha;
        private float _brokenProgress;
        private Tween _fade;
        private Tween _punch;
        private Tween _flashTween;
        private Sequence _finisherSequence;
        private Sequence _brokenSequence;

        private ChainBarPanel(RectTransform root, CanvasGroup group, RectTransform panel, Image background, Image flash,
            Image icon, TextMeshProUGUI name, TextMeshProUGUI count, TextMeshProUGUI hint, RectTransform pipRow,
            TextMeshProUGUI finisher, RectTransform brokenRect, TextMeshProUGUI broken)
        {
            _root = root;
            _group = group;
            _panel = panel;
            _background = background;
            _flash = flash;
            _icon = icon;
            _name = name;
            _count = count;
            _hint = hint;
            _pipRow = pipRow;
            _finisher = finisher;
            _brokenRect = brokenRect;
            _broken = broken;
            _nameText = new LocalizedText(name);
            _hintText = new LocalizedText(hint);
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

            Image background = UIBuildUtility.CreateImage("Panel", root, false);
            RectTransform panel = background.rectTransform;
            UIBuildUtility.Stretch(panel);
            Image flash = UIBuildUtility.CreateImage("Flash", panel, false);
            UIBuildUtility.Stretch(flash.rectTransform);
            Image icon = UIBuildUtility.CreateImage("Icon", panel, false);
            icon.preserveAspect = true;
            TextMeshProUGUI name = UIBuildUtility.CreateText("Name", panel, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI count = UIBuildUtility.CreateText("Count", panel, TextAlignmentOptions.MidlineRight);
            TextMeshProUGUI hint = UIBuildUtility.CreateText("Hint", panel, TextAlignmentOptions.MidlineLeft);
            hint.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform pipRow = UIBuildUtility.CreateRect("Pips", panel);

            TextMeshProUGUI finisher = UIBuildUtility.CreateText("Finisher", root, TextAlignmentOptions.Bottom);

            TextMeshProUGUI broken = UIBuildUtility.CreateText("Broken", parent, TextAlignmentOptions.Center);

            return new ChainBarPanel(root, group, panel, background, flash, icon, name, count, hint, pipRow, finisher,
                broken.rectTransform, broken);
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

            UIBuildUtility.StyleImage(_background, hudStyle.PanelSprite, hudStyle.PanelColor);
            UIBuildUtility.StyleImage(_flash, hudStyle.PanelSprite, Color.clear);
            SetFlashAlpha(_flashAlpha);
            UIBuildUtility.StyleText(_name, hudStyle.Font, style.TitleFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_count, hudStyle.Font, style.CountFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_hint, hudStyle.Font, style.HintFontSize, style.HintColor);
            UIBuildUtility.StyleText(_finisher, hudStyle.Font, style.FinisherFontSize, hudStyle.HudTextColor);
            UIBuildUtility.StyleText(_broken, hudStyle.Font, style.BrokenFontSize, style.BrokenColor);
            UIBuildUtility.Place(_brokenRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), style.BrokenOffset,
                style.BrokenSize);
            SetBrokenProgress(_brokenProgress);

            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].ApplyStyle(hudStyle, style);
            }

            _labelledKind = null;
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
            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].Dispose();
            }

            _nameText.Clear();
            _hintText.Clear();
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
            _labelledKind = null;
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
            bool hasIcon = kind.Icon != null;
            UIBuildUtility.StyleImage(_icon, hasIcon ? kind.Icon : _style.FallbackIconSprite, hasIcon ? Color.white : kind.Color);
            _icon.enabled = _icon.sprite != null;
            _name.color = kind.Color;
            _nameText.Set(LocalizationTableNames.Combat, kind.NameKey);
            _count.SetText("{0}", (float)_shownCount);
            _hintText.Set(LocalizationTableNames.Combat, kind.HintKey);

            EnsurePips(kind.Length);
            Layout(kind.Length);
            if (_labelledKind != kind)
            {
                _labelledKind = kind;
                for (int i = 0; i < kind.Length; i++)
                {
                    ChainMultiplierFormat.Format(_multiplierPattern, kind.GetPercent(i + 1), _labelBuilder);
                    _pips[i].SetLabel(_labelBuilder);
                }
            }

            for (int i = 0; i < _pips.Count; i++)
            {
                bool visible = i < kind.Length;
                _pips[i].SetVisible(visible);
                if (!visible)
                {
                    continue;
                }

                if (i < _shownCount)
                {
                    _pips[i].SetState(kind.Color, _style.PipLabelLitColor);
                }
                else if (i == _shownCount)
                {
                    Color next = kind.Color;
                    next.a *= _style.NextPipAlpha;
                    _pips[i].SetState(next, _style.PipLabelColor);
                }
                else
                {
                    _pips[i].SetState(_style.PipEmptyColor, _style.PipLabelColor);
                }
            }
        }

        private void EnsurePips(int count)
        {
            while (_pips.Count < count)
            {
                var pip = new Pip(_pipRow, _pips.Count);
                if (_hudStyle != null && _style != null)
                {
                    pip.ApplyStyle(_hudStyle, _style);
                }

                _pips.Add(pip);
            }
        }

        private void Layout(int pipCount)
        {
            ChainBarStyleSO style = _style;
            float padding = style.Padding;
            float width = style.Width;
            float inner = Mathf.Max(0f, width - padding * 2f);
            float titleRow = Mathf.Max(style.IconSize, Mathf.Max(style.TitleFontSize, style.CountFontSize) * LineHeight);
            float hintRow = style.HintFontSize * LineHeight;
            float maxPip = style.PipSize * style.FinisherPipScale;
            float labelRow = style.PipLabelFontSize * LineHeight;
            float pipRow = maxPip + style.PipLabelGap + labelRow;
            float height = padding * 2f + titleRow + style.RowSpacing + hintRow + style.RowSpacing + pipRow;

            UIBuildUtility.Place(_root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), style.Offset, new Vector2(width, height));

            float y = -padding;
            UIBuildUtility.Place(_icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(padding, y - titleRow * 0.5f), new Vector2(style.IconSize, style.IconSize));
            float nameX = padding + style.IconSize + style.IconGap;
            float nameWidth = Mathf.Max(0f, width - nameX - padding - style.CountWidth);
            UIBuildUtility.Place(_name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(nameX, y),
                new Vector2(nameWidth, titleRow));
            UIBuildUtility.Place(_count.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-padding, y),
                new Vector2(style.CountWidth, titleRow));

            y -= titleRow + style.RowSpacing;
            UIBuildUtility.Place(_hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding, y),
                new Vector2(inner, hintRow));

            y -= hintRow + style.RowSpacing;
            UIBuildUtility.Place(_pipRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding, y),
                new Vector2(inner, pipRow));

            float slot = pipCount > 0 ? inner / pipCount : inner;
            for (int i = 0; i < pipCount && i < _pips.Count; i++)
            {
                bool finisher = i == pipCount - 1;
                float size = finisher ? maxPip : style.PipSize;
                _pips[i].Place(slot * (i + 0.5f), maxPip, size, slot, labelRow, style.PipLabelGap);
            }

            UIBuildUtility.Place(_finisher.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), style.FinisherOffset,
                new Vector2(width, style.FinisherFontSize * LineHeight));
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
            KillTween(ref _punch);
            KillTween(ref _flashTween);
            KillFinisher();
            KillBroken();
            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].StopPunch();
            }

            _shown = false;
            _shownKind = null;
            _shownCount = 0;
            _group.alpha = 0f;
            _panel.localScale = Vector3.one;
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

            KillTween(ref _punch);
            _panel.localScale = Vector3.one;
            float punch = _style.PunchBase + _style.PunchPerMultiplier * strength;
            if (punch > 0f)
            {
                _punch = _panel.DOPunchScale(Vector3.one * punch, _style.PunchDuration, PunchVibrato, PunchElasticity)
                    .SetUpdate(true);
            }

            int pip = step - 1;
            if (pip >= 0 && pip < _pips.Count)
            {
                _pips[pip].Punch(_style.PipPunchScale, _style.PipPunchDuration);
            }

            KillTween(ref _flashTween);
            float flash = Mathf.Min(_style.FlashMaxAlpha, _style.FlashAlphaPerMultiplier * strength);
            if (flash <= 0f)
            {
                SetFlashAlpha(0f);
                return;
            }

            _flash.color = new Color(kind.Color.r, kind.Color.g, kind.Color.b, 0f);
            SetFlashAlpha(flash);
            _flashTween = DOTween.To(_getFlash, _setFlash, 0f, _style.FlashDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private void PlayFinisher(ChainKindSO kind)
        {
            KillFinisher();
            _finisher.color = new Color(kind.Color.r, kind.Color.g, kind.Color.b, 0f);
            SetFinisherAlpha(0f);
            Transform label = _finisher.transform;
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

        // Progress 0 to 1 over the whole message: it fades in, holds, fades out, and rises the whole time.
        private void SetBrokenProgress(float progress)
        {
            _brokenProgress = progress;
            if (_style == null)
            {
                return;
            }

            float total = Mathf.Max(0.0001f, _style.BrokenFadeIn + _style.BrokenHold + _style.BrokenFadeOut);
            float time = progress * total;
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
            _brokenRect.anchoredPosition = _style.BrokenOffset + new Vector2(0f, _style.BrokenRise * progress);
        }

        private void SetFlashAlpha(float alpha)
        {
            _flashAlpha = alpha;
            Color color = _flash.color;
            color.a = alpha;
            _flash.color = color;
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

        private sealed class Pip : IDisposable
        {
            private readonly Image _image;
            private readonly TextMeshProUGUI _label;
            private Tween _punch;

            public Pip(Transform parent, int index)
            {
                _image = UIBuildUtility.CreateImage("Pip" + (index + 1), parent, false);
                _label = UIBuildUtility.CreateText("Label", parent, TextAlignmentOptions.Top);
            }

            public void ApplyStyle(CombatHudStyleSO hudStyle, ChainBarStyleSO style)
            {
                _image.sprite = style.PipSprite;
                _image.type = Image.Type.Simple;
                _image.preserveAspect = true;
                UIBuildUtility.StyleText(_label, hudStyle.Font, style.PipLabelFontSize, style.PipLabelColor);
            }

            public void Place(float centerX, float rowPipHeight, float size, float slotWidth, float labelHeight, float gap)
            {
                UIBuildUtility.Place(_image.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(centerX, -rowPipHeight * 0.5f), new Vector2(size, size));
                UIBuildUtility.Place(_label.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(centerX, -(rowPipHeight + gap)), new Vector2(slotWidth, labelHeight));
            }

            public void SetVisible(bool visible)
            {
                if (_image.gameObject.activeSelf != visible)
                {
                    _image.gameObject.SetActive(visible);
                    _label.gameObject.SetActive(visible);
                }
            }

            public void SetLabel(StringBuilder text)
            {
                _label.SetText(text);
            }

            public void SetState(Color pipColor, Color labelColor)
            {
                _image.color = pipColor;
                _label.color = labelColor;
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
