// Scene wiring for Core: fills empty fields on selected Bootstrappers. Records Undo, never replaces an assigned value.
// The installer list is filled from ServiceInstallers in the Bootstrapper's hierarchy only when the list is empty.
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Core.EditorTools
{
    public static class CoreSceneWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Core";

        [MenuItem(MenuPath)]
        private static void WireSelected()
        {
            var report = new AssetBuilderReport();
            VoidEventChannelSO bootCompleted = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>(CoreAssetBuilder.BootCompletedChannelPath);
            GameObject[] selection = Selection.gameObjects;

            for (int i = 0; i < selection.Length; i++)
            {
                if (!selection[i].TryGetComponent(out Bootstrapper bootstrapper))
                {
                    continue;
                }

                if (bootCompleted != null)
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(bootstrapper, "_bootCompleted", bootCompleted, report);
                }
                else
                {
                    report.AddSkipped("BootCompleted channel is missing; run Create Default Assets > Core first");
                }

                WireInstallersIfEmpty(bootstrapper, report);
            }

            AssetBuilderRunner.Log("Wire Selected: Core", report);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireInstallersIfEmpty(Bootstrapper bootstrapper, AssetBuilderReport report)
        {
            using var serializedBootstrapper = new SerializedObject(bootstrapper);
            SerializedProperty installers = serializedBootstrapper.FindProperty("_installers");
            if (installers.arraySize > 0)
            {
                return;
            }

            ServiceInstaller[] found = bootstrapper.GetComponentsInChildren<ServiceInstaller>(true);
            if (found.Length == 0)
            {
                report.AddSkipped($"{bootstrapper.name}._installers (no ServiceInstaller in its hierarchy)");
                return;
            }

            installers.arraySize = found.Length;
            for (int i = 0; i < found.Length; i++)
            {
                installers.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            serializedBootstrapper.ApplyModifiedProperties();
            report.AddWired($"{bootstrapper.name}._installers -> {found.Length} installer(s) in hierarchy order");
        }
    }
}
