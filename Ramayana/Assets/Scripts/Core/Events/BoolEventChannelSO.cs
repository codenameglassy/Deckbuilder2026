// Event channel carrying a bool payload.
using UnityEngine;

namespace JourneyToLanka.Core.Events
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Events/Bool Event Channel", fileName = "BoolEventChannel")]
    public sealed class BoolEventChannelSO : EventChannelSO<bool>
    {
    }
}
