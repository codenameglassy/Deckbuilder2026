// Drives the targeting line while a card or a special arrow waits for its target. It follows TargetingPresenter:
// when targeting starts the line grows from its source (the lifted card, or the arrow's quiver slot), and it fades
// out when targeting ends. While an enemy is
// highlighted the line locks onto it at full intensity, snapping smoothly between targets with a short pulse. While
// a drag points at empty space (SetFreeEnd) it follows the pointer instead, dimmed by the style's unlocked intensity.
// Each late tick it projects the top of the enemy (through the combat camera anchor) into the line canvas, or maps
// the pointer into it, bends a cubic Bezier from the source's top edge up and over onto that point, and writes the four
// control points and the curve length to its own runtime copy of the line material. That is the whole per-frame
// cost: a few material values, no mesh or canvas rebuild and no allocation. It ticks only while targeting or fading,
// stays hidden until it has a point to aim at, and holds its last shape while it fades.
// The curve is built backwards from a fixed arrival direction (toward the target, tilted downward in proportion to
// how sideways the line runs, so a target straight above the source is approached straight on), with the head base
// and the last control point both placed behind the target along it, so the arrowhead always points at the target
// and the strip never folds back on itself, however steep or short the line is.
// Reveal, pulse and fade are DOTween core tweens on unscaled time. Every value comes from TargetingLineStyleSO.
using System;
using DG.Tweening;
using DG.Tweening.Core;
using JourneyToLanka.CombatScene;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JourneyToLanka.CombatUI
{
    internal sealed class TargetingLinePresenter : ILateTickable, IDisposable
    {
        private static readonly int ControlPoints01Id = Shader.PropertyToID("_P01");
        private static readonly int ControlPoints23Id = Shader.PropertyToID("_P23");
        private static readonly int ColorStartId = Shader.PropertyToID("_ColorStart");
        private static readonly int ColorEndId = Shader.PropertyToID("_ColorEnd");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int ChevronColorId = Shader.PropertyToID("_ChevronColor");
        private static readonly int WidthStartId = Shader.PropertyToID("_WidthStart");
        private static readonly int WidthEndId = Shader.PropertyToID("_WidthEnd");
        private static readonly int GlowScaleId = Shader.PropertyToID("_GlowScale");
        private static readonly int CoreWidthId = Shader.PropertyToID("_CoreWidth");
        private static readonly int GlowFalloffId = Shader.PropertyToID("_GlowFalloff");
        private static readonly int HeadLengthId = Shader.PropertyToID("_HeadLength");
        private static readonly int HeadWidthId = Shader.PropertyToID("_HeadWidth");
        private static readonly int ChevronSpacingId = Shader.PropertyToID("_ChevronSpacing");
        private static readonly int ChevronDutyId = Shader.PropertyToID("_ChevronDuty");
        private static readonly int ChevronSlantId = Shader.PropertyToID("_ChevronSlant");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int ChevronIntensityId = Shader.PropertyToID("_ChevronIntensity");
        private static readonly int ShimmerAmountId = Shader.PropertyToID("_ShimmerAmount");
        private static readonly int StartFadeId = Shader.PropertyToID("_StartFade");
        private static readonly int LengthId = Shader.PropertyToID("_Length");
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int PulseWidenId = Shader.PropertyToID("_PulseWiden");
        private static readonly int PulseBrightenId = Shader.PropertyToID("_PulseBrighten");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        private readonly RectTransform _canvasRect;
        private readonly TargetingLineGraphic _graphic;
        private readonly Action<int> _targetChanged;
        private readonly Action<bool> _activeChanged;
        private readonly DOGetter<float> _getReveal;
        private readonly DOSetter<float> _setReveal;
        private readonly DOGetter<float> _getPulse;
        private readonly DOSetter<float> _setPulse;
        private readonly DOGetter<float> _getAlpha;
        private readonly DOSetter<float> _setAlpha;
        private readonly TweenCallback _clearReveal;
        private readonly TweenCallback _clearPulse;
        private readonly TweenCallback _finishFade;
        private TargetingLineStyleSO _style;
        private CombatCameraAnchorSO _cameraAnchor;
        private Material _sourceMaterial;
        private Material _material;
        private TargetingPresenter _targeting;
        private CombatController _controller;
        private ITickService _ticks;
        private Tween _revealTween;
        private Tween _pulseTween;
        private Tween _fadeTween;
        private Vector2 _end;
        private Vector2 _endVelocity;
        private Vector2 _freeScreenPoint;
        private float _reveal;
        private float _pulse;
        private float _alpha;
        private int _target = -1;
        private int _lastTarget = -1;
        private bool _active;
        private bool _visible;
        private bool _hasEnd;
        private bool _hasFreeEnd;
        private bool _locked = true;
        private bool _registered;

        private TargetingLinePresenter(RectTransform canvasRect, TargetingLineGraphic graphic)
        {
            _canvasRect = canvasRect;
            _graphic = graphic;
            _targetChanged = HandleTargetChanged;
            _activeChanged = HandleActiveChanged;
            _getReveal = () => _reveal;
            _setReveal = value => SetFloat(ref _reveal, RevealId, value);
            _getPulse = () => _pulse;
            _setPulse = value => SetFloat(ref _pulse, PulseId, value);
            _getAlpha = () => _alpha;
            _setAlpha = value => SetFloat(ref _alpha, AlphaId, value);
            _clearReveal = () => _revealTween = null;
            _clearPulse = () => _pulseTween = null;
            _finishFade = () =>
            {
                _fadeTween = null;
                SetVisible(false);
                RefreshRegistration();
            };
        }

        public static TargetingLinePresenter Create(RectTransform canvasRect)
        {
            RectTransform rect = UIBuildUtility.CreateRect("TargetingLine", canvasRect);
            UIBuildUtility.Stretch(rect);
            rect.gameObject.AddComponent<CanvasRenderer>();
            TargetingLineGraphic graphic = rect.gameObject.AddComponent<TargetingLineGraphic>();
            graphic.raycastTarget = false;
            rect.gameObject.SetActive(false);
            return new TargetingLinePresenter(canvasRect, graphic);
        }

        public void SetStyle(TargetingLineStyleSO style, CombatCameraAnchorSO cameraAnchor)
        {
            _style = style;
            _cameraAnchor = cameraAnchor;
            if (_style == null || _style.Material == null)
            {
                return;
            }

            if (_sourceMaterial != _style.Material)
            {
                DestroyMaterial();
                _sourceMaterial = _style.Material;
                _material = new Material(_sourceMaterial) { name = _sourceMaterial.name + " (Runtime)" };
                _graphic.material = _material;
            }

            _graphic.SetSegments(_style.Segments);
            ApplyStaticProperties();
        }

        public void Attach(TargetingPresenter targeting, CombatController controller)
        {
            Detach();
            _targeting = targeting;
            _controller = controller;
            if (_targeting != null)
            {
                _targeting.TargetChanged += _targetChanged;
                _targeting.ActiveChanged += _activeChanged;
            }
        }

        public void Detach()
        {
            if (_targeting != null)
            {
                _targeting.TargetChanged -= _targetChanged;
                _targeting.ActiveChanged -= _activeChanged;
            }

            _targeting = null;
            _controller = null;
            _target = -1;
            _lastTarget = -1;
            HideNow();
        }

        public void Dispose()
        {
            Detach();
            DestroyMaterial();
        }

        public void SetFreeEnd(Vector2 screenPoint)
        {
            _freeScreenPoint = screenPoint;
            _hasFreeEnd = true;
            RefreshLocked(false);
        }

        public void LateTick(float deltaTime)
        {
            if (!_active)
            {
                return;
            }

            if (UpdateCurve(Time.unscaledDeltaTime) && !_visible)
            {
                Show();
            }
        }

        private void HandleActiveChanged(bool active)
        {
            if (active)
            {
                Begin();
            }
            else
            {
                End();
            }
        }

        private void HandleTargetChanged(int targetIndex)
        {
            if (targetIndex == _target)
            {
                return;
            }

            _target = targetIndex;
            if (targetIndex >= 0)
            {
                _lastTarget = targetIndex;
            }

            if (_active)
            {
                RefreshLocked(targetIndex >= 0 && _visible);
            }
        }

        private void Begin()
        {
            if (_style == null || _material == null)
            {
                return;
            }

            StopTweens();
            _active = true;
            _lastTarget = _target;
            _hasFreeEnd = false;
            _hasEnd = false;
            _endVelocity = Vector2.zero;
            SetVisible(false);
            SetFloat(ref _pulse, PulseId, 0f);
            SetFloat(ref _alpha, AlphaId, 1f);
            SetFloat(ref _reveal, RevealId, 0f);
            RefreshLocked(false);
            RefreshRegistration();

            if (UpdateCurve(0f))
            {
                Show();
            }
        }

        private void Show()
        {
            SetVisible(true);
            StopTween(ref _revealTween);
            _revealTween = DOTween.To(_getReveal, _setReveal, 1f, _style.RevealDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(_clearReveal);
        }

        private void End()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _hasFreeEnd = false;
            if (!_visible || _style == null)
            {
                HideNow();
                return;
            }

            StopTween(ref _fadeTween);
            _fadeTween = DOTween.To(_getAlpha, _setAlpha, 0f, _style.FadeOutDuration)
                .SetUpdate(true)
                .OnComplete(_finishFade);
        }

        private void HideNow()
        {
            _active = false;
            _hasFreeEnd = false;
            StopTweens();
            SetVisible(false);
            RefreshRegistration();
        }

        private void RefreshLocked(bool pulseOnLock)
        {
            bool locked = _target >= 0 || !_hasFreeEnd;
            if (locked == _locked && !pulseOnLock)
            {
                return;
            }

            _locked = locked;
            ApplyIntensity();
            if (pulseOnLock && locked && _style != null)
            {
                PlayPulse();
            }
        }

        private void ApplyIntensity()
        {
            if (_material != null && _style != null)
            {
                float intensity = _style.Intensity * (_locked ? 1f : _style.UnlockedIntensity);
                _material.SetFloat(IntensityId, intensity);
            }
        }

        private void PlayPulse()
        {
            StopTween(ref _pulseTween);
            SetFloat(ref _pulse, PulseId, 1f);
            _pulseTween = DOTween.To(_getPulse, _setPulse, 0f, _style.PulseDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(_clearPulse);
        }

        private bool UpdateCurve(float deltaTime)
        {
            if (!_active || _style == null || _material == null || _targeting == null || _controller == null
                || _cameraAnchor == null || !_cameraAnchor.TryGetValue(out Camera camera))
            {
                return false;
            }

            RectTransform source = _targeting.SourceRect;
            if (source == null || !TryGetEndScreenPoint(camera, out Vector2 screen))
            {
                return false;
            }

            Vector3 sourceTop = source.TransformPoint(new Vector3(0f, source.rect.yMax, 0f));
            Vector2 start = _canvasRect.InverseTransformPoint(sourceTop);

            Vector2 end = _canvasRect.InverseTransformPoint(new Vector3(screen.x, screen.y, 0f));
            if (!_hasEnd || deltaTime <= 0f)
            {
                _end = end;
                _endVelocity = Vector2.zero;
                _hasEnd = true;
            }
            else
            {
                _end = Vector2.SmoothDamp(_end, end, ref _endVelocity, _style.SnapSmoothTime, Mathf.Infinity, deltaTime);
            }

            Vector2 toEnd = _end - start;
            float distance = toEnd.magnitude;
            if (distance < 1f)
            {
                return false;
            }

            Vector2 towardEnd = toEnd / distance;
            Vector2 arrival = towardEnd + Vector2.down * (_style.ArrivalDrop * Mathf.Abs(towardEnd.x));
            float arrivalLength = arrival.magnitude;
            arrival = arrivalLength > 0.001f ? arrival / arrivalLength : Vector2.down;

            Vector2 p1 = start + Vector2.up * (distance * _style.CurveRise);
            Vector2 p3 = _end - arrival * _style.HeadLength;
            Vector2 p2 = p3 - arrival * (distance * _style.ArrivalPull);

            float chord = Vector2.Distance(start, p3);
            float polygon = Vector2.Distance(start, p1) + Vector2.Distance(p1, p2) + Vector2.Distance(p2, p3);

            _material.SetVector(ControlPoints01Id, new Vector4(start.x, start.y, p1.x, p1.y));
            _material.SetVector(ControlPoints23Id, new Vector4(p2.x, p2.y, p3.x, p3.y));
            _material.SetFloat(LengthId, (chord + polygon) * 0.5f);
            return true;
        }

        private bool TryGetEndScreenPoint(Camera camera, out Vector2 screenPoint)
        {
            int targetIndex = _target >= 0 ? _target : _hasFreeEnd ? -1 : _lastTarget;
            if (targetIndex < 0)
            {
                screenPoint = _freeScreenPoint;
                return _hasFreeEnd;
            }

            EnemyTargetView target = _controller.GetEnemyTarget(targetIndex);
            if (target == null)
            {
                screenPoint = default;
                return false;
            }

            Vector3 screen = camera.WorldToScreenPoint(target.TopCenter + Vector3.up * _style.TargetLift);
            screenPoint = new Vector2(screen.x, screen.y);
            return screen.z > 0f;
        }

        private void ApplyStaticProperties()
        {
            _material.SetColor(ColorStartId, _style.BodyColorAtCard);
            _material.SetColor(ColorEndId, _style.BodyColorAtTarget);
            _material.SetColor(CoreColorId, _style.CoreColor);
            _material.SetColor(GlowColorId, _style.GlowColor);
            _material.SetColor(ChevronColorId, _style.ChevronColor);
            _material.SetFloat(WidthStartId, _style.WidthAtCard);
            _material.SetFloat(WidthEndId, _style.WidthAtTarget);
            _material.SetFloat(GlowScaleId, _style.GlowScale);
            _material.SetFloat(CoreWidthId, _style.CoreWidth);
            _material.SetFloat(GlowFalloffId, _style.GlowFalloff);
            _material.SetFloat(HeadLengthId, _style.HeadLength);
            _material.SetFloat(HeadWidthId, _style.HeadWidth);
            _material.SetFloat(ChevronSpacingId, _style.ChevronSpacing);
            _material.SetFloat(ChevronDutyId, _style.ChevronDuty);
            _material.SetFloat(ChevronSlantId, _style.ChevronSlant);
            _material.SetFloat(FlowSpeedId, _style.FlowSpeed);
            _material.SetFloat(ChevronIntensityId, _style.ChevronIntensity);
            _material.SetFloat(ShimmerAmountId, _style.ShimmerAmount);
            _material.SetFloat(StartFadeId, _style.StartFade);
            _material.SetFloat(PulseWidenId, _style.PulseWiden);
            _material.SetFloat(PulseBrightenId, _style.PulseBrighten);
            ApplyIntensity();
            _material.SetFloat(RevealId, _reveal);
            _material.SetFloat(PulseId, _pulse);
            _material.SetFloat(AlphaId, _alpha);
        }

        private void SetFloat(ref float field, int propertyId, float value)
        {
            field = value;
            if (_material != null)
            {
                _material.SetFloat(propertyId, value);
            }
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (_graphic != null)
            {
                _graphic.gameObject.SetActive(visible);
            }
        }

        private void RefreshRegistration()
        {
            bool registered = _active || _visible;
            if (registered && _ticks == null)
            {
                ServiceLocator.TryGet(out _ticks);
            }

            if (_ticks == null || _registered == registered)
            {
                return;
            }

            _registered = registered;
            if (registered)
            {
                _ticks.AddLateTickable(this);
            }
            else
            {
                _ticks.RemoveLateTickable(this);
            }
        }

        private void StopTweens()
        {
            StopTween(ref _revealTween);
            StopTween(ref _pulseTween);
            StopTween(ref _fadeTween);
        }

        private static void StopTween(ref Tween tween)
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
            _sourceMaterial = null;
        }
    }
}
