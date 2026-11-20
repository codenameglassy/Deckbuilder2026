// Writes a baked PNG once and imports it as a sprite (single, full-rect mesh, clamped, uncompressed, with mipmaps) at
// the given pixels-per-unit and nine-slice border. An existing sprite at the path is returned untouched, so an
// artist's replacement saved at the same path is never overwritten; a file there that is not a sprite is reported and
// left alone. Shared by the combat scene's icon builders.
using System;
using System.IO;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class SpriteBakeUtility
    {
        public static Sprite LoadOrBake(string path, int pixelsPerUnit, Vector4 border, Func<byte[]> bake,
            AssetBuilderReport report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                report.AddExisted(path);
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                report.AddSkipped($"{path} (exists but is not imported as a sprite; set its Texture Type to Sprite)");
                return null;
            }

            File.WriteAllBytes(Path.GetFullPath(path), bake());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                report.AddSkipped($"{path} (written, but it did not import as a sprite)");
                return null;
            }

            report.AddCreated(path);
            return sprite;
        }
    }
}
