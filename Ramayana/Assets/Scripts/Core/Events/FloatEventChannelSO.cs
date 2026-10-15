// Event channel carrying a float payload.
using UnityEngine;

namespace JourneyToLanka.Core.Events
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Events/Float Event Channel", fileName = "FloatEventChannel")]
    public sealed class FloatEventChannelSO : EventChannelSO<float>
    {
    }
}
