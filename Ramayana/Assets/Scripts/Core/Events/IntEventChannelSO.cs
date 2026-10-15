// Event channel carrying an int payload.
using UnityEngine;

namespace JourneyToLanka.Core.Events
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Events/Int Event Channel", fileName = "IntEventChannel")]
    public sealed class IntEventChannelSO : EventChannelSO<int>
    {
    }
}
