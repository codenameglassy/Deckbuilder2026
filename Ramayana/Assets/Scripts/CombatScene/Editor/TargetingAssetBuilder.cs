// Creates the target marker's assets next to the other combat scene assets: an unlit URP material for the disc and
// the target marker style, with the material wired in when the style has none. Existing assets and assigned
// references are never changed.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class TargetingAssetBuilder
    {
        public const string MarkerStylePath = CombatSceneAssetBuilder.Root + "/TargetMarkerStyle.asset";
        public const string MarkerMaterialPath = CombatSceneAssetBuilder.Root + "/TargetMarker.mat";
        private const string MarkerShaderName = "Universal Render Pipeline/Unlit";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Targeting", priority = AssetBuilderConstants.FeatureMenuPriority + 19)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Targeting", Build);
        }

        [DefaultAssetBuilder(126)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatSceneAssetBuilder.Root, report);
            Material material = LoadOrCreateMaterial(report);
            TargetMarkerStyleSO style = AssetBuilderSerialization.LoadOrCreateConfigured<TargetMarkerStyleSO>(MarkerStylePath, report, null);
            if (style != null && material != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_material", material, report);
            }
        }

        private static Material LoadOrCreateMaterial(AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MarkerMaterialPath);
            if (existing != null)
            {
                report.AddExisted(MarkerMaterialPath);
                return existing;
            }

            Shader shader = Shader.Find(MarkerShaderName);
            if (shader == null)
            {
                report.AddSkipped($"{MarkerMaterialPath} (shader '{MarkerShaderName}' not found; is URP installed?)");
                return null;
            }

            var material = new Material(shader) { name = "TargetMarker" };
            AssetDatabase.CreateAsset(material, MarkerMaterialPath);
            report.AddCreated(MarkerMaterialPath);
            return material;
        }
    }
}
