// At the start of its holder's turn, the holder gains an amount of another status (Vow of the Exile: 1 Dharma each
// turn). The gain goes through the actions like any other, so statuses that react to it (Devotion) react here too.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Trigger/Turn Start Gain Status", fileName = "TurnStartGainStatusTrigger")]
    public sealed class TurnStartGainStatusTriggerSO : StatusTriggerSO
    {
        [SerializeField] private StatusDefinitionSO _status;
        [SerializeField, Min(1)] private int _amount = 1;

        public StatusDefinitionSO Status => _status;

        public int Amount => _amount;

        public override void OnOwnerTurnStart(in StatusInstance status, in StatusTickContext context)
        {
            if (_status == null || context.Actions == null || context.Owner == null || !context.Owner.IsAlive)
            {
                return;
            }

            context.Actions.ApplyStatus(context.Owner, _status, _amount, 0);
        }
    }
}
