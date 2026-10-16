// Scene wiring for the combat UI. Fills only empty serialized fields on the selected objects, with Undo:
// a CombatHudView gets its style, the CombatController anchor and the CombatStarted channel; a
// CombatControllerAnchorProvider gets the anchor. If the open scenes have no EventSystem, it adds one with the
// Input System UI module so keyboard and gamepad navigation work. Assigned values are never replaced.
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
            EventSystem existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    report.AddSkipped($"EventSystem '{existing.name}' has no Input System UI Input Module; replace its input module");
                }
                else
                {
                    report.AddExisted("EventSystem");
                }

                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            report.AddCreated("EventSystem (Input System UI Input Module)");
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
