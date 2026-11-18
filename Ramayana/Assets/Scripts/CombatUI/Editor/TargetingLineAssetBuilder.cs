// Creates the targeting line's assets: the material for the Targeting Line shader, the Targeting Line style with that
// material wired in, and the combat camera anchor the line uses to project enemies onto the screen. Existing assets
// are never changed and only empty references are filled. Runs after the Combat UI and Combat Scene builders.
using JourneyToLanka.CombatScene;
using JourneyToLanka.CombatScene.EditorTools;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class TargetingLineAssetBuilder
    {
        public const string ShaderName = "Journey to Lanka/UI/Targeting Line";
        public const string MaterialPath = CombatUIAssetBuilder.Root + "/TargetingLine.mat";
        public const string StylePath = CombatUIAssetBuilder.Root + "/TargetingLineStyle.asset";
        public const string CameraAnchorPath = CombatSceneAssetBuilder.Root + "/CombatCameraAnchor.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Targeting Line", priority = AssetBuilderConstants.FeatureMenuPriority + 22)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Targeting Line", Build);
        }

        [DefaultAssetBuilder(133)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatUIAssetBuilder.Root, report);
            AssetBuilderUtility.EnsureFolder(CombatSceneAssetBuilder.Root, report);

            Material material = LoadOrCreateMaterial(report);
            TargetingLineStyleSO style = AssetBuilderSerialization.LoadOrCreateConfigured<TargetingLineStyleSO>(StylePath, report, null);
            if (style != null && material != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_material", material, report);
            }

            AssetBuilderSerialization.LoadOrCreateConfigured<CombatCameraAnchorSO>(CameraAnchorPath, report,
                so => AssetBuilderSerialization.SetString(so, "_description", "The combat scene's camera, provided by the camera's anchor provider."));
        }

        private static Material LoadOrCreateMaterial(AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                report.AddExisted(MaterialPath);
                return existing;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                report.AddSkipped($"{MaterialPath} (shader '{ShaderName}' not found; check TargetingLine.shader compiled)");
                return null;
            }

            var material = new Material(shader) { name = "TargetingLine" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            report.AddCreated(MaterialPath);
            return material;
        }
    }
}
