// Builder helpers for Unity Localization: create a string table collection if missing, make sure it has a table
// for every project locale, and seed entries. Seeding fills only keys that are missing or empty, so text
// edited in the Localization Tables window is never overwritten.
using System.Collections.Generic;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class LocalizationBuilderUtility
    {
        public static StringTableCollection EnsureStringTableCollection(string tableName, string folder, AssetBuilderReport report)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection != null)
            {
                report.AddExisted($"String table '{tableName}'");
            }
            else
            {
                AssetBuilderUtility.EnsureFolder(folder, report);
                collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, folder);
                if (collection == null)
                {
                    report.AddSkipped($"String table '{tableName}' (creation failed)");
                    return null;
                }

                report.AddCreated($"String table '{tableName}'");
            }

            foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            {
                if (locale == null || collection.ContainsTable(locale.Identifier))
                {
                    continue;
                }

                collection.AddNewTable(locale.Identifier);
                report.AddCreated($"String table '{tableName}' ({locale.Identifier.Code})");
            }

            return collection;
        }

        public static void AddEntriesIfMissing(string tableName, LocaleIdentifier localeId,
            IReadOnlyList<KeyValuePair<string, string>> entries, AssetBuilderReport report)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                report.AddSkipped($"String table '{tableName}' (missing, run the Localization builder first)");
                return;
            }

            if (!(collection.GetTable(localeId) is StringTable table))
            {
                report.AddSkipped($"String table '{tableName}' has no {localeId.Code} table");
                return;
            }

            int added = 0;
            int existed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                KeyValuePair<string, string> pair = entries[i];
                StringTableEntry entry = table.GetEntry(pair.Key);
                if (entry != null && !string.IsNullOrEmpty(entry.Value))
                {
                    existed++;
                    continue;
                }

                table.AddEntry(pair.Key, pair.Value);
                added++;
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
                report.AddCreated($"{tableName} ({localeId.Code}): {added} entries");
            }

            if (existed > 0)
            {
                report.AddExisted($"{tableName} ({localeId.Code}): {existed} entries");
            }
        }
    }
}
