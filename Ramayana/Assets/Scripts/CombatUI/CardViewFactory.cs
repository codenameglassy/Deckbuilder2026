// Builds the card view template in code: the playable glow behind everything, the frame (the Button's target and
// the card's raycast surface), cost badge, name and type texts, a type icon, and the description text over its
// readability panel. The frame is a stretched child rather than the root's own image, because a child always draws
// over its parent's image and the glow has to sit underneath the frame.
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

            Image glow = UIBuildUtility.CreateImage("Glow", root, false);
            glow.color = new Color(1f, 1f, 1f, 0f);
            Image frame = UIBuildUtility.CreateImage("Frame", root, true);
            UIBuildUtility.Stretch(frame.rectTransform);
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
            Image typeIcon = UIBuildUtility.CreateImage("TypeIcon", root, false);
            typeIcon.preserveAspect = true;
            Image descriptionPanel = UIBuildUtility.CreateImage("DescriptionPanel", root, false);
            TextMeshProUGUI description = UIBuildUtility.CreateText("Description", root, TextAlignmentOptions.Center);

            var view = rootObject.AddComponent<CardView>();
            view.Initialize(frame, button, group, layoutElement, costBadge, cost, cardName, type, description,
                typeIcon, descriptionPanel, glow);
            return view;
        }
    }
}
