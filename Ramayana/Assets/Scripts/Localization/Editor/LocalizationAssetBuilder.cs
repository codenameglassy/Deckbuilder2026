// Sets up Unity Localization for the project: the Localization Settings asset (only if the project has none
// active), the English locale, and the UI and Combat string table collections with a table per locale.
// Never replaces existing settings, locales or tables. Runs early so feature string builders can seed entries.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace JourneyToLanka.Localization.EditorTools
{
    public static class LocalizationAssetBuilder
    {
        public const string Root = AssetBuilderConstants.DataRoot + "/Localization";
        public const string LocalesFolder = Root + "/Locales";
        public const string TablesFolder = Root + "/Tables";
        public const string SettingsPath = Root + "/LocalizationSettings.asset";
        public const string EnglishLocalePath = LocalesFolder + "/English (en).asset";

        public static readonly LocaleIdentifier English = new LocaleIdentifier("en");

        [MenuItem(AssetBuilderConstants.MenuRoot + "Localization", priority = AssetBuilderConstants.FeatureMenuPriority + 2)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Localization", Build);
        }

        [DefaultAssetBuilder(20)]
        public static void Build(AssetBuilderReport report)
        {
            EnsureSettings(report);
            if (EnsureEnglishLocale(report) == null)
            {
                return;
            }

            LocalizationBuilderUtility.EnsureStringTableCollection(LocalizationTableNames.UI, TablesFolder, report);
            LocalizationBuilderUtility.EnsureStringTableCollection(LocalizationTableNames.Combat, TablesFolder, report);
        }

        private static void EnsureSettings(AssetBuilderReport report)
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
            {
                report.AddExisted("Localization Settings (active)");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                AssetBuilderUtility.EnsureFolder(Root, report);
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                report.AddCreated(SettingsPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            report.AddWired("Localization Settings set as the project's active settings");
        }

        private static Locale EnsureEnglishLocale(AssetBuilderReport report)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(English);
            if (locale != null)
            {
                report.AddExisted("Locale en");
                return locale;
            }

            locale = AssetDatabase.LoadAssetAtPath<Locale>(EnglishLocalePath);
            if (locale == null)
            {
                AssetBuilderUtility.EnsureFolder(LocalesFolder, report);
                locale = Locale.CreateLocale(SystemLanguage.English);
                AssetDatabase.CreateAsset(locale, EnglishLocalePath);
                report.AddCreated(EnglishLocalePath);
            }

            LocalizationEditorSettings.AddLocale(locale, false);
            report.AddWired("Locale en added to the project");
            return locale;
        }
    }
}
