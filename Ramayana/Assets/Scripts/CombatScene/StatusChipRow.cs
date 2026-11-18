// The row of status chips under a fighter's health bar, one chip per status the fighter holds, centred under the bar
// in the order the status set keeps (damage statuses first, then counters such as Dharma). It listens to the bound
// StatusSet's Changed event and refreshes only then. Chips are created the first time a fighter holds that many
// statuses and reused from then on, so steady play allocates nothing. Built in code by CombatantView on the bar's
// canvas; every value comes from CombatantViewStyleSO. ShowPreview swaps in the statuses a considered card would
// leave (the preview's own copy): statuses the fighter does not have yet show as ghosts, and ones whose stacks would
// change show their new count in the preview colour. ClearPreview returns to the live statuses.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Statuses;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    internal sealed class StatusChipRow : IDisposable
    {
        private readonly RectTransform _root;
        private readonly List<StatusChip> _chips = new List<StatusChip>(6);
        private readonly Action<StatusSet> _changed;
        private CombatantViewStyleSO _style;
        private StatusSet _statuses;
        private StatusSet _preview;
        private int _shown;

        private StatusChipRow(RectTransform root)
        {
            _root = root;
            _changed = HandleChanged;
        }

        public static StatusChipRow Create(RectTransform bar)
        {
            var rootObject = new GameObject("Statuses", typeof(RectTransform));
            var root = (RectTransform)rootObject.transform;
            root.SetParent(bar, false);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = Vector2.zero;
            return new StatusChipRow(root);
        }

        public void ApplyStyle(CombatantViewStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            _root.anchoredPosition = style.StatusRowOffset;
            for (int i = 0; i < _chips.Count; i++)
            {
                _chips[i].ApplyStyle(style);
            }

            Layout();
        }

        public void Bind(StatusSet statuses)
        {
            if (_statuses != null)
            {
                _statuses.Changed -= _changed;
            }

            _statuses = statuses;
            if (_statuses != null)
            {
                _statuses.Changed += _changed;
            }

            Refresh();
        }

        public void ShowPreview(StatusSet preview)
        {
            _preview = preview;
            Refresh();
        }

        public void ClearPreview()
        {
            if (_preview == null)
            {
                return;
            }

            _preview = null;
            Refresh();
        }

        public void Dispose()
        {
            _preview = null;
            Bind(null);
            for (int i = 0; i < _chips.Count; i++)
            {
                _chips[i].Dispose();
            }
        }

        private void HandleChanged(StatusSet statuses)
        {
            Refresh();
        }

        private void Refresh()
        {
            StatusSet shown = _preview ?? _statuses;
            int count = shown != null ? shown.Count : 0;
            while (_chips.Count < count)
            {
                StatusChip chip = StatusChip.Create(_root);
                chip.ApplyStyle(_style);
                _chips.Add(chip);
            }

            for (int i = 0; i < _chips.Count; i++)
            {
                if (i < count)
                {
                    StatusInstance status = shown[i];
                    bool ghost = false;
                    bool changed = false;
                    if (_preview != null)
                    {
                        int live = _statuses != null ? _statuses.GetAmount(status.Definition) : 0;
                        ghost = live == 0;
                        changed = !ghost && live != status.Amount;
                    }

                    _chips[i].Show(status, ghost, changed);
                }
                else
                {
                    _chips[i].Hide();
                }
            }

            _shown = count;
            Layout();
        }

        private void Layout()
        {
            if (_style == null || _shown == 0)
            {
                return;
            }

            float step = _style.StatusChipSize.x + _style.StatusChipSpacing;
            float start = -0.5f * step * (_shown - 1);
            for (int i = 0; i < _shown; i++)
            {
                _chips[i].SetPosition(start + step * i);
            }
        }
    }
}
