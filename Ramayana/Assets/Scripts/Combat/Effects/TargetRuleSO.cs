// Strategy for which combatants an effect lands on: the chosen opponent, every opponent, and later others.
// Collect appends living targets to a caller-owned list so no allocation happens here.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    public abstract class TargetRuleSO : ScriptableObject
    {
        public abstract bool RequiresChosenTarget { get; }

        public abstract void Collect(in EffectContext context, List<ICombatant> results);
    }
}
