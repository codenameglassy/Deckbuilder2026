// Gives the health bar and the enemy intents what they need to keep their size on screen, to show Kavach as a shield
// and to mark incoming damage: bakes tiling red stripes, a nine-sliced loss tag and its pointer for the threat mark,
// and bakes a greyscale round shield (light face, a darker ring and four studs, inked outline; the Kavach number sits
// in its centre) as the Kavach badge sprite, so the style's Kavach Badge Color tints it, and assigns it to the
// combatant view style if it has no badge sprite; then assigns the combat camera anchor to the combatant view style and
// the combat feedback style where they have none. The anchor asset is the same one the targeting line uses
// (CombatCameraAnchor.asset next to the combat scene styles); it is created here if that builder has not run yet. The
// sprite is never rewritten once it exists and assigned references are never replaced. Runs after the Combat Scene,
// Combat Feedback and Intent Icons builders.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class HealthBarAssetBuilder
    {
        public const string KavachBadgePath = StatusVisualsAssetBuilder.SpritesFolder + "/KavachBadge.png";
        public const string CameraAnchorPath = CombatSceneAssetBuilder.Root + "/CombatCameraAnchor.asset";
        public const string ThreatStripesPath = StatusVisualsAssetBuilder.SpritesFolder + "/ThreatStripes.png";
        public const string ThreatTagPath = StatusVisualsAssetBuilder.SpritesFolder + "/ThreatTag.png";
        public const string ThreatPointerPath = StatusVisualsAssetBuilder.SpritesFolder + "/ThreatPointer.png";

        private const int StripeSize = 16;
        private const int StripePixelsPerUnit = 100;
        private const int TagSize = 48;
        private const int TagBorder = 14;
        private const float TagRadius = 10f;
        private const float TagEdge = 2f;
        private const int PointerWidth = 32;
        private const int PointerHeight = 16;

        private static readonly Color StripeLight = new Color(0.9f, 0.26f, 0.23f, 1f);
        private static readonly Color StripeDark = new Color(0.36f, 0.07f, 0.08f, 1f);
        private static readonly Color TagFace = new Color(0.16f, 0.16f, 0.16f, 0.95f);

        private const int BadgeSize = 128;
        private const float OutlineWidth = 0.08f;

        private static readonly Color Outline = new Color(0.08f, 0.06f, 0.06f);
        private static readonly Color Face = new Color(0.97f, 0.97f, 0.97f);
        private static readonly Color Ring = new Color(0.62f, 0.62f, 0.62f);

        [MenuItem(AssetBuilderConstants.MenuRoot + "Health Bar", priority = AssetBuilderConstants.FeatureMenuPriority + 16)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Health Bar", Build);
        }

        [DefaultAssetBuilder(129)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(StatusVisualsAssetBuilder.SpritesFolder, report);
            Sprite badge = SpriteBakeUtility.LoadOrBake(KavachBadgePath, BadgeSize, Vector4.zero, BakeBadge, report);
            CombatCameraAnchorSO anchor =
                AssetBuilderSerialization.LoadOrCreateConfigured<CombatCameraAnchorSO>(CameraAnchorPath, report, null);

            const string ViewPath = CombatSceneAssetBuilder.ViewStylePath;
            var viewStyle = AssetDatabase.LoadAssetAtPath<CombatantViewStyleSO>(ViewPath);
            const int Ppu = StripePixelsPerUnit;
            Sprite stripes = SpriteBakeUtility.LoadOrBake(ThreatStripesPath, Ppu, Vector4.zero, BakeStripes, report);
            Sprite tag = SpriteBakeUtility.LoadOrBake(ThreatTagPath, Ppu, Vector4.one * TagBorder, BakeTag, report);
            Sprite pointer = SpriteBakeUtility.LoadOrBake(ThreatPointerPath, Ppu, Vector4.zero, BakePointer, report);
            if (viewStyle != null)
            {
                Wire(viewStyle, "_kavachBadgeSprite", badge, report);
                Wire(viewStyle, "_cameraAnchor", anchor, report);
                Wire(viewStyle, "_threatStripeSprite", stripes, report);
                Wire(viewStyle, "_threatTagSprite", tag, report);
                Wire(viewStyle, "_threatPointerSprite", pointer, report);
            }
            else
            {
                report.AddSkipped($"{ViewPath} (missing, run Create Default Assets > Combat Scene first)");
            }

            const string FeedbackPath = CombatFeedbackAssetBuilder.FeedbackStylePath;
            var feedbackStyle = AssetDatabase.LoadAssetAtPath<CombatFeedbackStyleSO>(FeedbackPath);
            if (feedbackStyle != null)
            {
                Wire(feedbackStyle, "_cameraAnchor", anchor, report);
            }
            else
            {
                report.AddSkipped($"{FeedbackPath} (missing, run Create Default Assets > Combat Feedback first)");
            }
        }

        private static void Wire(Object owner, string field, Object value, AssetBuilderReport report)
        {
            if (value != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(owner, field, value, report);
            }
        }

        // Diagonal red stripes that tile seamlessly: the period divides the tile in both directions.
        private static byte[] BakeStripes()
        {
            var pixels = new Color32[StripeSize * StripeSize];
            int half = StripeSize / 2;
            for (int y = 0; y < StripeSize; y++)
            {
                for (int x = 0; x < StripeSize; x++)
                {
                    bool light = (x + y) % StripeSize < half;
                    pixels[y * StripeSize + x] = light ? StripeLight : StripeDark;
                }
            }

            return Encode(pixels, StripeSize, StripeSize);
        }

        // The loss tag: a dark rounded face with a light edge, nine-sliced, so its colour on the style tints both.
        private static byte[] BakeTag()
        {
            var pixels = new Color32[TagSize * TagSize];
            float half = TagSize * 0.5f;
            for (int y = 0; y < TagSize; y++)
            {
                for (int x = 0; x < TagSize; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfIconCanvas.Box(p, Vector2.zero, new Vector2(half - 1f, half - 1f), TagRadius);
                    float coverage = Mathf.Clamp01(0.5f - d);
                    float edge = Mathf.Clamp01(0.5f - (-d - TagEdge));
                    Color c = Color.Lerp(TagFace, Color.white, edge);
                    c.a = Mathf.Max(TagFace.a, edge) * coverage;
                    pixels[y * TagSize + x] = c;
                }
            }

            return Encode(pixels, TagSize, TagSize);
        }

        // A small white triangle pointing up, tinted to match the tag.
        private static byte[] BakePointer()
        {
            var pixels = new Color32[PointerWidth * PointerHeight];
            for (int y = 0; y < PointerHeight; y++)
            {
                for (int x = 0; x < PointerWidth; x++)
                {
                    var p = new Vector2((x + 0.5f) / PointerWidth * 2f - 1f, (y + 0.5f) / PointerHeight * 2f - 1f);
                    float d = SdfIconCanvas.Triangle(p, new Vector2(0f, 1f), 1f, 2f);
                    float coverage = Mathf.Clamp01(0.5f - d * PointerHeight * 0.5f);
                    pixels[y * PointerWidth + x] = new Color(1f, 1f, 1f, coverage);
                }
            }

            return Encode(pixels, PointerWidth, PointerHeight);
        }

        private static byte[] Encode(Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        // A round shield in greys, so the badge colour on the style decides its hue: a light face with a darker ring and
        // studs, all inked. The centre stays plain for the Kavach number.
        private static byte[] BakeBadge()
        {
            var canvas = new SdfIconCanvas(BadgeSize, OutlineWidth, Outline);
            canvas.Paint(p => SdfIconCanvas.Circle(p, Vector2.zero, 0.84f), Face);
            canvas.Overlay(p => Mathf.Abs(SdfIconCanvas.Circle(p, Vector2.zero, 0.66f)) - 0.03f, Ring, 1f);
            for (int i = 0; i < 4; i++)
            {
                float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                var stud = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.75f;
                canvas.Overlay(p => SdfIconCanvas.Circle(p, stud, 0.05f), Ring, 1f);
            }

            return canvas.Encode();
        }
    }
}
