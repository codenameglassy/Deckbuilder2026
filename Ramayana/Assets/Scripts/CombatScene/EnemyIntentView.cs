// Shows an enemy's next move above it: an attack row ("7", or "4x2" for multi-hit), a buff row and a debuff row for
// statuses the move applies (the first of each, with its amount), and a Kavach row, stacked bottom to top as Kavach,
// debuff, buff, attack with only the rows the move uses. Attack and Kavach rows use the optional icons from the
// feedback style; a status row uses the status's icon, or until it has one the feedback style's status disc in the
// status's colour. Damage is previewed through the combat resolver, so statuses and other modifiers show. Numbers
// and icons only, so it needs no localization. It refreshes when the enemy's intent changes, when the damage
// modifiers change (any status gained, spent or worn off), and hides when the enemy falls. CombatController binds it
// to the enemy on the same object. The intent keeps a uniform world scale under an unevenly scaled stand-in (a tall
// cube), so its numbers and icons are never stretched.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
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
        private Transform _buffRow;
        private Transform _debuffRow;
        private Transform _defendRow;
        private TextMeshPro _attackLabel;
        private TextMeshPro _buffLabel;
        private TextMeshPro _debuffLabel;
        private TextMeshPro _defendLabel;
        private SpriteRenderer _attackIcon;
        private SpriteRenderer _buffIcon;
        private SpriteRenderer _debuffIcon;
        private SpriteRenderer _defendIcon;
        private EnemyCombatant _enemy;
        private CombatModel _model;

        private void Awake()
        {
            var rootObject = new GameObject("Intent");
            _root = rootObject.transform;
            _root.SetParent(transform, false);

            _attackRow = CreateRow("Attack", out _attackIcon, out _attackLabel);
            _buffRow = CreateRow("Buff", out _buffIcon, out _buffLabel);
            _debuffRow = CreateRow("Debuff", out _debuffIcon, out _debuffLabel);
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

            if (_model != null)
            {
                _model.Resolver.ModifiersChanged += HandleModifiersChanged;
            }

            Refresh();
        }

        public void Unbind()
        {
            if (_model != null)
            {
                _model.Resolver.ModifiersChanged -= HandleModifiersChanged;
            }

            if (_enemy != null)
            {
                _enemy.IntentChanged -= HandleIntentChanged;
                _enemy.Fighter.Died -= HandleDied;
            }

            _enemy = null;
            _model = null;
        }

        private void HandleIntentChanged(EnemyCombatant enemy)
        {
            Refresh();
        }

        private void HandleModifiersChanged()
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
            _buffRow.gameObject.SetActive(intent.HasBuff);
            _debuffRow.gameObject.SetActive(intent.HasDebuff);
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

            if (intent.HasBuff)
            {
                ShowStatus(_buffIcon, _buffLabel, intent.FirstBuff, intent.FirstBuffAmount);
            }

            if (intent.HasDebuff)
            {
                ShowStatus(_debuffIcon, _debuffLabel, intent.FirstDebuff, intent.FirstDebuffAmount);
            }

            if (intent.HasBlock)
            {
                _defendLabel.SetText("{0}", (float)intent.Block);
            }

            float spacing = _style != null ? _style.IntentRowSpacing : 0f;
            int stacked = 0;
            stacked = Stack(_defendRow, intent.HasBlock, stacked, spacing);
            stacked = Stack(_debuffRow, intent.HasDebuff, stacked, spacing);
            stacked = Stack(_buffRow, intent.HasBuff, stacked, spacing);
            Stack(_attackRow, intent.HasAttack, stacked, spacing);
        }

        private void ShowStatus(SpriteRenderer icon, TextMeshPro label, StatusDefinitionSO status, int amount)
        {
            label.SetText("{0}", (float)amount);
            if (_style == null || status == null)
            {
                return;
            }

            bool hasIcon = status.Icon != null;
            Sprite sprite = hasIcon ? status.Icon : _style.StatusIntentSprite;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = hasIcon ? Color.white : status.Color;
            label.color = status.Color;
            float iconSize = _style.IntentIconSize;
            label.transform.localPosition = sprite != null ? new Vector3(iconSize * 0.5f, 0f, 0f) : Vector3.zero;
        }

        private void ApplyStyle()
        {
            if (_style == null || _root == null)
            {
                return;
            }

            _root.localPosition = _style.IntentOffset;
            _root.localScale = CombatantHealthBar.UniformWorldScale(transform, 1f);
            StyleRow(_attackIcon, _attackLabel, _style.AttackIcon, _style.AttackColor);
            StyleRow(_buffIcon, _buffLabel, _style.StatusIntentSprite, Color.white);
            StyleRow(_debuffIcon, _debuffLabel, _style.StatusIntentSprite, Color.white);
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

        private static int Stack(Transform row, bool active, int stacked, float spacing)
        {
            if (!active)
            {
                return stacked;
            }

            row.localPosition = new Vector3(0f, spacing * stacked, 0f);
            return stacked + 1;
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
