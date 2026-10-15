// Event channel carrying a CombatResult, raised once when a fight ends in victory or defeat.
using JourneyToLanka.Core.Events;
using UnityEngine;

namespace JourneyToLanka.Combat
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Events/Combat Result Event Channel", fileName = "CombatResultEventChannel")]
    public sealed class CombatResultEventChannelSO : EventChannelSO<CombatResult>
    {
    }
}
