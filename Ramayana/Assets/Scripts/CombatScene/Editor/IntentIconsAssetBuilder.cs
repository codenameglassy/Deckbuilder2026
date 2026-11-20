// Bakes the enemy intent icons as sprites, drawn from distance functions in an inked Mithila style: three attack tiers
// (a dagger, a sword, a gada), a pointed heater shield with a gold lotus for defending (it replaces the round shield
// baked by earlier versions, but never an artist's sprite), and a dark rounded plate (nine-sliced) that sits behind
// each intent row. The PNGs go under the combat scene's Sprites folder; icons are one world unit across (the intent
// view sizes any icon by its bounds anyway), and the plate is imported at a high pixels-per-unit so its nine-slice
// corners stay small next to a half-unit icon. The combat feedback style gets the sword as its attack fallback, the
// shield, the plate, and the three attack tiers (0, 10 and 20 damage) where those are still empty. It also creates the
// two intent layouts (Icon Badge, a large icon with the number on its corner, and Plate) and assigns Icon Badge to the
// style, and makes an outlined copy of the intent font's material for the numbers, where those are still empty. A
// sprite is never rewritten once it exists, so an artist's replacement at the same path is safe, and assigned
// references are never replaced. Runs after the Combat Feedback builder.
using JourneyToLanka.Core.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class IntentIconsAssetBuilder
    {
        public const string DaggerPath = StatusVisualsAssetBuilder.SpritesFolder + "/IntentDagger.png";
        public const string SwordPath = StatusVisualsAssetBuilder.SpritesFolder + "/IntentSword.png";
        public const string GadaPath = StatusVisualsAssetBuilder.SpritesFolder + "/IntentGada.png";
        public const string ShieldPath = StatusVisualsAssetBuilder.SpritesFolder + "/IntentShield.png";
        public const string GuardPath = StatusVisualsAssetBuilder.SpritesFolder + "/IntentGuard.png";
        public const string PlatePath = StatusVisualsAssetBuilder.SpritesFolder + "/IntentPlate.png";
        public const string LayoutsFolder = CombatSceneAssetBuilder.Root + "/IntentLayouts";
        public const string IconBadgeLayoutPath = LayoutsFolder + "/IntentLayout_IconBadge.asset";
        public const string PlateLayoutPath = LayoutsFolder + "/IntentLayout_Plate.asset";
        public const string NumberMaterialPath = CombatSceneAssetBuilder.Root + "/IntentNumberOutline.mat";

        private const float NumberOutlineWidth = 0.3f;
        private const float NumberFaceDilate = 0.15f;
        private static readonly Color NumberOutlineColor = new Color(0.08f, 0.05f, 0.04f, 1f);

        private const int IconSize = 128;
        private const int PlateSize = 64;
        private const int PlateBorder = 22;
        private const int PlatePixelsPerUnit = 256;
        private const float OutlineWidth = 0.07f;

        private static readonly Color Outline = new Color(0.08f, 0.05f, 0.04f);
        private static readonly Color Steel = new Color(0.86f, 0.88f, 0.93f);
        private static readonly Color Gold = new Color(0.96f, 0.74f, 0.28f);
        private static readonly Color Grip = new Color(0.5f, 0.28f, 0.16f);
        private static readonly Color Bronze = new Color(0.85f, 0.55f, 0.25f);
        private static readonly Color ShieldBlue = new Color(0.33f, 0.6f, 0.95f);
        private static readonly Color PlateFill = new Color(0.09f, 0.065f, 0.05f, 0.82f);
        private static readonly Color PlateEdge = new Color(0.95f, 0.75f, 0.4f, 0.55f);

        [MenuItem(AssetBuilderConstants.MenuRoot + "Intent Icons", priority = AssetBuilderConstants.FeatureMenuPriority + 16)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Intent Icons", Build);
        }

        [DefaultAssetBuilder(128)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(StatusVisualsAssetBuilder.SpritesFolder, report);
            Sprite dagger = SpriteBakeUtility.LoadOrBake(DaggerPath, IconSize, Vector4.zero, BakeDagger, report);
            Sprite sword = SpriteBakeUtility.LoadOrBake(SwordPath, IconSize, Vector4.zero, BakeSword, report);
            Sprite gada = SpriteBakeUtility.LoadOrBake(GadaPath, IconSize, Vector4.zero, BakeGada, report);
            Sprite guard = SpriteBakeUtility.LoadOrBake(GuardPath, IconSize, Vector4.zero, BakeGuard, report);
            Sprite plate = SpriteBakeUtility.LoadOrBake(PlatePath, PlatePixelsPerUnit, Vector4.one * PlateBorder,
                BakePlate, report);

            const string StylePath = CombatFeedbackAssetBuilder.FeedbackStylePath;
            var style = AssetDatabase.LoadAssetAtPath<CombatFeedbackStyleSO>(StylePath);
            if (style == null)
            {
                report.AddSkipped($"{StylePath} (missing, run Create Default Assets > Combat Feedback first)");
                return;
            }

            WireIfBoth(style, "_attackIcon", sword, report);
            ReplaceDefaultDefendIcon(style, guard, report);
            WireIfBoth(style, "_intentPlateSprite", plate, report);

            AssetBuilderUtility.EnsureFolder(LayoutsFolder, report);
            var badgeLayout = AssetBuilderSerialization.LoadOrCreateConfigured<IconBadgeIntentLayoutSO>(
                IconBadgeLayoutPath, report, null);
            AssetBuilderSerialization.LoadOrCreateConfigured<PlateIntentLayoutSO>(PlateLayoutPath, report, null);
            if (badgeLayout != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_intentLayout", badgeLayout, report);
            }

            Material numberMaterial = LoadOrCreateNumberMaterial(style, report);
            if (numberMaterial != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(style, "_intentNumberMaterial", numberMaterial, report);
            }

            if (dagger == null || sword == null || gada == null)
            {
                return;
            }

            Sprite[] tiers = { dagger, sword, gada };
            int[] minimums = { 0, 10, 20 };
            float[] scales = { 0.9f, 1f, 1.12f };
            AssetBuilderSerialization.FillArrayIfEmpty(style, "_attackTiers", tiers.Length, (element, i) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_minDamage").intValue = minimums[i];
                AssetBuilderSerialization.RequireRelative(element, "_icon").objectReferenceValue = tiers[i];
                AssetBuilderSerialization.RequireRelative(element, "_scale").floatValue = scales[i];
            }, report);
        }

        // A copy of the intent font's material with a thick dark outline and a little face dilation, so the number reads
        // over its icon and any background. It is tied to that font's atlas: if the intent font changes later, delete
        // the material and run this builder again.
        private static Material LoadOrCreateNumberMaterial(CombatFeedbackStyleSO style, AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(NumberMaterialPath);
            if (existing != null)
            {
                report.AddExisted(NumberMaterialPath);
                return existing;
            }

            TMP_FontAsset font = style.IntentFont != null ? style.IntentFont : TMP_Settings.defaultFontAsset;
            if (font == null || font.material == null)
            {
                report.AddSkipped($"{NumberMaterialPath} (no intent font or TMP default font to copy)");
                return null;
            }

            var material = new Material(font.material) { name = "IntentNumberOutline" };
            material.SetFloat("_OutlineWidth", NumberOutlineWidth);
            material.SetColor("_OutlineColor", NumberOutlineColor);
            material.SetFloat("_FaceDilate", NumberFaceDilate);
            material.EnableKeyword("OUTLINE_ON");
            AssetDatabase.CreateAsset(material, NumberMaterialPath);
            report.AddCreated(NumberMaterialPath);
            return material;
        }

        // Gives the style the guard shield as its defend icon when the slot is empty or still holds the round shield this
        // builder baked before. Any other sprite (an artist's) is left alone. The old round shield PNG is not deleted.
        private static void ReplaceDefaultDefendIcon(CombatFeedbackStyleSO style, Sprite guard, AssetBuilderReport report)
        {
            if (guard == null)
            {
                return;
            }

            using var serializedStyle = new SerializedObject(style);
            SerializedProperty field = AssetBuilderSerialization.Require(serializedStyle, "_defendIcon");
            Object current = field.objectReferenceValue;
            if (current == guard)
            {
                return;
            }

            if (current != null && AssetDatabase.GetAssetPath(current) != ShieldPath)
            {
                report.AddSkipped($"{style.name}._defendIcon (holds {current.name}, which this builder did not bake)");
                return;
            }

            field.objectReferenceValue = guard;
            serializedStyle.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(style);
            report.AddWired($"{style.name}._defendIcon -> {guard.name}");
        }

        private static void WireIfBoth(UnityEngine.Object owner, string field, Sprite sprite, AssetBuilderReport report)
        {
            if (sprite != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(owner, field, sprite, report);
            }
        }

        private static byte[] BakeDagger()
        {
            var canvas = new SdfIconCanvas(IconSize, OutlineWidth, Outline);
            PaintBlade(canvas, 1f, new Vector2(0f, 0.05f), false);
            return canvas.Encode();
        }

        private static byte[] BakeSword()
        {
            var canvas = new SdfIconCanvas(IconSize, OutlineWidth, Outline);
            PaintBlade(canvas, 1.1f, new Vector2(0f, 0.02f), true);
            return canvas.Encode();
        }

        // A blade standing on its grip, turned 45 degrees so it points to the upper right.
        private static void PaintBlade(SdfIconCanvas canvas, float scale, Vector2 shift, bool longBlade)
        {
            Vector2 Local(Vector2 p) => (SdfIconCanvas.Rotate(p, -45f) + shift) / scale;

            var gripHalf = new Vector2(0.065f, 0.19f);
            canvas.Paint(p => SdfIconCanvas.Box(Local(p), new Vector2(0f, -0.34f), gripHalf, 0.02f) * scale, Grip);
            canvas.Paint(p => SdfIconCanvas.Circle(Local(p), new Vector2(0f, -0.6f), 0.1f) * scale, Gold);

            if (longBlade)
            {
                canvas.Paint(p =>
                {
                    Vector2 q = Local(p);
                    float body = SdfIconCanvas.Box(q, new Vector2(0f, 0.27f), new Vector2(0.1f, 0.32f), 0f);
                    float tip = SdfIconCanvas.Triangle(q, new Vector2(0f, 0.9f), 0.1f, 0.32f);
                    return Mathf.Min(body, tip) * scale;
                }, Steel);
                canvas.Paint(p =>
                {
                    Vector2 q = Local(p);
                    float bar = SdfIconCanvas.Box(q, new Vector2(0f, -0.08f), new Vector2(0.34f, 0.055f), 0.03f);
                    float left = SdfIconCanvas.Circle(q, new Vector2(-0.36f, -0.08f), 0.08f);
                    float right = SdfIconCanvas.Circle(q, new Vector2(0.36f, -0.08f), 0.08f);
                    return Mathf.Min(bar, Mathf.Min(left, right)) * scale;
                }, Gold);
            }
            else
            {
                var tip = new Vector2(0f, 0.72f);
                var guardHalf = new Vector2(0.26f, 0.055f);
                canvas.Paint(p => SdfIconCanvas.Triangle(Local(p), tip, 0.14f, 0.78f) * scale, Steel);
                canvas.Paint(p => SdfIconCanvas.Box(Local(p), new Vector2(0f, -0.1f), guardHalf, 0.03f) * scale, Gold);
            }
        }

        // A gada (mace): a ridged bronze head with a finial on a long shaft, turned to point upper right.
        private static byte[] BakeGada()
        {
            var canvas = new SdfIconCanvas(IconSize, OutlineWidth, Outline);
            Vector2 Local(Vector2 p) => SdfIconCanvas.Rotate(p, -40f);

            var shaftHalf = new Vector2(0.07f, 0.48f);
            canvas.Paint(p => SdfIconCanvas.Box(Local(p), new Vector2(0f, -0.3f), shaftHalf, 0.03f), Grip);
            canvas.Paint(p => SdfIconCanvas.Circle(Local(p), new Vector2(0f, -0.8f), 0.1f), Gold);
            canvas.Paint(p => SdfIconCanvas.Circle(Local(p), new Vector2(0f, 0.82f), 0.09f), Gold);
            canvas.Paint(p => SdfIconCanvas.Circle(Local(p), new Vector2(0f, 0.38f), 0.36f), Bronze);
            canvas.Paint(p => SdfIconCanvas.Box(Local(p), Vector2.zero, new Vector2(0.16f, 0.05f), 0.02f), Gold);
            canvas.Overlay(p =>
            {
                Vector2 q = Local(p);
                float ridges = Mathf.Min(Mathf.Abs(q.x) - 0.012f,
                    Mathf.Min(Mathf.Abs(q.x - 0.17f) - 0.012f, Mathf.Abs(q.x + 0.17f) - 0.012f));
                float inside = SdfIconCanvas.Circle(q, new Vector2(0f, 0.38f), 0.31f);
                return Mathf.Max(ridges, inside);
            }, Outline, 0.6f);
            return canvas.Encode();
        }

        // A heater shield for guarding: a blue face that narrows to a point, an inner gold rule following its edge and a
        // flat gold lotus. The point leaves the lower right corner open for the number, as the blades do.
        private static byte[] BakeGuard()
        {
            var canvas = new SdfIconCanvas(IconSize, OutlineWidth, Outline);
            canvas.Paint(p => Heater(p, 0.95f, 0.05f), ShieldBlue);
            canvas.Overlay(p => Mathf.Abs(Heater(p, 0.72f, 0.07f)) - 0.025f, Gold, 1f);
            var centre = new Vector2(0f, -0.1f);
            canvas.Overlay(p => Mathf.Min(Petal(p, centre, 90f, 0.58f, 0.13f),
                Mathf.Min(Petal(p, centre, 140f, 0.46f, 0.1f), Petal(p, centre, 40f, 0.46f, 0.1f))), Gold, 1f);
            canvas.Overlay(p => SdfIconCanvas.Circle(p, centre, 0.07f), new Color(0.85f, 0.55f, 0.15f), 1f);
            return canvas.Encode();
        }

        // Heater shield: the lens where two large circles overlap, cut flat on top and trimmed at the sides.
        private static float Heater(Vector2 p, float scale, float shiftUp)
        {
            var q = new Vector2(p.x / scale, (p.y - shiftUp) / scale);
            float left = SdfIconCanvas.Circle(q, new Vector2(-0.42f, 0.2f), 1.05f);
            float right = SdfIconCanvas.Circle(q, new Vector2(0.42f, 0.2f), 1.05f);
            float top = q.y - 0.72f;
            float sides = Mathf.Abs(q.x) - 0.66f;
            return Mathf.Max(Mathf.Max(left, right), Mathf.Max(top, sides)) * scale;
        }

        // A petal from centre outward at the given angle in degrees, length and half width.
        private static float Petal(Vector2 p, Vector2 centre, float degrees, float length, float halfWidth)
        {
            Vector2 local = SdfIconCanvas.Rotate(p - centre, degrees);
            float halfLength = length * 0.5f;
            var q = new Vector2((local.x - halfLength) / halfLength, local.y / halfWidth);
            return (q.magnitude - 1f) * halfWidth;
        }

        // The plate behind an intent row: a dark rounded tile with a faint gold edge, nine-sliced by its border.
        private static byte[] BakePlate()
        {
            var pixels = new Color32[PlateSize * PlateSize];
            float half = PlateSize * 0.5f;
            const float Radius = 18f;
            const float Edge = 1.5f;
            for (int y = 0; y < PlateSize; y++)
            {
                for (int x = 0; x < PlateSize; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = SdfIconCanvas.Box(p, Vector2.zero, new Vector2(half - 1f, half - 1f), Radius);
                    float coverage = Mathf.Clamp01(0.5f - d);
                    float rim = Mathf.Clamp01(Edge * 0.5f + 0.5f - Mathf.Abs(d + Edge));
                    Color c = Color.Lerp(PlateFill, PlateEdge, rim * PlateEdge.a);
                    c.a = Mathf.Max(PlateFill.a, rim * PlateEdge.a) * coverage;
                    pixels[y * PlateSize + x] = c;
                }
            }

            var texture = new Texture2D(PlateSize, PlateSize, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }
    }
}
