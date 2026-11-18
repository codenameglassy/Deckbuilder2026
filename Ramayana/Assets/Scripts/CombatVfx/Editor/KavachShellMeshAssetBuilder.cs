// Makes the shell meshes the Kavach hull draws. For each KavachShellTarget row with a body renderer and no shell mesh,
// it copies the body's mesh (from its MeshFilter or SkinnedMeshRenderer), bakes the hull's extrusion directions into
// UV3 with KavachShellMeshBaker (smooth normals in tangent space, so hard edges stay closed and the push bends with
// skinning), pads the copy's bounds so the pushed hull is never culled early, and saves it as
// <Mesh>_KavachShell.asset under Data/CombatVfx/ShellMeshes, reusing an existing asset of that name. Source models
// are never changed. A body mesh must be readable (enable Read/Write on its model import); anything else is reported,
// not guessed. Run from Wire Selected > Kavach Shell Meshes on scene objects or prefabs, and by the Combat VFX scene
// wiring for stand-in views. Rows that already have a shell mesh are left alone; changes are recorded with Undo.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class KavachShellMeshAssetBuilder
    {
        public const string ShellMeshesFolder = CombatVfxAssetBuilder.Root + "/ShellMeshes";

        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Kavach Shell Meshes";
        private const string ShellSuffix = "_KavachShell";

        // Object units the shell mesh's bounds grow on every side, covering the push and any gain swell.
        private const float BoundsPadding = 0.5f;

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 33)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Kavach Shell Meshes", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        // Fills every row of this target that has a body renderer but no shell mesh.
        public static void WireTarget(KavachShellTarget target, AssetBuilderReport report)
        {
            if (target == null)
            {
                return;
            }

            IReadOnlyList<KavachShellTargetEntry> entries = target.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                KavachShellTargetEntry entry = entries[i];
                if (entry.Source == null || entry.ShellMesh != null)
                {
                    continue;
                }

                Mesh shellMesh = LoadOrCreateShellMesh(GetBodyMesh(entry.Source), report);
                if (shellMesh != null)
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(target, $"_entries.Array.data[{i}]._shellMesh", shellMesh, report);
                }
            }
        }

        public static Mesh LoadOrCreateShellMesh(Mesh source, AssetBuilderReport report)
        {
            if (source == null)
            {
                report.AddSkipped("Kavach shell mesh (the body renderer has no mesh)");
                return null;
            }

            AssetBuilderUtility.EnsureFolder(ShellMeshesFolder, report);
            string path = ShellMeshesFolder + "/" + source.name + ShellSuffix + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                report.AddExisted(path);
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                report.AddSkipped($"{path} (a different asset type occupies this path)");
                return null;
            }

            if (!source.isReadable)
            {
                report.AddSkipped($"{path} ({source.name} is not readable; enable Read/Write on its model import and run again)");
                return null;
            }

            Mesh shell = Object.Instantiate(source);
            shell.name = source.name + ShellSuffix;

            Vector3[] positions = shell.vertices;
            Vector3[] normals = shell.normals;
            if (normals.Length != positions.Length)
            {
                shell.RecalculateNormals();
                normals = shell.normals;
            }

            Vector4[] tangents = shell.tangents;
            if (tangents.Length != positions.Length)
            {
                tangents = BuildTangents(shell, normals);
                shell.tangents = tangents;
            }

            Vector3[] smoothed = KavachShellMeshBaker.SmoothNormals(positions, normals);
            shell.SetUVs(3, KavachShellMeshBaker.ToTangentSpace(smoothed, normals, tangents));

            Bounds bounds = shell.bounds;
            bounds.Expand(BoundsPadding * 2f);
            shell.bounds = bounds;

            AssetDatabase.CreateAsset(shell, path);
            report.AddCreated(path);
            return shell;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            GameObject[] selection = Selection.gameObjects;
            bool found = false;
            for (int i = 0; i < selection.Length; i++)
            {
                KavachShellTarget[] targets = selection[i].GetComponentsInChildren<KavachShellTarget>(true);
                for (int t = 0; t < targets.Length; t++)
                {
                    found = true;
                    WireTarget(targets[t], report);
                }
            }

            if (!found)
            {
                report.AddSkipped("Kavach shell meshes (no Kavach Shell Target on the selection)");
            }
        }

        private static Mesh GetBodyMesh(Renderer body)
        {
            if (body is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            return body.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
        }

        // Tangents for a mesh that has none: from its UVs when it has them, otherwise any direction at right angles to
        // each normal, which is all the tangent-space bake needs.
        private static Vector4[] BuildTangents(Mesh mesh, Vector3[] normals)
        {
            if (mesh.uv.Length == normals.Length)
            {
                mesh.RecalculateTangents();
                Vector4[] fromUvs = mesh.tangents;
                if (fromUvs.Length == normals.Length)
                {
                    return fromUvs;
                }
            }

            var tangents = new Vector4[normals.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 normal = normals[i];
                Vector3 helper = Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right;
                Vector3 tangent = Vector3.Cross(helper, normal).normalized;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, 1f);
            }

            return tangents;
        }
    }
}
