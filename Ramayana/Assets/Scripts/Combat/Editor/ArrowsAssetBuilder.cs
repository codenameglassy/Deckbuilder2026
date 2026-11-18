// Creates the reference build's twelve special arrows under Assets/_Project/Data/Combat/Arrows with their balance pass
// values, tiers and colours, and the effects they use (reusing the shared effect assets where one already exists):
// Agni (12 damage, then 3 Dahan), Vajra (12 Kavach), Vayu (draw 3), Surya (2 Prana), Dread (3 Bhaya), Mohana (2 Moha
// to all enemies), Indra (8 damage to all), Shakti (5 Focus this turn), Sanjivani (heal 20% of maximum health),
// Dharma (4 Dharma), Garuda (clear Moha, draw 2) and Narayana (25 damage), plus the drop table that rolls them after
// a won fight (40% to start, 10 less after a drop and 10 more after none, between 10% and 90%; tiers 62, 30 and 8).
// Values are set only on assets this run creates; references and lists are filled only while empty. Runs after
// Combat, Reward Cards, Statuses and Status Cards, and before the Run builder, which wires the drop table in.
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class ArrowsAssetBuilder
    {
        public const string ArrowsFolder = CombatAssetBuilder.Root + "/Arrows";
        public const string DropTablePath = ArrowsFolder + "/ArrowDropTable.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Special Arrows", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Special Arrows", Build);
        }

        [DefaultAssetBuilder(114)]
        public static void Build(AssetBuilderReport report)
        {
            var chosen = AssetDatabase.LoadAssetAtPath<ChosenTargetRuleSO>(CombatAssetBuilder.TargetingFolder + "/ChosenTarget.asset");
            var all = AssetDatabase.LoadAssetAtPath<AllOpponentsTargetRuleSO>(CombatAssetBuilder.TargetingFolder + "/AllOpponents.asset");
            var focus = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.FocusPath);
            var moha = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.MohaPath);
            var bhaya = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.BhayaPath);
            var dahan = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.DahanPath);
            var dharma = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.DharmaPath);
            if (chosen == null || all == null)
            {
                report.AddSkipped("Special arrows (targeting rules missing, run the Combat builder first)");
                return;
            }

            if (focus == null || moha == null || bhaya == null || dahan == null || dharma == null)
            {
                report.AddSkipped("Special arrows (statuses missing, run the Statuses builder first)");
                return;
            }

            DamageEffectSO damage12 = Damage("Damage_12", 12, chosen, report);
            DamageEffectSO damage25 = Damage("Damage_25", 25, chosen, report);
            DamageEffectSO damage8All = Damage("Damage_8_All", 8, all, report);
            ApplyStatusEffectSO dahan3 = Status("ApplyDahan_3", dahan, 3, 0, chosen, report);
            ApplyStatusEffectSO bhaya3 = Status("ApplyBhaya_3", bhaya, 3, 0, chosen, report);
            ApplyStatusEffectSO moha2All = Status("ApplyMoha_2_All", moha, 2, 0, all, report);
            ApplyStatusEffectSO focus5 = Status("GainFocus_5", focus, 5, 1, null, report);
            ApplyStatusEffectSO dharma4 = Status("GainDharma_4", dharma, 4, 0, null, report);
            BlockEffectSO block12 = AssetBuilderSerialization.LoadOrCreateConfigured<BlockEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/Block_12.asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_amount", 12));
            DrawCardsEffectSO draw2 = Draw("Draw_2", 2, report);
            DrawCardsEffectSO draw3 = Draw("Draw_3", 3, report);
            GainPranaEffectSO prana2 = AssetBuilderSerialization.LoadOrCreateConfigured<GainPranaEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/GainPrana_2.asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_amount", 2));
            HealPercentEffectSO heal20 = AssetBuilderSerialization.LoadOrCreateConfigured<HealPercentEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/HealPercent_20.asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_percent", 20));
            RemoveStatusEffectSO clearMoha = AssetBuilderSerialization.LoadOrCreateConfigured<RemoveStatusEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/RemoveMoha_Self.asset", report, null);
            AssetBuilderUtility.WireAssetFieldIfNull(clearMoha, "_status", moha, report);

            ArrowDefinitionSO agniArrow = Arrow("Agni", "agni", ArrowTier.Common, Hex(0xFF7A3D), report, damage12, dahan3);
            ArrowDefinitionSO vajraArrow = Arrow("Vajra", "vajra", ArrowTier.Common, Hex(0x8FD4FF), report, block12);
            ArrowDefinitionSO vayuArrow = Arrow("Vayu", "vayu", ArrowTier.Common, Hex(0xB6FFC9), report, draw3);
            ArrowDefinitionSO suryaArrow = Arrow("Surya", "surya", ArrowTier.Common, Hex(0xF2D58A), report, prana2);
            ArrowDefinitionSO dreadArrow = Arrow("Dread", "dread", ArrowTier.Common, Hex(0xFF9EB0), report, bhaya3);
            ArrowDefinitionSO mohanaArrow = Arrow("Mohana", "mohana", ArrowTier.Common, Hex(0xC7B4FF), report, moha2All);
            ArrowDefinitionSO indraArrow = Arrow("Indra", "indra", ArrowTier.Uncommon, Hex(0xE0E8FF), report, damage8All);
            ArrowDefinitionSO shaktiArrow = Arrow("Shakti", "shakti", ArrowTier.Uncommon, Hex(0xFFD98A), report, focus5);
            ArrowDefinitionSO sanjivaniArrow = Arrow("Sanjivani", "sanjivani", ArrowTier.Uncommon, Hex(0x7FE0A0), report, heal20);
            ArrowDefinitionSO dharmaArrow = Arrow("Dharma", "dharma", ArrowTier.Uncommon, Hex(0xF0C05A), report, dharma4);
            ArrowDefinitionSO garudaArrow = Arrow("Garuda", "garuda", ArrowTier.Rare, Hex(0xFFE8A0), report, clearMoha, draw2);
            ArrowDefinitionSO narayanaArrow = Arrow("Narayana", "narayana", ArrowTier.Rare, Hex(0xFFF6DE), report, damage25);

            ArrowDropTableSO dropTable = AssetBuilderSerialization.LoadOrCreateConfigured<ArrowDropTableSO>(DropTablePath, report, null);
            AssetBuilderSerialization.FillObjectArrayIfEmpty(dropTable, "_arrows", new Object[]
            {
                agniArrow, vajraArrow, vayuArrow, suryaArrow, dreadArrow, mohanaArrow,
                indraArrow, shaktiArrow, sanjivaniArrow, dharmaArrow, garudaArrow, narayanaArrow,
            }, report);
        }

        private static ArrowDefinitionSO Arrow(string fileName, string id, ArrowTier tier, Color color, AssetBuilderReport report,
            params CombatEffectSO[] effects)
        {
            ArrowDefinitionSO arrow = AssetBuilderSerialization.LoadOrCreateConfigured<ArrowDefinitionSO>(
                ArrowsFolder + "/Arrow_" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetString(so, "_id", id);
                    AssetBuilderSerialization.SetString(so, "_nameKey", "arrow." + id + ".name");
                    AssetBuilderSerialization.SetString(so, "_descriptionKey", "arrow." + id + ".description");
                    AssetBuilderSerialization.Require(so, "_tier").enumValueIndex = (int)tier;
                    AssetBuilderSerialization.Require(so, "_color").colorValue = color;
                });
            if (arrow != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(arrow, "_effects", effects, report);
            }

            return arrow;
        }

        private static DamageEffectSO Damage(string fileName, int amount, TargetRuleSO targets, AssetBuilderReport report)
        {
            DamageEffectSO effect = AssetBuilderSerialization.LoadOrCreateConfigured<DamageEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", amount);
                    AssetBuilderSerialization.SetInt(so, "_hits", 1);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(effect, "_targets", targets, report);
            return effect;
        }

        private static ApplyStatusEffectSO Status(string fileName, StatusDefinitionSO status, int amount, int duration,
            TargetRuleSO targets, AssetBuilderReport report)
        {
            ApplyStatusEffectSO effect = AssetBuilderSerialization.LoadOrCreateConfigured<ApplyStatusEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", amount);
                    AssetBuilderSerialization.SetInt(so, "_duration", duration);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(effect, "_status", status, report);
            if (targets != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(effect, "_targets", targets, report);
            }

            return effect;
        }

        private static DrawCardsEffectSO Draw(string fileName, int count, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<DrawCardsEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_count", count));
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }
}
