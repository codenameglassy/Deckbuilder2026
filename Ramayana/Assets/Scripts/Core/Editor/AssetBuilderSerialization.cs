// Serialized-property helpers for default asset builders, alongside AssetBuilderUtility.
// Values are written only into assets the current run created, and arrays are filled only while empty,
// so running a builder twice changes nothing and hand-edited data is never overwritten.
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JourneyToLanka.Core.EditorTools
{
    public static class AssetBuilderSerialization
    {
        public static T LoadOrCreateConfigured<T>(string assetPath, AssetBuilderReport report, Action<SerializedObject> configureNew)
            where T : ScriptableObject
        {
            bool existed = AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
            T asset = AssetBuilderUtility.LoadOrCreate<T>(assetPath, report);
            if (asset == null || existed || configureNew == null)
            {
                return asset;
            }

            using var serializedObject = new SerializedObject(asset);
            configureNew(serializedObject);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        public static void FillArrayIfEmpty(Object owner, string propertyPath, int count,
            Action<SerializedProperty, int> writeElement, AssetBuilderReport report)
        {
            if (owner == null)
            {
                report.AddSkipped($"{propertyPath} (owner is missing)");
                return;
            }

            string label = $"{owner.name}.{propertyPath}";
            using var serializedObject = new SerializedObject(owner);
            SerializedProperty array = serializedObject.FindProperty(propertyPath);

            if (array == null || !array.isArray || array.propertyType == SerializedPropertyType.String)
            {
                report.AddSkipped($"{label} (no array field with this name)");
                return;
            }

            if (array.arraySize > 0)
            {
                return;
            }

            array.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                writeElement(array.GetArrayElementAtIndex(i), i);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
            report.AddWired($"{label} -> {count} entries");
        }

        public static void FillObjectArrayIfEmpty(Object owner, string propertyPath, Object[] values, AssetBuilderReport report)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    string ownerName = owner != null ? owner.name : "(missing)";
                    report.AddSkipped($"{ownerName}.{propertyPath} (entry {i} is missing)");
                    return;
                }
            }

            FillArrayIfEmpty(owner, propertyPath, values.Length,
                (element, index) => element.objectReferenceValue = values[index], report);
        }

        public static void SetInt(SerializedObject serializedObject, string propertyPath, int value)
        {
            Require(serializedObject, propertyPath).intValue = value;
        }

        public static void SetString(SerializedObject serializedObject, string propertyPath, string value)
        {
            Require(serializedObject, propertyPath).stringValue = value;
        }

        public static SerializedProperty Require(SerializedObject serializedObject, string propertyPath)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{serializedObject.targetObject.GetType().Name} has no serialized field '{propertyPath}'.");
            }

            return property;
        }

        public static SerializedProperty RequireRelative(SerializedProperty parent, string relativePath)
        {
            SerializedProperty property = parent.FindPropertyRelative(relativePath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{parent.propertyPath} has no serialized field '{relativePath}'.");
            }

            return property;
        }
    }
}
