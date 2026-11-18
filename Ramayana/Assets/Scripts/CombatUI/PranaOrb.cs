// One orb in the Prana row: an Image drawn by its own runtime copy of the Prana Orb material, so its fill, flash and
// preview are material values that animate without rebuilding the canvas. SetFilled drains or lights it (with a
// flash ring), after an optional delay so a row can stagger; SetPreview fades the spend or deficit rim in and out;
// FlashDeficit pulses the red rim once, for a rejected card. Tweens are DOTween core on unscaled time with cached
// delegates; each property keeps at most one tween, replaced when a new change arrives. Owned by PranaPanel.
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class PranaOrb
    {
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int SocketColorId = Shader.PropertyToID("_SocketColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int PreviewColorId = Shader.PropertyToID("_PreviewColor");
        private static readonly int DeficitColorId = Shader.PropertyToID("_DeficitColor");
        private static readonly int OrbRadiusId = Shader.PropertyToID("_OrbRadius");
        private static readonly int RimWidthId = Shader.PropertyToID("_RimWidth");
        private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int SwirlAmountId = Shader.PropertyToID("_SwirlAmount");
        private static readonly int SwirlSpeedId = Shader.PropertyToID("_SwirlSpeed");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int PreviewId = Shader.PropertyToID("_Preview");
        private static readonly int DeficitId = Shader.PropertyToID("_Deficit");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");

        private readonly Image _image;
        private readonly float _seed;
        private readonly DOGetter<float> _getFill;
        private readonly DOSetter<float> _setFill;
        private readonly DOGetter<float> _getFlash;
        private readonly DOSetter<float> _setFlash;
        private readonly DOGetter<float> _getPreview;
        private readonly DOSetter<float> _setPreview;
        private readonly DOGetter<float> _getDeficit;
        private readonly DOSetter<float> _setDeficit;
        private readonly TweenCallback _clearFill;
        private readonly TweenCallback _clearFlash;
        private readonly TweenCallback _clearPreview;
        private readonly TweenCallback _clearDeficit;
        private readonly TweenCallback _flashOn;
        private Material _source;
        private Material _material;
        private Tween _fillTween;
        private Tween _flashTween;
        private Tween _previewTween;
        private Tween _deficitTween;
        private float _fill = 1f;
        private float _flash;
        private float _preview;
        private float _deficit;
        private float _deficitTarget;
        private bool _filled = true;

        public PranaOrb(Transform parent, int index)
        {
            _image = UIBuildUtility.CreateImage("Orb" + index, parent, false);
            RectTransform rect = _image.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            _seed = index * 0.618034f % 1f;
            _getFill = () => _fill;
            _setFill = value => Set(ref _fill, FillId, value);
            _getFlash = () => _flash;
            _setFlash = value => Set(ref _flash, FlashId, value);
            _getPreview = () => _preview;
            _setPreview = value => Set(ref _preview, PreviewId, value);
            _getDeficit = () => _deficit;
            _setDeficit = value => Set(ref _deficit, DeficitId, value);
            _clearFill = () => _fillTween = null;
            _clearFlash = () => _flashTween = null;
            _clearPreview = () => _previewTween = null;
            _clearDeficit = () => _deficitTween = null;
            _flashOn = () => _setFlash(1f);
        }

        public RectTransform Rect => _image.rectTransform;

        public void SetActive(bool active)
        {
            _image.gameObject.SetActive(active);
        }

        public void ApplyStyle(PranaOrbsStyleSO style)
        {
            if (style == null || style.OrbMaterial == null)
            {
                _image.enabled = false;
                return;
            }

            if (_source != style.OrbMaterial || _material == null)
            {
                DestroyMaterial();
                _source = style.OrbMaterial;
                _material = new Material(_source) { name = _source.name + " (Runtime)", hideFlags = HideFlags.DontSave };
                _image.material = _material;
            }

            _image.enabled = true;
            _image.color = Color.white;
            _material.SetColor(CoreColorId, style.CoreColor);
            _material.SetColor(GlowColorId, style.GlowColor);
            _material.SetColor(SocketColorId, style.SocketColor);
            _material.SetColor(RimColorId, style.RimColor);
            _material.SetColor(PreviewColorId, style.PreviewColor);
            _material.SetColor(DeficitColorId, style.DeficitColor);
            _material.SetFloat(OrbRadiusId, style.OrbRadius);
            _material.SetFloat(RimWidthId, style.RimWidth);
            _material.SetFloat(GlowWidthId, style.GlowWidth);
            _material.SetFloat(GlowStrengthId, style.GlowStrength);
            _material.SetFloat(SwirlAmountId, style.SwirlAmount);
            _material.SetFloat(SwirlSpeedId, style.SwirlSpeed);
            _material.SetFloat(PulseSpeedId, style.PulseSpeed);
            _material.SetFloat(SeedId, _seed);
            _material.SetFloat(FillId, _fill);
            _material.SetFloat(FlashId, _flash);
            _material.SetFloat(PreviewId, _preview);
            _material.SetFloat(DeficitId, _deficit);
        }

        public void SetFilled(bool filled, PranaOrbsStyleSO style, bool animate, float delay)
        {
            if (filled == _filled && _fillTween == null)
            {
                return;
            }

            _filled = filled;
            float target = filled ? 1f : 0f;
            if (!animate || style == null)
            {
                Stop(ref _fillTween);
                Stop(ref _flashTween);
                _setFill(target);
                _setFlash(0f);
                return;
            }

            float duration = filled ? style.RefillDuration : style.DrainDuration;
            Stop(ref _fillTween);
            _fillTween = Animate(_getFill, _setFill, target, duration, delay, filled ? Ease.OutBack : Ease.InQuad)
                .OnComplete(_clearFill);

            Stop(ref _flashTween);
            _setFlash(0f);
            _flashTween = DOTween.Sequence()
                .AppendInterval(delay)
                .AppendCallback(_flashOn)
                .Append(DOTween.To(_getFlash, _setFlash, 0f, style.FlashDuration).SetEase(Ease.OutQuad))
                .SetUpdate(true)
                .OnComplete(_clearFlash);
        }

        public void SetPreview(PranaOrbPreview preview, float duration)
        {
            float previewTarget = preview == PranaOrbPreview.Spend ? 1f : 0f;
            _deficitTarget = preview == PranaOrbPreview.Deficit ? 1f : 0f;
            if (!Mathf.Approximately(_preview, previewTarget) || _previewTween != null)
            {
                Stop(ref _previewTween);
                _previewTween = Animate(_getPreview, _setPreview, previewTarget, duration, 0f, Ease.OutQuad)
                    .OnComplete(_clearPreview);
            }

            if (!Mathf.Approximately(_deficit, _deficitTarget) || _deficitTween != null)
            {
                Stop(ref _deficitTween);
                _deficitTween = Animate(_getDeficit, _setDeficit, _deficitTarget, duration, 0f, Ease.OutQuad)
                    .OnComplete(_clearDeficit);
            }
        }

        public void FlashDeficit(float duration)
        {
            Stop(ref _deficitTween);
            _setDeficit(1.6f);
            _deficitTween = Animate(_getDeficit, _setDeficit, _deficitTarget, duration, 0f, Ease.InQuad)
                .OnComplete(_clearDeficit);
        }

        public void Dispose()
        {
            Stop(ref _fillTween);
            Stop(ref _flashTween);
            Stop(ref _previewTween);
            Stop(ref _deficitTween);
            DestroyMaterial();
        }

        private void Set(ref float field, int propertyId, float value)
        {
            field = value;
            if (_material != null)
            {
                _material.SetFloat(propertyId, value);
            }
        }

        private static Tween Animate(DOGetter<float> getter, DOSetter<float> setter, float target, float duration,
            float delay, Ease ease)
        {
            return DOTween.To(getter, setter, target, duration)
                .SetDelay(delay)
                .SetEase(ease)
                .SetUpdate(true);
        }

        private static void Stop(ref Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            Tween stopping = tween;
            tween = null;
            stopping.Kill();
        }

        private void DestroyMaterial()
        {
            if (_material != null)
            {
                Object.Destroy(_material);
            }

            _material = null;
            _source = null;
        }
    }
}
