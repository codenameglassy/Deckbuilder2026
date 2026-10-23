// Hand-off between the Play from Boot editor tool and the Boot scene: the tool records which scene you were
// working in before entering Play mode, and Boot loads that scene instead of the first scene. Editor only;
// in builds nothing is ever recorded and Boot always loads its first scene.
namespace JourneyToLanka.Scenes
{
    public static class EditorStartScene
    {
        public const string SessionKey = "JourneyToLanka.EditorStartScene";

#if UNITY_EDITOR
        public static bool TryConsume(out string scenePath)
        {
            scenePath = UnityEditor.SessionState.GetString(SessionKey, string.Empty);
            UnityEditor.SessionState.EraseString(SessionKey);
            return !string.IsNullOrEmpty(scenePath);
        }
#endif
    }
}
