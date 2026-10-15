// The enemy turn, paced by CombatPacingSO: after a start delay, each living enemy acts in field order, one action
// per interval, then the turn returns to the player after an end delay. Actions go through the command pipeline.
// At most one enemy acts per frame, so a long frame never resolves the whole turn at once.
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.StateMachines;

namespace JourneyToLanka.Combat.Flow
{
    internal sealed class EnemyTurnState : ITickableState<CombatFlow>
    {
        private float _timer;
        private int _nextEnemy;

        public void Enter(CombatFlow flow)
        {
            _timer = flow.Pacing.EnemyTurnStartDelay;
            _nextEnemy = 0;
            flow.SetPhase(CombatPhase.EnemyTurn);
        }

        public void Exit(CombatFlow flow)
        {
        }

        public void Tick(CombatFlow flow, float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            int enemy = FindLivingEnemy(flow.Model, _nextEnemy);
            if (enemy < 0)
            {
                flow.StartPlayerTurn();
                return;
            }

            flow.SubmitEnemyAction(enemy);
            _nextEnemy = enemy + 1;
            _timer = FindLivingEnemy(flow.Model, _nextEnemy) >= 0
                ? flow.Pacing.EnemyActionInterval
                : flow.Pacing.EnemyTurnEndDelay;
        }

        private static int FindLivingEnemy(CombatModel model, int fromIndex)
        {
            var enemies = model.Enemies;
            for (int i = fromIndex; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
