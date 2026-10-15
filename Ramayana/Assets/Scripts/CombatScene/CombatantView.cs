// Shows one combatant on its stand-in object (a cube for now): health and Kavach as world-space labels that the
// view builds itself from its style, and a tint on the body when the fighter falls. It only presents: it reacts
// to the bound Combatant's events and never changes combat state. Text is numbers only, so it needs no
// localization keys. Labels update through TMP's numeric SetText, which does not allocate; values are passed
// as float so the call is not ambiguous with the span-based int overloads in Unity 6's TextMeshPro.
using JourneyToLanka.Combat.Model;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Combatant View")]
    public sealed class CombatantView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private CombatantViewStyleSO _style;
        [SerializeField] private Renderer _body;

        private TextMeshPro _healthLabel;
        private TextMeshPro _blockLabel;
        private MaterialPropertyBlock _propertyBlock;
        private Combatant _combatant;

        private void Awake()
        {
            if (_body == null)
            {
                _body = GetComponent<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
            _healthLabel = CreateLabel("HealthLabel");
            _blockLabel = CreateLabel("KavachLabel");
            ApplyStyle();
            Refresh();
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
                _combatant.Damaged += HandleDamaged;
                _combatant.BlockChanged += HandleCombatantChanged;
                _combatant.Died += HandleCombatantChanged;
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
            _combatant.BlockChanged -= HandleCombatantChanged;
            _combatant.Died -= HandleCombatantChanged;
            _combatant = null;
        }

        private void HandleDamaged(Combatant combatant, DamageResult result)
        {
            Refresh();
        }

        private void HandleCombatantChanged(Combatant combatant)
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

            ApplyTint(!_combatant.IsAlive);
        }

        private void ApplyStyle()
        {
            if (_style == null || _healthLabel == null)
            {
                return;
            }

            StyleLabel(_healthLabel, _style.HealthColor, _style.HealthOffset);
            StyleLabel(_blockLabel, _style.BlockColor, _style.BlockOffset);

            if (_combatant != null)
            {
                ApplyTint(!_combatant.IsAlive);
            }
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

        private void ApplyTint(bool fallen)
        {
            if (_body == null)
            {
                return;
            }

            if (!fallen || _style == null)
            {
                _body.SetPropertyBlock(null);
                return;
            }

            _propertyBlock.SetColor(BaseColorId, _style.FallenTint);
            _body.SetPropertyBlock(_propertyBlock);
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
