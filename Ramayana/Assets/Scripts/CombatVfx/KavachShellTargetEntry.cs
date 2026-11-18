// One row of KavachShellTarget: a body renderer that gets a Kavach hull (a MeshRenderer with a MeshFilter, or a
// SkinnedMeshRenderer) and the shell copy of its mesh, with extrusion directions baked into UV3 by the Kavach shell
// mesh builder. Serializable; read-only at runtime.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public struct KavachShellTargetEntry
    {
        [SerializeField] private Renderer _source;
        [SerializeField] private Mesh _shellMesh;

        public Renderer Source => _source;

        public Mesh ShellMesh => _shellMesh;

        public bool IsValid => _source != null && _shellMesh != null;
    }
}
