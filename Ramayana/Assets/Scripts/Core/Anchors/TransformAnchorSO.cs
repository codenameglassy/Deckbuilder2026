// Runtime anchor holding a Transform, such as the main camera or the combat stage root.
using UnityEngine;

namespace JourneyToLanka.Core.Anchors
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Anchors/Transform Anchor", fileName = "TransformAnchor")]
    public sealed class TransformAnchorSO : RuntimeAnchorSO<Transform>
    {
    }
}
