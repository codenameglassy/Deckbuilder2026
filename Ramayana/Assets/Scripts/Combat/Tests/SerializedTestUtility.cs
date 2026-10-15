// Sets private [SerializeField] values on test ScriptableObjects through SerializedObject,
// the same path the inspector and the asset builders use, so tests never need reflection or test-only setters.
using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace JourneyToLanka.Combat.Tests
{
    public static class SerializedTestUtility
    {
        public static void SetInt(Object target, string propertyPath, int value)
        {
            using var serializedObject = new SerializedObject(target);
            Require(serializedObject, propertyPath).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObject(Object target, string propertyPath, Object value)
        {
            using var serializedObject = new SerializedObject(target);
            Require(serializedObject, propertyPath).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObjectArray(Object target, string propertyPath, params Object[] values)
        {
            using var serializedObject = new SerializedObject(target);
            SerializedProperty array = Require(serializedObject, propertyPath);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetDeckEntries(Object target, string propertyPath, Object[] cards, int[] counts)
        {
            using var serializedObject = new SerializedObject(target);
            SerializedProperty array = Require(serializedObject, propertyPath);
            array.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_card").objectReferenceValue = cards[i];
                element.FindPropertyRelative("_count").intValue = counts[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Require(SerializedObject serializedObject, string propertyPath)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                throw new ArgumentException(
                    $"{serializedObject.targetObject.GetType().Name} has no serialized field '{propertyPath}'.");
            }

            return property;
        }
    }
}
