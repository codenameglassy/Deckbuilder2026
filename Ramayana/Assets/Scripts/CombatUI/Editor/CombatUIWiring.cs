// Scene wiring for the combat UI. Fills only empty serialized fields on the selected objects, with Undo:
// a CombatHudView gets its style, the CombatController anchor and the CombatStarted channel; a
// CombatControllerAnchorProvider gets the anchor. Assigned values are never replaced. The HUD's canvas is also saved
// as Screen Space Overlay (its sort order is left as set), so player builds draw it in the right order (see
// UICanvasWiring). It also checks the EventSystem:
// the Boot scene owns the game's only EventSystem (it stays loaded under every gameplay scene), so this tool never adds
// one. It reports when the open scenes have none (open Boot alongside), more than one, or one without the Input
// System UI module.
using JourneyToLanka.CombatScene;
using JourneyToLanka.CombatScene.EditorTools;
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Core.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CombatUIWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Combat UI";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 14)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Combat UI", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            GameObject[] selection = Selection.gameObjects;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];

                if (target.TryGetComponent(out CombatHudView hud))
                {
                    Wire(hud, "_style", Load<CombatHudStyleSO>(CombatUIAssetBuilder.StylePath, report), report);
                    Wire(hud, "_controllerAnchor", Load<CombatControllerAnchorSO>(CombatUIAssetBuilder.ControllerAnchorPath, report), report);
                    Wire(hud, "_combatStarted", Load<VoidEventChannelSO>(CombatSceneAssetBuilder.CombatStartedPath, report), report);
                    UICanvasWiring.SyncOverlayCanvas(hud, null, report);
                }

                if (target.TryGetComponent(out CombatControllerAnchorProvider provider))
                {
                    Wire(provider, "_anchor", Load<CombatControllerAnchorSO>(CombatUIAssetBuilder.ControllerAnchorPath, report), report);
                }
            }

            EnsureEventSystem(report);
        }

        private static void EnsureEventSystem(AssetBuilderReport report)
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (eventSystems.Length == 0)
            {
                report.AddSkipped("EventSystem (none in the open scenes; the Boot scene owns it, open Boot alongside this scene)");
                return;
            }

            if (eventSystems.Length > 1)
            {
                for (int i = 0; i < eventSystems.Length; i++)
                {
                    report.AddSkipped($"EventSystem '{eventSystems[i].name}' in scene '{eventSystems[i].gameObject.scene.name}' (keep only the one in Boot)");
                }

                return;
            }

            EventSystem existing = eventSystems[0];
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                report.AddSkipped($"EventSystem '{existing.name}' has no Input System UI Input Module; replace its input module");
                return;
            }

            report.AddExisted("EventSystem");
        }

        private static void Wire(Object owner, string propertyPath, Object value, AssetBuilderReport report)
        {
            if (value != null)
            {
                AssetBuilderUtility.WireSceneFieldIfNull(owner, propertyPath, value, report);
            }
        }

        private static T Load<T>(string path, AssetBuilderReport report) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                report.AddSkipped($"{path} (missing, run Create Default Assets > All first)");
            }

            return asset;
        }
    }
}
