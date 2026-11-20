// One step of the attack intent's icon ladder: from this much total damage up, the intent shows this icon at this
// scale (a dagger for small hits, a sword, then a gada for heavy blows). Serialized inside CombatFeedbackStyleSO's
// list, which is read in order; plain data.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [Serializable]
    public struct IntentAttackTier
    {
        [SerializeField, Min(0)] private int _minDamage;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0.05f)] private float _scale;

        public int MinDamage => _minDamage;

        public Sprite Icon => _icon;

        public float Scale => _scale > 0f ? _scale : 1f;
    }
}
