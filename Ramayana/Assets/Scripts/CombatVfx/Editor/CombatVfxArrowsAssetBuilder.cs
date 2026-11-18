// Creates one hit profile per special arrow (Hit_Arrow_<Name>, twelve in all) and maps each arrow to it in the hit
// library. Each profile is coloured from its arrow's colour (Agni in the fire family, Dread and Mohana in the shadow
// family, the rest in the plain family) and gets its look materials from CombatVfxAssetBuilder. Arrows that strike
// enemies fly: Agni, Dread, Mohana, Indra and Narayana get a Polygon Arsenal Arrow Missile in the nearest colour,
// landing at the chest on the camera-facing surface, and an Arrival where it lands (a fire or lightning explosion, the
// terror or mesmerize debuff above the head, Narayana's orbital beam at the feet). Arrows that only affect Rama
// (Vajra, Vayu, Surya, Shakti, Sanjivani, Dharma, Garuda) do not fly; they get a Launch Accent at his feet (a buff,
// a heal or a sparkle aura). Colours are written only into profiles this run creates, accents are filled only where
// empty (placement is written with each prefab), and the library's arrow list is filled only while it is empty, so it
// runs safely any number of times and every choice stays editable. Without Polygon Arsenal the profiles are still made
// and the accents are skipped. Runs after the Special Arrows, Combat VFX Profiles (151) and Accents (153) builders.
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class CombatVfxArrowsAssetBuilder
    {
        public const string ArrowProfilePrefix = CombatVfxAssetBuilder.ProfilesFolder + "/Hit_Arrow_";

        private const int PlainFamily = 0;
        private const int FireFamily = 1;
        private const int ShadowFamily = 2;

        private const string MissileFolder = CombatVfxAccentsAssetBuilder.PackRoot + "/Missiles/Magic/Arrow";
        private const string MiniExplosionFolder = CombatVfxAccentsAssetBuilder.PackRoot + "/Explosions/Mini";
        private const string DebuffFolder = CombatVfxAccentsAssetBuilder.PackRoot + "/Curses & Debuffs/Debuffs";
        private const string BuffFolder = CombatVfxAccentsAssetBuilder.PackRoot + "/Buff";
        private const float MissileHeight = 0.6f;
        private const float AboveHeadHeight = 1.05f;

        // Name matches the arrow asset (Arrow_<Name>). Missile is the Arrow Missile colour (empty: the arrow does not
        // fly); Arrival and Launch are prefab paths (empty: none).
        private static readonly (string Name, int Family, string Missile, string Arrival, float ArrivalHeight,
            bool ArrivalOnSurface, float ArrivalDuration, string Launch, float LaunchDuration)[] Arrows =
        {
            ("Agni", FireFamily, "Red", MiniExplosionFolder + "/MiniExploFire.prefab", CombatVfxAccentsAssetBuilder.ChestHeight, true, 2f, "", 0f),
            ("Vajra", PlainFamily, "", "", 0f, false, 0f, BuffFolder + "/BuffBlue.prefab", 1.5f),
            ("Vayu", PlainFamily, "", "", 0f, false, 0f, BuffFolder + "/BuffGreen.prefab", 1.5f),
            ("Surya", PlainFamily, "", "", 0f, false, 0f, BuffFolder + "/BuffYellow.prefab", 1.5f),
            ("Dread", ShadowFamily, "Pink", DebuffFolder + "/TerrorDebuff.prefab", AboveHeadHeight, false, 1.5f, "", 0f),
            ("Mohana", ShadowFamily, "Purple", DebuffFolder + "/MesmerizeDebuff.prefab", AboveHeadHeight, false, 1.5f, "", 0f),
            ("Indra", PlainFamily, "Blue", MiniExplosionFolder + "/MiniExploLightningBlue.prefab", CombatVfxAccentsAssetBuilder.ChestHeight, true, 2f, "", 0f),
            ("Shakti", PlainFamily, "", "", 0f, false, 0f, BuffFolder + "/BuffRed.prefab", 1.5f),
            ("Sanjivani", PlainFamily, "", "", 0f, false, 0f, CombatVfxAccentsAssetBuilder.PrefabsRoot + "/Interactive/Healing/HealOnce.prefab", 2f),
            ("Dharma", PlainFamily, "", "", 0f, false, 0f, CombatVfxAccentsAssetBuilder.PackRoot + "/Aura/SparkleAura/SparkleAuraYellow.prefab", 1.2f),
            ("Garuda", PlainFamily, "", "", 0f, false, 0f, BuffFolder + "/BuffPurple.prefab", 1.5f),
            ("Narayana", PlainFamily, "Yellow", CombatVfxAccentsAssetBuilder.PackRoot + "/Orbital Beam/OrbitalBeamYellow.prefab", 0f, false, 1.2f, "", 0f),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat VFX Arrows", priority = AssetBuilderConstants.FeatureMenuPriority + 34)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat VFX Arrows", Build);
        }

        [DefaultAssetBuilder(154)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatVfxAssetBuilder.ProfilesFolder, report);

            Material plain = LoadMaterial(CombatVfxAssetBuilder.ImpactBurstMaterialPath, report);
            Material fire = LoadMaterial(CombatVfxAssetBuilder.ImpactBurstFireMaterialPath, report);
            Material shadow = LoadMaterial(CombatVfxAssetBuilder.ImpactBurstShadowMaterialPath, report);
            Material slash = LoadMaterial(CombatVfxAssetBuilder.SlashArcMaterialPath, report);
            Material cast = LoadMaterial(CombatVfxAssetBuilder.MandalaRingMaterialPath, report);
            Material sparks = LoadMaterial(CombatVfxAssetBuilder.SparkBurstMaterialPath, report);
            bool packImported = AssetDatabase.IsValidFolder(CombatVfxAccentsAssetBuilder.PackRoot);
            if (!packImported)
            {
                report.AddSkipped($"{CombatVfxAccentsAssetBuilder.PackRoot} (Polygon Arsenal is not imported; arrow accents stay empty)");
            }

            var arrows = new List<ArrowDefinitionSO>(Arrows.Length);
            var profiles = new List<HitVfxProfileSO>(Arrows.Length);
            for (int i = 0; i < Arrows.Length; i++)
            {
                var row = Arrows[i];
                string arrowPath = ArrowsAssetBuilder.ArrowsFolder + "/Arrow_" + row.Name + ".asset";
                var arrow = AssetDatabase.LoadAssetAtPath<ArrowDefinitionSO>(arrowPath);
                if (arrow == null)
                {
                    report.AddSkipped($"{ArrowProfilePrefix}{row.Name}.asset ({arrowPath} missing, run Create Default Assets > Special Arrows first)");
                    continue;
                }

                Color color = ArrowColor(arrow);
                HitVfxProfileSO profile = AssetBuilderSerialization.LoadOrCreateConfigured<HitVfxProfileSO>(
                    ArrowProfilePrefix + row.Name + ".asset", report, so => Configure(so, color));
                if (profile == null)
                {
                    continue;
                }

                Material impact = row.Family == FireFamily ? fire : row.Family == ShadowFamily ? shadow : plain;
                CombatVfxAssetBuilder.WireLooks(profile, impact, slash, sparks, cast, report);
                if (packImported)
                {
                    WireAccents(profile, row.Missile, row.Arrival, row.ArrivalHeight, row.ArrivalOnSurface,
                        row.ArrivalDuration, row.Launch, row.LaunchDuration, report);
                }

                arrows.Add(arrow);
                profiles.Add(profile);
            }

            var library = AssetDatabase.LoadAssetAtPath<HitVfxLibrarySO>(CombatVfxProfilesAssetBuilder.LibraryPath);
            if (library == null)
            {
                report.AddSkipped($"{CombatVfxProfilesAssetBuilder.LibraryPath} (missing, run Create Default Assets > Combat VFX Profiles first)");
                return;
            }

            AssetBuilderSerialization.FillArrayIfEmpty(library, "_arrows", arrows.Count, (element, index) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_key").objectReferenceValue = arrows[index];
                AssetBuilderSerialization.RequireRelative(element, "_profile").objectReferenceValue = profiles[index];
            }, report);
        }

        private static void WireAccents(HitVfxProfileSO profile, string missileColor, string arrivalPath, float arrivalHeight,
            bool arrivalOnSurface, float arrivalDuration, string launchPath, float launchDuration, AssetBuilderReport report)
        {
            if (missileColor.Length > 0)
            {
                CombatVfxAccentsAssetBuilder.WireLook(profile, "_missile", $"{MissileFolder}/ArrowMissile{missileColor}.prefab",
                    look => CombatVfxAccentsAssetBuilder.Place(look, MissileHeight, true), report);
            }

            if (arrivalPath.Length > 0)
            {
                CombatVfxAccentsAssetBuilder.WireLook(profile, "_arrival", arrivalPath, look =>
                {
                    CombatVfxAccentsAssetBuilder.Place(look, arrivalHeight, arrivalOnSurface);
                    AssetBuilderSerialization.RequireRelative(look, "_maxDuration").floatValue = arrivalDuration;
                }, report);
            }

            if (launchPath.Length > 0)
            {
                CombatVfxAccentsAssetBuilder.WireLook(profile, "_launchAccent", launchPath, look =>
                {
                    CombatVfxAccentsAssetBuilder.Place(look, 0f, false);
                    AssetBuilderSerialization.RequireRelative(look, "_maxDuration").floatValue = launchDuration;
                }, report);
            }
        }

        private static void Configure(SerializedObject so, Color color)
        {
            SetColor(so, "_impact._color", Glow(color, 3.2f));
            SetColor(so, "_impact._coreColor", Glow(Color.Lerp(color, Color.white, 0.7f), 4.5f));
            SetColor(so, "_slash._color", Glow(color, 3f));
            SetColor(so, "_slash._coreColor", Glow(Color.Lerp(color, Color.white, 0.75f), 5f));
            SetColor(so, "_sparks._color", Glow(color, 3.2f));
            SetColor(so, "_sparks._hotColor", Glow(Color.Lerp(color, Color.white, 0.8f), 6f));
            SetColor(so, "_cast._color", Glow(color, 2.6f));
            SetColor(so, "_cast._glowColor", Glow(color, 0.6f));
        }

        private static Color ArrowColor(ArrowDefinitionSO arrow)
        {
            using var serializedArrow = new SerializedObject(arrow);
            return AssetBuilderSerialization.Require(serializedArrow, "_color").colorValue;
        }

        private static Color Glow(Color color, float intensity)
        {
            return new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        }

        private static void SetColor(SerializedObject so, string path, Color value)
        {
            AssetBuilderSerialization.Require(so, path).colorValue = value;
        }

        private static Material LoadMaterial(string path, AssetBuilderReport report)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                report.AddSkipped($"{path} (missing, run Create Default Assets > Combat VFX first)");
            }

            return material;
        }
    }
}
