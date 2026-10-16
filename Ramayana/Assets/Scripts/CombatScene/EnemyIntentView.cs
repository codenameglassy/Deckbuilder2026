// Shows an enemy's next move above it: an attack row ("7", or "4x2" for multi-hit) and a Kavach row, each with an
// optional icon from the feedback style. Damage is previewed through the combat resolver, so modifiers will show.
// Numbers and icons only, so it needs no localization. It refreshes when the enemy's intent changes and hides when
// the enemy falls. CombatController binds it to the enemy on the same object.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Enemy Intent View")]
    public sealed class EnemyIntentView : MonoBehaviour
    {
        [SerializeField] private CombatFeedbackStyleSO _style;

        private Transform _root;
        private Transform _attackRow;
        private Transform _defendRow;
        private TextMeshPro _attackLabel;
        private TextMeshPro _defendLabel;
        private SpriteRenderer _attackIcon;
        private SpriteRenderer _defendIcon;
        private EnemyCombatant _enemy;
        private CombatModel _model;

        private void Awake()
        {
            var rootObject = new GameObject("Intent");
            _root = rootObject.transform;
            _root.SetParent(transform, false);

            _attackRow = CreateRow("Attack", out _attackIcon, out _attackLabel);
            _defendRow = CreateRow("Defend", out _defendIcon, out _defendLabel);

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

        public void Bind(EnemyCombatant enemy, CombatModel model)
        {
            Unbind();
            _enemy = enemy;
            _model = model;

            if (_enemy != null)
            {
                _enemy.IntentChanged += HandleIntentChanged;
                _enemy.Fighter.Died += HandleDied;
            }

            Refresh();
        }

        public void Unbind()
        {
            if (_enemy == null)
            {
                return;
            }

            _enemy.IntentChanged -= HandleIntentChanged;
            _enemy.Fighter.Died -= HandleDied;
            _enemy = null;
            _model = null;
        }

        private void HandleIntentChanged(EnemyCombatant enemy)
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
            if (_root == null)
            {
                return;
            }

            if (_enemy == null || _model == null || !_enemy.IsAlive)
            {
                _root.gameObject.SetActive(false);
                return;
            }

            MoveIntent intent = _enemy.GetIntent(_model.Resolver, _model.Player);
            _root.gameObject.SetActive(!intent.IsEmpty);
            _attackRow.gameObject.SetActive(intent.HasAttack);
            _defendRow.gameObject.SetActive(intent.HasBlock);

            if (intent.HasAttack)
            {
                if (intent.ShowsHits)
                {
                    _attackLabel.SetText("{0}x{1}", (float)intent.DamagePerHit, (float)intent.Hits);
                }
                else
                {
                    _attackLabel.SetText("{0}", (float)intent.TotalDamage);
                }
            }

            if (intent.HasBlock)
            {
                _defendLabel.SetText("{0}", (float)intent.Block);
            }

            float spacing = _style != null ? _style.IntentRowSpacing : 0f;
            _attackRow.localPosition = intent.HasBlock ? new Vector3(0f, spacing, 0f) : Vector3.zero;
            _defendRow.localPosition = Vector3.zero;
        }

        private void ApplyStyle()
        {
            if (_style == null || _root == null)
            {
                return;
            }

            _root.localPosition = _style.IntentOffset;
            StyleRow(_attackIcon, _attackLabel, _style.AttackIcon, _style.AttackColor);
            StyleRow(_defendIcon, _defendLabel, _style.DefendIcon, _style.DefendColor);
        }

        private void StyleRow(SpriteRenderer icon, TextMeshPro label, Sprite sprite, Color color)
        {
            float iconSize = _style.IntentIconSize;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.transform.localPosition = new Vector3(-iconSize, 0f, 0f);
            icon.transform.localScale = Vector3.one * iconSize;

            if (_style.IntentFont != null)
            {
                label.font = _style.IntentFont;
            }

            label.fontSize = _style.IntentFontSize;
            label.color = color;
            label.rectTransform.sizeDelta = _style.IntentLabelSize;
            label.transform.localPosition = sprite != null ? new Vector3(iconSize * 0.5f, 0f, 0f) : Vector3.zero;
        }

        private Transform CreateRow(string rowName, out SpriteRenderer icon, out TextMeshPro label)
        {
            var row = new GameObject(rowName).transform;
            row.SetParent(_root, false);

            var iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row, false);
            icon = iconObject.AddComponent<SpriteRenderer>();

            var labelObject = new GameObject("Value");
            labelObject.transform.SetParent(row, false);
            label = labelObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return row;
        }
    }
}
