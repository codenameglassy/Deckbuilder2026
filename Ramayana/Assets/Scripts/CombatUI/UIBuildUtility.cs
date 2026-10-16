// Small helpers for building UGUI hierarchies in code: RectTransform children, images and TextMeshPro texts with
// raycasts off, and placement by anchors and offsets. Builders create objects; views apply style values to them.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    internal static class UIBuildUtility
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, bool raycastTarget)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = alignment;
            return text;
        }

        public static void Stretch(RectTransform rect)
        {
            SetOffsets(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public static void SetOffsets(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void StyleText(TMP_Text text, TMP_FontAsset font, float size, Color color)
        {
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.color = color;
        }

        public static void StyleImage(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
        }
    }
}
