// Intent layout with a plate: the icon and number centred together as one group, then the style's nine-sliced intent
// plate fitted around them with the style's padding. Uses the style's intent icon size and the gap between icon and
// number. Sizes are in intent units.
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Intent Layout/Plate", fileName = "IntentLayout_Plate")]
    public sealed class PlateIntentLayoutSO : IntentLayoutSO
    {
        [SerializeField, Min(0f)] private float _rowSpacing = 0.6f;

        public override float RowSpacing => _rowSpacing;

        public override float RowHalfHeight(CombatFeedbackStyleSO style)
        {
            return style.IntentIconSize * 0.5f + style.IntentPlatePadding.y;
        }

        internal override void Layout(IntentRow row, CombatFeedbackStyleSO style)
        {
            float iconSize = row.Icon.enabled ? style.IntentIconSize * row.IconScale : 0f;
            float gap = row.Icon.enabled ? style.IntentIconGap : 0f;
            row.Label.ForceMeshUpdate();
            Bounds text = row.Label.textBounds;
            float textWidth = Mathf.Max(text.size.x, 0f);
            float width = iconSize + gap + textWidth;
            float left = -width * 0.5f;

            row.Icon.transform.localScale = Vector3.one * IconScaleFor(row.Icon.sprite, iconSize);
            row.Icon.transform.localPosition = new Vector3(left + iconSize * 0.5f, 0f, 0f);
            float labelX = left + iconSize + gap + textWidth * 0.5f - text.center.x;
            row.Label.transform.localPosition = new Vector3(labelX, -text.center.y, 0f);

            Sprite plate = style.IntentPlateSprite;
            row.Plate.enabled = plate != null;
            Vector2 padding = style.IntentPlatePadding;
            float height = Mathf.Max(iconSize, text.size.y) + padding.y * 2f;
            if (plate != null)
            {
                row.Plate.size = new Vector2(width + padding.x * 2f, height);
            }
        }
    }
}
