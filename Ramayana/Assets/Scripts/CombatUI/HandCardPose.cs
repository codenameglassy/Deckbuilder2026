// Where one card sits in the hand, in the hand area's own space: the card's centre measured from the bottom centre of
// the hand area, its rotation in degrees around Z (positive turns counter-clockwise), and its uniform scale.
// Plain data produced by a HandLayoutSO strategy and consumed by the HandLayoutAnimator.
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    public readonly struct HandCardPose
    {
        public HandCardPose(Vector2 position, float rotation, float scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public Vector2 Position { get; }

        public float Rotation { get; }

        public float Scale { get; }
    }
}
