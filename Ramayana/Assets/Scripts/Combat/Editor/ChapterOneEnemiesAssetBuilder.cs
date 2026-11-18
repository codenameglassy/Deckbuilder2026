// Creates the Cursed Hound from the reference build's chapter one with its balance pass values (20 to 24 health;
// 4 damage twice, then 7 damage, then defend 5), reusing the existing effects, and the two-hound encounter.
// Values are set only on assets this run creates; lists are filled only while empty. Runs after Combat.
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class ChapterOneEnemiesAssetBuilder
    {
        public const string HoundPath = CombatAssetBuilder.EnemiesFolder + "/CursedHound.asset";
        public const string HoundsEncounterPath = CombatAssetBuilder.EncountersFolder + "/Encounter_Hounds.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Chapter One Enemies", priority = AssetBuilderConstants.FeatureMenuPriority + 9)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Chapter One Enemies", Build);
        }

        [DefaultAssetBuilder(102)]
        public static void Build(AssetBuilderReport report)
        {
            var damage4x2 = AssetDatabase.LoadAssetAtPath<DamageEffectSO>(CombatAssetBuilder.EffectsFolder + "/Damage_4x2.asset");
            var damage7 = AssetDatabase.LoadAssetAtPath<DamageEffectSO>(CombatAssetBuilder.EffectsFolder + "/Damage_7.asset");
            var block5 = AssetDatabase.LoadAssetAtPath<BlockEffectSO>(CombatAssetBuilder.EffectsFolder + "/Block_5.asset");
            if (damage4x2 == null || damage7 == null || block5 == null)
            {
                report.AddSkipped("Cursed Hound (combat effects missing, run the Combat builder first)");
                return;
            }

            EnemyMoveSO attack4x2 = Move("CursedHound_Attack4x2", "hound_attack_4x2", report, damage4x2);
            EnemyMoveSO attack7 = Move("CursedHound_Attack7", "hound_attack_7", report, damage7);
            EnemyMoveSO defend5 = Move("CursedHound_Defend5", "hound_defend_5", report, block5);

            EnemyDefinitionSO hound = AssetBuilderSerialization.LoadOrCreateConfigured<EnemyDefinitionSO>(HoundPath, report, so =>
            {
                AssetBuilderSerialization.SetString(so, "_id", "hound");
                AssetBuilderSerialization.SetString(so, "_nameKey", "enemy.hound.name");
                AssetBuilderSerialization.SetInt(so, "_minHealth", 20);
                AssetBuilderSerialization.SetInt(so, "_maxHealth", 24);
                AssetBuilderSerialization.SetInt(so, "_startMoveIndex", 0);
            });
            if (hound == null)
            {
                return;
            }

            AssetBuilderSerialization.FillObjectArrayIfEmpty(hound, "_moves", new Object[] { attack4x2, attack7, defend5 }, report);

            CombatEncounterSO encounter = AssetBuilderSerialization.LoadOrCreateConfigured<CombatEncounterSO>(HoundsEncounterPath, report,
                so => AssetBuilderSerialization.SetString(so, "_id", "encounter_hounds"));
            if (encounter != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(encounter, "_enemies", new Object[] { hound, hound }, report);
            }
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
    }
}
