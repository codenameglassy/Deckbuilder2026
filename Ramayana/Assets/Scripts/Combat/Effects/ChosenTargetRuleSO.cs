// Targets the single opponent the actor chose. For the player that is the enemy they aimed at;
// for an enemy move it is the player.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Targeting/Chosen Target", fileName = "ChosenTarget")]
    public sealed class ChosenTargetRuleSO : TargetRuleSO
    {
        public override bool RequiresChosenTarget => true;

        public override void Collect(in EffectContext context, List<ICombatant> results)
        {
            ICombatant target = context.ChosenTarget;
            if (target != null && target.IsAlive)
            {
                results.Add(target);
            }
        }
    }
}
