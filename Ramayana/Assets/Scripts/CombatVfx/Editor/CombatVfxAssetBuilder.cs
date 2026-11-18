// Creates the shared combat VFX assets: the billboard quad mesh, one material per look (Impact Burst in its radiant,
// fire and shadow families, Slash Arc, Mandala Ring, Spark Burst), the default hit profile with every look's material
// wired in, and the combat VFX style wired to the mesh, the default hit profile and the combat impact style (its
// heavy-hit threshold). A family is a shader keyword set on its material here, never at runtime, so every variant a
// build needs is one these materials use. Every material is a .mat asset reached from the style, so its shader and
// keywords are always in player builds; nothing calls Shader.Find at runtime. Existing assets are never changed and
// only empty references are filled. Runs after the Combat Impact builder (127); the profiles and the hit library are
// made by CombatVfxProfilesAssetBuilder (151).
using JourneyToLanka.CombatScene;
using JourneyToLanka.CombatScene.EditorTools;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class CombatVfxAssetBuilder
    {
        public const string Root = AssetBuilderConstants.DataRoot + "/CombatVfx";
        public const string MaterialsFolder = Root + "/Materials";
        public const string MeshesFolder = Root + "/Meshes";
        public const string ProfilesFolder = Root + "/Profiles";
        public const string QuadMeshPath = MeshesFolder + "/VfxQuad.asset";
        public const string DefaultHitPath = ProfilesFolder + "/Hit_Default.asset";
        public const string StylePath = Root + "/CombatVfxStyle.asset";

        public const string ImpactBurstShaderName = "Journey to Lanka/VFX/Impact Burst";
        public const string SlashArcShaderName = "Journey to Lanka/VFX/Slash Arc";
        public const string MandalaRingShaderName = "Journey to Lanka/VFX/Mandala Ring";
        public const string SparkBurstShaderName = "Journey to Lanka/VFX/Spark Burst";

        public const string ImpactBurstMaterialPath = MaterialsFolder + "/ImpactBurst.mat";
        public const string ImpactBurstFireMaterialPath = MaterialsFolder + "/ImpactBurst_Fire.mat";
        public const string ImpactBurstShadowMaterialPath = MaterialsFolder + "/ImpactBurst_Shadow.mat";
        public const string SlashArcMaterialPath = MaterialsFolder + "/SlashArc.mat";
        public const string MandalaRingMaterialPath = MaterialsFolder + "/MandalaRing.mat";
        public const string SparkBurstMaterialPath = MaterialsFolder + "/SparkBurst.mat";

        private const string FamilyProperty = "_Family";
        private const string FireKeyword = "_FAMILY_FIRE";
        private const string ShadowKeyword = "_FAMILY_SHADOW";
        private const float FireFamilyIndex = 1f;
        private const float ShadowFamilyIndex = 2f;

        // The shader turns, rolls and pushes the quad toward the camera, so the bounds are larger than the quad to keep
        // it from being culled while any part of it is on screen.
        private const float QuadBoundsSize = 3f;

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat VFX", priority = AssetBuilderConstants.FeatureMenuPriority + 30)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat VFX", Build);
        }

        [DefaultAssetBuilder(150)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(MaterialsFolder, report);
            AssetBuilderUtility.EnsureFolder(MeshesFolder, report);
            AssetBuilderUtility.EnsureFolder(ProfilesFolder, report);

            Mesh quad = LoadOrCreateQuadMesh(report);
            Material impact = LoadOrCreateMaterial(ImpactBurstMaterialPath, ImpactBurstShaderName, report);
            LoadOrCreateFamilyMaterial(ImpactBurstFireMaterialPath, FireFamilyIndex, FireKeyword, report);
            LoadOrCreateFamilyMaterial(ImpactBurstShadowMaterialPath, ShadowFamilyIndex, ShadowKeyword, report);
            Material slash = LoadOrCreateMaterial(SlashArcMaterialPath, SlashArcShaderName, report);
            Material cast = LoadOrCreateMaterial(MandalaRingMaterialPath, MandalaRingShaderName, report);
            Material sparks = LoadOrCreateMaterial(SparkBurstMaterialPath, SparkBurstShaderName, report);

            HitVfxProfileSO defaultHit = AssetBuilderSerialization.LoadOrCreateConfigured<HitVfxProfileSO>(DefaultHitPath, report, null);
            WireLooks(defaultHit, impact, slash, sparks, cast, report);

            CombatVfxStyleSO style = AssetBuilderSerialization.LoadOrCreateConfigured<CombatVfxStyleSO>(StylePath, report, null);
            if (style == null)
            {
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(style, "_quadMesh", quad, report);
            AssetBuilderUtility.WireAssetFieldIfNull(style, "_defaultHit", defaultHit, report);
            AssetBuilderUtility.WireAssetFieldIfNull(style, "_impactStyle",
                AssetDatabase.LoadAssetAtPath<CombatImpactStyleSO>(CombatImpactAssetBuilder.ImpactStylePath), report);
        }

        // Fills a profile's empty look materials. Any argument may be null; a missing material is reported, not wired.
        public static void WireLooks(HitVfxProfileSO profile, Material impact, Material slash, Material sparks, Material cast,
            AssetBuilderReport report)
        {
            if (profile == null)
            {
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(profile, "_impact._material", impact, report);
            AssetBuilderUtility.WireAssetFieldIfNull(profile, "_slash._material", slash, report);
            AssetBuilderUtility.WireAssetFieldIfNull(profile, "_sparks._material", sparks, report);
            AssetBuilderUtility.WireAssetFieldIfNull(profile, "_cast._material", cast, report);
        }

        public static Material LoadOrCreateMaterial(string materialPath, string shaderName, AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
            {
                report.AddExisted(materialPath);
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(materialPath) != null)
            {
                report.AddSkipped($"{materialPath} (a different asset type occupies this path)");
                return null;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                report.AddSkipped($"{materialPath} (shader '{shaderName}' not found; check that it compiled)");
                return null;
            }

            int slash = materialPath.LastIndexOf('/');
            var material = new Material(shader) { name = materialPath.Substring(slash + 1).Replace(".mat", string.Empty) };
            AssetDatabase.CreateAsset(material, materialPath);
            report.AddCreated(materialPath);
            return material;
        }

        private static void LoadOrCreateFamilyMaterial(string materialPath, float familyIndex, string keyword,
            AssetBuilderReport report)
        {
            bool existed = AssetDatabase.LoadMainAssetAtPath(materialPath) != null;
            Material material = LoadOrCreateMaterial(materialPath, ImpactBurstShaderName, report);
            if (material == null || existed)
            {
                return;
            }

            material.SetFloat(FamilyProperty, familyIndex);
            material.EnableKeyword(keyword);
            EditorUtility.SetDirty(material);
        }

        private static Mesh LoadOrCreateQuadMesh(AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(QuadMeshPath);
            if (existing != null)
            {
                report.AddExisted(QuadMeshPath);
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(QuadMeshPath) != null)
            {
                report.AddSkipped($"{QuadMeshPath} (a different asset type occupies this path)");
                return null;
            }

            var mesh = new Mesh { name = "VfxQuad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            });
            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * QuadBoundsSize);

            AssetDatabase.CreateAsset(mesh, QuadMeshPath);
            report.AddCreated(QuadMeshPath);
            return mesh;
        }
    }
}
