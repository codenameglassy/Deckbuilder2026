// Creates the first reward cards from the reference build's chapter one pool that the current effects can express,
// with their upgraded forms: Twin Shafts, Storm of Arrows, Lakshmana's Watch and Sage's Counsel, plus the draw
// effects they need, and the slice reward pool that lists them. Values come from the reference build. Values are
// set only on assets this run creates; references and lists are filled only while empty. Runs after Combat.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class RewardCardsAssetBuilder
    {
        public const string RewardsFolder = CombatAssetBuilder.Root + "/Rewards";
        public const string SlicePoolPath = RewardsFolder + "/RewardPool_Slice.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Reward Cards", priority = AssetBuilderConstants.FeatureMenuPriority + 9)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Reward Cards", Build);
        }

        [DefaultAssetBuilder(101)]
        public static void Build(AssetBuilderReport report)
        {
            var attack = AssetDatabase.LoadAssetAtPath<CardTypeSO>(CombatAssetBuilder.CardTypesFolder + "/Attack.asset");
            var skill = AssetDatabase.LoadAssetAtPath<CardTypeSO>(CombatAssetBuilder.CardTypesFolder + "/Skill.asset");
            var chosenTarget = AssetDatabase.LoadAssetAtPath<ChosenTargetRuleSO>(CombatAssetBuilder.TargetingFolder + "/ChosenTarget.asset");
            if (attack == null || skill == null || chosenTarget == null)
            {
                report.AddSkipped("Reward cards (card types or targeting missing, run the Combat builder first)");
                return;
            }

            DamageEffectSO damage4x2 = Damage("Damage_4x2", 4, 2, chosenTarget, report);
            DamageEffectSO damage5x2 = Damage("Damage_5x2", 5, 2, chosenTarget, report);
            DamageEffectSO damage3x4 = Damage("Damage_3x4", 3, 4, chosenTarget, report);
            DamageEffectSO damage4x4 = Damage("Damage_4x4", 4, 4, chosenTarget, report);
            BlockEffectSO block8 = Block("Block_8", 8, report);
            BlockEffectSO block11 = Block("Block_11", 11, report);
            DrawCardsEffectSO draw1 = Draw("Draw_1", 1, report);
            DrawCardsEffectSO draw2 = Draw("Draw_2", 2, report);
            DrawCardsEffectSO draw3 = Draw("Draw_3", 3, report);

            CardDefinitionSO twinPlus = Card("TwinShaftsPlus", "twin_plus", 1, attack, null, report, damage5x2);
            CardDefinitionSO twin = Card("TwinShafts", "twin", 1, attack, twinPlus, report, damage4x2);
            CardDefinitionSO stormPlus = Card("StormOfArrowsPlus", "storm_plus", 2, attack, null, report, damage4x4);
            CardDefinitionSO storm = Card("StormOfArrows", "storm", 2, attack, stormPlus, report, damage3x4);
            CardDefinitionSO watchPlus = Card("LakshmanasWatchPlus", "watch_plus", 1, skill, null, report, block11, draw1);
            CardDefinitionSO watch = Card("LakshmanasWatch", "watch", 1, skill, watchPlus, report, block8, draw1);
            CardDefinitionSO counselPlus = Card("SagesCounselPlus", "counsel_plus", 0, skill, null, report, draw3);
            CardDefinitionSO counsel = Card("SagesCounsel", "counsel", 0, skill, counselPlus, report, draw2);

            CardRewardPoolSO pool = AssetBuilderSerialization.LoadOrCreateConfigured<CardRewardPoolSO>(SlicePoolPath, report, null);
            if (pool != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(pool, "_cards", new Object[] { twin, storm, watch, counsel }, report);
            }
        }

        private static DamageEffectSO Damage(string fileName, int amount, int hits, TargetRuleSO targets, AssetBuilderReport report)
        {
            DamageEffectSO effect = AssetBuilderSerialization.LoadOrCreateConfigured<DamageEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", amount);
                    AssetBuilderSerialization.SetInt(so, "_hits", hits);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(effect, "_targets", targets, report);
            return effect;
        }

        private static BlockEffectSO Block(string fileName, int amount, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<BlockEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_amount", amount));
        }

        private static DrawCardsEffectSO Draw(string fileName, int count, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<DrawCardsEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_count", count));
        }

        private static CardDefinitionSO Card(string fileName, string id, int cost, CardTypeSO type,
            CardDefinitionSO upgradedForm, AssetBuilderReport report, params CombatEffectSO[] effects)
        {
            CardDefinitionSO card = AssetBuilderSerialization.LoadOrCreateConfigured<CardDefinitionSO>(
                CombatAssetBuilder.CardsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetString(so, "_id", id);
                    AssetBuilderSerialization.SetString(so, "_nameKey", "card." + id + ".name");
                    AssetBuilderSerialization.SetString(so, "_descriptionKey", "card." + id + ".description");
                    AssetBuilderSerialization.SetInt(so, "_cost", cost);
                });

            if (card == null)
            {
                return null;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(card, "_type", type, report);
            if (upgradedForm != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(card, "_upgradedForm", upgradedForm, report);
            }

            AssetBuilderSerialization.FillObjectArrayIfEmpty(card, "_effects", effects, report);
            return card;
        }
    }
}
