// Creates the vertical slice run: Rama with the slice starting deck, then a Nishachar Scout, two Cursed Hounds and
// a second Scout, played in the registered combat scene, with the slice reward pool after each won fight, the normal
// card reward odds as the run's default, and the special-arrow drop table. It also creates the CardRewardOffered and
// CardRewardChosen channels. References and lists are filled only while empty, so an existing run keeps its
// encounters.
// The combat scene is taken from Data/Scenes when exactly one Scene Definition exists there; otherwise assign it.
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Scenes;
using JourneyToLanka.Scenes.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Run.EditorTools
{
    public static class RunAssetBuilder
    {
        public const string Root = AssetBuilderConstants.DataRoot + "/Run";
        public const string SliceRunPath = Root + "/Run_Slice.asset";
        public const string ChannelsFolder = Root + "/Channels";
        public const string CardRewardOfferedPath = ChannelsFolder + "/CardRewardOffered.asset";
        public const string CardRewardChosenPath = ChannelsFolder + "/CardRewardChosen.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Run", priority = AssetBuilderConstants.FeatureMenuPriority + 17)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Run", Build);
        }

        [DefaultAssetBuilder(140)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderSerialization.LoadOrCreateConfigured<VoidEventChannelSO>(CardRewardOfferedPath, report,
                so => AssetBuilderSerialization.SetString(so, "_description", "Raised by the run director when the player should pick a card reward."));
            AssetBuilderSerialization.LoadOrCreateConfigured<IntEventChannelSO>(CardRewardChosenPath, report,
                so => AssetBuilderSerialization.SetString(so, "_description", "Raised by the reward screen with the chosen card index, or -1 to take nothing."));

            RunDefinitionSO run = AssetBuilderSerialization.LoadOrCreateConfigured<RunDefinitionSO>(SliceRunPath, report,
                so => AssetBuilderSerialization.SetString(so, "_id", "run_slice"));
            if (run == null)
            {
                return;
            }

            AssetBuilderUtility.WireAssetFieldIfNull(run, "_player",
                AssetDatabase.LoadAssetAtPath<PlayerDefinitionSO>(CombatAssetBuilder.PlayerPath), report);

            var scout = AssetDatabase.LoadAssetAtPath<CombatEncounterSO>(CombatAssetBuilder.ScoutEncounterPath);
            var hounds = AssetDatabase.LoadAssetAtPath<CombatEncounterSO>(ChapterOneEnemiesAssetBuilder.HoundsEncounterPath);
            if (scout != null && hounds != null)
            {
                AssetBuilderSerialization.FillObjectArrayIfEmpty(run, "_encounters", new Object[] { scout, hounds, scout }, report);
            }
            else
            {
                report.AddSkipped("Run encounters (Scout or Hounds encounter missing, run the Combat builders first)");
            }

            AssetBuilderUtility.WireAssetFieldIfNull(run, "_rewardPool",
                AssetDatabase.LoadAssetAtPath<CardRewardPoolSO>(RewardCardsAssetBuilder.SlicePoolPath), report);

            var rewardOdds = AssetDatabase.LoadAssetAtPath<CardRewardOddsSO>(CardRarityAssetBuilder.NormalOddsPath);
            if (rewardOdds != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(run, "_rewardOdds", rewardOdds, report);
            }
            else
            {
                report.AddSkipped($"{SliceRunPath} reward odds ({CardRarityAssetBuilder.NormalOddsPath} missing, run Card Rarity first)");
            }

            var arrowDrops = AssetDatabase.LoadAssetAtPath<ArrowDropTableSO>(ArrowsAssetBuilder.DropTablePath);
            if (arrowDrops != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(run, "_arrowDrops", arrowDrops, report);
            }
            else
            {
                report.AddSkipped($"{SliceRunPath} arrow drops ({ArrowsAssetBuilder.DropTablePath} missing, run Special Arrows first)");
            }

            SceneDefinitionSO combatScene = FindSingleSceneDefinition();
            if (combatScene != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(run, "_combatScene", combatScene, report);
            }
            else
            {
                report.AddSkipped($"{SliceRunPath} combat scene (register the combat scene, or assign it by hand if there are several)");
            }
        }

        private static SceneDefinitionSO FindSingleSceneDefinition()
        {
            if (!AssetDatabase.IsValidFolder(SceneAssetBuilder.Root))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("t:" + nameof(SceneDefinitionSO), new[] { SceneAssetBuilder.Root });
            return guids.Length == 1
                ? AssetDatabase.LoadAssetAtPath<SceneDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
