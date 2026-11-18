// Shows what the card the player is considering would do, before it is played. It dry-runs the card through
// CombatModel.PreviewPlay and draws each affected fighter's outcome on their health bar (CombatantView.ShowOutcome):
// ghost damage on enemies, broken or gained Kavach, and a lethal tag. A card that needs a chosen target previews on
// the highlighted enemy while targeting, or on the only living enemy when there is one; untargeted cards preview at
// once (an attack on every enemy, Kavach on Rama). It re-runs when the card, the highlighted enemy, a fighter's
// health or Kavach, or the damage modifiers change, and clears outside the player's turn. PreviewChanged lets the
// HUD show the threat panel as it would be after the card (Outcome while IsActive). Statuses the card would apply or
// spend show on each fighter's status chips too (CombatantView.ShowStatusPreview), even when the card changes no
// health or Kavach, and such a card also counts as an active preview. A special arrow being considered in the quiver
// (SetArrow) previews the same way through CombatModel.PreviewArrow and takes priority over the card; it re-runs when
// the quiver changes too. It only presents.
using System;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.CombatScene;
using JourneyToLanka.Localization;
using UnityEngine.Localization;

namespace JourneyToLanka.CombatUI
{
    internal sealed class OutcomePreviewPresenter : IDisposable
    {
        private readonly CombatOutcomePreview _outcome = new CombatOutcomePreview();
        private readonly LocalizedString _lethalString = new LocalizedString(LocalizationTableNames.UI, CombatUIKeys.Lethal);
        private readonly Action<int> _targetChanged;
        private readonly Action<CombatPhase> _phaseChanged;
        private readonly Action<Combatant> _blockChanged;
        private readonly Action<Combatant, DamageResult> _damaged;
        private readonly Action _modifiersChanged;
        private readonly Action<Quiver> _quiverChanged;
        private readonly LocalizedString.ChangeHandler _lethalChanged;
        private CombatFlow _flow;
        private CombatController _controller;
        private TargetingPresenter _targeting;
        private CardInstance _card;
        private int _arrowSlot = -1;
        private string _lethalLabel = string.Empty;
        private int _targetIndex = -1;
        private bool _active;

        public OutcomePreviewPresenter()
        {
            _targetChanged = HandleTargetChanged;
            _phaseChanged = _ => Refresh();
            _blockChanged = _ => Refresh();
            _damaged = (_, __) => Refresh();
            _modifiersChanged = Refresh;
            _quiverChanged = _ => Refresh();
            _lethalChanged = HandleLethalChanged;
            _lethalString.StringChanged += _lethalChanged;
        }

        public event Action PreviewChanged;

        public bool IsActive => _active;

        public CombatOutcomePreview Outcome => _outcome;

        public void Attach(CombatFlow flow, CombatController controller, TargetingPresenter targeting)
        {
            Detach();
            _flow = flow;
            _controller = controller;
            _targeting = targeting;
            CombatModel model = flow.Model;

            flow.PhaseChanged += _phaseChanged;
            model.Resolver.ModifiersChanged += _modifiersChanged;
            model.Quiver.Changed += _quiverChanged;
            model.Player.BlockChanged += _blockChanged;
            model.Player.Damaged += _damaged;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                Combatant fighter = model.Enemies[i].Fighter;
                fighter.BlockChanged += _blockChanged;
                fighter.Damaged += _damaged;
            }

            if (_targeting != null)
            {
                _targeting.TargetChanged += _targetChanged;
            }
        }

        public void Detach()
        {
            if (_flow == null)
            {
                return;
            }

            ClearViews();
            CombatModel model = _flow.Model;
            _flow.PhaseChanged -= _phaseChanged;
            model.Resolver.ModifiersChanged -= _modifiersChanged;
            model.Quiver.Changed -= _quiverChanged;
            model.Player.BlockChanged -= _blockChanged;
            model.Player.Damaged -= _damaged;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                Combatant fighter = model.Enemies[i].Fighter;
                fighter.BlockChanged -= _blockChanged;
                fighter.Damaged -= _damaged;
            }

