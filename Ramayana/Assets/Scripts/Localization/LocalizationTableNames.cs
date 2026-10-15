// Names of the project's string table collections, shared by runtime text components and the editor builders
// so a table is never referenced by a hand-typed string. UI holds interface text; Combat holds cards,
// card types, enemies and character names.
namespace JourneyToLanka.Localization
{
    public static class LocalizationTableNames
    {
        public const string UI = "UI";
        public const string Combat = "Combat";
    }
}
