// Whenever its holder gains a chosen status, the holder gains Kavach for every stack gained (Devotion: 2 Kavach per
// Dharma). Spending the status gains nothing.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Trigger/Status Gained Block", fileName = "StatusGainedBlockTrigger")]
    public sealed class StatusGainedBlockTriggerSO : StatusTriggerSO
    {
        [SerializeField] private StatusDefinitionSO _status;
        [SerializeField, Min(1)] private int _blockPerStack = 2;

        public StatusDefinitionSO Status => _status;

        public int BlockPerStack => _blockPerStack;

        public override void OnOwnerGainedStatus(in StatusInstance status, StatusDefinitionSO gained, int amount,
            in StatusTickContext context)
        {
            if (gained == null || gained != _status || amount <= 0 || context.Actions == null || context.Owner == null)
            {
                return;
            }

            context.Actions.GainBlock(context.Owner, amount * _blockPerStack);
        }
    }
}
