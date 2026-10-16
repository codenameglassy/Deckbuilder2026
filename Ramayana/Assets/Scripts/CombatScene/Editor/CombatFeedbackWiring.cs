// Scene wiring for combat feedback. Fills only empty serialized fields on the selected objects, with Undo:
// a DamageNumberSpawner and an EnemyIntentView get the feedback style; a CombatantView gets the scene's
// DamageNumberSpawner (taken from the selection, or found in the open scenes). Assigned values are never replaced.
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public static class CombatFeedbackWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Combat Feedback";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 16)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Combat Feedback", WireSelection);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanWireSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void WireSelection(AssetBuilderReport report)
        {
            GameObject[] selection = Selection.gameObjects;
            var style = AssetDatabase.LoadAssetAtPath<CombatFeedbackStyleSO>(CombatFeedbackAssetBuilder.FeedbackStylePath);
            if (style == null)
            {
                report.AddSkipped($"{CombatFeedbackAssetBuilder.FeedbackStylePath} (missing, run Create Default Assets > All first)");
            }

            DamageNumberSpawner spawner = FindSpawner(selection);

            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];

                if (style != null && target.TryGetComponent(out DamageNumberSpawner selectedSpawner))
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(selectedSpawner, "_style", style, report);
                }

                if (style != null && target.TryGetComponent(out EnemyIntentView intent))
                {
                    AssetBuilderUtility.WireSceneFieldIfNull(intent, "_style", style, report);
                }

                if (target.TryGetComponent(out CombatantView view))
                {
                    if (spawner != null)
                    {
                        AssetBuilderUtility.WireSceneFieldIfNull(view, "_damageNumbers", spawner, report);
                    }
                    else
                    {
                        report.AddSkipped($"{target.name}._damageNumbers (no Damage Number Spawner in the open scenes)");
                    }
                }
            }
        }

        private static DamageNumberSpawner FindSpawner(GameObject[] selection)
        {
            for (int i = 0; i < selection.Length; i++)
            {
                if (selection[i].TryGetComponent(out DamageNumberSpawner spawner))
                {
                    return spawner;
                }
            }

            return Object.FindAnyObjectByType<DamageNumberSpawner>();
        }
    }
}
