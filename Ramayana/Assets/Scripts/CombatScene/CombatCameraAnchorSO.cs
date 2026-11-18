// Runtime anchor for the combat scene's camera, so screen-space UI such as the targeting line can project enemies
// onto the screen without Find calls or Camera.main. Readers null check Value on every read.
using JourneyToLanka.Core.Anchors;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Anchors/Combat Camera Anchor", fileName = "CombatCameraAnchor")]
    public sealed class CombatCameraAnchorSO : RuntimeAnchorSO<Camera>
    {
    }
}
