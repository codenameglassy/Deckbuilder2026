// Runs a single feature builder from its menu item: build, save once, log one summary.
using System;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Core.EditorTools
{
    public static class AssetBuilderRunner
    {
        public static void Run(string title, Action<AssetBuilderReport> build)
        {
            var report = new AssetBuilderReport();
            try
            {
                build(report);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                report.AddSkipped($"{title} aborted: {exception.Message}");
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }

            Log(title, report);
        }

        public static void Log(string title, AssetBuilderReport report)
        {
            string summary = report.BuildSummary(title);
            if (report.SkippedCount > 0)
            {
                Debug.LogWarning(summary);
            }
            else
            {
                Debug.Log(summary);
            }
        }
    }
}
