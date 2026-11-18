// The fighter info panel: a small panel above a fighter with its localized name, health, Kavach (when it has any), an
// enemy's next move spelled out ("Attack 7" or "Attack 4x2", "Defend 6"), and one line per status it holds with the
// status's name, stacks and description, refreshed whenever any of them changes. It shows for the enemy highlighted
// while targeting first, then for Rama while the inspect input is held (SetSelfInspect, so keyboard and gamepad
// players can read their own statuses), then for whichever fighter is under the mouse, Rama included. It hides when
// the fighter falls. The panel is screen-space UI on its own small canvas: each late tick while shown it projects a
// point above the top of the fighter (through the combat camera anchor) and keeps the panel there, clamped inside the
// screen edges, so it is always fully readable at a constant size whatever the fighter's scale or the aspect ratio.
// It moves the panel only when the point moves. Status lines are created the first time a fighter shows that many
// and reused after. Every value comes from CombatInspectStyleSO; text uses the Combat HUD style's font. It only
// presents.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.CombatScene;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal sealed class EnemyInfoPresenter : ILateTickable, IDisposable
    {
        private const int HealthRow = 0;
        private const int KavachRow = 1;
        private const int AttackRow = 2;
        private const int DefendRow = 3;
        private const int RowCount = 4;
        private const int NoSubject = -1;
        private const int PlayerSubject = -2;
        private const float NameLineHeight = 1.4f;
        private const float MoveThresholdSquared = 0.25f;

        private readonly RectTransform _canvasRect;
        private readonly RectTransform _panel;
        private readonly Image _background;
        private readonly VerticalLayoutGroup _layout;
        private readonly TextMeshProUGUI _name;
        private readonly LayoutElement _nameElement;
        private readonly LocalizedText _nameText;
        private readonly RectTransform[] _rows = new RectTransform[RowCount];
        private readonly LayoutElement[] _rowElements = new LayoutElement[RowCount];
        private readonly TextMeshProUGUI[] _captions = new TextMeshProUGUI[RowCount];
        private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[RowCount];
        private readonly LocalizedText[] _captionTexts = new LocalizedText[RowCount];
        private readonly List<StatusInfoLine> _statusLines = new List<StatusInfoLine>(6);
        private readonly Action<EnemyTargetView> _hovered;
        private readonly Action<EnemyTargetView> _unhovered;
        private readonly Action<CombatantView> _playerHovered;
        private readonly Action<CombatantView> _playerUnhovered;
        private readonly Action<int> _targetChanged;
        private readonly Action<EnemyCombatant> _intentChanged;
        private readonly Action<Combatant, DamageResult> _damaged;
        private readonly Action<Combatant, int> _healed;
        private readonly Action<Combatant> _changed;
        private readonly Action _modifiersChanged;
        private CombatHudStyleSO _hudStyle;
        private CombatInspectStyleSO _style;
        private CombatFlow _flow;
        private CombatController _controller;
        private TargetingPresenter _targeting;
        private CombatCameraAnchorSO _cameraAnchor;
        private ITickService _ticks;
        private Combatant _shownFighter;
        private EnemyCombatant _shownEnemy;
        private EnemyTargetView _shownTarget;
        private CombatantView _shownView;
        private Vector2 _lastPosition;
        private bool _registered;
        private bool _selfInspect;
        private int _shownSubject = NoSubject;
        private int _hoveredSubject = NoSubject;
        private int _targetedIndex = -1;

        private EnemyInfoPresenter(RectTransform canvasRect, RectTransform panel, Image background, VerticalLayoutGroup layout,
            TextMeshProUGUI name, LayoutElement nameElement)
        {
            _canvasRect = canvasRect;
            _panel = panel;
            _background = background;
            _layout = layout;
            _name = name;
            _nameElement = nameElement;
            _nameText = new LocalizedText(name);
            _hovered = HandleHovered;
            _unhovered = HandleUnhovered;
            _playerHovered = HandlePlayerHovered;
            _playerUnhovered = HandlePlayerUnhovered;
            _targetChanged = HandleTargetChanged;
            _intentChanged = _ => Refresh();
            _damaged = (_, __) => Refresh();
            _healed = (_, __) => Refresh();
            _changed = _ => Refresh();
            _modifiersChanged = Refresh;
        }

        public static EnemyInfoPresenter Create(RectTransform canvasRect)
        {
            Image background = UIBuildUtility.CreateImage("EnemyInfo", canvasRect, false);
            RectTransform panel = background.rectTransform;
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI name = UIBuildUtility.CreateText("Name", panel, TextAlignmentOptions.Center);
            LayoutElement nameElement = name.gameObject.AddComponent<LayoutElement>();

            var presenter = new EnemyInfoPresenter(canvasRect, panel, background, layout, name, nameElement);
            presenter.CreateRow(HealthRow, "Health", CombatUIKeys.InfoHealth);
            presenter.CreateRow(KavachRow, "Kavach", CombatUIKeys.InfoKavach);
            presenter.CreateRow(AttackRow, "Attack", CombatUIKeys.IntentAttack);
            presenter.CreateRow(DefendRow, "Defend", CombatUIKeys.IntentDefend);

            panel.gameObject.SetActive(false);
            return presenter;
        }

        public void ApplyStyle(CombatHudStyleSO hudStyle, CombatInspectStyleSO style)
        {
            _hudStyle = hudStyle;
            _style = style;
            if (_hudStyle == null || _style == null)
            {
                return;
            }

            int padding = Mathf.RoundToInt(style.InfoPadding);
            _layout.padding = new RectOffset(padding, padding, padding, padding);
            _layout.spacing = style.InfoSpacing;
            _panel.sizeDelta = new Vector2(style.InfoWidth, _panel.sizeDelta.y);
            UIBuildUtility.StyleImage(_background, style.InfoBackgroundSprite, style.InfoBackgroundColor);

            UIBuildUtility.StyleText(_name, hudStyle.Font, style.InfoNameFontSize, style.InfoNameColor);
            _nameElement.preferredHeight = style.InfoNameFontSize * NameLineHeight;

            for (int i = 0; i < RowCount; i++)
            {
                _rowElements[i].preferredHeight = style.InfoRowHeight;
                UIBuildUtility.StyleText(_captions[i], hudStyle.Font, style.InfoRowFontSize, style.InfoTextColor);
                UIBuildUtility.StyleText(_values[i], hudStyle.Font, style.InfoRowFontSize, style.InfoTextColor);
            }

            _values[HealthRow].color = style.InfoHealthColor;
            _values[KavachRow].color = style.InfoKavachColor;
            _values[AttackRow].color = style.InfoAttackColor;
            _values[DefendRow].color = style.InfoDefendColor;

            for (int i = 0; i < _statusLines.Count; i++)
            {
                _statusLines[i].ApplyStyle(hudStyle, style);
            }

            if (_shownFighter != null)
            {
                Refresh();
            }
        }

        public void Attach(CombatFlow flow, CombatController controller, TargetingPresenter targeting, CombatCameraAnchorSO cameraAnchor)
        {
            Detach();
            _flow = flow;
            _controller = controller;
            _targeting = targeting;
            _cameraAnchor = cameraAnchor;

            for (int i = 0; i < flow.Model.Enemies.Count; i++)
            {
                EnemyTargetView target = controller != null ? controller.GetEnemyTarget(i) : null;
                if (target != null)
                {
                    target.Hovered += _hovered;
                    target.Unhovered += _unhovered;
                }
            }

            if (controller != null && controller.PlayerView != null)
            {
                controller.PlayerView.PointerEntered += _playerHovered;
                controller.PlayerView.PointerExited += _playerUnhovered;
            }

            if (_targeting != null)
            {
                _targeting.TargetChanged += _targetChanged;
            }
        }

        public void Detach()
        {
            Hide();
            if (_flow == null)
            {
                return;
            }

            for (int i = 0; i < _flow.Model.Enemies.Count; i++)
            {
                EnemyTargetView target = _controller != null ? _controller.GetEnemyTarget(i) : null;
                if (target != null)
                {
                    target.Hovered -= _hovered;
                    target.Unhovered -= _unhovered;
                }
            }

            if (_controller != null && _controller.PlayerView != null)
            {
                _controller.PlayerView.PointerEntered -= _playerHovered;
                _controller.PlayerView.PointerExited -= _playerUnhovered;
            }

            if (_targeting != null)
            {
                _targeting.TargetChanged -= _targetChanged;
            }

            _flow = null;
            _controller = null;
            _targeting = null;
            _cameraAnchor = null;
            _hoveredSubject = NoSubject;
            _targetedIndex = -1;
        }

        public void Dispose()
        {
            Detach();
            _nameText.Clear();
            for (int i = 0; i < RowCount; i++)
            {
                _captionTexts[i]?.Clear();
            }

            for (int i = 0; i < _statusLines.Count; i++)
            {
                _statusLines[i].Dispose();
            }
        }

        public void SetSelfInspect(bool held)
        {
            if (_selfInspect == held)
            {
                return;
            }

            _selfInspect = held;
            UpdateShown();
        }

        public void LateTick(float deltaTime)
        {
            Reposition(false);
        }

        private void HandleHovered(EnemyTargetView target)
        {
            _hoveredSubject = target.EnemyIndex;
            UpdateShown();
        }

        private void HandleUnhovered(EnemyTargetView target)
        {
            if (_hoveredSubject == target.EnemyIndex)
            {
                _hoveredSubject = NoSubject;
                UpdateShown();
            }
        }

        private void HandlePlayerHovered(CombatantView view)
        {
            _hoveredSubject = PlayerSubject;
            UpdateShown();
        }

        private void HandlePlayerUnhovered(CombatantView view)
        {
            if (_hoveredSubject == PlayerSubject)
            {
                _hoveredSubject = NoSubject;
                UpdateShown();
            }
        }

        private void HandleTargetChanged(int targetIndex)
        {
            _targetedIndex = targetIndex;
            UpdateShown();
        }

        private void UpdateShown()
        {
            int subject = _targetedIndex >= 0 ? _targetedIndex : _selfInspect ? PlayerSubject : _hoveredSubject;
            if (!CanShow(subject))
            {
                Hide();
                return;
            }

            if (subject != _shownSubject)
            {
                Show(subject);
            }
        }

        private bool CanShow(int subject)
        {
            if (_flow == null || _style == null || _hudStyle == null || _controller == null)
            {
                return false;
            }

            CombatModel model = _flow.Model;
            if (subject == PlayerSubject)
            {
                return model.Player.IsAlive && _controller.PlayerView != null;
            }

            return subject >= 0 && subject < model.Enemies.Count && model.Enemies[subject].IsAlive
                && _controller.GetEnemyTarget(subject) != null;
        }

        private void Show(int subject)
        {
            Unsubscribe();
            CombatModel model = _flow.Model;
            _shownSubject = subject;
            if (subject == PlayerSubject)
            {
                _shownFighter = model.Player;
                _shownView = _controller.PlayerView;
            }
            else
            {
                _shownEnemy = model.Enemies[subject];
                _shownFighter = _shownEnemy.Fighter;
                _shownTarget = _controller.GetEnemyTarget(subject);
                _shownEnemy.IntentChanged += _intentChanged;
            }

            _shownFighter.Damaged += _damaged;
            _shownFighter.Healed += _healed;
            _shownFighter.BlockChanged += _changed;
            _shownFighter.Died += _changed;
            model.Resolver.ModifiersChanged += _modifiersChanged;

            _panel.gameObject.SetActive(true);
            Refresh();
            Reposition(true);
            SetRegistered(true);
        }

        private void Hide()
        {
            Unsubscribe();
            SetRegistered(false);
            _shownSubject = NoSubject;
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }
        }

        private void Unsubscribe()
        {
            if (_shownEnemy != null)
            {
                _shownEnemy.IntentChanged -= _intentChanged;
            }

            if (_shownFighter != null)
            {
                _shownFighter.Damaged -= _damaged;
                _shownFighter.Healed -= _healed;
                _shownFighter.BlockChanged -= _changed;
                _shownFighter.Died -= _changed;
                if (_flow != null)
                {
                    _flow.Model.Resolver.ModifiersChanged -= _modifiersChanged;
                }
            }

            _shownFighter = null;
            _shownEnemy = null;
            _shownTarget = null;
            _shownView = null;
        }

        private void Reposition(bool force)
        {
            if (_shownFighter == null || _style == null || _cameraAnchor == null || !_cameraAnchor.TryGetValue(out Camera camera))
            {
                return;
            }

            Vector3 top;
            if (_shownTarget != null)
            {
                top = _shownTarget.TopCenter;
            }
            else if (_shownView != null)
            {
                top = _shownView.TopCenter;
            }
            else
            {
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(top + Vector3.up * _style.InfoWorldLift);
            if (screen.z <= 0f)
            {
                return;
            }

            Vector2 position = (Vector2)_canvasRect.InverseTransformPoint(new Vector3(screen.x, screen.y, 0f)) + _style.InfoScreenOffset;
            Rect bounds = _canvasRect.rect;
            Vector2 size = _panel.rect.size;
            float margin = _style.InfoScreenMargin;
            float halfWidth = size.x * 0.5f;

            if (bounds.width > size.x + margin * 2f)
            {
                position.x = Mathf.Clamp(position.x, bounds.xMin + margin + halfWidth, bounds.xMax - margin - halfWidth);
            }

            if (bounds.height > size.y + margin * 2f)
            {
                position.y = Mathf.Clamp(position.y, bounds.yMin + margin, bounds.yMax - margin - size.y);
            }

            if (!force && (position - _lastPosition).sqrMagnitude < MoveThresholdSquared)
            {
                return;
            }

            _lastPosition = position;
            _panel.localPosition = position;
        }

        private void SetRegistered(bool registered)
        {
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

        private void Refresh()
        {
            if (_shownFighter == null || _flow == null)
            {
                return;
            }

            if (!_shownFighter.IsAlive)
            {
                Hide();
                return;
            }

            CombatModel model = _flow.Model;
            Combatant fighter = _shownFighter;
            _nameText.Set(LocalizationTableNames.Combat, fighter.NameKey);
            _values[HealthRow].SetText("{0}/{1}", (float)fighter.Health, (float)fighter.MaxHealth);

            bool hasKavach = fighter.Block > 0;
            _rows[KavachRow].gameObject.SetActive(hasKavach);
            if (hasKavach)
            {
                _values[KavachRow].SetText("{0}", (float)fighter.Block);
            }

            MoveIntent intent = _shownEnemy != null ? _shownEnemy.GetIntent(model.Resolver, model.Player) : default;
            _rows[AttackRow].gameObject.SetActive(intent.HasAttack);
            if (intent.HasAttack)
            {
                if (intent.ShowsHits)
                {
                    _values[AttackRow].SetText("{0}x{1}", (float)intent.DamagePerHit, (float)intent.Hits);
                }
                else
                {
                    _values[AttackRow].SetText("{0}", (float)intent.TotalDamage);
                }
            }

            _rows[DefendRow].gameObject.SetActive(intent.HasBlock);
            if (intent.HasBlock)
            {
                _values[DefendRow].SetText("{0}", (float)intent.Block);
            }

            RefreshStatuses(fighter.Statuses);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
        }

        private void RefreshStatuses(StatusSet statuses)
        {
            int count = statuses.Count;
            while (_statusLines.Count < count)
            {
                StatusInfoLine line = StatusInfoLine.Create(_panel);
                line.ApplyStyle(_hudStyle, _style);
                _statusLines.Add(line);
            }

            for (int i = 0; i < _statusLines.Count; i++)
            {
                if (i < count)
                {
                    _statusLines[i].Show(statuses[i]);
                }
                else
                {
                    _statusLines[i].Hide();
                }
            }
        }

        private void CreateRow(int index, string rowName, string captionKey)
        {
            RectTransform row = UIBuildUtility.CreateRect(rowName, _panel);
            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();

            TextMeshProUGUI caption = UIBuildUtility.CreateText("Caption", row, TextAlignmentOptions.MidlineLeft);
            UIBuildUtility.Stretch(caption.rectTransform);
            TextMeshProUGUI value = UIBuildUtility.CreateText("Value", row, TextAlignmentOptions.MidlineRight);
            UIBuildUtility.Stretch(value.rectTransform);

            _rows[index] = row;
            _rowElements[index] = element;
            _captions[index] = caption;
            _values[index] = value;
            _captionTexts[index] = new LocalizedText(caption);
            _captionTexts[index].Set(LocalizationTableNames.UI, captionKey);
        }
    }
}
