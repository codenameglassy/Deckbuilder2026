// Creates the chapter one enemies whose patterns use statuses, with the reference build's balance pass values:
// the Corrupted Yaksha (40 to 46 health; attack 9, gain 3 Focus, attack 7), whose Focus lasts through its next
// action, and the elite Maya-Marich (64 to 70 health; attack 12, inflict 2 Moha, defend 10, attack 7 twice), each with
// its own encounter. The run's encounter list is left as it is; add these encounters to it in the inspector. Values
// are set only on assets this run creates; lists are filled only while empty. Runs after Combat and Status Cards.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class StatusEnemiesAssetBuilder
    {
        public const string YakshaPath = CombatAssetBuilder.EnemiesFolder + "/CorruptedYaksha.asset";
        public const string MayaMarichPath = CombatAssetBuilder.EnemiesFolder + "/MayaMarich.asset";
        public const string YakshaEncounterPath = CombatAssetBuilder.EncountersFolder + "/Encounter_Yaksha.asset";
        public const string MayaMarichEncounterPath = CombatAssetBuilder.EncountersFolder + "/Encounter_MayaMarich.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Status Enemies", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Status Enemies", Build);
        }

        [DefaultAssetBuilder(111)]
        public static void Build(AssetBuilderReport report)
        {
            var chosen = AssetDatabase.LoadAssetAtPath<ChosenTargetRuleSO>(CombatAssetBuilder.TargetingFolder + "/ChosenTarget.asset");
            var damage7 = AssetDatabase.LoadAssetAtPath<DamageEffectSO>(CombatAssetBuilder.EffectsFolder + "/Damage_7.asset");
            var moha2 = AssetDatabase.LoadAssetAtPath<ApplyStatusEffectSO>(CombatAssetBuilder.EffectsFolder + "/ApplyMoha_2.asset");
            var focus = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(StatusAssetBuilder.FocusPath);
            if (chosen == null || damage7 == null || moha2 == null || focus == null)
            {
                report.AddSkipped("Status enemies (effects or statuses missing, run Combat, Statuses and Status Cards first)");
                return;
            }

            DamageEffectSO damage9 = Damage("Damage_9", 9, 1, chosen, report);
            DamageEffectSO damage12 = Damage("Damage_12", 12, 1, chosen, report);
            DamageEffectSO damage7x2 = Damage("Damage_7x2", 7, 2, chosen, report);
            BlockEffectSO block10 = AssetBuilderSerialization.LoadOrCreateConfigured<BlockEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/Block_10.asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_amount", 10));
            ApplyStatusEffectSO enemyFocus3 = AssetBuilderSerialization.LoadOrCreateConfigured<ApplyStatusEffectSO>(
                CombatAssetBuilder.EffectsFolder + "/EnemyFocus_3.asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", 3);
                    AssetBuilderSerialization.SetInt(so, "_duration", 2);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(enemyFocus3, "_status", focus, report);

            EnemyMoveSO yakshaAttack9 = Move("CorruptedYaksha_Attack9", "yaksha_attack_9", report, damage9);
            EnemyMoveSO yakshaFocus3 = Move("CorruptedYaksha_Focus3", "yaksha_focus_3", report, enemyFocus3);
            EnemyMoveSO yakshaAttack7 = Move("CorruptedYaksha_Attack7", "yaksha_attack_7", report, damage7);
            EnemyDefinitionSO yaksha = Enemy(YakshaPath, "yaksha", 40, 46, report, yakshaAttack9, yakshaFocus3, yakshaAttack7);

            EnemyMoveSO mayaAttack12 = Move("MayaMarich_Attack12", "maya_attack_12", report, damage12);
            EnemyMoveSO mayaMoha2 = Move("MayaMarich_Moha2", "maya_moha_2", report, moha2);
            EnemyMoveSO mayaDefend10 = Move("MayaMarich_Defend10", "maya_defend_10", report, block10);
            EnemyMoveSO mayaAttack7x2 = Move("MayaMarich_Attack7x2", "maya_attack_7x2", report, damage7x2);
            EnemyDefinitionSO maya = Enemy(MayaMarichPath, "maya", 64, 70, report, mayaAttack12, mayaMoha2, mayaDefend10, mayaAttack7x2);

            Encounter(YakshaEncounterPath, "encounter_yaksha", yaksha, report);
            Encounter(MayaMarichEncounterPath, "encounter_maya", maya, report);
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

        private static EnemyMoveSO Move(string fileName, string id, AssetBuilderReport report, CombatEffectSO effect)
        {
            EnemyMoveSO move = AssetBuilderSerialization.LoadOrCreateConfigured<EnemyMoveSO>(
                CombatAssetBuilder.EnemyMovesFolder + "/" + fileName + ".asset", report,
                so => AssetBuilderSerialization.SetString(so, "_id", id));
            if (move != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(move, "_effects", new Object[] { effect }, report);
            }

            return move;
        }

        private static EnemyDefinitionSO Enemy(string path, string id, int minHealth, int maxHealth, AssetBuilderReport report,
            params EnemyMoveSO[] moves)
        {
            EnemyDefinitionSO enemy = AssetBuilderSerialization.LoadOrCreateConfigured<EnemyDefinitionSO>(path, report, so =>
            {
                AssetBuilderSerialization.SetString(so, "_id", id);
                AssetBuilderSerialization.SetString(so, "_nameKey", "enemy." + id + ".name");
                AssetBuilderSerialization.SetInt(so, "_minHealth", minHealth);
                AssetBuilderSerialization.SetInt(so, "_maxHealth", maxHealth);
                AssetBuilderSerialization.SetInt(so, "_startMoveIndex", 0);
            });
            if (enemy != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(enemy, "_moves", moves, report);
            }

            return enemy;
        }

        private static void Encounter(string path, string id, EnemyDefinitionSO enemy, AssetBuilderReport report)
        {
            CombatEncounterSO encounter = AssetBuilderSerialization.LoadOrCreateConfigured<CombatEncounterSO>(path, report,
                so => AssetBuilderSerialization.SetString(so, "_id", id));
            if (encounter != null && enemy != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(encounter, "_enemies", new Object[] { enemy }, report);
            }
        }
    }
}
