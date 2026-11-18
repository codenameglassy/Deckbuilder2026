// How one status shows on a fighter: Aura loops on every fighter holding the status for as long as they hold it (it
// should be a looping prefab; a one-shot ends on its own), and Apply Accent plays each time the status is applied
// (gained or added to, not when it is spent or wears off). Either may be empty. A serializable row of
// StatusVfxProfileSO, read-only at runtime.
using System;
using JourneyToLanka.Combat.Statuses;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public sealed class StatusVfxEntry
    {
        [SerializeField] private StatusDefinitionSO _status;
        [Tooltip("Loops on the fighter while the status lasts, placed like any accent and kept on the fighter as it moves.")]
        [SerializeField] private PrefabVfxLook _aura = new PrefabVfxLook();
        [Tooltip("Plays each time the status is applied.")]
        [SerializeField] private PrefabVfxLook _applyAccent = new PrefabVfxLook();

        public StatusDefinitionSO Status => _status;

        public PrefabVfxLook Aura => _aura;

        public PrefabVfxLook ApplyAccent => _applyAccent;
    }
}
