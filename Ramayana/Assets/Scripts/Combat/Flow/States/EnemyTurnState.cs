// The enemy turn, paced by CombatPacingSO: after a start delay, each living enemy in field order announces its action
// (CombatFlow.EnemyActionStarted), waits the windup, then acts through the command pipeline; the next enemy starts
// after the action interval. After the end delay the round ends (statuses tick, Dahan burns) and, unless that ended
// the fight, the turn returns to the player. With no windup an enemy announces and acts in the same frame. At most
// one enemy acts per frame, so a long frame never resolves the whole turn.
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.StateMachines;

namespace JourneyToLanka.Combat.Flow
{
    internal sealed class EnemyTurnState : ITickableState<CombatFlow>
    {
        private float _timer;
        private int _nextEnemy;
        private int _windingUp = -1;

        public void Enter(CombatFlow flow)
        {
            _timer = flow.Pacing.EnemyTurnStartDelay;
            _nextEnemy = 0;
            _windingUp = -1;
            flow.SetPhase(CombatPhase.EnemyTurn);
        }

        public void Exit(CombatFlow flow)
        {
            _windingUp = -1;
        }

        public void Tick(CombatFlow flow, float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            if (_windingUp >= 0)
            {
                Act(flow);
                return;
            }

            int enemy = FindLivingEnemy(flow.Model, _nextEnemy);
            if (enemy < 0)
            {
                if (!flow.ResolveRoundEnd())
                {
                    flow.StartPlayerTurn();
                }

                return;
            }

            _windingUp = enemy;
            _timer = flow.Pacing.EnemyWindup;
            flow.AnnounceEnemyAction(enemy);

            if (_timer <= 0f)
            {
                Act(flow);
            }
        }

        private void Act(CombatFlow flow)
        {
            int enemy = _windingUp;
            _windingUp = -1;
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
