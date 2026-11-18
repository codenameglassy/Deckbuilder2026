// The statuses one fighter holds, kept in damage-rule order so damage can be modified in a single pass without
// sorting. Apply adds to an existing status (capped by its definition, keeping the longer duration) or adds a new
// one; statuses that reach zero are removed. TickOwnerTurnEnd and TickRoundEnd run each status's decay strategy.
// Changed fires whenever anything here changes, so previews and the UI refresh only then. CopyFrom lets a preview
// work on a copy. TriggerOwnerTurnStart and TriggerStatusGained run each status's trigger strategy; a trigger may
// itself change statuses (Vow of the Exile gains Dharma, which Devotion reacts to), so each run first notes which
// statuses to trigger in a reusable buffer per nesting level and skips any that are gone by their turn. Allocates
// only when a fighter first gains more statuses, or nests triggers deeper, than it has room for.
using System;
using System.Collections.Generic;

namespace JourneyToLanka.Combat.Statuses
{
    public sealed class StatusSet
    {
        private readonly List<StatusInstance> _statuses = new List<StatusInstance>(6);
        private readonly List<List<StatusDefinitionSO>> _triggerBuffers = new List<List<StatusDefinitionSO>>(2);
        private int _triggerDepth;

        public event Action<StatusSet> Changed;

        public int Count => _statuses.Count;

        public StatusInstance this[int index] => _statuses[index];

        public int GetAmount(StatusDefinitionSO definition)
        {
            int index = IndexOf(definition);
            return index >= 0 ? _statuses[index].Amount : 0;
        }

        public bool Has(StatusDefinitionSO definition)
        {
            return GetAmount(definition) > 0;
        }

        public void Apply(StatusDefinitionSO definition, int amount, int duration)
        {
            if (definition == null || amount == 0)
            {
                return;
            }

            if (duration <= 0)
            {
                duration = definition.DefaultDuration;
            }

            int index = IndexOf(definition);
            if (index >= 0)
            {
                StatusInstance current = _statuses[index];
                current.Amount = definition.Cap(Math.Max(0, current.Amount + amount));
                current.Duration = Math.Max(current.Duration, duration);
                SetOrRemove(index, current);
            }
            else if (amount > 0)
            {
                Insert(new StatusInstance(definition, definition.Cap(amount), duration));
            }
            else
            {
                return;
            }

            Changed?.Invoke(this);
        }

        public bool Remove(StatusDefinitionSO definition)
        {
            int index = IndexOf(definition);
            if (index < 0)
            {
                return false;
            }

            _statuses.RemoveAt(index);
            Changed?.Invoke(this);
            return true;
        }

        public void Clear()
        {
            if (_statuses.Count == 0)
            {
                return;
            }

            _statuses.Clear();
            Changed?.Invoke(this);
        }

        public void TickOwnerTurnEnd()
        {
            bool changed = false;
            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                StatusInstance status = _statuses[i];
                StatusDecaySO decay = status.Definition.Decay;
                if (decay == null)
                {
                    continue;
                }

                int amount = status.Amount;
                int duration = status.Duration;
                decay.OnOwnerTurnEnd(ref status);
                if (status.Amount != amount || status.Duration != duration)
                {
                    SetOrRemove(i, status);
                    changed = true;
                }
            }

            if (changed)
            {
                Changed?.Invoke(this);
            }
        }

        public void TickRoundEnd(in StatusTickContext context)
        {
            bool changed = false;
            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                if (i >= _statuses.Count)
                {
                    continue;
                }

                StatusInstance status = _statuses[i];
                StatusDecaySO decay = status.Definition.Decay;
                if (decay == null)
                {
                    continue;
                }

                int amount = status.Amount;
                int duration = status.Duration;
                decay.OnRoundEnd(ref status, context);
                int index = IndexOf(status.Definition);
                if (index >= 0 && (status.Amount != amount || status.Duration != duration))
                {
                    SetOrRemove(index, status);
                    changed = true;
                }
            }

            if (changed)
            {
                Changed?.Invoke(this);
            }
        }

        public void TriggerOwnerTurnStart(in StatusTickContext context)
        {
            List<StatusDefinitionSO> triggered = RentTriggered();
            try
            {
                for (int i = 0; i < triggered.Count; i++)
                {
                    int index = IndexOf(triggered[i]);
                    if (index >= 0)
                    {
                        StatusInstance status = _statuses[index];
                        status.Definition.Trigger.OnOwnerTurnStart(status, context);
                    }
                }
            }
            finally
            {
                _triggerDepth--;
            }
        }

        public void TriggerStatusGained(StatusDefinitionSO gained, int amount, in StatusTickContext context)
        {
            if (gained == null || amount <= 0)
            {
                return;
            }

            List<StatusDefinitionSO> triggered = RentTriggered();
            try
            {
                for (int i = 0; i < triggered.Count; i++)
                {
                    int index = IndexOf(triggered[i]);
                    if (index >= 0)
                    {
                        StatusInstance status = _statuses[index];
                        status.Definition.Trigger.OnOwnerGainedStatus(status, gained, amount, context);
                    }
                }
            }
            finally
            {
                _triggerDepth--;
            }
        }

        public void CopyFrom(StatusSet other)
        {
            _statuses.Clear();
            if (other == null)
            {
                return;
            }

            for (int i = 0; i < other._statuses.Count; i++)
            {
                _statuses.Add(other._statuses[i]);
            }
        }

        public int ModifyOutgoing(int amount)
        {
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                StatusDamageRuleSO rule = status.Definition.DamageRule;
                if (rule != null)
                {
                    amount = rule.ModifyOutgoing(amount, status);
                }
            }

            return amount;
        }

        public int ModifyIncoming(int amount)
        {
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusInstance status = _statuses[i];
                StatusDamageRuleSO rule = status.Definition.DamageRule;
                if (rule != null)
                {
                    amount = rule.ModifyIncoming(amount, status);
                }
            }

            return amount;
        }

        private List<StatusDefinitionSO> RentTriggered()
        {
            if (_triggerDepth == _triggerBuffers.Count)
            {
                _triggerBuffers.Add(new List<StatusDefinitionSO>(4));
            }

            List<StatusDefinitionSO> buffer = _triggerBuffers[_triggerDepth];
            _triggerDepth++;
            buffer.Clear();
            for (int i = 0; i < _statuses.Count; i++)
            {
                StatusDefinitionSO definition = _statuses[i].Definition;
                if (definition.Trigger != null)
                {
                    buffer.Add(definition);
                }
            }

            return buffer;
        }

        private void SetOrRemove(int index, StatusInstance status)
        {
            if (status.Amount <= 0)
            {
                _statuses.RemoveAt(index);
            }
            else
            {
                _statuses[index] = status;
            }
        }

        private void Insert(StatusInstance status)
        {
            int order = status.Definition.DamageOrder;
            int index = _statuses.Count;
            for (int i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].Definition.DamageOrder > order)
                {
                    index = i;
                    break;
                }
            }

            _statuses.Insert(index, status);
        }

        private int IndexOf(StatusDefinitionSO definition)
        {
            for (int i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].Definition == definition)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
