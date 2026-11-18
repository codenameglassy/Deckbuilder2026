// Every value for how hits feel at the scene level: the hit-stop (how long and how slow a hit freezes the fight,
// longer for heavy hits and killing blows), what counts as a heavy hit, and the camera shake when Rama or an enemy
// loses health. Fully blocked hits cause neither, so Kavach reads as safety. Shared flyweight with no runtime state.
// The director reads these values each time a hit lands, so edits in Play mode apply to the next hit.
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combat Impact Style", fileName = "CombatImpactStyle")]
    public sealed class CombatImpactStyleSO : ScriptableObject
    {
        [Header("Hit Stop")]
        [SerializeField, Min(0f)] private float _hitStopDuration = 0.05f;
        [SerializeField, Min(0f)] private float _heavyHitStopDuration = 0.09f;
        [SerializeField, Min(0f)] private float _killHitStopDuration = 0.14f;
        [SerializeField, Range(0f, 1f)] private float _hitStopTimeScale = 0.05f;

        [Header("Heavy Hit")]
        [SerializeField, Range(0.01f, 1f)] private float _heavyHitFraction = 0.2f;

        [Header("Camera Shake")]
        [SerializeField, Min(0f)] private float _playerHitShakeStrength = 0.15f;
        [SerializeField, Min(0f)] private float _playerHitShakeDuration = 0.25f;
        [SerializeField, Min(0f)] private float _enemyHitShakeStrength = 0.06f;
        [SerializeField, Min(0f)] private float _enemyHitShakeDuration = 0.15f;
        [SerializeField, Min(1f)] private float _heavyShakeMultiplier = 2f;
        [SerializeField, Min(1)] private int _shakeVibrato = 14;

        public float HitStopDuration => _hitStopDuration;
        public float HeavyHitStopDuration => _heavyHitStopDuration;
        public float KillHitStopDuration => _killHitStopDuration;
        public float HitStopTimeScale => _hitStopTimeScale;

        public float HeavyHitFraction => _heavyHitFraction;

        public float PlayerHitShakeStrength => _playerHitShakeStrength;
        public float PlayerHitShakeDuration => _playerHitShakeDuration;
        public float EnemyHitShakeStrength => _enemyHitShakeStrength;
        public float EnemyHitShakeDuration => _enemyHitShakeDuration;
        public float HeavyShakeMultiplier => _heavyShakeMultiplier;
        public int ShakeVibrato => _shakeVibrato;
    }
}
