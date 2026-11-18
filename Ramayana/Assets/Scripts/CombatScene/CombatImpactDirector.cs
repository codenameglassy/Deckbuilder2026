// Makes hits feel physical. It sits beside CombatController and listens to the fight it starts (CombatBegan, a plain
// C# event on the same object):
//   An enemy starting an attack (CombatFlow.EnemyActionStarted) lunges at Rama over the pacing windup, so it lands as
//   the damage resolves. Moves without an attack do not lunge.
//   A card that hits an enemy makes Rama thrust toward the enemy it hit last.
//   A fired special arrow (CombatModel.ArrowFired) raises its name over Rama and, when it hit an enemy, thrusts the
//   same way.
//   Any hit that takes health triggers a hit-stop and a camera shake, stronger for heavy hits and killing blows.
// It only presents: it never changes combat state. Every value comes from CombatImpactStyleSO and each view's style.
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatController))]
    [AddComponentMenu("Journey to Lanka/Combat/Combat Impact Director")]
    public sealed class CombatImpactDirector : MonoBehaviour
    {
        [SerializeField] private CombatImpactStyleSO _style;
        [SerializeField] private HitStop _hitStop;
        [SerializeField] private CombatCameraShake _cameraShake;

        private CombatController _controller;
        private CombatFlow _flow;
        private int _lastHitEnemy = -1;

        private void Awake()
        {
            _controller = GetComponent<CombatController>();
            if (_style == null)
            {
                Debug.LogError($"{name}: no Combat Impact Style assigned. Run Tools > Journey to Lanka > Wire Selected > Combat Impact.", this);
            }
        }

        private void OnEnable()
        {
            _controller.CombatBegan += Attach;
            if (_controller.Flow != null)
            {
                Attach(_controller.Flow);
            }
        }

        private void OnDisable()
        {
            _controller.CombatBegan -= Attach;
            Detach();
        }

        private void Attach(CombatFlow flow)
        {
            if (flow == _flow)
            {
                return;
            }

            Detach();
            _flow = flow;
            CombatModel model = flow.Model;

            flow.EnemyActionStarted += HandleEnemyActionStarted;
            model.CardPlayed += HandleCardPlayed;
            model.ArrowFired += HandleArrowFired;
            model.Player.Damaged += HandlePlayerDamaged;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                model.Enemies[i].Fighter.Damaged += HandleEnemyDamaged;
            }
        }

        private void Detach()
        {
            if (_flow == null)
            {
                return;
            }

            CombatModel model = _flow.Model;
            _flow.EnemyActionStarted -= HandleEnemyActionStarted;
            model.CardPlayed -= HandleCardPlayed;
            model.ArrowFired -= HandleArrowFired;
            model.Player.Damaged -= HandlePlayerDamaged;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                model.Enemies[i].Fighter.Damaged -= HandleEnemyDamaged;
            }

            _flow = null;
            _lastHitEnemy = -1;
        }

        private void HandleEnemyActionStarted(int enemyIndex)
        {
            CombatModel model = _flow.Model;
            if (enemyIndex < 0 || enemyIndex >= model.Enemies.Count)
            {
                return;
            }

            if (!model.Enemies[enemyIndex].GetIntent(model.Resolver, model.Player).HasAttack)
            {
                return;
            }

            CombatantView enemyView = _controller.GetEnemyView(enemyIndex);
            CombatantView playerView = _controller.PlayerView;
            if (enemyView != null && playerView != null)
            {
                enemyView.PlayLunge(playerView.transform.position, _flow.Pacing.EnemyWindup);
            }
        }

        private void HandleCardPlayed(CardInstance card, ICombatant target)
        {
            ThrustAtLastHit();
        }

        private void HandleArrowFired(ArrowDefinitionSO arrow, ICombatant target)
        {
            CombatantView playerView = _controller.PlayerView;
            if (playerView != null)
            {
                playerView.ShowArrowName(arrow);
            }

            ThrustAtLastHit();
        }

        private void ThrustAtLastHit()
        {
            int hitEnemy = _lastHitEnemy;
            _lastHitEnemy = -1;
            if (hitEnemy < 0)
            {
                return;
            }

            CombatantView playerView = _controller.PlayerView;
            CombatantView enemyView = _controller.GetEnemyView(hitEnemy);
            if (playerView != null && enemyView != null)
            {
                playerView.PlayThrust(enemyView.transform.position);
            }
        }

        private void HandlePlayerDamaged(Combatant player, DamageResult result)
        {
            if (_style != null && result.HealthLost > 0)
            {
                PlayImpact(player, result, _style.PlayerHitShakeStrength, _style.PlayerHitShakeDuration);
            }
        }

        private void HandleEnemyDamaged(Combatant enemy, DamageResult result)
        {
            if (_flow != null && _flow.IsPlayerTurn && result.Amount > 0)
            {
                _lastHitEnemy = IndexOfEnemy(enemy);
            }

            if (_style != null && result.HealthLost > 0)
            {
                PlayImpact(enemy, result, _style.EnemyHitShakeStrength, _style.EnemyHitShakeDuration);
            }
        }

        private void PlayImpact(Combatant target, in DamageResult result, float shakeStrength, float shakeDuration)
        {
            bool heavy = result.Killed || result.HealthLost >= Mathf.CeilToInt(target.MaxHealth * _style.HeavyHitFraction);

            if (_hitStop != null)
            {
                float stop = result.Killed ? _style.KillHitStopDuration
                    : heavy ? _style.HeavyHitStopDuration
                    : _style.HitStopDuration;
                _hitStop.Trigger(stop, _style.HitStopTimeScale);
            }

            if (_cameraShake != null)
            {
                float multiplier = heavy ? _style.HeavyShakeMultiplier : 1f;
                _cameraShake.Shake(shakeStrength * multiplier, shakeDuration * multiplier, _style.ShakeVibrato);
            }
        }

        private int IndexOfEnemy(Combatant enemy)
        {
            var enemies = _flow.Model.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Fighter == enemy)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
