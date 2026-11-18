// Creates the hit profiles and the hit library: one profile per card type (Attack, Skill, Dharma, Invocation, Astra,
// Power), coloured from that type's card accent so a card's blows match its frame; a shared enemy profile in the
// shadow family (crimson ring over dark smoke, no cast mandala); a default arrow profile in radiant gold until each
// arrow gets its own; and a burn profile in the fire family for round-end damage (no slash, no cast, embers that
// drift up). Each profile gets its look materials from CombatVfxAssetBuilder. The library maps each card type to its
// profile and points its card, enemy, arrow and round-end defaults at these profiles, and is wired into the combat
// VFX style. Colours are written only into profiles this run creates; existing assets are never changed and only
// empty references or empty lists are filled. Runs after the Card Frame (135) and Combat VFX (150) builders.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.CombatUI;
using JourneyToLanka.CombatUI.EditorTools;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class CombatVfxProfilesAssetBuilder
    {
        public const string LibraryPath = CombatVfxAssetBuilder.ProfilesFolder + "/HitVfxLibrary.asset";
        public const string EnemyProfilePath = CombatVfxAssetBuilder.ProfilesFolder + "/Hit_Enemy.asset";
        public const string ArrowProfilePath = CombatVfxAssetBuilder.ProfilesFolder + "/Hit_Arrow.asset";
        public const string BurnProfilePath = CombatVfxAssetBuilder.ProfilesFolder + "/Hit_Burn.asset";
        public const string CardProfilePrefix = CombatVfxAssetBuilder.ProfilesFolder + "/Hit_Card_";

        private static readonly string[] CardTypeNames = { "Attack", "Skill", "Dharma", "Invocation", "Astra", "Power" };

        private static readonly string[] CardVisualsPaths =
        {
            CardFrameAssetBuilder.AttackVisualsPath,
            CardFrameAssetBuilder.SkillVisualsPath,
            CardFrameAssetBuilder.DharmaVisualsPath,
            CardFrameAssetBuilder.InvocationVisualsPath,
            CardFrameAssetBuilder.AstraVisualsPath,
            CardFrameAssetBuilder.PowerVisualsPath,
        };

        private static readonly Color FallbackAccent = new Color(0.93f, 0.68f, 0.18f);

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat VFX Profiles", priority = AssetBuilderConstants.FeatureMenuPriority + 31)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat VFX Profiles", Build);
        }

        [DefaultAssetBuilder(151)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatVfxAssetBuilder.ProfilesFolder, report);

            Material impact = Load(CombatVfxAssetBuilder.ImpactBurstMaterialPath, report);
            Material impactFire = Load(CombatVfxAssetBuilder.ImpactBurstFireMaterialPath, report);
            Material impactShadow = Load(CombatVfxAssetBuilder.ImpactBurstShadowMaterialPath, report);
            Material slash = Load(CombatVfxAssetBuilder.SlashArcMaterialPath, report);
            Material cast = Load(CombatVfxAssetBuilder.MandalaRingMaterialPath, report);
            Material sparks = Load(CombatVfxAssetBuilder.SparkBurstMaterialPath, report);

            var cardTypes = new List<CardTypeSO>(CardTypeNames.Length);
            var cardProfiles = new List<HitVfxProfileSO>(CardTypeNames.Length);
            for (int i = 0; i < CardTypeNames.Length; i++)
            {
                string typePath = CombatAssetBuilder.CardTypesFolder + "/" + CardTypeNames[i] + ".asset";
                var cardType = AssetDatabase.LoadAssetAtPath<CardTypeSO>(typePath);
                if (cardType == null)
                {
                    report.AddSkipped($"{CardProfilePrefix}{CardTypeNames[i]}.asset ({typePath} missing, run Create Default Assets > All first)");
                    continue;
                }

                Color accent = LoadAccent(CardVisualsPaths[i]);
                HitVfxProfileSO profile = AssetBuilderSerialization.LoadOrCreateConfigured<HitVfxProfileSO>(
                    CardProfilePrefix + CardTypeNames[i] + ".asset", report, so => ConfigureCard(so, accent));
                CombatVfxAssetBuilder.WireLooks(profile, impact, slash, sparks, cast, report);
                if (profile != null)
                {
                    cardTypes.Add(cardType);
                    cardProfiles.Add(profile);
                }
            }

            HitVfxProfileSO enemy = AssetBuilderSerialization.LoadOrCreateConfigured<HitVfxProfileSO>(EnemyProfilePath, report, ConfigureEnemy);
            CombatVfxAssetBuilder.WireLooks(enemy, impactShadow, slash, sparks, cast, report);

            HitVfxProfileSO arrow = AssetBuilderSerialization.LoadOrCreateConfigured<HitVfxProfileSO>(ArrowProfilePath, report, ConfigureArrow);
            CombatVfxAssetBuilder.WireLooks(arrow, impact, slash, sparks, cast, report);

            HitVfxProfileSO burn = AssetBuilderSerialization.LoadOrCreateConfigured<HitVfxProfileSO>(BurnProfilePath, report, ConfigureBurn);
            CombatVfxAssetBuilder.WireLooks(burn, impactFire, slash, sparks, cast, report);

            HitVfxLibrarySO library = AssetBuilderSerialization.LoadOrCreateConfigured<HitVfxLibrarySO>(LibraryPath, report, null);
            if (library == null)
            {
                return;
            }

            AssetBuilderSerialization.FillArrayIfEmpty(library, "_cardTypes", cardTypes.Count, (element, index) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_key").objectReferenceValue = cardTypes[index];
                AssetBuilderSerialization.RequireRelative(element, "_profile").objectReferenceValue = cardProfiles[index];
            }, report);

            AssetBuilderUtility.WireAssetFieldIfNull(library, "_cardDefault",
                AssetDatabase.LoadAssetAtPath<HitVfxProfileSO>(CombatVfxAssetBuilder.DefaultHitPath), report);
            AssetBuilderUtility.WireAssetFieldIfNull(library, "_enemyDefault", enemy, report);
            AssetBuilderUtility.WireAssetFieldIfNull(library, "_arrowDefault", arrow, report);
            AssetBuilderUtility.WireAssetFieldIfNull(library, "_roundEnd", burn, report);

            var style = AssetDatabase.LoadAssetAtPath<CombatVfxStyleSO>(CombatVfxAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatVfxAssetBuilder.StylePath} (missing, run Create Default Assets > Combat VFX first)");
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(style, "_hitLibrary", library, report);
        }

        private static void ConfigureCard(SerializedObject so, Color accent)
        {
            SetColor(so, "_impact._color", Glow(accent, 3.2f));
            SetColor(so, "_impact._coreColor", Glow(Color.Lerp(accent, Color.white, 0.7f), 4.5f));
            SetColor(so, "_slash._color", Glow(accent, 3f));
            SetColor(so, "_slash._coreColor", Glow(Color.Lerp(accent, Color.white, 0.75f), 5f));
            SetColor(so, "_sparks._color", Glow(accent, 3.2f));
            SetColor(so, "_sparks._hotColor", Glow(Color.Lerp(accent, Color.white, 0.8f), 6f));
            SetColor(so, "_cast._color", Glow(accent, 2.6f));
            SetColor(so, "_cast._glowColor", Glow(accent, 0.6f));
        }

        private static void ConfigureEnemy(SerializedObject so)
        {
            SetColor(so, "_impact._color", new Color(2.6f, 0.35f, 0.5f, 1f));
            SetColor(so, "_impact._coreColor", new Color(3.5f, 1.6f, 1.6f, 1f));
            SetFloat(so, "_impact._shadowOpacity", 0.6f);
            SetColor(so, "_slash._color", new Color(2.8f, 0.3f, 0.35f, 1f));
            SetColor(so, "_slash._coreColor", new Color(4f, 2f, 2f, 1f));
            SetColor(so, "_sparks._color", new Color(3f, 0.5f, 0.4f, 1f));
            SetColor(so, "_sparks._hotColor", new Color(5f, 3f, 2.5f, 1f));
            SetBool(so, "_cast._enabled", false);
        }

        private static void ConfigureArrow(SerializedObject so)
        {
            SetColor(so, "_impact._color", new Color(3.5f, 2.6f, 1.2f, 1f));
            SetColor(so, "_impact._coreColor", new Color(5.5f, 5f, 4.2f, 1f));
            SetColor(so, "_slash._color", new Color(3.4f, 2.4f, 1f, 1f));
            SetColor(so, "_slash._coreColor", new Color(5.5f, 5f, 4.2f, 1f));
            SetColor(so, "_sparks._color", new Color(3.6f, 2.4f, 0.8f, 1f));
            SetColor(so, "_sparks._hotColor", new Color(6f, 5.5f, 4.5f, 1f));
            SetColor(so, "_cast._color", new Color(3.4f, 2.4f, 1f, 1f));
            SetColor(so, "_cast._glowColor", new Color(0.8f, 0.55f, 0.2f, 1f));
        }

        private static void ConfigureBurn(SerializedObject so)
        {
            SetFloat(so, "_impact._size", 1.3f);
            SetFloat(so, "_impact._duration", 0.4f);
            SetColor(so, "_impact._color", new Color(4f, 1.2f, 0.2f, 1f));
            SetColor(so, "_impact._coreColor", new Color(5f, 3f, 1f, 1f));
            SetInt(so, "_impact._rayCount", 0);
            SetBool(so, "_slash._enabled", false);
            SetInt(so, "_sparks._count", 10);
            SetFloat(so, "_sparks._lifetime", 0.8f);
            SetFloat(so, "_sparks._speedMin", 1f);
            SetFloat(so, "_sparks._speedMax", 3f);
            SetFloat(so, "_sparks._drag", 2f);
            SetFloat(so, "_sparks._gravity", -2f);
            SetFloat(so, "_sparks._spread", 40f);
            SetFloat(so, "_sparks._upBias", 1.5f);
            SetColor(so, "_sparks._color", new Color(3.5f, 0.9f, 0.15f, 1f));
            SetColor(so, "_sparks._hotColor", new Color(5f, 3.2f, 1.2f, 1f));
            SetBool(so, "_cast._enabled", false);
        }

        private static Color LoadAccent(string visualsPath)
        {
            var visuals = AssetDatabase.LoadAssetAtPath<CardTypeVisualsSO>(visualsPath);
            return visuals != null ? visuals.AccentColor : FallbackAccent;
        }

        private static Color Glow(Color color, float intensity)
        {
            return new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        }

        private static Material Load(string path, AssetBuilderReport report)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                report.AddSkipped($"{path} (missing, run Create Default Assets > Combat VFX first)");
            }

            return material;
        }

        private static void SetColor(SerializedObject so, string path, Color value)
        {
            AssetBuilderSerialization.Require(so, path).colorValue = value;
        }

        private static void SetFloat(SerializedObject so, string path, float value)
        {
            AssetBuilderSerialization.Require(so, path).floatValue = value;
        }

        private static void SetInt(SerializedObject so, string path, int value)
        {
            AssetBuilderSerialization.Require(so, path).intValue = value;
        }

        private static void SetBool(SerializedObject so, string path, bool value)
        {
            AssetBuilderSerialization.Require(so, path).boolValue = value;
        }
    }
}
