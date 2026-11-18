// Creates the card frame's assets: the material for the Card Frame shader, the material for the Card Glow shader (the
// playable-card halo), a Card Type Visuals asset for Attack (warm red-orange), Skill (teal), Dharma (marigold),
// Invocation (violet), Astra (radiant sky blue), Power (forest green) and a neutral default, each linked to its card type, and wires them
// into the Combat HUD style (frame and glow materials, the type list and the default) where those are still empty.
// Types added after the list was first filled are appended to it if missing; nothing in it is removed or reordered.
// Existing assets, colours and assigned references are never changed. Runs after the Combat, Status Cards, Power
// Cards and Combat UI builders, which create the card types and the style.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CardFrameAssetBuilder
    {
        public const string ShaderName = "Journey to Lanka/UI/Card Frame";
        public const string MaterialPath = CombatUIAssetBuilder.Root + "/CardFrame.mat";
        public const string GlowShaderName = "Journey to Lanka/UI/Card Glow";
        public const string GlowMaterialPath = CombatUIAssetBuilder.Root + "/CardGlow.mat";
        public const string VisualsFolder = CombatUIAssetBuilder.Root + "/CardTypes";
        public const string AttackVisualsPath = VisualsFolder + "/CardTypeVisuals_Attack.asset";
        public const string SkillVisualsPath = VisualsFolder + "/CardTypeVisuals_Skill.asset";
        public const string DharmaVisualsPath = VisualsFolder + "/CardTypeVisuals_Dharma.asset";
        public const string InvocationVisualsPath = VisualsFolder + "/CardTypeVisuals_Invocation.asset";
        public const string AstraVisualsPath = VisualsFolder + "/CardTypeVisuals_Astra.asset";
        public const string PowerVisualsPath = VisualsFolder + "/CardTypeVisuals_Power.asset";
        public const string DefaultVisualsPath = VisualsFolder + "/CardTypeVisuals_Default.asset";

        private static readonly Color AttackAccent = new Color(0.86f, 0.33f, 0.18f);
        private static readonly Color AttackLabel = new Color(1f, 0.84f, 0.74f);
        private static readonly Color SkillAccent = new Color(0.16f, 0.58f, 0.62f);
        private static readonly Color SkillLabel = new Color(0.75f, 0.94f, 0.94f);
        private static readonly Color DharmaAccent = new Color(0.93f, 0.68f, 0.18f);
        private static readonly Color DharmaLabel = new Color(1f, 0.93f, 0.72f);
        private static readonly Color InvocationAccent = new Color(0.55f, 0.3f, 0.8f);
        private static readonly Color InvocationLabel = new Color(0.9f, 0.83f, 1f);
        private static readonly Color AstraAccent = new Color(0.45f, 0.78f, 0.98f);
        private static readonly Color AstraLabel = new Color(0.9f, 0.97f, 1f);
        private static readonly Color PowerAccent = new Color(0.36f, 0.68f, 0.42f);
        private static readonly Color PowerLabel = new Color(0.86f, 1f, 0.88f);
        private static readonly Color DefaultAccent = new Color(0.7f, 0.55f, 0.3f);
        private static readonly Color DefaultLabel = new Color(0.96f, 0.91f, 0.8f);

        [MenuItem(AssetBuilderConstants.MenuRoot + "Card Frame", priority = AssetBuilderConstants.FeatureMenuPriority + 24)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Card Frame", Build);
        }

        [DefaultAssetBuilder(135)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatUIAssetBuilder.Root, report);
            AssetBuilderUtility.EnsureFolder(VisualsFolder, report);

            Material material = LoadOrCreateMaterial(MaterialPath, ShaderName, "CardFrame", report);
            Material glowMaterial = LoadOrCreateMaterial(GlowMaterialPath, GlowShaderName, "CardGlow", report);
            CardTypeVisualsSO attack = Visuals(AttackVisualsPath, "Attack", AttackAccent, AttackLabel, report);
            CardTypeVisualsSO skill = Visuals(SkillVisualsPath, "Skill", SkillAccent, SkillLabel, report);
            CardTypeVisualsSO dharma = Visuals(DharmaVisualsPath, "Dharma", DharmaAccent, DharmaLabel, report);
            CardTypeVisualsSO invocation = Visuals(InvocationVisualsPath, "Invocation", InvocationAccent, InvocationLabel, report);
            CardTypeVisualsSO astra = Visuals(AstraVisualsPath, "Astra", AstraAccent, AstraLabel, report);
            CardTypeVisualsSO power = Visuals(PowerVisualsPath, "Power", PowerAccent, PowerLabel, report);
            CardTypeVisualsSO fallback = Visuals(DefaultVisualsPath, null, DefaultAccent, DefaultLabel, report);

            var style = AssetDatabase.LoadAssetAtPath<CombatHudStyleSO>(CombatUIAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatUIAssetBuilder.StylePath} (missing, run Create Default Assets > Combat UI first)");
                return;
            }

            if (material != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_cardFrameMaterial", material, report);
            }

            if (glowMaterial != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_cardGlowMaterial", glowMaterial, report);
            }

            if (fallback != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_defaultCardTypeVisuals", fallback, report);
            }

            if (attack != null && skill != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(style, "_cardTypeVisuals", new Object[] { attack, skill }, report);
            }

            if (dharma != null && invocation != null && astra != null && power != null)
            {
                AssetBuilderSerialization.AppendObjectsIfMissing(style, "_cardTypeVisuals",
                    new Object[] { dharma, invocation, astra, power }, report);
            }
        }

        private static CardTypeVisualsSO Visuals(string path, string cardTypeFileName, Color accent, Color label,
            AssetBuilderReport report)
        {
            CardTypeVisualsSO visuals = AssetBuilderSerialization.LoadOrCreateConfigured<CardTypeVisualsSO>(path, report, so =>
            {
                AssetBuilderSerialization.Require(so, "_accentColor").colorValue = accent;
                AssetBuilderSerialization.Require(so, "_typeLabelColor").colorValue = label;
            });

            if (visuals == null || cardTypeFileName == null)
            {
                return visuals;
            }

            string typePath = CombatAssetBuilder.CardTypesFolder + "/" + cardTypeFileName + ".asset";
            var cardType = AssetDatabase.LoadAssetAtPath<CardTypeSO>(typePath);
            if (cardType == null)
            {
                report.AddSkipped($"{path} type link ({typePath} missing, run Create Default Assets > Combat first)");
                return visuals;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(visuals, "_type", cardType, report);
            return visuals;
        }

        private static Material LoadOrCreateMaterial(string path, string shaderName, string materialName, AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                report.AddExisted(path);
                return existing;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                report.AddSkipped($"{path} (shader '{shaderName}' not found; check that it compiled)");
                return null;
            }

            var material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            report.AddCreated(path);
            return material;
        }
    }
}
