// Spends all of a status the actor holds, then deals base damage plus a fixed amount per stack spent to the targets
// its rule selects (Brahmastra: all Dharma, 10 plus 7 per Dharma). As in the reference build the status is spent
// before the hit, and the hit skips every damage modifier but still meets Kavach. On a card it adds two values: the
// base damage, then the damage per stack.
using System.Collections.Generic;
using JourneyToLanka.Combat.Statuses;
using UnityEngine;
using UnityEngine.Pool;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Spend Status For Damage", fileName = "SpendStatusDamageEffect")]
    public sealed class SpendStatusDamageEffectSO : CombatEffectSO
    {
        [SerializeField] private StatusDefinitionSO _status;
        [SerializeField, Min(0)] private int _baseDamage = 10;
        [SerializeField, Min(0)] private int _damagePerStack = 7;
        [SerializeField] private TargetRuleSO _targets;

        public StatusDefinitionSO Status => _status;

        public int BaseDamage => _baseDamage;

        public int DamagePerStack => _damagePerStack;

        public TargetRuleSO Targets => _targets;

        public override bool RequiresChosenTarget => _targets != null && _targets.RequiresChosenTarget;

        public override void AddCardValues(in CardTextContext context, List<CardValue> values)
        {
            values.Add(CardValue.Fixed(_baseDamage));
            values.Add(CardValue.Fixed(_damagePerStack));
        }

        public override void Apply(in EffectContext context)
        {
            if (_status == null || _targets == null)
            {
                Debug.LogError($"{name}: a status and a target rule must both be assigned, so it does nothing.", this);
                return;
            }

            int stacks = context.Actions.GetStatusAmount(context.Source, _status);
            if (stacks > 0)
            {
                context.Actions.ApplyStatus(context.Source, _status, -stacks, 0);
            }

            int damage = _baseDamage + _damagePerStack * stacks;
            List<ICombatant> targets = ListPool<ICombatant>.Get();
            try
            {
                _targets.Collect(context, targets);
                for (int i = 0; i < targets.Count; i++)
                {
                    ICombatant target = targets[i];
                    if (target.IsAlive)
                    {
                        context.Actions.DealUnmodifiedDamage(context.Source, target, damage);
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
