// Bakes icons for the five core statuses and gives each status its icon where it has none. Every icon is a disc in
// the status's own colour with a cream glyph inked on top, in the same Mithila style as the intent icons, so the
// status chips and the status rows of enemy intents keep their colour coding: Focus an eye, Moha a spiral, Bhaya a
// cracked heart, Dahan a flame, Dharma a wheel. The disc colour is read from the status asset when the icon is baked.
// PNGs go under the combat scene's Sprites folder; a sprite is never rewritten once it exists, so an artist's
// replacement at the same path is safe, and a status that already has an icon keeps it. Other statuses (the powers)
// keep their disc and short label until they get icons. Runs after the Status and Status Visuals builders.
using System;
using JourneyToLanka.Combat.EditorTools;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class StatusIconsAssetBuilder
    {
        public const string FocusIconPath = StatusVisualsAssetBuilder.SpritesFolder + "/StatusFocus.png";
        public const string MohaIconPath = StatusVisualsAssetBuilder.SpritesFolder + "/StatusMoha.png";
        public const string BhayaIconPath = StatusVisualsAssetBuilder.SpritesFolder + "/StatusBhaya.png";
        public const string DahanIconPath = StatusVisualsAssetBuilder.SpritesFolder + "/StatusDahan.png";
        public const string DharmaIconPath = StatusVisualsAssetBuilder.SpritesFolder + "/StatusDharma.png";

        private const int IconSize = 128;
        private const float OutlineWidth = 0.07f;
        private const float DiscShade = 0.85f;

        private static readonly Color Outline = new Color(0.08f, 0.05f, 0.04f);
        private static readonly Color Cream = new Color(0.98f, 0.94f, 0.85f);
        private static readonly Color FlameCore = new Color(1f, 0.82f, 0.35f);

        [MenuItem(AssetBuilderConstants.MenuRoot + "Status Icons", priority = AssetBuilderConstants.FeatureMenuPriority + 16)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Status Icons", Build);
        }

        [DefaultAssetBuilder(130)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(StatusVisualsAssetBuilder.SpritesFolder, report);
            Give(StatusAssetBuilder.FocusPath, FocusIconPath, BakeFocus, report);
            Give(StatusAssetBuilder.MohaPath, MohaIconPath, BakeMoha, report);
            Give(StatusAssetBuilder.BhayaPath, BhayaIconPath, BakeBhaya, report);
            Give(StatusAssetBuilder.DahanPath, DahanIconPath, BakeDahan, report);
            Give(StatusAssetBuilder.DharmaPath, DharmaIconPath, BakeDharma, report);
        }

        private static void Give(string statusPath, string iconPath, Func<Color, byte[]> bake,
            AssetBuilderReport report)
        {
            var status = AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(statusPath);
            if (status == null)
            {
                report.AddSkipped($"{statusPath} (missing, run Create Default Assets > Status first)");
                return;
            }

            Color colour = status.Color;
            Sprite icon = SpriteBakeUtility.LoadOrBake(iconPath, IconSize, Vector4.zero, () => bake(colour), report);
            if (icon != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(status, "_icon", icon, report);
            }
        }

        private static SdfIconCanvas StartDisc(Color colour)
        {
            var canvas = new SdfIconCanvas(IconSize, OutlineWidth, Outline);
            var disc = new Color(colour.r * DiscShade, colour.g * DiscShade, colour.b * DiscShade, 1f);
            canvas.Paint(p => SdfIconCanvas.Circle(p, Vector2.zero, 0.86f), disc);
            return canvas;
        }

        // An eye: the overlap of two circles, an iris in the status colour and a dark pupil.
        private static byte[] BakeFocus(Color colour)
        {
            SdfIconCanvas canvas = StartDisc(colour);
            canvas.Paint(p => Mathf.Max(SdfIconCanvas.Circle(p, new Vector2(0f, -0.42f), 0.72f),
                SdfIconCanvas.Circle(p, new Vector2(0f, 0.42f), 0.72f)), Cream);
            var iris = new Color(colour.r * 0.7f, colour.g * 0.7f, colour.b * 0.7f, 1f);
            canvas.Paint(p => SdfIconCanvas.Circle(p, Vector2.zero, 0.2f), iris);
            canvas.Overlay(p => SdfIconCanvas.Circle(p, Vector2.zero, 0.08f), Outline, 1f);
            return canvas.Encode();
        }

        // A spiral of three turns, cut off at the edge of the glyph area.
        private static byte[] BakeMoha(Color colour)
        {
            const float Start = 0.06f;
            const float Growth = 0.085f;
            const float HalfWidth = 0.055f;
            const float Reach = 0.66f;
            SdfIconCanvas canvas = StartDisc(colour);
            canvas.Paint(p =>
            {
                float radius = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);
                if (angle < 0f)
                {
                    angle += Mathf.PI * 2f;
                }

                float turns = (radius - Start - Growth * angle) / (Mathf.PI * 2f * Growth);
                float turn = Mathf.Clamp(Mathf.Round(turns), 0f, 2f);
                float distance = Mathf.Abs(radius - (Start + Growth * (angle + Mathf.PI * 2f * turn))) - HalfWidth;
                return Mathf.Max(distance, radius - Reach);
            }, Cream);
            return canvas.Encode();
        }

        // A heart split by a zigzag crack.
        private static byte[] BakeBhaya(Color colour)
        {
            const float Scale = 0.74f;
            SdfIconCanvas canvas = StartDisc(colour);
            canvas.Paint(p => Heart(p / Scale) * Scale, Cream);
            Vector2[] crack =
            {
                new Vector2(0f, 0.3f), new Vector2(-0.1f, 0.12f), new Vector2(0.08f, -0.02f),
                new Vector2(-0.06f, -0.18f), new Vector2(0.02f, -0.32f),
            };
            canvas.Overlay(p =>
            {
                float line = float.MaxValue;
                for (int i = 0; i < crack.Length - 1; i++)
                {
                    line = Mathf.Min(line, SegmentDistance(p, crack[i], crack[i + 1]));
                }

                return Mathf.Max(line - 0.03f, Heart(p / Scale) * Scale + 0.04f);
            }, Outline, 1f);
            return canvas.Encode();
        }

        // A flame with a golden core.
        private static byte[] BakeDahan(Color colour)
        {
            SdfIconCanvas canvas = StartDisc(colour);
            canvas.Paint(p => Flame(p, 0.8f, -0.08f), Cream);
            canvas.Paint(p => Flame(p, 0.42f, -0.26f), FlameCore);
            return canvas.Encode();
        }

        // A wheel: rim, eight spokes, eight knobs outside the rim and a hub.
        private static byte[] BakeDharma(Color colour)
        {
            SdfIconCanvas canvas = StartDisc(colour);
            canvas.Paint(p =>
            {
                float radius = p.magnitude;
                float rim = Mathf.Abs(radius - 0.5f) - 0.07f;
                float angle = Mathf.Atan2(p.y, p.x);
                float sector = Mathf.Repeat(angle, Mathf.PI / 4f) - Mathf.PI / 8f;
                float spoke = Mathf.Max(Mathf.Abs(radius * Mathf.Sin(sector)) - 0.035f,
                    Mathf.Max(radius - 0.5f, 0.12f - radius));
                float knobs = float.MaxValue;
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    var knob = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.66f;
                    knobs = Mathf.Min(knobs, SdfIconCanvas.Circle(p, knob, 0.07f));
                }

                return Mathf.Min(Mathf.Min(rim, spoke), knobs);
            }, Cream);
            canvas.Paint(p => SdfIconCanvas.Circle(p, Vector2.zero, 0.16f), Cream);
            return canvas.Encode();
        }

        // Heart distance (after Inigo Quilez), point down, roughly unit size, shifted to sit centred.
        private static float Heart(Vector2 p)
        {
            float x = Mathf.Abs(p.x);
            float y = p.y + 0.55f;
            if (y + x > 1f)
            {
                return new Vector2(x - 0.25f, y - 0.75f).magnitude - Mathf.Sqrt(2f) / 4f;
            }

            float toTop = new Vector2(x, y - 1f).sqrMagnitude;
            float t = Mathf.Max(x + y, 0f) * 0.5f;
            float toDiagonal = new Vector2(x - t, y - t).sqrMagnitude;
            return Mathf.Sqrt(Mathf.Min(toTop, toDiagonal)) * Mathf.Sign(x - y);
        }

        // A teardrop flame: a round body with a tip that narrows upward, scaled and shifted down by dy.
        private static float Flame(Vector2 p, float scale, float dy)
        {
            float x = Mathf.Abs(p.x / scale);
            float y = (p.y - dy) / scale;
            float body = new Vector2(x, y + 0.25f).magnitude - 0.5f;
            float tip = Mathf.Max(x - 0.5f * Mathf.Pow(Mathf.Clamp01((0.8f - y) / 1.05f), 0.75f), y - 0.8f);
            tip = y > -0.25f ? Mathf.Max(tip, -(y + 0.25f)) : float.MaxValue;
            return Mathf.Min(body, tip) * scale;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
        }
    }
}
