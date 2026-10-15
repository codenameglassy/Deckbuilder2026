// Targets every living opponent, in field order. Used by cards such as Vanara Volley that strike all enemies.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Targeting/All Opponents", fileName = "AllOpponents")]
    public sealed class AllOpponentsTargetRuleSO : TargetRuleSO
    {
        public override bool RequiresChosenTarget => false;

        public override void Collect(in EffectContext context, List<ICombatant> results)
        {
            IReadOnlyList<ICombatant> opponents = context.Opponents;
            if (opponents == null)
            {
                return;
            }

            for (int i = 0; i < opponents.Count; i++)
            {
                ICombatant opponent = opponents[i];
                if (opponent != null && opponent.IsAlive)
                {
                    results.Add(opponent);
                }
            }
        }
    }
}
