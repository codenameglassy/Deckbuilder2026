// Timing for the fight: the pause before the first enemy acts, the windup between an enemy starting its action and
// the action resolving (views play the attack during it), the gap after each action before the next enemy starts,
// the pause before control returns to the player, and the flight of a special arrow between Rama loosing it and it
// resolving (views fly the arrow during it, so its damage lands on arrival; zero resolves it at once). Shared
// flyweight profile with no runtime state; tune it by feel.
using UnityEngine;

namespace JourneyToLanka.Combat.Flow
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combat Pacing", fileName = "CombatPacing")]
    public sealed class CombatPacingSO : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _enemyTurnStartDelay = 0.5f;
        [SerializeField, Min(0f)] private float _enemyWindup = 0.2f;
        [SerializeField, Min(0f)] private float _enemyActionInterval = 0.8f;
        [SerializeField, Min(0f)] private float _enemyTurnEndDelay = 0.5f;
        [Tooltip("Seconds between Rama loosing a special arrow and it resolving. Player input is refused meanwhile.")]
        [SerializeField, Min(0f)] private float _arrowFlight = 0.45f;

        public float EnemyTurnStartDelay => _enemyTurnStartDelay;

        public float EnemyWindup => _enemyWindup;

        public float EnemyActionInterval => _enemyActionInterval;

        public float EnemyTurnEndDelay => _enemyTurnEndDelay;

        public float ArrowFlight => _arrowFlight;
    }
}
