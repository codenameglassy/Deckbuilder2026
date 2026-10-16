// Builds the card view template in code: frame with button, cost badge, and name, type and description texts.
// The template sits under an inactive holder so it never wakes up; the hand pool clones it. All sizes, colours
// and positions come from the style when the view applies it.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal static class CardViewFactory
    {
        public static CardView CreateTemplate(Transform inactiveHolder)
        {
            RectTransform root = UIBuildUtility.CreateRect("CardView", inactiveHolder);
            GameObject rootObject = root.gameObject;

            var frame = rootObject.AddComponent<Image>();
            frame.raycastTarget = true;
            var button = rootObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.ColorTint;
            var group = rootObject.AddComponent<CanvasGroup>();
            var layoutElement = rootObject.AddComponent<LayoutElement>();

            Image costBadge = UIBuildUtility.CreateImage("CostBadge", root, false);
            TextMeshProUGUI cost = UIBuildUtility.CreateText("Cost", costBadge.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(cost.rectTransform);

            TextMeshProUGUI cardName = UIBuildUtility.CreateText("Name", root, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI type = UIBuildUtility.CreateText("Type", root, TextAlignmentOptions.Center);
            TextMeshProUGUI description = UIBuildUtility.CreateText("Description", root, TextAlignmentOptions.Center);

            var view = rootObject.AddComponent<CardView>();
            view.Initialize(frame, button, group, layoutElement, costBadge, cost, cardName, type, description);
            return view;
        }
    }
}
