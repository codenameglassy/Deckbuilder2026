// Strips all Kavach from the targets its rule selects (Vayavyastra, Gale Arrow). Placed before a damage effect in a
// card's list, the hit that follows lands on health, as in the reference build. No card value of its own.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Remove Block", fileName = "RemoveBlockEffect")]
    public sealed class RemoveBlockEffectSO : CombatEffectSO
    {
        [SerializeField] private TargetRuleSO _targets;

        public TargetRuleSO Targets => _targets;

        public override bool RequiresChosenTarget => _targets != null && _targets.RequiresChosenTarget;

        public override void Apply(in EffectContext context)
        {
            if (_targets == null)
            {
                Debug.LogError($"{name}: no target rule assigned, so it removes no Kavach.", this);
                return;
            }

            List<ICombatant> targets = ListPool<ICombatant>.Get();
            try
            {
                _targets.Collect(context, targets);
                for (int i = 0; i < targets.Count; i++)
                {
                    ICombatant target = targets[i];
                    if (target.IsAlive)
                    {
                        context.Actions.RemoveBlock(target);
                    }
                }
            }
            finally
            {
                ListPool<ICombatant>.Release(targets);
            }
        }
    }
}
