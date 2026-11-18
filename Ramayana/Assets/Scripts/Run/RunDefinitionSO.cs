// A run as authored data: who plays (the player definition supplies starting health, deck and quiver), the fights in
// order, the scene each fight is played in, the card reward offered after each won fight (how many distinct cards to
// offer, the pool they come from, and the default rarity odds for encounters that set none; without any odds every
// card in the pool is equally likely), and how special arrows drop after a won fight (none without a drop table). The
// vertical slice is a straight line of encounters; a branching map will replace the list later without changing the
// run state's job.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Arrows;
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
        [Tooltip("Rarity odds for card rewards after fights whose encounter sets none (normal fights).")]
        [SerializeField] private CardRewardOddsSO _rewardOdds;
        [SerializeField] private ArrowDropTableSO _arrowDrops;

        public string Id => _id;

        public PlayerDefinitionSO Player => _player;

        public IReadOnlyList<CombatEncounterSO> Encounters => _encounters;

        public SceneDefinitionSO CombatScene => _combatScene;

        public CardRewardPoolSO RewardPool => _rewardPool;

        public int RewardChoices => _rewardChoices;

        public CardRewardOddsSO RewardOdds => _rewardOdds;

        public ArrowDropTableSO ArrowDrops => _arrowDrops;
    }
}
