// Base for every effect strategy shared by cards and enemy moves (damage, Kavach, and later statuses, draw, heal).
// Effects are immutable flyweights: all values are authored on the asset and nothing is written at runtime.
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    public abstract class CombatEffectSO : ScriptableObject
    {
        public virtual bool RequiresChosenTarget => false;

        public abstract void Apply(in EffectContext context);

        public static void ApplyAll(CombatEffectSO[] effects, in EffectContext context)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                CombatEffectSO effect = effects[i];
                if (effect != null)
                {
                    effect.Apply(context);
                }
            }
        }

        public static bool AnyRequiresChosenTarget(CombatEffectSO[] effects)
        {
            if (effects == null)
            {
                return false;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                CombatEffectSO effect = effects[i];
                if (effect != null && effect.RequiresChosenTarget)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
