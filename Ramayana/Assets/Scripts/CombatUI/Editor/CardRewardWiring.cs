// Scene wiring for the card reward screen. Fills only empty serialized fields on a selected CardRewardView, with
// Undo: its style, the Combat HUD style for the cards, and the CardRewardOffered and CardRewardChosen channels.
// It also saves the view's canvas as Screen Space Overlay at the style's sort order, so player builds draw the
// reward screen above the combat HUD (see UICanvasWiring).
using JourneyToLanka.Core.EditorTools;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Run.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class CardRewardWiring
    {
        private const string MenuPath = AssetBuilderConstants.WireSelectedMenuRoot + "Card Reward";

        [MenuItem(MenuPath, priority = AssetBuilderConstants.FeatureMenuPriority + 18)]
        private static void WireSelected()
        {
            AssetBuilderRunner.Run("Wire Card Reward", WireSelection);
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
                if (!selection[i].TryGetComponent(out CardRewardView view))
                {
                    continue;
                }

                Wire(view, "_style", AssetDatabase.LoadAssetAtPath<CardRewardStyleSO>(CardRewardAssetBuilder.StylePath), report);
                Wire(view, "_cardStyle", AssetDatabase.LoadAssetAtPath<CombatHudStyleSO>(CombatUIAssetBuilder.StylePath), report);
                Wire(view, "_rewardOffered", AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>(RunAssetBuilder.CardRewardOfferedPath), report);
                Wire(view, "_rewardChosen", AssetDatabase.LoadAssetAtPath<IntEventChannelSO>(RunAssetBuilder.CardRewardChosenPath), report);

                var style = AssetDatabase.LoadAssetAtPath<CardRewardStyleSO>(CardRewardAssetBuilder.StylePath);
                if (style != null)
                {
                    UICanvasWiring.SyncOverlayCanvas(view, style.SortingOrder, report);
                }
            }
        }

        private static void Wire(Object owner, string propertyPath, Object value, AssetBuilderReport report)
        {
            if (value == null)
            {
                report.AddSkipped($"{owner.name}.{propertyPath} (asset missing, run Create Default Assets > All first)");
                return;
            }

            AssetBuilderUtility.WireSceneFieldIfNull(owner, propertyPath, value, report);
        }
    }
}
