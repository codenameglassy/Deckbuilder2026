// One row of an enemy intent: its root, the plate behind it, the icon, the number, and the scale the icon is drawn at
// (attack tiers draw bigger blades for bigger hits). Built and laid out by EnemyIntentView; plain holder.
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    internal sealed class IntentRow
    {
        public IntentRow(Transform root, SpriteRenderer plate, SpriteRenderer icon, TextMeshPro label)
        {
            Root = root;
            Plate = plate;
            Icon = icon;
            Label = label;
        }

        public Transform Root { get; }

        public SpriteRenderer Plate { get; }

        public SpriteRenderer Icon { get; }

        public TextMeshPro Label { get; }

        public float IconScale { get; set; } = 1f;
    }
}
