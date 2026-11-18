// How statuses show on fighters: one row per status (its looping aura and its apply accent), listed in priority order.
// A fighter shows at most Max Auras Per Fighter auras at once: SelectAuras walks the rows from the top and picks the
// statuses the fighter holds that have an aura, skipping a row whose aura prefab is already picked, so statuses that
// share one aura (the powers) show it once; the rest are left to the status chips. An aura looks the same at any
// stack count. Find returns a status's row for its apply accent. Shared flyweight with no runtime state; Changed
// (Editor only) lets the aura presenter re-select live.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Statuses;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat VFX/Status VFX Profile", fileName = "StatusVfx")]
    public sealed class StatusVfxProfileSO : ScriptableObject
    {
        [Tooltip("One row per status, highest priority first.")]
        [SerializeField] private StatusVfxEntry[] _entries = Array.Empty<StatusVfxEntry>();
        [Tooltip("Most auras one fighter shows at once; lower rows wait until a higher one ends.")]
        [SerializeField, Min(0)] private int _maxAurasPerFighter = 2;

        public event Action Changed;

        public int Count => _entries.Length;

        public int MaxAurasPerFighter => _maxAurasPerFighter;

        public StatusVfxEntry EntryAt(int index)
        {
            return _entries[index];
        }

        public StatusVfxEntry Find(StatusDefinitionSO status)
        {
            if (status == null)
            {
                return null;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                StatusVfxEntry entry = _entries[i];
                if (entry != null && entry.Status == status)
                {
                    return entry;
                }
            }

            return null;
        }

        // Fills chosen with the row indices whose auras the fighter should show, in priority order.
        public void SelectAuras(StatusSet statuses, List<int> chosen)
        {
            chosen.Clear();
            if (statuses == null)
            {
                return;
            }

            for (int i = 0; i < _entries.Length && chosen.Count < _maxAurasPerFighter; i++)
            {
                StatusVfxEntry entry = _entries[i];
                if (entry == null || entry.Status == null || entry.Aura == null || !entry.Aura.IsEnabled
                    || !statuses.Has(entry.Status) || IsPrefabChosen(chosen, entry.Aura.Prefab))
                {
                    continue;
                }

                chosen.Add(i);
            }
        }

        public void CollectApplyAccents(List<PrefabVfxLook> looks)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i] != null)
                {
                    looks.Add(_entries[i].ApplyAccent);
                }
            }
        }

        private bool IsPrefabChosen(List<int> chosen, GameObject prefab)
        {
            for (int i = 0; i < chosen.Count; i++)
            {
                if (_entries[chosen[i]].Aura.Prefab == prefab)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
