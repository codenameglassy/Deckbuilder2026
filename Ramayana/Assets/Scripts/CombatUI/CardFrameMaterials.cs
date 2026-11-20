// Runtime materials for the card frame shader: one per Combat HUD style, card type and card rarity, created on first
// use from the style's frame material and shared by every card of that type and rarity (hand, zoom, flights, pile
// viewer, reward screen), so all of them batch the same way and a style edit reaches every card at once. Get re-applies
// the style's frame values with the type's accent and the rarity's trim each time a card styles or binds itself, which
// costs a handful of material sets and keeps the live preview working with or without domain reload. The style and
// visuals assets are never modified; the copies are runtime-only (not saved) and are rebuilt if their source shader
// changes or they are destroyed. GetGlow does the same for the playable-card glow: one runtime material per style,
// shared by every card. GetArt and GetRound do it for the Card Art shader: one copy for the art window (the style's
// corner radius and vignette) and one fully rounded copy for pills and orbs (the type ribbon and the cost orb), each
// shared by every card. The frame's banner and art socket follow CardFaceLayout, so they always line up with the card's
// own children. GetBack does the same for the Card Back shader: one runtime copy per style, shared by the draw and
// discard stacks and every face-down card flight, with the frame's proportions so backs and faces line up; ApplyBack
// styles an Image as a card back (the style's Card Back Sprite when one is set, else the shader) and BackColor gives the
// Image colour for a tint either way. A small rendering cache, not a gameplay service: it holds nothing but these
// materials.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal static class CardFrameMaterials
    {
        private static readonly int AccentId = Shader.PropertyToID("_Accent");
        private static readonly int BodyTopId = Shader.PropertyToID("_BodyTop");
        private static readonly int BodyBottomId = Shader.PropertyToID("_BodyBottom");
        private static readonly int AccentTintId = Shader.PropertyToID("_AccentTint");
        private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
        private static readonly int BorderWidthId = Shader.PropertyToID("_BorderWidth");
        private static readonly int TrimColorId = Shader.PropertyToID("_TrimColor");
        private static readonly int TrimWidthId = Shader.PropertyToID("_TrimWidth");
        private static readonly int TrimInsetId = Shader.PropertyToID("_TrimInset");
        private static readonly int BannerRectId = Shader.PropertyToID("_BannerRect");
        private static readonly int BannerRadiusId = Shader.PropertyToID("_BannerRadius");
        private static readonly int ArtRectId = Shader.PropertyToID("_ArtRect");
        private static readonly int ArtRadiusId = Shader.PropertyToID("_ArtRadius");
        private static readonly int ArtRimWidthId = Shader.PropertyToID("_ArtRimWidth");
        private static readonly int ArtRimColorId = Shader.PropertyToID("_ArtRimColor");
        private static readonly int BevelId = Shader.PropertyToID("_Bevel");
        private static readonly int BevelWidthId = Shader.PropertyToID("_BevelWidth");
        private static readonly int InnerLineId = Shader.PropertyToID("_InnerLine");
        private static readonly int InnerLineGapId = Shader.PropertyToID("_InnerLineGap");
        private static readonly int OrnamentId = Shader.PropertyToID("_Ornament");
        private static readonly int OrnamentSizeId = Shader.PropertyToID("_OrnamentSize");
        private static readonly int BannerPatternId = Shader.PropertyToID("_BannerPattern");
        private static readonly int ShimmerId = Shader.PropertyToID("_Shimmer");
        private static readonly int ShimmerSpeedId = Shader.PropertyToID("_ShimmerSpeed");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int VignetteWidthId = Shader.PropertyToID("_VignetteWidth");
        private static readonly int TopLightId = Shader.PropertyToID("_TopLight");
        private static readonly int BandStrengthId = Shader.PropertyToID("_BandStrength");
        private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int SheenId = Shader.PropertyToID("_Sheen");
        private static readonly int BackBodyColorId = Shader.PropertyToID("_BodyColor");
        private static readonly int BackEmblemSizeId = Shader.PropertyToID("_EmblemSize");
        private static readonly int BackPatternId = Shader.PropertyToID("_Pattern");
        private static readonly int HaloColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int HaloStrengthId = Shader.PropertyToID("_Strength");
        private static readonly int HaloInsetId = Shader.PropertyToID("_Inset");
        private static readonly int HaloCornerRadiusId = Shader.PropertyToID("_CornerRadius");
        private static readonly int HaloPulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int HaloPulseSpeedId = Shader.PropertyToID("_PulseSpeed");

        private static readonly Dictionary<(CombatHudStyleSO, CardTypeVisualsSO, CardRarityVisualsSO), Material>
            Materials = new Dictionary<(CombatHudStyleSO, CardTypeVisualsSO, CardRarityVisualsSO), Material>(16);

        private static readonly Dictionary<CombatHudStyleSO, Material> GlowMaterials =
            new Dictionary<CombatHudStyleSO, Material>(2);

        private static readonly Dictionary<CombatHudStyleSO, Material> ArtMaterials =
            new Dictionary<CombatHudStyleSO, Material>(2);

        private static readonly Dictionary<CombatHudStyleSO, Material> RoundMaterials =
            new Dictionary<CombatHudStyleSO, Material>(2);

        private static readonly Dictionary<CombatHudStyleSO, Material> BackMaterials =
            new Dictionary<CombatHudStyleSO, Material>(2);

        private const float RoundCornerRadius = 0.5f;
        private const float RoundVignetteWidth = 0.5f;

        public static Material Get(CombatHudStyleSO style, CardTypeVisualsSO visuals, CardRarityVisualsSO rarity)
        {
            if (style == null || style.CardFrameMaterial == null)
            {
                return null;
            }

            Material source = style.CardFrameMaterial;
            var key = (style, visuals, rarity);
            Materials.TryGetValue(key, out Material material);
            if (Rebuild(ref material, source, MaterialLabel(visuals, rarity)))
            {
                Materials[key] = material;
            }

            Apply(material, style, visuals, rarity);
            return material;
        }

        public static Material GetGlow(CombatHudStyleSO style)
        {
            if (style == null || style.CardGlowMaterial == null)
            {
                return null;
            }

            GlowMaterials.TryGetValue(style, out Material material);
            if (Rebuild(ref material, style.CardGlowMaterial, "Glow"))
            {
                GlowMaterials[style] = material;
            }

            Vector2 card = style.CardSize;
            float spread = style.CardGlowSpread;
            Vector2 quad = card + new Vector2(spread * 2f, spread * 2f);
            material.SetColor(HaloColorId, style.CardGlowColor);
            material.SetFloat(HaloStrengthId, style.CardGlowStrength);
            material.SetVector(HaloInsetId, new Vector4(spread / Mathf.Max(quad.x, 1f), spread / Mathf.Max(quad.y, 1f), 0f, 0f));
            material.SetFloat(HaloCornerRadiusId, style.FrameCornerRadius);
            material.SetFloat(HaloPulseAmountId, style.CardGlowPulseAmount);
            material.SetFloat(HaloPulseSpeedId, style.CardGlowPulseSpeed);
            return material;
        }

        public static Material GetArt(CombatHudStyleSO style)
        {
            Material material = GetArtCopy(style, ArtMaterials, "Art");
            if (material != null)
            {
                material.SetFloat(CornerRadiusId, style.CardArtCornerRadius);
                material.SetFloat(VignetteId, style.CardArtVignette);
                material.SetFloat(VignetteWidthId, style.CardArtVignetteWidth);
                material.SetFloat(TopLightId, style.CardArtTopLight);
            }

            return material;
        }

        public static Material GetRound(CombatHudStyleSO style)
        {
            Material material = GetArtCopy(style, RoundMaterials, "Round");
            if (material != null)
            {
                material.SetFloat(CornerRadiusId, RoundCornerRadius);
                material.SetFloat(VignetteId, style.CardRoundShade);
                material.SetFloat(VignetteWidthId, RoundVignetteWidth);
                material.SetFloat(TopLightId, style.CardArtTopLight);
            }

            return material;
        }

        public static Material GetBack(CombatHudStyleSO style)
        {
            if (style == null || style.CardBackMaterial == null)
            {
                return null;
            }

            BackMaterials.TryGetValue(style, out Material material);
            if (Rebuild(ref material, style.CardBackMaterial, "Back"))
            {
                BackMaterials[style] = material;
            }

            material.SetColor(BackBodyColorId, style.CardBackColor);
            material.SetFloat(CornerRadiusId, style.FrameCornerRadius);
            material.SetFloat(BorderWidthId, style.FrameBorderWidth);
            material.SetColor(TrimColorId, style.FrameTrimColor);
            material.SetFloat(TrimWidthId, style.FrameTrimWidth);
            material.SetFloat(TrimInsetId, style.FrameTrimInset);
            material.SetFloat(InnerLineGapId, style.FrameInnerLineGap);
            material.SetFloat(BackEmblemSizeId, style.CardBackEmblemSize);
            material.SetFloat(BackPatternId, style.CardBackPattern);
            return material;
        }

        public static void ApplyBack(Image image, CombatHudStyleSO style, Color tint)
        {
            if (image == null || style == null)
            {
                return;
            }

            Material material = style.CardBackSprite == null ? GetBack(style) : null;
            UIBuildUtility.StyleImage(image, material == null ? style.CardBackSprite : null, BackColor(style, tint));
            image.material = material;
        }

        public static Color BackColor(CombatHudStyleSO style, Color tint)
        {
            bool shaded = style.CardBackSprite == null && style.CardBackMaterial != null;
            return shaded ? tint : style.CardBackColor * tint;
        }

        private static Material GetArtCopy(CombatHudStyleSO style, Dictionary<CombatHudStyleSO, Material> cache,
            string label)
        {
            if (style == null || style.CardArtMaterial == null)
            {
                return null;
            }

            cache.TryGetValue(style, out Material material);
            if (Rebuild(ref material, style.CardArtMaterial, label))
            {
                cache[style] = material;
            }

            return material;
        }

        private static bool Rebuild(ref Material material, Material source, string label)
        {
            if (material != null && material.shader == source.shader)
            {
                return false;
            }

            if (material != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(material);
                }
                else
                {
                    Object.DestroyImmediate(material);
                }
            }

            material = new Material(source)
            {
                name = source.name + " (" + label + ")",
                hideFlags = HideFlags.DontSave,
            };
            return true;
        }

        private static string MaterialLabel(CardTypeVisualsSO visuals, CardRarityVisualsSO rarity)
        {
            string type = visuals != null ? visuals.name : "Default";
            return rarity != null ? type + ", " + rarity.name : type;
        }

        private static void Apply(Material material, CombatHudStyleSO style, CardTypeVisualsSO visuals,
            CardRarityVisualsSO rarity)
        {
            Color trim = rarity != null ? rarity.TrimColor : style.FrameTrimColor;
            material.SetColor(AccentId, visuals != null ? visuals.AccentColor : style.CardColor);
            material.SetColor(BodyTopId, style.FrameBodyTopColor);
            material.SetColor(BodyBottomId, style.FrameBodyBottomColor);
            material.SetFloat(AccentTintId, style.FrameAccentTint);
            material.SetFloat(CornerRadiusId, style.FrameCornerRadius);
            material.SetFloat(BorderWidthId, style.FrameBorderWidth);
            material.SetColor(TrimColorId, trim);
            material.SetFloat(TrimWidthId, style.FrameTrimWidth);
            material.SetFloat(TrimInsetId, style.FrameTrimInset);
            CardFaceLayout layout = CardFaceLayout.From(style);
            material.SetVector(BannerRectId, CardFaceLayout.Normalized(layout.Banner, style.CardSize));
            material.SetFloat(BannerRadiusId, style.FrameBannerRadius);
            material.SetFloat(BandStrengthId, style.FrameBandStrength);
            material.SetVector(ArtRectId, CardFaceLayout.Normalized(layout.Art, style.CardSize));
            material.SetFloat(ArtRadiusId, style.CardArtCornerRadius);
            material.SetFloat(ArtRimWidthId, style.FrameArtRimWidth);
            material.SetColor(ArtRimColorId, trim);
            material.SetFloat(ShimmerId, rarity != null ? rarity.Shimmer : 0f);
            material.SetFloat(ShimmerSpeedId, style.FrameShimmerSpeed);
            material.SetFloat(BevelId, style.FrameBevel);
            material.SetFloat(BevelWidthId, style.FrameBevelWidth);
            material.SetFloat(InnerLineId, style.FrameInnerLine);
            material.SetFloat(InnerLineGapId, style.FrameInnerLineGap);
            material.SetFloat(OrnamentId, style.FrameOrnament);
            material.SetFloat(OrnamentSizeId, style.FrameOrnamentSize);
            material.SetFloat(BannerPatternId, style.FrameBannerPattern);
            material.SetFloat(GlowWidthId, style.FrameGlowWidth);
            material.SetFloat(GlowStrengthId, style.FrameGlowStrength);
            material.SetFloat(SheenId, style.FrameSheen);
        }
    }
}
