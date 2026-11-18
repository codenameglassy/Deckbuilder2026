// Applies a status to the targets its rule selects, or to the actor itself when no rule is set (Focus on Rama, an
// enemy's own Focus buff). Duration 0 uses the status's default. Effects run in order, so a card that applies Bhaya
// before its hit makes that hit land harder, as in the reference build. As an intent it reports the status as a buff
// or debuff; on a card it adds one value, the amount applied.
using System.Collections.Generic;
using JourneyToLanka.Combat.Statuses;
using UnityEngine;
using UnityEngine.Pool;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Apply Status", fileName = "ApplyStatusEffect")]
    public sealed class ApplyStatusEffectSO : CombatEffectSO
    {
        [SerializeField] private StatusDefinitionSO _status;
        [SerializeField, Min(1)] private int _amount = 1;
        [SerializeField, Min(0)] private int _duration;
        [Tooltip("Who receives the status. Leave empty to apply it to whoever plays the card or makes the move.")]
        [SerializeField] private TargetRuleSO _targets;

        public StatusDefinitionSO Status => _status;

        public int Amount => _amount;

        public int Duration => _duration;

        public TargetRuleSO Targets => _targets;

        public override bool RequiresChosenTarget => _targets != null && _targets.RequiresChosenTarget;

        public override void AddToIntent(in IntentContext context, ref MoveIntent intent)
        {
            intent.AddStatus(_status, _amount);
        }

        public override void AddCardValues(in CardTextContext context, List<CardValue> values)
        {
            values.Add(CardValue.Fixed(_amount));
        }

        public override void Apply(in EffectContext context)
        {
            if (_status == null)
            {
                Debug.LogError($"{name}: no status assigned, so it applies nothing.", this);
                return;
            }

            if (_targets == null)
            {
                ICombatant source = context.Source;
                if (source != null && source.IsAlive)
                {
                    context.Actions.ApplyStatus(source, _status, _amount, _duration);
                }

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
                        context.Actions.ApplyStatus(target, _status, _amount, _duration);
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
