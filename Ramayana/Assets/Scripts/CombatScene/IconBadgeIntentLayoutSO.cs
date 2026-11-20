// Intent layout A: no plate, a large icon, and the number tucked into the icon's empty lower right corner (the blades
// run from lower left to upper right), drawn in the style's outlined intent number material so it reads against any
// background. The number is placed by its left edge and vertical centre, so a wide "4x2" grows to the right, away
// from the icon, instead of spreading over it. A row without an icon centres its number. Sizes are in intent units;
// the number's left edge and centre height are fractions of the icon size measured from the icon's centre.
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Intent Layout/Icon Badge",
        fileName = "IntentLayout_IconBadge")]
    public sealed class IconBadgeIntentLayoutSO : IntentLayoutSO
    {
        [SerializeField, Min(0.05f)] private float _iconSize = 1.1f;
        [Tooltip("Number's left edge (x) and vertical centre (y), as fractions of the icon size from its centre.")]
        [SerializeField] private Vector2 _numberLeftEdge = new Vector2(0.12f, -0.3f);
        [SerializeField, Min(0f)] private float _rowSpacing = 0.95f;

        public override float RowSpacing => _rowSpacing;

        public override float RowHalfHeight(CombatFeedbackStyleSO style)
        {
            return _iconSize * 0.5f;
        }

        internal override void Layout(IntentRow row, CombatFeedbackStyleSO style)
        {
            row.Plate.enabled = false;
            row.Label.ForceMeshUpdate();
            Bounds text = row.Label.textBounds;

            if (!row.Icon.enabled)
            {
                row.Label.transform.localPosition = new Vector3(-text.center.x, -text.center.y, 0f);
                return;
            }

            float size = _iconSize * row.IconScale;
            row.Icon.transform.localScale = Vector3.one * IconScaleFor(row.Icon.sprite, size);
            row.Icon.transform.localPosition = Vector3.zero;
            float x = _numberLeftEdge.x * size - text.min.x;
            float y = _numberLeftEdge.y * size - text.center.y;
            row.Label.transform.localPosition = new Vector3(x, y, 0f);
        }
    }
}
