// Shows an enemy's next move above it: an attack row ("7", or "4x2" for multi-hit), a buff row and a debuff row for
// statuses the move applies (the first of each, with its amount), and a Kavach row, stacked bottom to top as Kavach,
// debuff, buff, attack with only the rows the move uses. How a row arranges its icon, number and plate is the style's
// IntentLayoutSO strategy (a large icon with the number on its corner, or icon and number on a fitted plate), and the
// number can use the style's outlined material so it reads against any background. The lowest row sits a fixed number
// of reference pixels above the fighter's health bar (CombatantView on the same object), falling back to the style's
// intent offset when there is no bar or the gap is negative. The attack icon follows the style's ladder by
// total damage (a dagger, a sword, a gada); the Kavach row uses the style's shield; a status row uses the status's
// icon, or until it has one the style's status disc in the status's colour. Icons are sized by their own bounds, so a
// sprite of any resolution or pixels-per-unit shows at the style's icon size. Damage is previewed through the combat
// resolver, so statuses and other modifiers show. Numbers and icons only, so it needs no localization. It refreshes
// when the enemy's intent changes (and then swells briefly, a DOTween core punch on its own pop transform), when the
// damage modifiers change (any status gained, spent or worn off), and hides when the enemy falls. Rows are measured
// and laid out only on refresh, never per frame. CombatController binds it to the enemy on the same object. The intent
// keeps a uniform world scale under an unevenly scaled stand-in (a tall cube), so its numbers and icons are never
// stretched, and while enabled it late-ticks to stay the same size on screen through the style's camera anchor
// (ScreenScaleKeeper); with Intent Screen Scale at zero it keeps its world size instead.
using DG.Tweening;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Enemy Intent View")]
    public sealed class EnemyIntentView : MonoBehaviour, ILateTickable
    {
        private const int PlateSortingOrder = -1;
        private const int PopVibrato = 6;
        private const float PopElasticity = 0.5f;

        [SerializeField] private CombatFeedbackStyleSO _style;

        private Transform _root;
        private Transform _pop;
        private IntentRow _attackRow;
        private IntentRow _buffRow;
        private IntentRow _debuffRow;
        private IntentRow _defendRow;
        private EnemyCombatant _enemy;
        private CombatModel _model;
        private Tween _popTween;
        private ScreenScaleKeeper _screenScale;
        private ITickService _ticks;
        private bool _lateRegistered;
        private CombatantView _bar;
        private IntentLayoutSO _layout;

        private void Awake()
        {
            var rootObject = new GameObject("Intent");
            _root = rootObject.transform;
            _root.SetParent(transform, false);

            var popObject = new GameObject("Pop");
            _pop = popObject.transform;
            _pop.SetParent(_root, false);

            _attackRow = CreateRow("Attack");
            _buffRow = CreateRow("Buff");
            _debuffRow = CreateRow("Debuff");
            _defendRow = CreateRow("Defend");
            _screenScale = new ScreenScaleKeeper(_root);
            TryGetComponent(out _bar);

            ApplyStyle();
            Refresh();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _ticks);
            SetLateRegistered(isActiveAndEnabled);
        }

        private void OnEnable()
        {
            if (_style != null)
            {
                _style.Changed += HandleStyleChanged;
            }

            ListenToLayout(true);
            SetLateRegistered(true);
        }

        private void OnDisable()
        {
            if (_style != null)
            {
                _style.Changed -= HandleStyleChanged;
            }

            ListenToLayout(false);
            SetLateRegistered(false);
        }

        // After this frame's motion: keeps the intent the same size on screen, then sits its lowest row a fixed number
        // of reference pixels above the top of the fighter's health bar. Skipped while the intent is hidden.
        public void LateTick(float deltaTime)
        {
            if (_style == null || _root == null || !_root.gameObject.activeSelf)
            {
                return;
            }

            CombatCameraAnchorSO anchor = _style.CameraAnchor;
            if (anchor == null || !anchor.TryGetValue(out Camera camera))
            {
                return;
            }

            if (_style.IntentScreenScale > 0f)
            {
                _screenScale.Apply(camera, _style.IntentScreenScale);
            }

            if (_style.IntentBarGap < 0f || _bar == null || !_bar.TryGetBarTop(out Vector3 barTop))
            {
                return;
            }

            float gap = _style.IntentBarGap * ScreenScaleKeeper.WorldPerReferencePixel(camera, barTop);
            IntentLayoutSO layout = _style.IntentLayout;
            float halfHeight = layout != null ? layout.RowHalfHeight(_style) : _style.IntentIconSize * 0.5f;
            _root.position = barTop + Vector3.up * (gap + halfHeight * _root.lossyScale.y);
        }

        private void ListenToLayout(bool listen)
        {
            if (_layout != null)
            {
                _layout.Changed -= HandleStyleChanged;
            }

            _layout = listen && _style != null ? _style.IntentLayout : null;
            if (_layout != null)
            {
                _layout.Changed += HandleStyleChanged;
            }
        }

        private void SetLateRegistered(bool registered)
        {
            if (_ticks == null || _lateRegistered == registered)
            {
                return;
            }

            _lateRegistered = registered;
            if (registered)
            {
                _ticks.AddLateTickable(this);
            }
            else
            {
                _ticks.RemoveLateTickable(this);
            }
        }

        private void OnDestroy()
        {
            StopPop();
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
            PlayPop();
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
            ListenToLayout(isActiveAndEnabled);
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
            _attackRow.Root.gameObject.SetActive(intent.HasAttack);
            _buffRow.Root.gameObject.SetActive(intent.HasBuff);
            _debuffRow.Root.gameObject.SetActive(intent.HasDebuff);
            _defendRow.Root.gameObject.SetActive(intent.HasBlock);
            if (_style == null)
            {
                return;
            }

            if (intent.HasAttack)
            {
                if (intent.ShowsHits)
                {
                    _attackRow.Label.SetText("{0}x{1}", (float)intent.DamagePerHit, (float)intent.Hits);
                }
                else
                {
                    _attackRow.Label.SetText("{0}", (float)intent.TotalDamage);
                }

                Sprite icon = _style.AttackIconFor(intent.TotalDamage, out float scale);
                SetIcon(_attackRow, icon, Color.white, scale);
                Layout(_attackRow);
            }

            if (intent.HasBuff)
            {
                ShowStatus(_buffRow, intent.FirstBuff, intent.FirstBuffAmount);
            }

            if (intent.HasDebuff)
            {
                ShowStatus(_debuffRow, intent.FirstDebuff, intent.FirstDebuffAmount);
            }

            if (intent.HasBlock)
            {
                _defendRow.Label.SetText("{0}", (float)intent.Block);
                SetIcon(_defendRow, _style.DefendIcon, Color.white, 1f);
                Layout(_defendRow);
            }

            IntentLayoutSO layout = _style.IntentLayout;
            float spacing = layout != null ? layout.RowSpacing : _style.IntentRowSpacing;
            int stacked = 0;
            stacked = Stack(_defendRow, intent.HasBlock, stacked, spacing);
            stacked = Stack(_debuffRow, intent.HasDebuff, stacked, spacing);
            stacked = Stack(_buffRow, intent.HasBuff, stacked, spacing);
            Stack(_attackRow, intent.HasAttack, stacked, spacing);
        }

        private void ShowStatus(IntentRow row, StatusDefinitionSO status, int amount)
        {
            row.Label.SetText("{0}", (float)amount);
            if (status == null)
            {
                SetIcon(row, null, Color.white, 1f);
                Layout(row);
                return;
            }

            bool hasIcon = status.Icon != null;
            Sprite sprite = hasIcon ? status.Icon : _style.StatusIntentSprite;
            SetIcon(row, sprite, hasIcon ? Color.white : status.Color, 1f);
            row.Label.color = status.Color;
            Layout(row);
        }

        private void SetIcon(IntentRow row, Sprite sprite, Color colour, float scale)
        {
            row.Icon.sprite = sprite;
            row.Icon.enabled = sprite != null;
            row.Icon.color = colour;
            row.IconScale = scale;
        }

        // Hands the row to the style's layout strategy. Without one, the icon sits left of a centred number, no plate.
        private void Layout(IntentRow row)
        {
            IntentLayoutSO layout = _style.IntentLayout;
            if (layout != null)
            {
                layout.Layout(row, _style);
                return;
            }

            row.Plate.enabled = false;
            float iconSize = _style.IntentIconSize * row.IconScale;
            Sprite sprite = row.Icon.sprite;
            float spriteSize = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            row.Icon.transform.localScale = Vector3.one * (iconSize / Mathf.Max(spriteSize, 0.0001f));
            row.Icon.transform.localPosition = new Vector3(-iconSize, 0f, 0f);
            row.Label.transform.localPosition = new Vector3(iconSize * 0.5f, 0f, 0f);
        }

        private void ApplyStyle()
        {
            if (_style == null || _root == null)
            {
                return;
            }

            StopPop();
            _root.localPosition = _style.IntentOffset;
            _root.localScale = CombatantHealthBar.UniformWorldScale(transform, 1f);
            _screenScale?.Reset();
            StyleRow(_attackRow, _style.AttackColor);
            StyleRow(_buffRow, Color.white);
            StyleRow(_debuffRow, Color.white);
            StyleRow(_defendRow, _style.DefendColor);
        }

        private void StyleRow(IntentRow row, Color colour)
        {
            if (_style.IntentFont != null)
            {
                row.Label.font = _style.IntentFont;
            }

            if (_style.IntentNumberMaterial != null)
            {
                row.Label.fontSharedMaterial = _style.IntentNumberMaterial;
            }

            row.Label.fontSize = _style.IntentFontSize;
            row.Label.color = colour;
            row.Label.rectTransform.sizeDelta = _style.IntentLabelSize;

            row.Plate.sprite = _style.IntentPlateSprite;
            row.Plate.color = _style.IntentPlateColor;
            row.Plate.drawMode = SpriteDrawMode.Sliced;
        }

        private void PlayPop()
        {
            if (_style == null || _pop == null || !_root.gameObject.activeInHierarchy || _style.IntentPopScale <= 0f)
            {
                return;
            }

            StopPop();
            _popTween = _pop.DOPunchScale(Vector3.one * _style.IntentPopScale, _style.IntentPopDuration, PopVibrato,
                PopElasticity);
        }

        private void StopPop()
        {
            if (_popTween != null && _popTween.IsActive())
            {
                _popTween.Kill();
            }

            _popTween = null;
            if (_pop != null)
            {
                _pop.localScale = Vector3.one;
            }
        }

        private static int Stack(IntentRow row, bool active, int stacked, float spacing)
        {
            if (!active)
            {
                return stacked;
            }

            row.Root.localPosition = new Vector3(0f, spacing * stacked, 0f);
            return stacked + 1;
        }

        private IntentRow CreateRow(string rowName)
        {
            var root = new GameObject(rowName).transform;
            root.SetParent(_pop, false);

            var plateObject = new GameObject("Plate");
            plateObject.transform.SetParent(root, false);
            var plate = plateObject.AddComponent<SpriteRenderer>();
            plate.sortingOrder = PlateSortingOrder;
            plate.drawMode = SpriteDrawMode.Sliced;

            var iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(root, false);
            var icon = iconObject.AddComponent<SpriteRenderer>();

            var labelObject = new GameObject("Value");
            labelObject.transform.SetParent(root, false);
            var label = labelObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            return new IntentRow(root, plate, icon, label);
        }
    }
}