            if (_targeting != null)
            {
                _targeting.TargetChanged -= _targetChanged;
            }

            _flow = null;
            _controller = null;
            _targeting = null;
            _card = null;
            _arrowSlot = -1;
            _targetIndex = -1;
            SetActive(false);
        }

        public void Dispose()
        {
            Detach();
            _lethalString.StringChanged -= _lethalChanged;
            PreviewChanged = null;
        }

        public void SetCard(CardInstance card)
        {
            if (card == _card)
            {
                return;
            }

            _card = card;
            Refresh();
        }

        public void SetArrow(int slot)
        {
            if (slot == _arrowSlot)
            {
                return;
            }

            _arrowSlot = slot;
            Refresh();
        }

        private void HandleTargetChanged(int targetIndex)
        {
            _targetIndex = targetIndex;
            Refresh();
        }

        private void HandleLethalChanged(string value)
        {
            _lethalLabel = value ?? string.Empty;
            Refresh();
        }

        private void Refresh()
        {
            if (_flow == null)
            {
                return;
            }

            CombatModel model = _flow.Model;
            ArrowDefinitionSO arrow = model.Quiver.HasArrow(_arrowSlot) ? model.Quiver[_arrowSlot] : null;
            bool preview = (arrow != null || _card != null) && _flow.IsPlayerTurn;
            if (!preview)
            {
                ClearViews();
                _outcome.Begin(model.Resolver);
                SetActive(false);
                return;
            }

            if (arrow != null)
            {
                model.PreviewArrow(_arrowSlot, ResolveTarget(model, arrow.RequiresChosenTarget), _outcome);
            }
            else
            {
                model.PreviewPlay(_card, ResolveTarget(model, _card.RequiresChosenTarget), _outcome);
            }

            bool statuses = ShowOn(_controller != null ? _controller.PlayerView : null, model.Player);
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                statuses |= ShowOn(_controller != null ? _controller.GetEnemyView(i) : null, model.Enemies[i].Fighter);
            }

            _active = _outcome.Count > 0 || statuses;
            PreviewChanged?.Invoke();
        }

        private int ResolveTarget(CombatModel model, bool requiresChosenTarget)
        {
            if (!requiresChosenTarget)
            {
                return -1;
            }

            if (_targeting != null && _targeting.IsActive && _targetIndex >= 0)
            {
                return _targetIndex;
            }

            int living = 0;
            int only = -1;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                if (model.Enemies[i].IsAlive)
                {
                    living++;
                    only = i;
                }
            }

            return living == 1 ? only : -1;
        }

        private bool ShowOn(CombatantView view, Combatant fighter)
        {
            bool hasStatuses = _outcome.TryGetStatuses(fighter, out StatusSet statuses);
            if (view == null)
            {
                return hasStatuses;
            }

            if (_outcome.TryGet(fighter, out CombatantOutcome outcome) && outcome.HasChange)
            {
                view.ShowOutcome(outcome, _lethalLabel);
            }
            else
            {
                view.ClearOutcome();
            }

            if (hasStatuses)
            {
                view.ShowStatusPreview(statuses);
            }

            return hasStatuses;
        }

        private void ClearViews()
        {
            if (_flow == null || _controller == null)
            {
                return;
            }

            if (_controller.PlayerView != null)
            {
                _controller.PlayerView.ClearOutcome();
            }

            for (int i = 0; i < _flow.Model.Enemies.Count; i++)
            {
                CombatantView view = _controller.GetEnemyView(i);
                if (view != null)
                {
                    view.ClearOutcome();
                }
            }
        }

        private void SetActive(bool active)
        {
            if (_active == active)
            {
                return;
            }

            _active = active;
            PreviewChanged?.Invoke();
        }
    }
}
