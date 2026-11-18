// Runtime materials for the card frame shader: one per Combat HUD style and card type, created on first use from the
// style's frame material and shared by every card of that type (hand, zoom, flights, pile viewer, reward screen), so
// all of them batch the same way and a style edit reaches every card at once. Get re-applies the style's frame values
// and the type's accent each time a card styles or binds itself, which costs a handful of material sets and keeps the
// live preview working with or without domain reload. The style and visuals assets are never modified; the copies
// are runtime-only (not saved) and are rebuilt if their source shader changes or they are destroyed.
// GetGlow does the same for the playable-card glow: one runtime material per style, shared by every card.
// A small rendering cache, not a gameplay service: it holds nothing but these materials.
using System.Collections.Generic;
using UnityEngine;

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
        private static readonly int BandHeightId = Shader.PropertyToID("_BandHeight");
        private static readonly int BandStrengthId = Shader.PropertyToID("_BandStrength");
        private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int SheenId = Shader.PropertyToID("_Sheen");
        private static readonly int HaloColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int HaloStrengthId = Shader.PropertyToID("_Strength");
        private static readonly int HaloInsetId = Shader.PropertyToID("_Inset");
        private static readonly int HaloCornerRadiusId = Shader.PropertyToID("_CornerRadius");
        private static readonly int HaloPulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int HaloPulseSpeedId = Shader.PropertyToID("_PulseSpeed");

        private static readonly Dictionary<(CombatHudStyleSO Style, CardTypeVisualsSO Visuals), Material> Materials =
            new Dictionary<(CombatHudStyleSO, CardTypeVisualsSO), Material>(8);

        private static readonly Dictionary<CombatHudStyleSO, Material> GlowMaterials =
            new Dictionary<CombatHudStyleSO, Material>(2);

        public static Material Get(CombatHudStyleSO style, CardTypeVisualsSO visuals)
        {
            if (style == null || style.CardFrameMaterial == null)
            {
                return null;
            }

            Material source = style.CardFrameMaterial;
            var key = (style, visuals);
            Materials.TryGetValue(key, out Material material);
            if (Rebuild(ref material, source, visuals != null ? visuals.name : "Default"))
            {
                Materials[key] = material;
            }

            Apply(material, style, visuals);
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

        public static float BandHeightFraction(CombatHudStyleSO style)
        {
            float cardHeight = Mathf.Max(style.CardSize.y, 1f);
            return Mathf.Clamp01((style.CardPadding * 1.5f + style.CostBadgeSize) / cardHeight);
        }

        private static void Apply(Material material, CombatHudStyleSO style, CardTypeVisualsSO visuals)
        {
            material.SetColor(AccentId, visuals != null ? visuals.AccentColor : style.CardColor);
            material.SetColor(BodyTopId, style.FrameBodyTopColor);
            material.SetColor(BodyBottomId, style.FrameBodyBottomColor);
            material.SetFloat(AccentTintId, style.FrameAccentTint);
            material.SetFloat(CornerRadiusId, style.FrameCornerRadius);
            material.SetFloat(BorderWidthId, style.FrameBorderWidth);
            material.SetColor(TrimColorId, style.FrameTrimColor);
            material.SetFloat(TrimWidthId, style.FrameTrimWidth);
            material.SetFloat(TrimInsetId, style.FrameTrimInset);
            material.SetFloat(BandHeightId, BandHeightFraction(style));
            material.SetFloat(BandStrengthId, style.FrameBandStrength);
            material.SetFloat(GlowWidthId, style.FrameGlowWidth);
            material.SetFloat(GlowStrengthId, style.FrameGlowStrength);
            material.SetFloat(SheenId, style.FrameSheen);
        }
    }
}
