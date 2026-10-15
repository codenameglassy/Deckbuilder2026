// Shared menu paths, data roots and menu priorities for every feature's default asset builder and scene wiring.
namespace JourneyToLanka.Core.EditorTools
{
    public static class AssetBuilderConstants
    {
        public const string MenuRoot = "Tools/Journey to Lanka/Create Default Assets/";
        public const string WireSelectedMenuRoot = "Tools/Journey to Lanka/Wire Selected/";
        public const string DataRoot = "Assets/_Project/Data";
        public const string ScenesRoot = "Assets/_Project/Scenes";

        public const int AllMenuPriority = 0;
        public const int FeatureMenuPriority = 100;
    }
}
