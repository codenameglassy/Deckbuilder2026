// Deals a base amount of damage one or more times to the targets its rule selects.
// Each hit goes through ICombatActions, so modifiers and Kavach apply per hit, as in the reference build.
// Hits on a target stop as soon as it falls. As an intent it shows the previewed damage per hit and the hit count.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Damage", fileName = "DamageEffect")]
    public sealed class DamageEffectSO : CombatEffectSO
    {
        [SerializeField, Min(0)] private int _amount = 6;
        [SerializeField, Min(1)] private int _hits = 1;
        [SerializeField] private TargetRuleSO _targets;

        public int Amount => _amount;

        public int Hits => _hits;

        public TargetRuleSO Targets => _targets;

        public override bool RequiresChosenTarget => _targets != null && _targets.RequiresChosenTarget;

        public override void AddToIntent(in IntentContext context, ref MoveIntent intent)
        {
            int perHit = context.Preview != null
                ? context.Preview.PreviewDamage(context.Source, context.Target, _amount)
                : _amount;
            intent.AddAttack(perHit, _hits);
        }

        public override void Apply(in EffectContext context)
        {
            if (_targets == null)
            {
                Debug.LogError($"{name}: no target rule assigned, so it deals no damage.", this);
                return;
            }

            List<ICombatant> targets = ListPool<ICombatant>.Get();
            try
            {
                _targets.Collect(context, targets);

                for (int t = 0; t < targets.Count; t++)
                {
                    ICombatant target = targets[t];
                    for (int hit = 0; hit < _hits; hit++)
                    {
                        if (!target.IsAlive)
                        {
                            break;
                        }

                        context.Actions.DealDamage(context.Source, target, _amount);
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
