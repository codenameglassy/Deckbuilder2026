// Timing for the enemy turn: the pause before the first enemy acts, the gap between enemy actions, and the pause
// before control returns to the player. Shared flyweight profile with no runtime state; tune it by feel.
using UnityEngine;

namespace JourneyToLanka.Combat.Flow
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combat Pacing", fileName = "CombatPacing")]
    public sealed class CombatPacingSO : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _enemyTurnStartDelay = 0.5f;
        [SerializeField, Min(0f)] private float _enemyActionInterval = 0.8f;
        [SerializeField, Min(0f)] private float _enemyTurnEndDelay = 0.5f;

        public float EnemyTurnStartDelay => _enemyTurnStartDelay;

        public float EnemyActionInterval => _enemyActionInterval;

        public float EnemyTurnEndDelay => _enemyTurnEndDelay;
    }
}
