// EditMode tests for the hand layout strategies with their default values: both layouts are symmetric and centred,
// fit the hand width when crowded, and push neighbours outward around a hovered card; the flat row never rotates; the
// fan stands its middle card upright and highest and tilts outer cards away from the centre; a hovered card stays in
// its slot, straightens, grows and sits fully above the hand's bottom edge in both layouts; and hover detection works
// from the resting layout, picking the nearest card in an overlap, ignoring points outside the hand, and letting the
// hovered card's zone reach the top of its raised pose.
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class HandLayoutTests
    {
        private const float Tolerance = 0.001f;
        private static readonly Vector2 CardSize = new Vector2(200f, 280f);

        private FlatHandLayoutSO _flat;
        private FanHandLayoutSO _fan;

        [SetUp]
        public void SetUp()
        {
            _flat = ScriptableObject.CreateInstance<FlatHandLayoutSO>();
            _fan = ScriptableObject.CreateInstance<FanHandLayoutSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_flat);
            Object.DestroyImmediate(_fan);
        }

        [Test]
        public void BothLayouts_AreSymmetricAboutTheCentre()
        {
            AssertSymmetric(_flat, Input(5));
            AssertSymmetric(_flat, Input(6));
            AssertSymmetric(_fan, Input(5));
            AssertSymmetric(_fan, Input(6));
        }

        [Test]
        public void SingleCard_SitsCentredAndUpright()
        {
            HandCardPose flat = _flat.GetPose(Input(1), 0);
            HandCardPose fan = _fan.GetPose(Input(1), 0);

            Assert.AreEqual(0f, flat.Position.x, Tolerance);
            Assert.AreEqual(CardSize.y * 0.5f, flat.Position.y, Tolerance);
            Assert.AreEqual(0f, fan.Position.x, Tolerance);
            Assert.AreEqual(0f, fan.Rotation, Tolerance);
        }

        [Test]
        public void FlatRow_NeverRotates()
        {
            HandLayoutInput input = Input(7);
            for (int i = 0; i < input.Count; i++)
            {
                Assert.AreEqual(0f, _flat.GetPose(input, i).Rotation, Tolerance);
            }
        }

        [Test]
        public void CrowdedHand_FitsTheHandWidth()
        {
            HandLayoutInput input = Input(10, 1300f);
            AssertFits(_flat, input);
            AssertFits(_fan, input);
        }

        [Test]
        public void Fan_MiddleCardIsUprightAndHighest_OuterCardsTiltAway()
        {
            HandLayoutInput input = Input(5);
            HandCardPose left = _fan.GetPose(input, 0);
            HandCardPose middle = _fan.GetPose(input, 2);
            HandCardPose right = _fan.GetPose(input, 4);

            Assert.AreEqual(0f, middle.Rotation, Tolerance);
            Assert.Greater(middle.Position.y, left.Position.y);
            Assert.Greater(middle.Position.y, right.Position.y);
            Assert.Greater(left.Rotation, 0f, "The left card should turn counter-clockwise, away from the centre.");
            Assert.Less(right.Rotation, 0f, "The right card should turn clockwise, away from the centre.");
        }

        [Test]
        public void Hover_PushesNeighboursOutward_NearestMost()
        {
            AssertHoverSpreads(_flat);
            AssertHoverSpreads(_fan);
        }

        [Test]
        public void Fan_HoveredCardRisesAndStraightens()
        {
            HandCardPose resting = _fan.GetPose(Input(5), 0);
            HandCardPose hovered = _fan.GetPose(Input(5).WithHovered(0), 0);

            Assert.Greater(hovered.Position.y, resting.Position.y);
            Assert.AreEqual(0f, hovered.Rotation, Tolerance);
        }

        [Test]
        public void HoveredCard_StaysInItsSlot_GrowsAndSitsAboveTheHandBottom()
        {
            AssertHoverPose(_flat);
            AssertHoverPose(_fan);
        }

        [Test]
        public void FindCardAt_ReturnsTheCardUnderTheCentreOfEachCard()
        {
            HandLayoutInput input = Input(5);
            for (int i = 0; i < input.Count; i++)
            {
                Assert.AreEqual(i, _fan.FindCardAt(input, _fan.GetPose(input, i).Position, -1));
                Assert.AreEqual(i, _flat.FindCardAt(input, _flat.GetPose(input, i).Position, -1));
            }
        }

        [Test]
        public void FindCardAt_PicksTheNearestCardInAnOverlap()
        {
            HandLayoutInput input = Input(5);
            Vector2 a = _fan.GetPose(input, 1).Position;
            Vector2 b = _fan.GetPose(input, 2).Position;
            Vector2 nearerToB = Vector2.Lerp(a, b, 0.6f);

            Assert.AreEqual(2, _fan.FindCardAt(input, nearerToB, -1));
        }

        [Test]
        public void FindCardAt_UsesTheRestingLayout_SoHoverCannotJitter()
        {
            HandLayoutInput hovered = Input(5).WithHovered(2);
            Vector2 restingCentreOfThree = _fan.GetPose(Input(5), 3).Position;

            Assert.AreEqual(3, _fan.FindCardAt(hovered, restingCentreOfThree, 2));
        }

        [Test]
        public void FindCardAt_IgnoresPointsOutsideTheHand()
        {
            HandLayoutInput input = Input(5);
            Assert.AreEqual(-1, _fan.FindCardAt(input, new Vector2(0f, CardSize.y * 3f), -1));
            Assert.AreEqual(-1, _fan.FindCardAt(input, new Vector2(2000f, CardSize.y * 0.5f), -1));
            Assert.AreEqual(-1, _flat.FindCardAt(input, new Vector2(0f, -CardSize.y), -1));
        }

        [Test]
        public void FindCardAt_HoveredCardZoneReachesAboveIt()
        {
            HandLayoutInput input = Input(5);
            HandCardPose middle = _flat.GetPose(input, 2);
            HandCardPose raised = _flat.GetPose(input.WithHovered(2), 2);
            float raisedTop = raised.Position.y + CardSize.y * 0.5f * raised.Scale;
            float restingTop = middle.Position.y + CardSize.y * 0.5f;
            Vector2 aboveMiddle = new Vector2(middle.Position.x, (restingTop + raisedTop) * 0.5f);

            Assert.AreEqual(-1, _flat.FindCardAt(input, aboveMiddle, -1));
            Assert.AreEqual(2, _flat.FindCardAt(input, aboveMiddle, 2));
        }

        private static HandLayoutInput Input(int count, float width = 1300f)
        {
            return new HandLayoutInput(count, width, CardSize, -1);
        }

        private static void AssertSymmetric(HandLayoutSO layout, HandLayoutInput input)
        {
            for (int i = 0; i < input.Count; i++)
            {
                HandCardPose pose = layout.GetPose(input, i);
                HandCardPose mirror = layout.GetPose(input, input.Count - 1 - i);
                Assert.AreEqual(-pose.Position.x, mirror.Position.x, Tolerance, $"{layout.name} card {i} x");
                Assert.AreEqual(pose.Position.y, mirror.Position.y, Tolerance, $"{layout.name} card {i} y");
                Assert.AreEqual(-pose.Rotation, mirror.Rotation, Tolerance, $"{layout.name} card {i} rotation");
            }
        }

        private static void AssertFits(HandLayoutSO layout, HandLayoutInput input)
        {
            float halfCard = input.CardSize.x * 0.5f;
            float left = layout.GetPose(input, 0).Position.x - halfCard;
            float right = layout.GetPose(input, input.Count - 1).Position.x + halfCard;
            Assert.GreaterOrEqual(left, -input.Width * 0.5f - Tolerance);
            Assert.LessOrEqual(right, input.Width * 0.5f + Tolerance);
        }

        private static void AssertHoverPose(HandLayoutSO layout)
        {
            HandLayoutInput resting = Input(5);
            HandLayoutInput hovered = resting.WithHovered(1);
            HandCardPose before = layout.GetPose(resting, 1);
            HandCardPose after = layout.GetPose(hovered, 1);

            Assert.AreEqual(before.Position.x, after.Position.x, Tolerance, $"{layout.name} slot x");
            Assert.AreEqual(0f, after.Rotation, Tolerance, $"{layout.name} rotation");
            Assert.AreEqual(layout.HoverScale, after.Scale, Tolerance, $"{layout.name} scale");
            float bottom = after.Position.y - CardSize.y * 0.5f * after.Scale;
            Assert.AreEqual(layout.HoverBottom, bottom, Tolerance, $"{layout.name} bottom edge");
        }

        private static void AssertHoverSpreads(HandLayoutSO layout)
        {
            HandLayoutInput resting = Input(5);
            HandLayoutInput hovered = resting.WithHovered(2);

            float leftNear = layout.GetPose(hovered, 1).Position.x - layout.GetPose(resting, 1).Position.x;
            float leftFar = layout.GetPose(hovered, 0).Position.x - layout.GetPose(resting, 0).Position.x;
            float rightNear = layout.GetPose(hovered, 3).Position.x - layout.GetPose(resting, 3).Position.x;

            Assert.Less(leftNear, 0f, "The left neighbour should move left.");
            Assert.Greater(rightNear, 0f, "The right neighbour should move right.");
            Assert.Greater(Mathf.Abs(leftNear), Mathf.Abs(leftFar), "The nearest neighbour should move most.");
            Assert.AreEqual(layout.GetPose(resting, 2).Position.x, layout.GetPose(hovered, 2).Position.x, Tolerance);
        }
    }
}
