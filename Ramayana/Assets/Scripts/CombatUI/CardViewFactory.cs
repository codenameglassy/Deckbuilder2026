// Builds the card view template in code, back to front: the playable glow, the frame (the Button's target and the
// card's raycast surface, which also draws the name banner and the art socket), the art window, the type ribbon with
// its icon and label, the description over its readability panel, the name, and the cost orb last so it sits on top
// where it overhangs the corner. The frame is a stretched child rather than the root's own image, because a child
// always draws over its parent's image and the glow has to sit underneath the frame. The art window, ribbon and cost
// orb carry a CardArtMeshEffect for the Card Art shader's rounded shape. The template sits under an inactive holder so
// it never wakes up; the hand pool clones it. All sizes, colours and positions come from the style when the view
// applies it.
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

            Image art = CreateRoundImage("Art", root);
            Image ribbon = CreateRoundImage("TypeRibbon", root);
            Image typeIcon = UIBuildUtility.CreateImage("TypeIcon", ribbon.transform, false);
            typeIcon.preserveAspect = true;
            TextMeshProUGUI type = UIBuildUtility.CreateText("Type", ribbon.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(type.rectTransform);

            Image descriptionPanel = UIBuildUtility.CreateImage("DescriptionPanel", root, false);
            TextMeshProUGUI description = UIBuildUtility.CreateText("Description", root, TextAlignmentOptions.Center);
            TextMeshProUGUI cardName = UIBuildUtility.CreateText("Name", root, TextAlignmentOptions.Center);

            Image costBadge = CreateRoundImage("CostBadge", root);
            TextMeshProUGUI cost = UIBuildUtility.CreateText("Cost", costBadge.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(cost.rectTransform);

            var view = rootObject.AddComponent<CardView>();
            view.Initialize(frame, button, group, layoutElement, costBadge, cost, cardName, type, description,
                typeIcon, descriptionPanel, glow, art, ribbon);
            return view;
        }

        private static Image CreateRoundImage(string name, Transform parent)
        {
            Image image = UIBuildUtility.CreateImage(name, parent, false);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.gameObject.AddComponent<CardArtMeshEffect>();
            return image;
        }
    }
}
