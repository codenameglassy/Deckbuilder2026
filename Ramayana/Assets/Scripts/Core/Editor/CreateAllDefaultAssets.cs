// "All" menu item. Discovers every [DefaultAssetBuilder] method, runs them in order, saves once, logs one summary.
// Stops at the first failing builder so later builders never run against missing dependencies.
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Core.EditorTools
{
    public static class CreateAllDefaultAssets
    {
        [MenuItem(AssetBuilderConstants.MenuRoot + "All", priority = AssetBuilderConstants.AllMenuPriority)]
        private static void CreateAll()
        {
            List<MethodInfo> builders = CollectBuilders();
            var report = new AssetBuilderReport();
            var arguments = new object[] { report };

            try
            {
                for (int i = 0; i < builders.Count; i++)
                {
                    MethodInfo builder = builders[i];
                    try
                    {
                        builder.Invoke(null, arguments);
                    }
                    catch (TargetInvocationException exception)
                    {
                        Debug.LogException(exception.InnerException ?? exception);
                        report.AddSkipped($"{builder.DeclaringType?.Name} failed; {builders.Count - i - 1} remaining builder(s) not run");
                        break;
                    }
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }

            AssetBuilderRunner.Log("All", report);
        }

        private static List<MethodInfo> CollectBuilders()
        {
            TypeCache.MethodCollection methods = TypeCache.GetMethodsWithAttribute<DefaultAssetBuilderAttribute>();
            var valid = new List<MethodInfo>(methods.Count);

            foreach (MethodInfo method in methods)
            {
                if (HasValidSignature(method))
                {
                    valid.Add(method);
                }
                else
                {
                    Debug.LogError($"{method.DeclaringType?.FullName}.{method.Name} has [DefaultAssetBuilder] but is not 'static void (AssetBuilderReport)'.");
                }
            }

            valid.Sort(CompareBuilders);
            return valid;
        }

        private static bool HasValidSignature(MethodInfo method)
        {
            if (!method.IsStatic || method.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(AssetBuilderReport);
        }

        private static int CompareBuilders(MethodInfo a, MethodInfo b)
        {
            int orderA = a.GetCustomAttribute<DefaultAssetBuilderAttribute>().Order;
            int orderB = b.GetCustomAttribute<DefaultAssetBuilderAttribute>().Order;
            int result = orderA.CompareTo(orderB);
            return result != 0
                ? result
                : string.CompareOrdinal(a.DeclaringType?.FullName, b.DeclaringType?.FullName);
        }
    }
}
