// One enemy performs its current move. AI goes through the same command pipeline as the player.
// The enemy slot is its only data, so the flow creates one per slot up front and the enemy turn allocates nothing.
using JourneyToLanka.Core.Commands;

namespace JourneyToLanka.Combat.Flow
{
    public sealed class EnemyActCommand : ICommand
    {
        private readonly CombatFlow _flow;

        public EnemyActCommand(CombatFlow flow, int enemyIndex)
        {
            _flow = flow;
            EnemyIndex = enemyIndex;
        }

        public int EnemyIndex { get; }

        public bool CanExecute()
        {
            var enemies = _flow.Model.Enemies;
            return _flow.IsEnemyTurn
                && EnemyIndex >= 0
                && EnemyIndex < enemies.Count
                && enemies[EnemyIndex].IsAlive;
        }

        public void Execute()
        {
            _flow.ResolveEnemyAction(EnemyIndex);
        }
    }
}
