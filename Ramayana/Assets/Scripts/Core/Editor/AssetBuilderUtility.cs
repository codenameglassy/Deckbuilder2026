// Idempotent, non-destructive helpers for builders: create folders and assets only when missing,
// and fill object references only when they are null. Never overwrites existing data.
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Core.EditorTools
{
    public static class AssetBuilderUtility
    {
        public static void EnsureFolder(string folderPath, AssetBuilderReport report)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int slash = folderPath.LastIndexOf('/');
            if (slash <= 0)
            {
                report.AddSkipped($"{folderPath} (invalid folder path, must start with Assets/)");
                return;
            }

            string parentPath = folderPath.Substring(0, slash);
            string folderName = folderPath.Substring(slash + 1);

            EnsureFolder(parentPath, report);
            if (!AssetDatabase.IsValidFolder(parentPath))
            {
                return;
            }

            string guid = AssetDatabase.CreateFolder(parentPath, folderName);
            if (string.IsNullOrEmpty(guid))
            {
                report.AddSkipped($"{folderPath} (folder creation failed)");
                return;
            }

            report.AddCreated(folderPath + "/");
        }

        public static T LoadOrCreate<T>(string assetPath, AssetBuilderReport report) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                report.AddExisted(assetPath);
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                report.AddSkipped($"{assetPath} (a different asset type occupies this path)");
                return null;
            }

            int slash = assetPath.LastIndexOf('/');
            if (slash > 0)
            {
                EnsureFolder(assetPath.Substring(0, slash), report);
            }

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            report.AddCreated(assetPath);
            return created;
        }

        public static void WireAssetFieldIfNull(Object owner, string propertyPath, Object value, AssetBuilderReport report)
        {
            WireIfNull(owner, propertyPath, value, report, false);
        }

        public static void WireSceneFieldIfNull(Object owner, string propertyPath, Object value, AssetBuilderReport report)
        {
            WireIfNull(owner, propertyPath, value, report, true);
        }

        private static void WireIfNull(Object owner, string propertyPath, Object value, AssetBuilderReport report, bool recordUndo)
        {
            if (owner == null || value == null)
            {
                report.AddSkipped($"{propertyPath} (owner or value is missing)");
                return;
            }

            string label = $"{owner.name}.{propertyPath}";
            using var serializedObject = new SerializedObject(owner);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);

            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                report.AddSkipped($"{label} (no object reference field with this name)");
                return;
            }

            if (property.objectReferenceValue != null)
            {
                return;
            }

            property.objectReferenceValue = value;
            if (property.objectReferenceValue == null)
            {
                report.AddSkipped($"{label} (type mismatch with {value.name})");
                return;
            }

            if (recordUndo)
            {
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(owner);
            }

            report.AddWired($"{label} -> {value.name}");
        }
    }
}
