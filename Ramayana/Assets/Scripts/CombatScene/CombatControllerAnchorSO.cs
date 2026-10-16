// Runtime anchor for the scene's CombatController, so other systems such as the combat HUD can reach the running
// fight without Find calls or direct scene references. Readers null check Value on every read.
using JourneyToLanka.Core.Anchors;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Anchors/Combat Controller Anchor", fileName = "CombatControllerAnchor")]
    public sealed class CombatControllerAnchorSO : RuntimeAnchorSO<CombatController>
    {
    }
}
