// Fills the combat VFX accents with Polygon Arsenal prefabs: every card profile in the hit library (and the card
// default) gets a Sword Impact on heavy hits and a Sword Impact Epic on killing blows, in the pack colour nearest the
// hue of that profile's impact colour (gold, green, blue or purple, gold for pale colours); every enemy profile in the
// library (and the enemy default) gets Death Normal where the enemy falls; the Kavach profile gets Shield Defense Blue
// on gain and Surface Impact Glass on break. Placement is written with each prefab: hit accents at the chest on the
// camera-facing surface, deaths and gains at the feet. Only empty accents are filled (an accent with a prefab is
// never changed), so it runs safely any number of times and every choice stays editable on the profiles. Without the
// pack imported it skips everything and reports it. Runs after the Combat VFX Profiles (151) and Kavach VFX (152)
// builders.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class CombatVfxAccentsAssetBuilder
    {
        public const string PrefabsRoot = "Assets/Polygon Arsenal/Prefabs";
        public const string PackRoot = PrefabsRoot + "/Combat";
        public const string SwordImpactFolder = PackRoot + "/Melee & Sword/Sword Impact";
        public const string DeathNormalPath = PackRoot + "/Death/DeathNormal.prefab";
        public const string ShieldDefenseBluePath = PackRoot + "/Aura/ShieldAura/ShieldDefenseBlue.prefab";
        public const string SurfaceImpactGlassPath = PackRoot + "/Surface Impact/SurfaceImpactGlass.prefab";

        private const float PaleSaturation = 0.15f;
        internal const float ChestHeight = 0.65f;

        private static readonly string[] ImpactColorNames = { "Gold", "Green", "Blue", "Purple" };
        private static readonly float[] ImpactColorHues = { 0.12f, 0.33f, 0.58f, 0.77f };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat VFX Accents", priority = AssetBuilderConstants.FeatureMenuPriority + 33)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat VFX Accents", Build);
        }

        [DefaultAssetBuilder(153)]
        public static void Build(AssetBuilderReport report)
        {
            if (!AssetDatabase.IsValidFolder(PackRoot))
            {
                report.AddSkipped($"{PackRoot} (Polygon Arsenal is not imported; accents stay empty)");
                return;
            }

            var library = AssetDatabase.LoadAssetAtPath<HitVfxLibrarySO>(CombatVfxProfilesAssetBuilder.LibraryPath);
            if (library == null)
            {
                report.AddSkipped($"{CombatVfxProfilesAssetBuilder.LibraryPath} (missing, run Create Default Assets > Combat VFX Profiles first)");
            }
            else
            {
                WireLibrary(library, report);
            }

            var kavach = AssetDatabase.LoadAssetAtPath<KavachVfxProfileSO>(KavachVfxAssetBuilder.ProfilePath);
            if (kavach == null)
            {
                report.AddSkipped($"{KavachVfxAssetBuilder.ProfilePath} (missing, run Create Default Assets > Kavach VFX first)");
                return;
            }

            WireLook(kavach, "_gainAccent", ShieldDefenseBluePath, look => Place(look, 0f, false), report);
            WireLook(kavach, "_breakAccent", SurfaceImpactGlassPath, look => Place(look, ChestHeight, true), report);
        }

        private static void WireLibrary(HitVfxLibrarySO library, AssetBuilderReport report)
        {
            using var serializedLibrary = new SerializedObject(library);

            SerializedProperty cardTypes = AssetBuilderSerialization.Require(serializedLibrary, "_cardTypes");
            for (int i = 0; i < cardTypes.arraySize; i++)
            {
                WireCardAccents(ProfileOf(cardTypes.GetArrayElementAtIndex(i)), report);
            }

            WireCardAccents(AssetBuilderSerialization.Require(serializedLibrary, "_cardDefault").objectReferenceValue as HitVfxProfileSO, report);

            SerializedProperty enemies = AssetBuilderSerialization.Require(serializedLibrary, "_enemies");
            for (int i = 0; i < enemies.arraySize; i++)
            {
                WireDeath(ProfileOf(enemies.GetArrayElementAtIndex(i)), report);
            }

            WireDeath(AssetBuilderSerialization.Require(serializedLibrary, "_enemyDefault").objectReferenceValue as HitVfxProfileSO, report);
        }

        private static void WireCardAccents(HitVfxProfileSO profile, AssetBuilderReport report)
        {
            if (profile == null)
            {
                return;
            }

            string color = NearestImpactColor(profile);
            WireLook(profile, "_heavyAccent", $"{SwordImpactFolder}/SwordImpact{color}.prefab",
                look => Place(look, ChestHeight, true), report);
            WireLook(profile, "_killAccent", $"{SwordImpactFolder}/SwordImpactEpic{color}.prefab",
                look => Place(look, ChestHeight, true), report);
        }

        private static void WireDeath(HitVfxProfileSO profile, AssetBuilderReport report)
        {
            if (profile != null)
            {
                WireLook(profile, "_death", DeathNormalPath, look => Place(look, 0f, false), report);
            }
        }

        // Fills one empty accent: assigns the prefab and writes its placement. An accent that already has a prefab is
        // left exactly as it is. Shared with the other accent builders.
        internal static void WireLook(Object owner, string lookPath, string prefabPath,
            System.Action<SerializedProperty> configure, AssetBuilderReport report)
        {
            using var serializedOwner = new SerializedObject(owner);
            SerializedProperty look = AssetBuilderSerialization.Require(serializedOwner, lookPath);
            SerializedProperty prefab = AssetBuilderSerialization.RequireRelative(look, "_prefab");
            if (prefab.objectReferenceValue != null)
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null)
            {
                report.AddSkipped($"{owner.name}.{lookPath} ({prefabPath} missing)");
                return;
            }

            prefab.objectReferenceValue = asset;
            configure(look);
            serializedOwner.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
            report.AddWired($"{owner.name}.{lookPath} -> {asset.name}");
        }

        internal static void Place(SerializedProperty look, float height, bool onCameraSurface)
        {
            AssetBuilderSerialization.RequireRelative(look, "_height").floatValue = height;
            AssetBuilderSerialization.RequireRelative(look, "_onCameraSurface").boolValue = onCameraSurface;
        }

        private static HitVfxProfileSO ProfileOf(SerializedProperty entry)
        {
            return AssetBuilderSerialization.RequireRelative(entry, "_profile").objectReferenceValue as HitVfxProfileSO;
        }

        // The Sword Impact colour whose hue is nearest the profile's impact colour; pale colours read as gold.
        private static string NearestImpactColor(HitVfxProfileSO profile)
        {
            using var serializedProfile = new SerializedObject(profile);
            Color impact = AssetBuilderSerialization.Require(serializedProfile, "_impact._color").colorValue;
            Color.RGBToHSV(impact, out float hue, out float saturation, out float _);
            if (saturation < PaleSaturation)
            {
                return ImpactColorNames[0];
            }

            int nearest = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < ImpactColorHues.Length; i++)
            {
                float distance = Mathf.Abs(hue - ImpactColorHues[i]);
                distance = Mathf.Min(distance, 1f - distance);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }

            return ImpactColorNames[nearest];
        }
    }
}
