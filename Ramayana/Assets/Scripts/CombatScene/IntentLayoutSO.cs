// How an enemy intent row arranges its icon, number and plate: a swappable strategy asset referenced by the combat
// feedback style, so a new look is a new asset and never a change to EnemyIntentView. Row Spacing is the distance
// between stacked rows and RowHalfHeight how far a row reaches below its centre, both in intent units, so the view
// can stack rows and sit the lowest one just above the health bar. Layout runs on refresh only, never per frame.
// Shared and read-only at runtime. In the Editor, changing a value raises Changed so intents re-lay themselves out.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    public abstract class IntentLayoutSO : ScriptableObject
    {
        public event Action Changed;

        public abstract float RowSpacing { get; }

        public abstract float RowHalfHeight(CombatFeedbackStyleSO style);

        internal abstract void Layout(IntentRow row, CombatFeedbackStyleSO style);

        // Scale that draws a sprite of any resolution or pixels-per-unit at the given size in intent units.
        protected static float IconScaleFor(Sprite sprite, float size)
        {
            float spriteSize = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            return size / Mathf.Max(spriteSize, 0.0001f);
        }

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
