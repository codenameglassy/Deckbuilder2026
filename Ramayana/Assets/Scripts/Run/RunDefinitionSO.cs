// A run as authored data: who plays (the player definition supplies starting health and deck), the fights in
// order, the scene each fight is played in, and the card reward offered after each won fight (how many distinct
// cards to offer and the pool they come from). The vertical slice is a straight line of encounters; a branching
// map will replace the list later without changing the run state's job.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Scenes;
using UnityEngine;

namespace JourneyToLanka.Run
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Run/Run Definition", fileName = "RunDefinition")]
    public sealed class RunDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private PlayerDefinitionSO _player;
        [SerializeField] private CombatEncounterSO[] _encounters = Array.Empty<CombatEncounterSO>();
        [SerializeField] private SceneDefinitionSO _combatScene;
        [SerializeField] private CardRewardPoolSO _rewardPool;
        [SerializeField, Min(1)] private int _rewardChoices = 3;

        public string Id => _id;

        public PlayerDefinitionSO Player => _player;

        public IReadOnlyList<CombatEncounterSO> Encounters => _encounters;

        public SceneDefinitionSO CombatScene => _combatScene;

        public CardRewardPoolSO RewardPool => _rewardPool;

        public int RewardChoices => _rewardChoices;
    }
}
