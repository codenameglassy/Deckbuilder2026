// Creates the vertical slice run: Rama with the slice starting deck, three Nishachar Scout fights in a row, played
// in the registered combat scene, with the slice reward pool after each won fight. It also creates the
// CardRewardOffered and CardRewardChosen channels. References are filled only while empty.
// The combat scene is taken from Data/Scenes when exactly one Scene Definition exists there; otherwise assign it.
using JourneyToLanka.Combat;
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

        private const int SliceEncounterCount = 3;

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
            if (scout != null)
            {
                var encounters = new Object[SliceEncounterCount];
                for (int i = 0; i < encounters.Length; i++)
                {
                    encounters[i] = scout;
                }

                AssetBuilderSerialization.FillObjectArrayIfEmpty(run, "_encounters", encounters, report);
            }
            else
            {
                report.AddSkipped($"{CombatAssetBuilder.ScoutEncounterPath} (missing, run the Combat builder first)");
            }

            AssetBuilderUtility.WireAssetFieldIfNull(run, "_rewardPool",
                AssetDatabase.LoadAssetAtPath<CardRewardPoolSO>(RewardCardsAssetBuilder.SlicePoolPath), report);

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
