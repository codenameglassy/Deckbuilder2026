// Creates the reference build's status and Dharma cards with their balance pass values and upgraded forms: Terror of
// the Bow, Break the Illusion, Agneyastra, Sanjeevani Leaf, Tapasya, Vow of Dharma, Meditation, Piercing Vow,
// Ancient Endurance and Brahmastra, the Dharma, Invocation and Astra card types they use, and their effects. Effects
// that come before a hit in a card's list resolve first, so Bhaya raises the hit that follows it. The reward cards
// are appended to the slice reward pool if they are not already there; nothing in the pool is removed or reordered.
// Vow of Dharma is a starter (the Starting Deck builder puts it in the deck), and Brahmastra is created but kept out
// of the pool until it gets its mantra minigame. Values are set only on assets this run creates; references and lists
// are filled only while empty. Runs after Combat, Reward Cards and Statuses.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class StatusCardsAssetBuilder
    {
        public const string BonusesFolder = CombatAssetBuilder.EffectsFolder + "/Bonuses";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Status Cards", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Status Cards", Build);
        }

        [DefaultAssetBuilder(104)]
        public static void Build(AssetBuilderReport report)
        {
            var attack = AssetDatabase.LoadAssetAtPath<CardTypeSO>(CombatAssetBuilder.CardTypesFolder + "/Attack.asset");
            var skill = AssetDatabase.LoadAssetAtPath<CardTypeSO>(CombatAssetBuilder.CardTypesFolder + "/Skill.asset");
            var chosen = AssetDatabase.LoadAssetAtPath<ChosenTargetRuleSO>(CombatAssetBuilder.TargetingFolder + "/ChosenTarget.asset");
            var focus = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.FocusPath);
            var moha = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.MohaPath);
            var bhaya = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.BhayaPath);
            var dahan = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.DahanPath);
            var dharma = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.DharmaPath);
            if (attack == null || skill == null || chosen == null)
            {
                report.AddSkipped("Status cards (card types or targeting missing, run the Combat builder first)");
                return;
            }

            if (focus == null || moha == null || bhaya == null || dahan == null || dharma == null)
            {
                report.AddSkipped("Status cards (statuses missing, run the Statuses builder first)");
                return;
            }

            CardTypeSO dharmaType = CardType("Dharma", "dharma", report);
            CardTypeSO invocation = CardType("Invocation", "invocation", report);
            CardTypeSO astra = CardType("Astra", "astra", report);

            ApplyStatusEffectSO bhaya2 = Status("ApplyBhaya_2", bhaya, 2, 0, chosen, report);
            ApplyStatusEffectSO bhaya3 = Status("ApplyBhaya_3", bhaya, 3, 0, chosen, report);
            ApplyStatusEffectSO moha2 = Status("ApplyMoha_2", moha, 2, 0, chosen, report);
            ApplyStatusEffectSO moha3 = Status("ApplyMoha_3", moha, 3, 0, chosen, report);
            ApplyStatusEffectSO dahan3 = Status("ApplyDahan_3", dahan, 3, 0, chosen, report);
            ApplyStatusEffectSO dahan4 = Status("ApplyDahan_4", dahan, 4, 0, chosen, report);
            ApplyStatusEffectSO focus3 = Status("GainFocus_3", focus, 3, 1, null, report);
            ApplyStatusEffectSO focus4 = Status("GainFocus_4", focus, 4, 1, null, report);
            ApplyStatusEffectSO dharma2 = Status("GainDharma_2", dharma, 2, 0, null, report);
            ApplyStatusEffectSO dharma3 = Status("GainDharma_3", dharma, 3, 0, null, report);
            ApplyStatusEffectSO dharma4 = Status("GainDharma_4", dharma, 4, 0, null, report);

            StatusThresholdDamageBonusSO dharmaBonus = AssetBuilderSerialization.LoadOrCreateConfigured<StatusThresholdDamageBonusSO>(
                BonusesFolder + "/DharmaBonus_3_Plus6.asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_threshold", 3);
                    AssetBuilderSerialization.SetInt(so, "_bonus", 6);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(dharmaBonus, "_status", dharma, report);

            DamageEffectSO damage4 = Damage("Damage_4", 4, chosen, null, report);
            DamageEffectSO damage6 = Damage("Damage_6", 6, chosen, null, report);
            DamageEffectSO damage11 = Damage("Damage_11", 11, chosen, null, report);
            DamageEffectSO damage15 = Damage("Damage_15", 15, chosen, null, report);
            DamageEffectSO pierce5 = Damage("Damage_5_DharmaBonus", 5, chosen, dharmaBonus, report);
            DamageEffectSO pierce8 = Damage("Damage_8_DharmaBonus", 8, chosen, dharmaBonus, report);

            BlockEffectSO block4 = Block("Block_4", 4, false, report);
            BlockEffectSO block6 = Block("Block_6", 6, false, report);
            BlockEffectSO retain11 = Block("Block_11_Retain", 11, true, report);
            BlockEffectSO retain15 = Block("Block_15_Retain", 15, true, report);

            HealEffectSO heal8 = Heal("Heal_8", 8, report);
            HealEffectSO heal12 = Heal("Heal_12", 12, report);
            DrawCardsEffectSO draw1 = AssetBuilderSerialization.LoadOrCreateConfigured<DrawCardsEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/Draw_1.asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_count", 1));

            SpendStatusDamageEffectSO brahmastraHit = AssetBuilderSerialization.LoadOrCreateConfigured<SpendStatusDamageEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/SpendDharma_10_Plus7.asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_baseDamage", 10);
                    AssetBuilderSerialization.SetInt(so, "_damagePerStack", 7);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(brahmastraHit, "_status", dharma, report);
            AssetBuilderUtility.WireAssetFieldIfNull(brahmastraHit, "_targets", chosen, report);

            CardDefinitionSO fearPlus = Card("TerrorOfTheBowPlus", "fear_plus", 1, skill, false, null, report, bhaya3, damage6);
            CardDefinitionSO fear = Card("TerrorOfTheBow", "fear", 1, skill, false, fearPlus, report, bhaya2, damage4);
            CardDefinitionSO illusionPlus = Card("BreakTheIllusionPlus", "breakil_plus", 1, skill, false, null, report, moha3, block6);
            CardDefinitionSO illusion = Card("BreakTheIllusion", "breakil", 1, skill, false, illusionPlus, report, moha2, block4);
            CardDefinitionSO agneyastraPlus = Card("AgneyastraPlus", "agneya_plus", 2, invocation, false, null, report, dahan4, damage15);
            CardDefinitionSO agneyastra = Card("Agneyastra", "agneya", 2, invocation, false, agneyastraPlus, report, dahan3, damage11);
            CardDefinitionSO herbPlus = Card("SanjeevaniLeafPlus", "herb_plus", 1, skill, true, null, report, heal12);
            CardDefinitionSO herb = Card("SanjeevaniLeaf", "herb", 1, skill, true, herbPlus, report, heal8);
            CardDefinitionSO tapasyaPlus = Card("TapasyaPlus", "tapasya_plus", 1, dharmaType, false, null, report, focus4);
            CardDefinitionSO tapasya = Card("Tapasya", "tapasya", 1, dharmaType, false, tapasyaPlus, report, focus3);
            CardDefinitionSO vowPlus = Card("VowOfDharmaPlus", "vow_plus", 0, dharmaType, false, null, report, dharma3);
            Card("VowOfDharma", "vow", 0, dharmaType, false, vowPlus, report, dharma2);
            CardDefinitionSO meditationPlus = Card("MeditationPlus", "medit_plus", 1, dharmaType, false, null, report, dharma4, draw1);
            CardDefinitionSO meditation = Card("Meditation", "medit", 1, dharmaType, false, meditationPlus, report, dharma3, draw1);
            CardDefinitionSO piercePlus = Card("PiercingVowPlus", "pierce_plus", 1, attack, false, null, report, pierce8);
            CardDefinitionSO pierce = Card("PiercingVow", "pierce", 1, attack, false, piercePlus, report, pierce5);
            CardDefinitionSO endurePlus = Card("AncientEndurancePlus", "endure_plus", 1, skill, false, null, report, retain15);
            CardDefinitionSO endure = Card("AncientEndurance", "endure", 1, skill, false, endurePlus, report, retain11);
            Card("Brahmastra", "brahma", 3, astra, true, null, report, brahmastraHit);

            var pool = AssetDatabase.LoadAssetAtPath<CardRewardPoolSO>(RewardCardsAssetBuilder.SlicePoolPath);
            if (pool == null)
            {
                report.AddSkipped($"{RewardCardsAssetBuilder.SlicePoolPath} (missing, run the Reward Cards builder first)");
                return;
            }

            AssetBuilderSerialization.AppendObjectsIfMissing(pool, "_cards", new Object[]
            {
                fear, illusion, agneyastra, herb, tapasya, meditation, pierce, endure,
            }, report);
        }

        private static CardTypeSO CardType(string fileName, string id, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<CardTypeSO>(
                CombatAssetBuilder.CardTypesFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetString(so, "_id", id);
                    AssetBuilderSerialization.SetString(so, "_nameKey", "card.type." + id);
                });
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

        private static DamageEffectSO Damage(string fileName, int amount, TargetRuleSO targets, DamageBonusSO bonus,
            AssetBuilderReport report)
        {
            DamageEffectSO effect = AssetBuilderSerialization.LoadOrCreateConfigured<DamageEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", amount);
                    AssetBuilderSerialization.SetInt(so, "_hits", 1);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(effect, "_targets", targets, report);
            if (bonus != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(effect, "_bonus", bonus, report);
            }

            return effect;
        }

        private static BlockEffectSO Block(string fileName, int amount, bool retain, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<BlockEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", amount);
                    AssetBuilderSerialization.Require(so, "_retain").boolValue = retain;
                });
        }

        private static HealEffectSO Heal(string fileName, int amount, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<HealEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/" + fileName + ".asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_amount", amount));
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
