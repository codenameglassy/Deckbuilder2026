// Creates the rest of the reference build's chapter one cards with their balance pass values: the three powers
// (Vow of the Exile, Devotion, Blessing of Agni) as statuses that last the whole fight, their triggers and the Power
// card type, and the attacks Vayavyastra, Gale Arrow and Windforce Volley with their upgraded forms. Power cards
// exhaust once played, since a power is already active for the rest of the fight. Blessing of Agni reuses the flat
// damage rule Focus uses, so it adds before Moha and Bhaya. Every new card is appended to the slice reward pool if it
// is not already there; nothing in the pool is removed or reordered. Values are set only on assets this run creates;
// references and lists are filled only while empty. Runs after Combat, Reward Cards, Statuses and Status Enemies.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class PowerCardsAssetBuilder
    {
        public const string PowersFolder = StatusAssetBuilder.Root + "/Powers";
        public const string TriggersFolder = StatusAssetBuilder.Root + "/Triggers";
        public const string ExilePath = PowersFolder + "/VowOfTheExile.asset";
        public const string DevotionPath = PowersFolder + "/Devotion.asset";
        public const string AgniPath = PowersFolder + "/BlessingOfAgni.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Power Cards", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Power Cards", Build);
        }

        [DefaultAssetBuilder(112)]
        public static void Build(AssetBuilderReport report)
        {
            var attack = AssetDatabase.LoadAssetAtPath<CardTypeSO>(CombatAssetBuilder.CardTypesFolder + "/Attack.asset");
            var invocation = AssetDatabase.LoadAssetAtPath<CardTypeSO>(CombatAssetBuilder.CardTypesFolder + "/Invocation.asset");
            var chosen = AssetDatabase.LoadAssetAtPath<ChosenTargetRuleSO>(CombatAssetBuilder.TargetingFolder + "/ChosenTarget.asset");
            var dharma = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.DharmaPath);
            var flatRule = AssetDatabase.LoadAssetAtPath<FlatOutgoingDamageRuleSO>(StatusAssetBuilder.RulesFolder + "/FocusDamageRule.asset");
            if (attack == null || invocation == null || chosen == null)
            {
                report.AddSkipped("Power cards (card types or targeting missing, run Combat and Status Cards first)");
                return;
            }

            if (dharma == null || flatRule == null)
            {
                report.AddSkipped("Power cards (Dharma or the flat damage rule missing, run the Statuses builder first)");
                return;
            }

            CardTypeSO power = AssetBuilderSerialization.LoadOrCreateConfigured<CardTypeSO>(
                CombatAssetBuilder.CardTypesFolder + "/Power.asset", report, so =>
                {
                    AssetBuilderSerialization.SetString(so, "_id", "power");
                    AssetBuilderSerialization.SetString(so, "_nameKey", "card.type.power");
                });

            TurnStartGainStatusTriggerSO exileTrigger = AssetBuilderSerialization.LoadOrCreateConfigured<TurnStartGainStatusTriggerSO>(
                TriggersFolder + "/ExileTrigger.asset", report, so => AssetBuilderSerialization.SetInt(so, "_amount", 1));
            AssetBuilderUtility.WireAssetFieldIfNull(exileTrigger, "_status", dharma, report);
            StatusGainedBlockTriggerSO devotionTrigger = AssetBuilderSerialization.LoadOrCreateConfigured<StatusGainedBlockTriggerSO>(
                TriggersFolder + "/DevotionTrigger.asset", report, so => AssetBuilderSerialization.SetInt(so, "_blockPerStack", 2));
            AssetBuilderUtility.WireAssetFieldIfNull(devotionTrigger, "_status", dharma, report);

            StatusDefinitionSO exile = Power(ExilePath, "exile", new Color(0.42f, 0.74f, 0.46f), 1, null, exileTrigger, report);
            StatusDefinitionSO devotion = Power(DevotionPath, "devotion", new Color(0.96f, 0.66f, 0.72f), 1, null, devotionTrigger, report);
            StatusDefinitionSO agni = Power(AgniPath, "agni", new Color(1f, 0.45f, 0.25f), 2, flatRule, null, report);

            ApplyStatusEffectSO gainExile = GainPower("GainPower_Exile", exile, 1, report);
            ApplyStatusEffectSO gainDevotion = GainPower("GainPower_Devotion", devotion, 1, report);
            ApplyStatusEffectSO gainAgni = GainPower("GainPower_Agni", agni, 2, report);

            RemoveBlockEffectSO strip = AssetBuilderSerialization.LoadOrCreateConfigured<RemoveBlockEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/RemoveBlock_Chosen.asset", report, null);
            AssetBuilderUtility.WireAssetFieldIfNull(strip, "_targets", chosen, report);

            DamageEffectSO damage8 = Damage("Damage_8", 8, 1, chosen, report);
            DamageEffectSO damage12 = Damage("Damage_12", 12, 1, chosen, report);
            DamageEffectSO damage9 = Damage("Damage_9", 9, 1, chosen, report);
            DamageEffectSO damage13 = Damage("Damage_13", 13, 1, chosen, report);
            DamageEffectSO damage4x3 = Damage("Damage_4x3", 4, 3, chosen, report);
            DamageEffectSO damage5x3 = Damage("Damage_5x3", 5, 3, chosen, report);

            CardDefinitionSO exileCard = Card("VowOfTheExile", "exile", 2, power, true, null, report, gainExile);
            CardDefinitionSO devotionCard = Card("Devotion", "devotion", 1, power, true, null, report, gainDevotion);
            CardDefinitionSO agniCard = Card("BlessingOfAgni", "agnibless", 2, power, true, null, report, gainAgni);
            CardDefinitionSO vayuPlus = Card("VayavyastraPlus", "vayu_plus", 1, invocation, false, null, report, strip, damage12);
            CardDefinitionSO vayu = Card("Vayavyastra", "vayu", 1, invocation, false, vayuPlus, report, strip, damage8);
            CardDefinitionSO galePlus = Card("GaleArrowPlus", "gale_plus", 1, invocation, false, null, report, strip, damage13);
            CardDefinitionSO gale = Card("GaleArrow", "gale", 1, invocation, false, galePlus, report, strip, damage9);
            CardDefinitionSO windforcePlus = Card("WindforceVolleyPlus", "windforce_plus", 2, attack, false, null, report, damage5x3);
            CardDefinitionSO windforce = Card("WindforceVolley", "windforce", 2, attack, false, windforcePlus, report, damage4x3);

            var pool = AssetDatabase.LoadAssetAtPath<CardRewardPoolSO>(RewardCardsAssetBuilder.SlicePoolPath);
            if (pool == null)
            {
                report.AddSkipped($"{RewardCardsAssetBuilder.SlicePoolPath} (missing, run the Reward Cards builder first)");
                return;
            }

            AssetBuilderSerialization.AppendObjectsIfMissing(pool, "_cards", new Object[]
            {
                exileCard, devotionCard, agniCard, vayu, gale, windforce,
            }, report);
        }

        private static StatusDefinitionSO Power(string path, string id, Color color, int maxAmount, StatusDamageRuleSO rule,
            StatusTriggerSO trigger, AssetBuilderReport report)
        {
            StatusDefinitionSO status = AssetBuilderSerialization.LoadOrCreateConfigured<StatusDefinitionSO>(path, report, so =>
            {
                AssetBuilderSerialization.SetString(so, "_id", id);
                AssetBuilderSerialization.SetString(so, "_nameKey", "status." + id + ".name");
                AssetBuilderSerialization.SetString(so, "_descriptionKey", "status." + id + ".description");
                AssetBuilderSerialization.SetString(so, "_shortNameKey", "status." + id + ".short");
                AssetBuilderSerialization.Require(so, "_color").colorValue = color;
                AssetBuilderSerialization.Require(so, "_isDebuff").boolValue = false;
                AssetBuilderSerialization.SetInt(so, "_maxAmount", maxAmount);
                AssetBuilderSerialization.SetInt(so, "_defaultDuration", 0);
            });

            if (rule != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(status, "_damageRule", rule, report);
            }

            if (trigger != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(status, "_trigger", trigger, report);
            }

            return status;
        }

        private static ApplyStatusEffectSO GainPower(string fileName, StatusDefinitionSO status, int amount, AssetBuilderReport report)
        {
            ApplyStatusEffectSO effect = AssetBuilderSerialization.LoadOrCreateConfigured<ApplyStatusEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", amount);
                    AssetBuilderSerialization.SetInt(so, "_duration", 0);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(effect, "_status", status, report);
            return effect;
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

        private static CardDefinitionSO Card(string fileName, string id, int cost, CardTypeSO type, bool exhausts,
            CardDefinitionSO upgradedForm, AssetBuilderReport report, params CombatEffectSO[] effects)
        {
            CardDefinitionSO card = AssetBuilderSerialization.LoadOrCreateConfigured<CardDefinitionSO>(
                CombatAssetBuilder.CardsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetString(so, "_id", id);
                    AssetBuilderSerialization.SetString(so, "_nameKey", "card." + id + ".name");
                    AssetBuilderSerialization.SetString(so, "_descriptionKey", "card." + id + ".description");
                    AssetBuilderSerialization.SetInt(so, "_cost", cost);
                    AssetBuilderSerialization.Require(so, "_exhausts").boolValue = exhausts;
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
