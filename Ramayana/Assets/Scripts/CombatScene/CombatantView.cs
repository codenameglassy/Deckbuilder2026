// Shows one combatant on its stand-in object (a cube for now): a world-space health bar with a Kavach badge (see
// CombatantHealthBar), a tint when the fighter falls, a flash and shake when it loses health, and floating damage
// numbers through the scene's DamageNumberSpawner. Gaining Kavach is shown by the badge punch on the bar and by the
// combat VFX (the hull shell and its gain accent), so the view never scales the fighter. It only presents: it reacts
// to the bound Combatant's events and never changes combat state. Text is numbers only, so it needs no localization
// keys. Flash and shake run from the tick service only while one is playing; the bar's drain and badge punch are
// DOTween tweens owned by the bar, killed when this view is destroyed. PlayLunge (an enemy's attack: wind back, dart
// at the target so it lands at the end of the windup, return) and PlayThrust (Rama's quick strike) are DOTween core
// sequences on game time. Every motion is an offset composed with the hit shake over the rest position, so the two
// never fight over the transform. ShowOutcome and ClearOutcome draw a card's previewed outcome on the bar (the
// health it would lose as a pulsing ghost, the Kavach it would break or gain, and a lethal tag), for the HUD.
// The bar also refreshes when the fighter is healed. A row of status chips under the bar (StatusChipRow) shows the
// fighter's statuses and their stacks, refreshing only when they change. Healing raises a green number and every
// status applied or spent a short popup ("Bhaya 2"), through the scene's DamageNumberSpawner; ShowArrowName raises a
// fired special arrow's name above the fighter the same way. ShowStatusPreview shows
// the statuses a considered card would leave on the chips (new ones as ghosts, changed counts in the preview colour);
// ClearOutcome clears it with the bar's preview. PointerEntered and PointerExited report the pointer over the
// fighter's collider (through the scene camera's PhysicsRaycaster), so inspect widgets can show it, and TopCenter is
// the top of that collider in world space (the transform position without one).
using System;
using DG.Tweening;
using DG.Tweening.Core;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Combatant View")]
    public sealed class CombatantView : MonoBehaviour, ITickable, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private CombatantViewStyleSO _style;
        [SerializeField] private Renderer _body;
        [SerializeField] private DamageNumberSpawner _damageNumbers;

        private CombatantHealthBar _bar;
        private StatusChipRow _statusChips;
        private Collider _collider;
        private MaterialPropertyBlock _propertyBlock;
        private Combatant _combatant;
        private Transform _transform;
        private Vector3 _restPosition;
        private Color _baseColor = Color.white;
        private ITickService _ticks;
        private bool _registered;
        private float _flashRemaining;
        private float _shakeRemaining;
        private Vector3 _shakeOffset;
        private Vector3 _motionDirection;
        private float _motion;
        private Sequence _motionSequence;
        private DOGetter<float> _getMotion;
        private DOSetter<float> _setMotion;
        private TweenCallback _clearMotion;

        public event Action<CombatantView> PointerEntered;

        public event Action<CombatantView> PointerExited;

        public Combatant Combatant => _combatant;

        public Vector3 TopCenter
        {
            get
            {
                if (_collider == null)
                {
                    return transform.position;
                }

                Bounds bounds = _collider.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }
        }

        private void Awake()
        {
            _transform = transform;
            TryGetComponent(out _collider);
            _restPosition = _transform.localPosition;

            if (_body == null)
            {
                _body = GetComponent<Renderer>();
            }

            if (_body != null && _body.sharedMaterial != null && _body.sharedMaterial.HasProperty(BaseColorId))
            {
                _baseColor = _body.sharedMaterial.GetColor(BaseColorId);
            }

            _getMotion = () => _motion;
            _setMotion = SetMotion;
            _clearMotion = () => _motionSequence = null;
            _propertyBlock = new MaterialPropertyBlock();
            _bar = CombatantHealthBar.Create(_transform);
            _statusChips = StatusChipRow.Create(_bar.Root);
            ApplyStyle();
            Refresh();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _ticks);
        }

        private void OnEnable()
        {
            if (_style != null)
            {
                _style.Changed += HandleStyleChanged;
            }
        }

        private void OnDisable()
        {
            if (_style != null)
            {
                _style.Changed -= HandleStyleChanged;
            }

            StopReactions();
        }

        private void OnDestroy()
        {
            Unbind();
            StopMotion();
            _statusChips?.Dispose();
            _bar?.Dispose();
            PointerEntered = null;
            PointerExited = null;
        }

        public void Bind(Combatant combatant)
        {
            Unbind();
            _combatant = combatant;

            if (_combatant != null)
            {
                _combatant.Damaged += HandleDamaged;
                _combatant.Healed += HandleHealed;
                _combatant.StatusApplied += HandleStatusApplied;
                _combatant.BlockChanged += HandleBlockChanged;
                _combatant.Died += HandleDied;
            }

            _statusChips?.Bind(_combatant != null ? _combatant.Statuses : null);
            Refresh();
        }

        public void Unbind()
        {
            if (_combatant == null)
            {
                return;
            }

            _combatant.Damaged -= HandleDamaged;
            _combatant.Healed -= HandleHealed;
            _combatant.StatusApplied -= HandleStatusApplied;
            _combatant.BlockChanged -= HandleBlockChanged;
            _combatant.Died -= HandleDied;
            _combatant = null;
            _statusChips?.Bind(null);
        }

        public void ShowOutcome(in CombatantOutcome outcome, string lethalLabel)
        {
            _bar?.ShowPreview(outcome.EndHealth, outcome.EndBlock, outcome.IsLethal, lethalLabel);
        }

        public void ShowStatusPreview(StatusSet previewStatuses)
        {
            _statusChips?.ShowPreview(previewStatuses);
        }

        public void ClearOutcome()
        {
            _bar?.ClearPreview();
            _statusChips?.ClearPreview();
        }

        public void ShowArrowName(ArrowDefinitionSO arrow)
        {
            if (_style != null && _damageNumbers != null)
            {
                _damageNumbers.SpawnArrow(_transform.TransformPoint(_style.DamageNumberOffset), arrow);
            }
        }

        public void PlayLunge(Vector3 targetPosition, float windup)
        {
            if (_style == null || !TrySetMotionDirection(targetPosition))
            {
                return;
            }

            StopMotion();
            float windBack = Mathf.Max(0f, windup) * _style.LungeWindBackShare;
            float strike = Mathf.Max(0f, windup) - windBack;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(_getMotion, _setMotion, -_style.LungeWindBack, windBack).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(_getMotion, _setMotion, _style.LungeDistance, strike).SetEase(Ease.InQuad));
            sequence.Append(DOTween.To(_getMotion, _setMotion, 0f, _style.LungeReturnDuration).SetEase(Ease.OutQuad));
            sequence.OnComplete(_clearMotion);
            _motionSequence = sequence;
        }

        public void PlayThrust(Vector3 targetPosition)
        {
            if (_style == null || !TrySetMotionDirection(targetPosition))
            {
                return;
            }

            StopMotion();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(_getMotion, _setMotion, _style.ThrustDistance, _style.ThrustDuration).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(_getMotion, _setMotion, 0f, _style.ThrustReturnDuration).SetEase(Ease.InOutQuad));
            sequence.OnComplete(_clearMotion);
            _motionSequence = sequence;
        }

        public void Tick(float deltaTime)
        {
            if (_style == null)
            {
                StopReactions();
                return;
            }

            _flashRemaining = Mathf.Max(0f, _flashRemaining - deltaTime);
            _shakeRemaining = Mathf.Max(0f, _shakeRemaining - deltaTime);

            _shakeOffset = Vector3.zero;
            if (_shakeRemaining > 0f)
            {
                float shakeDuration = Mathf.Max(0.0001f, _style.ShakeDuration);
                float elapsed = shakeDuration - _shakeRemaining;
                float strength = _style.ShakeStrength * (_shakeRemaining / shakeDuration);
                _shakeOffset.x = Mathf.Sin(elapsed * _style.ShakeFrequency) * strength;
            }

            ApplyPosition();

            if (_flashRemaining > 0f)
            {
                float progress = 1f - _flashRemaining / Mathf.Max(0.0001f, _style.FlashDuration);
                SetBodyColor(Color.Lerp(_style.FlashColor, RestColor(), progress));
            }
            else
            {
                ApplyTint();
            }

            if (_flashRemaining <= 0f && _shakeRemaining <= 0f)
            {
                SetRegistered(false);
            }
        }

        private void HandleDamaged(Combatant combatant, DamageResult result)
        {
            Refresh();

            if (_style == null)
            {
                return;
            }

            Vector3 numberPosition = _transform.TransformPoint(_style.DamageNumberOffset);

            if (result.HealthLost > 0)
            {
                _flashRemaining = _style.FlashDuration;
                _shakeRemaining = _style.ShakeDuration;
                SetRegistered(true);

                if (_damageNumbers != null)
                {
                    _damageNumbers.Spawn(numberPosition, result.HealthLost, false);
                }
            }

            if (result.Blocked > 0 && _damageNumbers != null)
            {
                _damageNumbers.Spawn(numberPosition, result.Blocked, true);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExited?.Invoke(this);
        }

        private void HandleHealed(Combatant combatant, int restored)
        {
            Refresh();

            if (_style != null && _damageNumbers != null)
            {
                _damageNumbers.SpawnHeal(_transform.TransformPoint(_style.DamageNumberOffset), restored);
            }
        }

        private void HandleStatusApplied(Combatant combatant, StatusDefinitionSO status, int amount)
        {
            if (_style != null && _damageNumbers != null)
            {
                _damageNumbers.SpawnStatus(_transform.TransformPoint(_style.DamageNumberOffset), status, amount,
                    combatant.Statuses.GetAmount(status));
            }
        }

        private void HandleBlockChanged(Combatant combatant)
        {
            Refresh();
        }

        private void HandleDied(Combatant combatant)
        {
            Refresh();
        }

        private void HandleStyleChanged()
        {
            ApplyStyle();
            Refresh();
        }

        private void Refresh()
        {
            if (_bar == null)
            {
                return;
            }

            bool bound = _combatant != null;
            _bar.SetVisible(bound);
            if (!bound)
            {
                return;
            }

            _bar.SetValues(_combatant.Health, _combatant.MaxHealth, _combatant.Block);

            if (_flashRemaining <= 0f)
            {
                ApplyTint();
            }
        }

        private void ApplyStyle()
        {
            if (_style == null || _bar == null)
            {
                return;
            }

            _bar.ApplyStyle(_style);
            _statusChips?.ApplyStyle(_style);
            ApplyTint();
        }

        private bool IsFallen => _combatant != null && !_combatant.IsAlive;

        private Color RestColor()
        {
            return IsFallen && _style != null ? _style.FallenTint : _baseColor;
        }

        private void ApplyTint()
        {
            if (_body == null)
            {
                return;
            }

            if (!IsFallen || _style == null)
            {
                _body.SetPropertyBlock(null);
                return;
            }

            SetBodyColor(_style.FallenTint);
        }

        private void SetBodyColor(Color color)
        {
            if (_body == null)
            {
                return;
            }

            _propertyBlock.SetColor(BaseColorId, color);
            _body.SetPropertyBlock(_propertyBlock);
        }

        private void StopReactions()
        {
            _flashRemaining = 0f;
            _shakeRemaining = 0f;
            _shakeOffset = Vector3.zero;
            StopMotion();

            if (_transform != null)
            {
                _transform.localPosition = _restPosition;
            }

            ApplyTint();
            SetRegistered(false);
        }

        private bool TrySetMotionDirection(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - _transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            Transform parent = _transform.parent;
            _motionDirection = parent != null ? parent.InverseTransformDirection(direction) : direction;
            return true;
        }

        private void SetMotion(float distance)
        {
            _motion = distance;
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            _transform.localPosition = _restPosition + _shakeOffset + _motionDirection * _motion;
        }

        private void StopMotion()
        {
            _motion = 0f;
            if (_motionSequence == null)
            {
                return;
            }

            Sequence sequence = _motionSequence;
            _motionSequence = null;
            sequence.Kill();
        }

        private void SetRegistered(bool registered)
        {
            if (_ticks == null || _registered == registered)
            {
                return;
            }

            _registered = registered;
            if (registered)
            {
                _ticks.AddTickable(this);
            }
            else
            {
                _ticks.RemoveTickable(this);
            }
        }
    }
}
