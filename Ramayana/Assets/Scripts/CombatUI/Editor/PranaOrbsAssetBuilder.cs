// Creates the Prana orbs' assets: the material for the Prana Orb shader and the Prana Orbs style with that material
// wired in, then assigns the style to the Combat HUD style if it has none. Existing assets, values and assigned
// references are never changed. Runs after the Combat UI builder, which creates the HUD style.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class PranaOrbsAssetBuilder
    {
        public const string ShaderName = "Journey to Lanka/UI/Prana Orb";
        public const string MaterialPath = CombatUIAssetBuilder.Root + "/PranaOrb.mat";
        public const string StylePath = CombatUIAssetBuilder.Root + "/PranaOrbsStyle.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Prana Orbs", priority = AssetBuilderConstants.FeatureMenuPriority + 25)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Prana Orbs", Build);
        }

        [DefaultAssetBuilder(136)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatUIAssetBuilder.Root, report);

            Material material = LoadOrCreateMaterial(report);
            PranaOrbsStyleSO style = AssetBuilderSerialization.LoadOrCreateConfigured<PranaOrbsStyleSO>(StylePath, report, null);
            if (style != null && material != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_orbMaterial", material, report);
            }

            var hudStyle = AssetDatabase.LoadAssetAtPath<CombatHudStyleSO>(CombatUIAssetBuilder.StylePath);
            if (hudStyle == null)
            {
                report.AddSkipped($"{CombatUIAssetBuilder.StylePath} (missing, run Create Default Assets > Combat UI first)");
                return;
            }

            if (style != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(hudStyle, "_pranaOrbs", style, report);
            }
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
                report.AddSkipped($"{MaterialPath} (shader '{ShaderName}' not found; check PranaOrb.shader compiled)");
                return null;
            }

            var material = new Material(shader) { name = "PranaOrb" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            report.AddCreated(MaterialPath);
            return material;
        }
    }
}
