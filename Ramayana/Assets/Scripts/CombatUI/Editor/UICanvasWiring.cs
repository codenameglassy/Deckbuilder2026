// Saves a root UI canvas's render mode and sort order into the scene. Views also set these in Awake, but a player
// build works out root canvas draw order from the values saved in the scene and does not re-sort when Awake changes
// them, so a canvas saved as World Space drew in the wrong order in builds. Saving the runtime values makes the
// Editor and the player agree. Records Undo and marks the scene dirty; reports through the shared builder report.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    internal static class UICanvasWiring
    {
        public static void SyncOverlayCanvas(Component view, int? sortingOrder, AssetBuilderReport report)
        {
            if (view == null || !view.TryGetComponent(out Canvas canvas))
            {
                return;
            }

            bool modeMatches = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
            bool orderMatches = !sortingOrder.HasValue || canvas.sortingOrder == sortingOrder.Value;
            if (modeMatches && orderMatches)
            {
                report.AddExisted($"{view.name} canvas render mode and sort order");
                return;
            }

            Undo.RecordObject(canvas, "Wire UI Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (sortingOrder.HasValue)
            {
                canvas.sortingOrder = sortingOrder.Value;
            }

            EditorUtility.SetDirty(canvas);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            report.AddWired($"{view.name} canvas saved as Screen Space Overlay, sort order {canvas.sortingOrder}");
        }
    }
}
