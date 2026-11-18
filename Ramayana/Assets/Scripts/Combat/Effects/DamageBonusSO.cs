// Strategy for extra base damage a damage effect adds to every hit while a condition holds, such as Piercing Vow's
// bonus at 3 Dharma. It reads combat state only through the effect context's actions, so the preview answers from its
// own simulation. AddCardValues appends the numbers the card text shows for it. Shared, immutable assets.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    public abstract class DamageBonusSO : ScriptableObject
    {
        public abstract int GetBonus(in EffectContext context);

        public virtual void AddCardValues(List<CardValue> values)
        {
        }
    }
}
