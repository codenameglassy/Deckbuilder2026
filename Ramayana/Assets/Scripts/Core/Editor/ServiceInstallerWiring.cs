// Scene wiring helper for Boot: adds a component to the Boot object if it is missing and, for service installers,
// appends it to the Bootstrapper's installer list after the existing entries. Recorded with Undo; existing
// components and list entries are never replaced or reordered.
using JourneyToLanka.Core.Services;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Core.EditorTools
{
    public static class ServiceInstallerWiring
    {
        public static T GetOrAddComponent<T>(GameObject target, AssetBuilderReport report) where T : Component
        {
            if (target.TryGetComponent(out T existing))
            {
                report.AddExisted($"{target.name}.{typeof(T).Name}");
                return existing;
            }

            T added = Undo.AddComponent<T>(target);
            report.AddCreated($"{target.name}.{typeof(T).Name}");
            return added;
        }

        public static void EnsureInstallerListed(Bootstrapper bootstrapper, ServiceInstaller installer, AssetBuilderReport report)
        {
            using var serializedObject = new SerializedObject(bootstrapper);
            SerializedProperty installers = AssetBuilderSerialization.Require(serializedObject, "_installers");

            for (int i = 0; i < installers.arraySize; i++)
            {
                if (installers.GetArrayElementAtIndex(i).objectReferenceValue == installer)
                {
                    report.AddExisted($"{bootstrapper.name} installer list contains {installer.GetType().Name}");
                    return;
                }
            }

            int index = installers.arraySize;
            installers.arraySize = index + 1;
            installers.GetArrayElementAtIndex(index).objectReferenceValue = installer;
            serializedObject.ApplyModifiedProperties();
            report.AddWired($"{bootstrapper.name} installer list += {installer.GetType().Name}");
        }
    }
}
