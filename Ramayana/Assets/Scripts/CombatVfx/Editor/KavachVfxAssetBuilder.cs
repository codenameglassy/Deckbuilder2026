// Creates the Kavach VFX assets: the Kavach Shell and Kavach Shards materials, and the Kavach profile with both
// materials wired in (its shard burst seeded as teal glass fragments thrown in every direction), then wires the
// profile into the combat VFX style. The hull's shell meshes come from each fighter's body meshes and are made by
// KavachShellMeshAssetBuilder. Materials are .mat
// assets reached from the style, so both shaders are always in player builds. Existing assets are never changed and
// only empty references are filled. Runs after the Combat VFX builder (150).
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class KavachVfxAssetBuilder
    {
        public const string ShellMaterialPath = CombatVfxAssetBuilder.MaterialsFolder + "/KavachShell.mat";
        public const string ShardsMaterialPath = CombatVfxAssetBuilder.MaterialsFolder + "/KavachShards.mat";
        public const string ProfilePath = CombatVfxAssetBuilder.ProfilesFolder + "/Kavach.asset";

        public const string ShellShaderName = "Journey to Lanka/VFX/Kavach Shell";
        public const string ShardsShaderName = "Journey to Lanka/VFX/Kavach Shards";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Kavach VFX", priority = AssetBuilderConstants.FeatureMenuPriority + 32)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Kavach VFX", Build);
        }

        [DefaultAssetBuilder(152)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatVfxAssetBuilder.MaterialsFolder, report);
            AssetBuilderUtility.EnsureFolder(CombatVfxAssetBuilder.ProfilesFolder, report);

            Material shell = CombatVfxAssetBuilder.LoadOrCreateMaterial(ShellMaterialPath, ShellShaderName, report);
            Material shards = CombatVfxAssetBuilder.LoadOrCreateMaterial(ShardsMaterialPath, ShardsShaderName, report);

            KavachVfxProfileSO profile = AssetBuilderSerialization.LoadOrCreateConfigured<KavachVfxProfileSO>(ProfilePath, report, ConfigureNew);
            if (profile != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(profile, "_shellMaterial", shell, report);
                AssetBuilderUtility.WireAssetFieldIfNull(profile, "_shards._material", shards, report);
            }

            var style = AssetDatabase.LoadAssetAtPath<CombatVfxStyleSO>(CombatVfxAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatVfxAssetBuilder.StylePath} (missing, run Create Default Assets > Combat VFX first)");
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(style, "_kavach", profile, report);
        }

        private static void ConfigureNew(SerializedObject so)
        {
            AssetBuilderSerialization.Require(so, "_shards._count").intValue = 28;
            AssetBuilderSerialization.Require(so, "_shards._maxCount").intValue = 64;
            AssetBuilderSerialization.Require(so, "_shards._lifetime").floatValue = 0.7f;
            AssetBuilderSerialization.Require(so, "_shards._lifetimeJitter").floatValue = 0.4f;
            AssetBuilderSerialization.Require(so, "_shards._speedMin").floatValue = 2f;
            AssetBuilderSerialization.Require(so, "_shards._speedMax").floatValue = 5f;
            AssetBuilderSerialization.Require(so, "_shards._drag").floatValue = 2.5f;
            AssetBuilderSerialization.Require(so, "_shards._gravity").floatValue = 7f;
            AssetBuilderSerialization.Require(so, "_shards._upBias").floatValue = 0.5f;
            AssetBuilderSerialization.Require(so, "_shards._hotColor").colorValue = new Color(2.5f, 4.5f, 5f, 1f);
            AssetBuilderSerialization.Require(so, "_shards._color").colorValue = new Color(0.5f, 1.8f, 2.4f, 1f);
            AssetBuilderSerialization.Require(so, "_shards._sparkSize").floatValue = 0.12f;
            AssetBuilderSerialization.Require(so, "_shards._stretch").floatValue = 0f;
            AssetBuilderSerialization.Require(so, "_shards._fadePower").floatValue = 1.2f;
            AssetBuilderSerialization.Require(so, "_shards._cameraPush").floatValue = 0.3f;
        }
    }
}
