// Creates the fallback disc sprite the status UI draws in a status's colour until the status has its own icon: a
// soft-edged white circle written once as a PNG under Assets/_Project/Data/CombatScene/Sprites and imported as a
// sprite. It is wired into the combatant view style (status chips) and the combat feedback style (status intents)
// only where those fields are still empty. The sprite is never rewritten once it exists, and assigned references are
// never replaced. Runs after the Combat Scene and Combat Feedback builders, which create the two styles.
using System.IO;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class StatusVisualsAssetBuilder
    {
        public const string SpritesFolder = CombatSceneAssetBuilder.Root + "/Sprites";
        public const string DiscPath = SpritesFolder + "/StatusDisc.png";

        private const int DiscSize = 128;
        private const float EdgeSoftness = 1.5f;

        [MenuItem(AssetBuilderConstants.MenuRoot + "Status Visuals", priority = AssetBuilderConstants.FeatureMenuPriority + 16)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Status Visuals", Build);
        }

        [DefaultAssetBuilder(127)]
        public static void Build(AssetBuilderReport report)
        {
            Sprite disc = LoadOrCreateDisc(report);
            if (disc == null)
            {
                return;
            }

            var viewStyle = AssetDatabase.LoadAssetAtPath<CombatantViewStyleSO>(CombatSceneAssetBuilder.ViewStylePath);
            if (viewStyle != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(viewStyle, "_statusChipSprite", disc, report);
            }
            else
            {
                report.AddSkipped($"{CombatSceneAssetBuilder.ViewStylePath} (missing, run Create Default Assets > Combat Scene first)");
            }

            var feedbackStyle = AssetDatabase.LoadAssetAtPath<CombatFeedbackStyleSO>(CombatFeedbackAssetBuilder.FeedbackStylePath);
            if (feedbackStyle != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(feedbackStyle, "_statusIntentSprite", disc, report);
            }
            else
            {
                report.AddSkipped($"{CombatFeedbackAssetBuilder.FeedbackStylePath} (missing, run Create Default Assets > Combat Feedback first)");
            }
        }

        private static Sprite LoadOrCreateDisc(AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(DiscPath);
            if (existing != null)
            {
                report.AddExisted(DiscPath);
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(DiscPath) != null)
            {
                report.AddSkipped($"{DiscPath} (exists but is not imported as a sprite; set its Texture Type to Sprite)");
                return null;
            }

            AssetBuilderUtility.EnsureFolder(SpritesFolder, report);
            File.WriteAllBytes(Path.GetFullPath(DiscPath), EncodeDisc());
            AssetDatabase.ImportAsset(DiscPath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(DiscPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DiscPath);
            if (sprite == null)
            {
                report.AddSkipped($"{DiscPath} (written, but it did not import as a sprite)");
                return null;
            }

            report.AddCreated(DiscPath);
            return sprite;
        }

        private static byte[] EncodeDisc()
        {
            var texture = new Texture2D(DiscSize, DiscSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[DiscSize * DiscSize];
            float centre = (DiscSize - 1) * 0.5f;
            float radius = DiscSize * 0.5f - EdgeSoftness;
            for (int y = 0; y < DiscSize; y++)
            {
                for (int x = 0; x < DiscSize; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((radius - distance) / EdgeSoftness + 0.5f);
                    pixels[y * DiscSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }
    }
}
