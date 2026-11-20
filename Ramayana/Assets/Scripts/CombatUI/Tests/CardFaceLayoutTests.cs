// EditMode tests for CardFaceLayout: with the default card values every part stays inside the card and they stack
// banner, art, ribbon, description from top to bottom without overlapping (the ribbon straddles the art's bottom
// edge by design); the ribbon is centred; the name keeps clear of an overhanging cost orb by the same inset on both
// sides; an art window too tall for the card squeezes the description to zero instead of inverting it; and normalized
// rects are fractions of the card size.
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class CardFaceLayoutTests
    {
        private const float Tolerance = 0.001f;
        private static readonly Vector2 CardSize = new Vector2(200f, 280f);
        private static readonly Vector2 RibbonSize = new Vector2(84f, 24f);

        private static CardFaceLayout Default()
        {
            return CardFaceLayout.Compute(CardSize, 12f, 34f, 112f, 6f, RibbonSize, 48f, 14f);
        }

        [Test]
        public void Parts_StayInsideTheCard()
        {
            CardFaceLayout layout = Default();
            var card = new Rect(Vector2.zero, CardSize);

            AssertInside(card, layout.Banner, "banner");
            AssertInside(card, layout.Name, "name");
            AssertInside(card, layout.Art, "art");
            AssertInside(card, layout.Ribbon, "ribbon");
            AssertInside(card, layout.Description, "description");
        }

        [Test]
        public void Parts_StackFromTopToBottom()
        {
            CardFaceLayout layout = Default();

            Assert.LessOrEqual(layout.Art.yMax, layout.Banner.yMin);
            Assert.LessOrEqual(layout.Description.yMax, layout.Ribbon.yMin);
            Assert.AreEqual(layout.Art.yMin, layout.Ribbon.center.y, Tolerance);
            Assert.Greater(layout.Description.height, 0f);
        }

        [Test]
        public void Ribbon_IsCentred()
        {
            CardFaceLayout layout = Default();

            Assert.AreEqual(CardSize.x * 0.5f, layout.Ribbon.center.x, Tolerance);
        }

        [Test]
        public void Name_KeepsClearOfTheCostOrb_AndStaysCentred()
        {
            CardFaceLayout layout = Default();
            float orbRight = layout.CostCentre.x + 24f;

            Assert.GreaterOrEqual(layout.Name.xMin, orbRight);
            Assert.AreEqual(layout.Banner.center.x, layout.Name.center.x, Tolerance);
        }

        [Test]
        public void CostOrb_OverhangsTheCorner()
        {
            CardFaceLayout layout = Default();

            Assert.AreEqual(-14f, layout.CostCentre.x - 24f, Tolerance);
            Assert.AreEqual(CardSize.y + 14f, layout.CostCentre.y + 24f, Tolerance);
        }

        [Test]
        public void TooTallArt_SqueezesTheDescriptionToZero()
        {
            CardFaceLayout layout = CardFaceLayout.Compute(CardSize, 12f, 34f, 1000f, 6f, RibbonSize, 48f, 14f);

            Assert.GreaterOrEqual(layout.Art.height, 0f);
            Assert.GreaterOrEqual(layout.Art.yMin, 12f - Tolerance);
            Assert.AreEqual(0f, layout.Description.height, Tolerance);
        }

        [Test]
        public void Normalized_IsAFractionOfTheCard()
        {
            Vector4 normalized = CardFaceLayout.Normalized(new Rect(20f, 28f, 100f, 140f), CardSize);

            Assert.AreEqual(0.1f, normalized.x, Tolerance);
            Assert.AreEqual(0.1f, normalized.y, Tolerance);
            Assert.AreEqual(0.6f, normalized.z, Tolerance);
            Assert.AreEqual(0.6f, normalized.w, Tolerance);
        }

        private static void AssertInside(Rect outer, Rect inner, string label)
        {
            Assert.GreaterOrEqual(inner.xMin, outer.xMin - Tolerance, label + " left");
            Assert.GreaterOrEqual(inner.yMin, outer.yMin - Tolerance, label + " bottom");
            Assert.LessOrEqual(inner.xMax, outer.xMax + Tolerance, label + " right");
            Assert.LessOrEqual(inner.yMax, outer.yMax + Tolerance, label + " top");
            Assert.GreaterOrEqual(inner.width, 0f, label + " width");
            Assert.GreaterOrEqual(inner.height, 0f, label + " height");
        }
    }
}
