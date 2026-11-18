// Marks which of a fighter's renderers wear the Kavach hull, each with its shell mesh. Lives on the fighter's view
// (or a model prefab under it) and is set in the Editor: Wire Selected > Combat VFX fills it for stand-in views and
// bakes their shell meshes, and Wire Selected > Kavach Shell Meshes bakes any rows still missing one. Listing
// renderers explicitly keeps props such as a bow out of the hull and avoids searching the hierarchy at runtime.
// Holds no runtime state; KavachShellPresenter builds the hull from it when a fight starts.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat VFX/Kavach Shell Target")]
    public sealed class KavachShellTarget : MonoBehaviour
    {
        [SerializeField] private KavachShellTargetEntry[] _entries = Array.Empty<KavachShellTargetEntry>();

        public IReadOnlyList<KavachShellTargetEntry> Entries => _entries;
    }
}
