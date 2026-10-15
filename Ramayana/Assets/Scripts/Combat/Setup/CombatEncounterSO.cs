// A fight as authored data: the enemies that appear, in field order. Field order is also acting order.
// The reference build never puts more than three enemies in one fight.
using System;
using System.Collections.Generic;
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

        public string Id => _id;

        public IReadOnlyList<EnemyDefinitionSO> Enemies => _enemies;

        private void OnValidate()
        {
            if (_enemies != null && _enemies.Length > MaxEnemies)
            {
                Debug.LogWarning($"{name}: {_enemies.Length} enemies, but the combat stage has only {MaxEnemies} slots.", this);
            }
        }
    }
}
