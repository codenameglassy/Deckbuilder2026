// Where each part of a card face sits, in card pixels with the origin at the card's bottom-left corner (y up), worked
// out from the Combat HUD style's card values. Top to bottom: the name banner, the art window, the type ribbon
// centred on the art window's bottom edge, and the description box filling what is left above the bottom padding.
// The cost orb is centred near the top-left corner and overhangs it by Cost Overhang; where it covers the banner, the
// name keeps clear of it by the same inset on both sides, so a centred name stays centred on the card. Every rect is
// clamped to a non-negative size, so a too-tall art window shrinks the description to nothing instead of inverting
// it. Plain C#, shared by the card view (placing its children) and the frame shader (drawing the banner and the art
// socket), and EditMode tested.
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    public readonly struct CardFaceLayout
    {
        public const float NameMargin = 4f;

        public readonly Rect Banner;
        public readonly Rect Name;
        public readonly Rect Art;
        public readonly Rect Ribbon;
        public readonly Rect Description;
        public readonly Vector2 CostCentre;

        private CardFaceLayout(Rect banner, Rect name, Rect art, Rect ribbon, Rect description, Vector2 costCentre)
        {
            Banner = banner;
            Name = name;
            Art = art;
            Ribbon = ribbon;
            Description = description;
            CostCentre = costCentre;
        }

        public static CardFaceLayout From(CombatHudStyleSO style)
        {
            return Compute(style.CardSize, style.CardPadding, style.CardBannerHeight, style.CardArtHeight,
                style.CardSectionGap, style.CardRibbonSize, style.CostBadgeSize, style.CostBadgeOverhang);
        }

        public static CardFaceLayout Compute(Vector2 cardSize, float padding, float bannerHeight, float artHeight,
            float gap, Vector2 ribbonSize, float costSize, float costOverhang)
        {
            float width = Mathf.Max(cardSize.x, 0f);
            float height = Mathf.Max(cardSize.y, 0f);
            float innerWidth = Mathf.Max(width - padding * 2f, 0f);
            float top = height - padding;

            float bannerH = Mathf.Clamp(bannerHeight, 0f, Mathf.Max(top - padding, 0f));
            var banner = new Rect(padding, top - bannerH, innerWidth, bannerH);

            float artTop = banner.yMin - gap;
            float artH = Mathf.Clamp(artHeight, 0f, Mathf.Max(artTop - padding, 0f));
            var art = new Rect(padding, artTop - artH, innerWidth, artH);

            float ribbonW = Mathf.Clamp(ribbonSize.x, 0f, innerWidth);
            float ribbonH = Mathf.Max(ribbonSize.y, 0f);
            var ribbon = new Rect((width - ribbonW) * 0.5f, art.yMin - ribbonH * 0.5f, ribbonW, ribbonH);

            float descriptionTop = ribbon.yMin - gap;
            var description = new Rect(padding, padding, innerWidth, Mathf.Max(descriptionTop - padding, 0f));

            float costRadius = Mathf.Max(costSize, 0f) * 0.5f;
            var costCentre = new Vector2(costRadius - costOverhang, height - (costRadius - costOverhang));

            float inset = NameMargin;
            bool costTouchesBanner = costCentre.y - costRadius < banner.yMax && costCentre.y + costRadius > banner.yMin;
            if (costTouchesBanner)
            {
                inset = Mathf.Max(inset, costCentre.x + costRadius - banner.xMin + NameMargin);
            }

            float nameWidth = Mathf.Max(banner.width - inset * 2f, 0f);
            var name = new Rect(banner.xMin + (banner.width - nameWidth) * 0.5f, banner.yMin, nameWidth, banner.height);
            return new CardFaceLayout(banner, name, art, ribbon, description, costCentre);
        }

        public static Vector4 Normalized(Rect rect, Vector2 cardSize)
        {
            float width = Mathf.Max(cardSize.x, 1f);
            float height = Mathf.Max(cardSize.y, 1f);
            return new Vector4(rect.xMin / width, rect.yMin / height, rect.xMax / width, rect.yMax / height);
        }
    }
}
