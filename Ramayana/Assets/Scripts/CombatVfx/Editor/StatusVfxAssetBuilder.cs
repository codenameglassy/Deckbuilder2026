// Creates the status VFX profile (Profiles/StatusVfx.asset) and wires it into the combat VFX style. The defaults are
// the values tuned in play: each aura has a place on the fighter, so any two read at once, either a wide ring at the
// feet or around the body (rings wider than a fighter, so the body never hides them) or an effect on the camera-facing
// surface in front of the body. Rows, in priority order: Dahan (fire ring at the feet, soft red), Bhaya (shadow ring
// just above the feet), Moha (purple swirl at twice its size in front of the body, lifted a little), Focus (yellow
// charge in front of the body at the feet, twice its size, a red buff when gained), Dharma (holy ring at the feet),
// then the three powers, each with its own look and a yellow buff when gained: Vow of the Exile (holy aura around the
// body at twice its size, green), Devotion (the project's white damage aura, small and pink in front of the body, with
// a pink buff) and Blessing of Agni (fire ring around the waist). The white damage aura is a project prefab, a
// recoloured copy of a Polygon Arsenal damage aura kept under CombatVfx/Prefabs so a pack update never touches it. The
// Dread and Mohana arrows keep their terror and mesmerize bursts above the head on landing, which do not match these
// auras.
// The rows are written only while the profile has none (statuses that do not exist yet are left out), and each row's
// aura and apply accent are filled only where empty, so it runs safely any number of times and every choice stays
// editable. Rows added through serialization start zeroed rather than at the look's field defaults, so every look's
// scale, tint and max duration are given their defaults while they still hold those invalid zero values; any value
// that was set is left alone. Without Polygon Arsenal the rows are still made and the effects are skipped. Runs after
// the Statuses and Power Cards builders and after Combat VFX (150).
using System.Collections.Generic;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.EditorTools
{
    public static class StatusVfxAssetBuilder
    {
        public const string ProfilePath = CombatVfxAssetBuilder.ProfilesFolder + "/StatusVfx.asset";

        public const string WhiteAuraPath = "Assets/_Project/Data/CombatVfx/Prefabs/DamageAuraWhite.prefab";

        private const string AuraFolder = CombatVfxAccentsAssetBuilder.PackRoot + "/Aura";
        private const string DamageAuraFolder = AuraFolder + "/DamageAura";
        private const string BuffFolder = CombatVfxAccentsAssetBuilder.PackRoot + "/Buff";
        private const string PowerApplyPath = BuffFolder + "/BuffYellow.prefab";
        private const float ApplyDuration = 1.5f;
        private const float DefaultScale = 1f;
        private const float DefaultMaxDuration = 3f;
        private const float MinScale = 0.01f;
        private const float MinMaxDuration = 0.05f;

        private static readonly Color White = Color.white;
        private static readonly Color DahanTint = new Color(0.915f, 0.6f, 0.6f, 1f);
        private static readonly Color ExileTint = new Color(0.525f, 0.953f, 0.283f, 1f);
        private static readonly Color DevotionTint = new Color(0.84f, 0.487f, 0.775f, 1f);
        private static readonly Color DevotionApplyTint = new Color(0.962f, 0.286f, 0.878f, 1f);

        // Status asset; aura prefab, height, on the camera surface, offset, scale and tint; apply accent prefab (empty:
        // none) and its tint. Order is priority.
        private static readonly (string Status, string Aura, float Height, bool OnSurface, Vector3 Offset, float Scale,
            Color Tint, string Apply, Color ApplyTint)[] Rows =
        {
            (StatusAssetBuilder.DahanPath, DamageAuraFolder + "/DamageAuraFire.prefab", 0f, false, Vector3.zero, 0.8f,
                DahanTint, "", White),
            (StatusAssetBuilder.BhayaPath, DamageAuraFolder + "/DamageAuraShadow.prefab", 0.2f, false, Vector3.zero, 0.85f,
                White, "", White),
            (StatusAssetBuilder.MohaPath, AuraFolder + "/SwirlAura/SwirlAuraPurple.prefab", 0.5f, true,
                new Vector3(0f, 0.25f, 0f), 2f, White, "", White),
            (StatusAssetBuilder.FocusPath, AuraFolder + "/ChargeAura/AuraChargeYellow.prefab", 0.1f, true, Vector3.zero, 2f,
                White, BuffFolder + "/BuffRed.prefab", White),
            (StatusAssetBuilder.DharmaPath, DamageAuraFolder + "/DamageAuraHoly.prefab", 0f, false, Vector3.zero, 0.85f,
                White, "", White),
            (PowerCardsAssetBuilder.ExilePath, AuraFolder + "/ElementalAura/ElementalAuraHoly.prefab", 0.35f, false,
                Vector3.zero, 2f, ExileTint, PowerApplyPath, White),
            (PowerCardsAssetBuilder.DevotionPath, WhiteAuraPath, 0.5f, true, Vector3.zero, 0.55f,
                DevotionTint, PowerApplyPath, DevotionApplyTint),
            (PowerCardsAssetBuilder.AgniPath, DamageAuraFolder + "/DamageAuraFire.prefab", 0.5f, false, Vector3.zero, 0.8f,
                White, PowerApplyPath, White),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Status VFX", priority = AssetBuilderConstants.FeatureMenuPriority + 35)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Status VFX", Build);
        }

        [DefaultAssetBuilder(155)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(CombatVfxAssetBuilder.ProfilesFolder, report);
            var profile = AssetBuilderUtility.LoadOrCreate<StatusVfxProfileSO>(ProfilePath, report);
            if (profile == null)
            {
                return;
            }

            var statuses = new List<StatusDefinitionSO>(Rows.Length);
            for (int i = 0; i < Rows.Length; i++)
            {
                var status = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(Rows[i].Status);
                if (status == null)
                {
                    report.AddSkipped($"{ProfilePath} row for {Rows[i].Status} (missing, run Create Default Assets > Statuses and Power Cards first)");
                    continue;
                }

                statuses.Add(status);
            }

            AssetBuilderSerialization.FillArrayIfEmpty(profile, "_entries", statuses.Count, (element, index) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_status").objectReferenceValue = statuses[index];
            }, report);

            if (AssetDatabase.IsValidFolder(CombatVfxAccentsAssetBuilder.PackRoot))
            {
                WireEffects(profile, report);
            }
            else
            {
                report.AddSkipped($"{CombatVfxAccentsAssetBuilder.PackRoot} (Polygon Arsenal is not imported; status effects stay empty)");
            }

            RepairLookDefaults(profile, report);

            var style = AssetDatabase.LoadAssetAtPath<CombatVfxStyleSO>(CombatVfxAssetBuilder.StylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatVfxAssetBuilder.StylePath} (missing, run Create Default Assets > Combat VFX first)");
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(style, "_statuses", profile, report);
        }

        // Fills the empty aura and apply accent of every row whose status has a default.
        private static void WireEffects(StatusVfxProfileSO profile, AssetBuilderReport report)
        {
            for (int i = 0; i < profile.Count; i++)
            {
                StatusVfxEntry entry = profile.EntryAt(i);
                int row = entry != null ? RowOf(entry.Status) : -1;
                if (row < 0)
                {
                    continue;
                }

                var defaults = Rows[row];
                string entryPath = $"_entries.Array.data[{i}]";
                CombatVfxAccentsAssetBuilder.WireLook(profile, entryPath + "._aura", defaults.Aura, look =>
                {
                    CombatVfxAccentsAssetBuilder.Place(look, defaults.Height, defaults.OnSurface);
                    AssetBuilderSerialization.RequireRelative(look, "_offset").vector3Value = defaults.Offset;
                    AssetBuilderSerialization.RequireRelative(look, "_scale").floatValue = defaults.Scale;
                    AssetBuilderSerialization.RequireRelative(look, "_tint").colorValue = defaults.Tint;
                }, report);

                if (defaults.Apply.Length > 0)
                {
                    CombatVfxAccentsAssetBuilder.WireLook(profile, entryPath + "._applyAccent", defaults.Apply, look =>
                    {
                        CombatVfxAccentsAssetBuilder.Place(look, 0f, false);
                        AssetBuilderSerialization.RequireRelative(look, "_tint").colorValue = defaults.ApplyTint;
                        AssetBuilderSerialization.RequireRelative(look, "_maxDuration").floatValue = ApplyDuration;
                    }, report);
                }
            }
        }

        // Gives every row's aura and apply accent a valid scale, tint and max duration where they still hold the zeroes
        // a serialized array element starts with. A zero scale or a fully transparent black tint is never authored on
        // purpose (the scale field's minimum is 0.01), so only those values are replaced.
        private static void RepairLookDefaults(StatusVfxProfileSO profile, AssetBuilderReport report)
        {
            using var serializedProfile = new SerializedObject(profile);
            SerializedProperty entries = AssetBuilderSerialization.Require(serializedProfile, "_entries");
            int repaired = 0;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (RepairLook(AssetBuilderSerialization.RequireRelative(entry, "_aura"), DefaultMaxDuration))
                {
                    repaired++;
                }

                if (RepairLook(AssetBuilderSerialization.RequireRelative(entry, "_applyAccent"), ApplyDuration))
                {
                    repaired++;
                }
            }

            if (repaired == 0)
            {
                return;
            }

            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            report.AddWired($"{profile.name} look defaults (scale, tint, max duration) on {repaired} effects");
        }

        private static bool RepairLook(SerializedProperty look, float maxDuration)
        {
            bool changed = false;

            SerializedProperty scale = AssetBuilderSerialization.RequireRelative(look, "_scale");
            if (scale.floatValue < MinScale)
            {
                scale.floatValue = DefaultScale;
                changed = true;
            }

            SerializedProperty tint = AssetBuilderSerialization.RequireRelative(look, "_tint");
            if (tint.colorValue == Color.clear)
            {
                tint.colorValue = Color.white;
                changed = true;
            }

            SerializedProperty duration = AssetBuilderSerialization.RequireRelative(look, "_maxDuration");
            if (duration.floatValue < MinMaxDuration)
            {
                duration.floatValue = maxDuration;
                changed = true;
            }

            return changed;
        }

        private static int RowOf(StatusDefinitionSO status)
        {
            if (status == null)
            {
                return -1;
            }

            string path = AssetDatabase.GetAssetPath(status);
            for (int i = 0; i < Rows.Length; i++)
            {
                if (Rows[i].Status == path)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
