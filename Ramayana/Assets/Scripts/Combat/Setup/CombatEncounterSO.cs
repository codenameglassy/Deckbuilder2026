// A fight as authored data: the enemies that appear, in field order (field order is also acting order), and the
// rarity odds of the card reward for winning it. Elite fights point at the elite odds; an encounter without odds
// uses its run's default. The reference build never puts more than three enemies in one fight.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Enemies;
using UnityEngine;

namespace JourneyToLanka.Combat
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Encounter", fileName = "Encounter")]
    public sealed class CombatEncounterSO : ScriptableObject
    {
        public const int MaxEnemies = 3;

        [SerializeField] private string _id;
        [SerializeField] private EnemyDefinitionSO[] _enemies = Array.Empty<EnemyDefinitionSO>();
        [Tooltip("Rarity odds of the card reward for winning this fight. Leave empty to use the run's default odds.")]
        [SerializeField] private CardRewardOddsSO _rewardOdds;

        public string Id => _id;

        public IReadOnlyList<EnemyDefinitionSO> Enemies => _enemies;

        public CardRewardOddsSO RewardOdds => _rewardOdds;

        private void OnValidate()
        {
            if (_enemies != null && _enemies.Length > MaxEnemies)
            {
                Debug.LogWarning($"{name}: {_enemies.Length} enemies, but the combat stage has only {MaxEnemies} slots.", this);
            }
        }
    }
}
