// Deals a base amount of damage one or more times to the targets its rule selects.
// Each hit goes through ICombatActions, so modifiers and Kavach apply per hit, as in the reference build.
// Hits on a target stop as soon as it falls. As an intent it shows the previewed damage per hit and the hit count.
// On a card it adds two values: damage per hit (live, previewed against the chosen enemy when there is one), then
// the hit count. An optional bonus strategy raises the base of every hit while its condition holds (Piercing Vow's
// extra damage at 3 Dharma); it adds its own values after those two, and the live number shows the base without it.
// Intents ignore the bonus, since no enemy move uses one.
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
        [Tooltip("Optional extra base damage per hit while a condition holds. Leave empty for none.")]
        [SerializeField] private DamageBonusSO _bonus;

        public int Amount => _amount;

        public int Hits => _hits;

        public TargetRuleSO Targets => _targets;

        public DamageBonusSO Bonus => _bonus;

        public override bool RequiresChosenTarget => _targets != null && _targets.RequiresChosenTarget;

        public override void AddToIntent(in IntentContext context, ref MoveIntent intent)
        {
            int perHit = context.Preview != null
                ? context.Preview.PreviewDamage(context.Source, context.Target, _amount)
                : _amount;
            intent.AddAttack(perHit, _hits);
        }

        public override void AddCardValues(in CardTextContext context, List<CardValue> values)
        {
            int perHit = context.Preview != null
                ? context.Preview.PreviewDamage(context.Source, context.Target, _amount)
                : _amount;
            values.Add(new CardValue(_amount, perHit));
            values.Add(CardValue.Fixed(_hits));
            if (_bonus != null)
            {
                _bonus.AddCardValues(values);
            }
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
                int amount = _bonus != null ? _amount + _bonus.GetBonus(context) : _amount;

                for (int t = 0; t < targets.Count; t++)
                {
                    ICombatant target = targets[t];
                    for (int hit = 0; hit < _hits; hit++)
                    {
                        if (!target.IsAlive)
                        {
                            break;
                        }

                        context.Actions.DealDamage(context.Source, target, amount);
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
