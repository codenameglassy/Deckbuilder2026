// Puts the TMP font asset selected in the Project window into every UI style font field listed by UIFontAssetBuilder,
// replacing whatever is assigned, as one Undo step. This is the deliberate way to try or change the UI font; the
// UI Fonts default asset builder only fills empty fields and never replaces one. Select LiberationSans SDF to go back
// to the default look.
using JourneyToLanka.Core.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class UIFontSwapper
    {
        private const string MenuPath = "Tools/Journey to Lanka/UI Fonts/Apply Selected Font to UI Styles";
        private const string UndoName = "Apply UI Font";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority)]
        private static void ApplySelected()
        {
            TMP_FontAsset font = Selection.activeObject as TMP_FontAsset;
            AssetBuilderRunner.Run(UndoName, report => Apply(font, report));
        }

        [MenuItem(MenuPath, true)]
        private static bool CanApplySelected()
        {
            return Selection.activeObject is TMP_FontAsset;
        }

        public static void Apply(TMP_FontAsset font, AssetBuilderReport report)
        {
            if (font == null)
            {
                report.AddSkipped("No TMP font asset is selected in the Project window");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            int undoGroup = Undo.GetCurrentGroup();

            (string AssetPath, string FieldName)[] fields = UIFontAssetBuilder.FontFields;
            for (int i = 0; i < fields.Length; i++)
            {
                ApplyToField(fields[i].AssetPath, fields[i].FieldName, font, report);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void ApplyToField(string assetPath, string fieldName, TMP_FontAsset font, AssetBuilderReport report)
        {
            Object style = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (style == null)
            {
                report.AddSkipped($"{assetPath} (style asset is missing, run its builder first)");
                return;
            }

            string label = $"{style.name}.{fieldName}";
            using var serializedObject = new SerializedObject(style);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                report.AddSkipped($"{label} (no object reference field with this name)");
                return;
            }

            Object previous = property.objectReferenceValue;
            if (previous == font)
            {
                return;
            }

            property.objectReferenceValue = font;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(style);

            string previousName = previous != null ? previous.name : "default font";
            report.AddWired($"{label}: {previousName} -> {font.name}");
        }
    }
}
