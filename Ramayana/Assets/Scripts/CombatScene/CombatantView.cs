// Shows one combatant on its stand-in object (a cube for now): health and Kavach as world-space labels built from
// its style, a tint when the fighter falls, a flash and shake when it loses health, a pulse when it gains Kavach,
// and floating damage numbers through the scene's DamageNumberSpawner. It only presents: it reacts to the bound
// Combatant's events and never changes combat state. Text is numbers only, so it needs no localization keys.
// Reactions run from the tick service only while one is playing; values are passed to TMP's SetText as float so
// the call is not ambiguous with the span-based int overloads in Unity 6's TextMeshPro.
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Combatant View")]
    public sealed class CombatantView : MonoBehaviour, ITickable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private CombatantViewStyleSO _style;
        [SerializeField] private Renderer _body;
        [SerializeField] private DamageNumberSpawner _damageNumbers;

        private TextMeshPro _healthLabel;
        private TextMeshPro _blockLabel;
        private MaterialPropertyBlock _propertyBlock;
        private Combatant _combatant;
        private Transform _transform;
        private Vector3 _restPosition;
        private Vector3 _restScale;
        private Color _baseColor = Color.white;
        private ITickService _ticks;
        private bool _registered;
        private int _lastBlock;
        private float _flashRemaining;
        private float _shakeRemaining;
        private float _pulseRemaining;

        private void Awake()
        {
            _transform = transform;
            _restPosition = _transform.localPosition;
            _restScale = _transform.localScale;

            if (_body == null)
            {
                _body = GetComponent<Renderer>();
            }

            if (_body != null && _body.sharedMaterial != null && _body.sharedMaterial.HasProperty(BaseColorId))
            {
                _baseColor = _body.sharedMaterial.GetColor(BaseColorId);
            }

            _propertyBlock = new MaterialPropertyBlock();
            _healthLabel = CreateLabel("HealthLabel");
            _blockLabel = CreateLabel("KavachLabel");
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
        }

        public void Bind(Combatant combatant)
        {
            Unbind();
            _combatant = combatant;

            if (_combatant != null)
            {
                _lastBlock = _combatant.Block;
                _combatant.Damaged += HandleDamaged;
                _combatant.BlockChanged += HandleBlockChanged;
                _combatant.Died += HandleDied;
            }

            Refresh();
        }

        public void Unbind()
        {
            if (_combatant == null)
            {
                return;
            }

            _combatant.Damaged -= HandleDamaged;
            _combatant.BlockChanged -= HandleBlockChanged;
            _combatant.Died -= HandleDied;
            _combatant = null;
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
            _pulseRemaining = Mathf.Max(0f, _pulseRemaining - deltaTime);

            Vector3 position = _restPosition;
            if (_shakeRemaining > 0f)
            {
                float shakeDuration = Mathf.Max(0.0001f, _style.ShakeDuration);
                float elapsed = shakeDuration - _shakeRemaining;
                float strength = _style.ShakeStrength * (_shakeRemaining / shakeDuration);
                position.x += Mathf.Sin(elapsed * _style.ShakeFrequency) * strength;
            }

            _transform.localPosition = position;

            float pulse = 0f;
            if (_pulseRemaining > 0f)
            {
                float progress = 1f - _pulseRemaining / Mathf.Max(0.0001f, _style.BlockPulseDuration);
                pulse = _style.BlockPulseScale * Mathf.Sin(progress * Mathf.PI);
            }

            _transform.localScale = _restScale * (1f + pulse);

            if (_flashRemaining > 0f)
            {
                float progress = 1f - _flashRemaining / Mathf.Max(0.0001f, _style.FlashDuration);
                SetBodyColor(Color.Lerp(_style.FlashColor, RestColor(), progress));
            }
            else
            {
                ApplyTint();
            }

            if (_flashRemaining <= 0f && _shakeRemaining <= 0f && _pulseRemaining <= 0f)
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

        private void HandleBlockChanged(Combatant combatant)
        {
            if (combatant.Block > _lastBlock && _style != null)
            {
                _pulseRemaining = _style.BlockPulseDuration;
                SetRegistered(true);
            }

            _lastBlock = combatant.Block;
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
            if (_healthLabel == null)
            {
                return;
            }

            bool bound = _combatant != null;
            _healthLabel.gameObject.SetActive(bound);
            _blockLabel.gameObject.SetActive(bound && _combatant.Block > 0);

            if (!bound)
            {
                return;
            }

            _healthLabel.SetText("{0}/{1}", (float)_combatant.Health, (float)_combatant.MaxHealth);
            if (_combatant.Block > 0)
            {
                _blockLabel.SetText("{0}", (float)_combatant.Block);
            }

            if (_flashRemaining <= 0f)
            {
                ApplyTint();
            }
        }

        private void ApplyStyle()
        {
            if (_style == null || _healthLabel == null)
            {
                return;
            }

            StyleLabel(_healthLabel, _style.HealthColor, _style.HealthOffset);
            StyleLabel(_blockLabel, _style.BlockColor, _style.BlockOffset);
            ApplyTint();
        }

        private void StyleLabel(TextMeshPro label, Color color, Vector3 offset)
        {
            if (_style.Font != null)
            {
                label.font = _style.Font;
            }

            label.fontSize = _style.FontSize;
            label.color = color;
            label.rectTransform.sizeDelta = _style.LabelSize;
            label.transform.localPosition = offset;
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
            _pulseRemaining = 0f;

            if (_transform != null)
            {
                _transform.localPosition = _restPosition;
                _transform.localScale = _restScale;
            }

            ApplyTint();
            SetRegistered(false);
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

        private TextMeshPro CreateLabel(string labelName)
        {
            var labelObject = new GameObject(labelName);
            labelObject.transform.SetParent(transform, false);

            var label = labelObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }
    }
}
