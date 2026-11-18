// Adds a fixed bonus to each hit while the actor holds at least a threshold of a status (Piercing Vow: 6 more at 3
// Dharma). On a card it adds two values: the threshold, then the bonus.
using System.Collections.Generic;
using JourneyToLanka.Combat.Statuses;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Bonus/Status Threshold", fileName = "StatusThresholdDamageBonus")]
    public sealed class StatusThresholdDamageBonusSO : DamageBonusSO
    {
        [SerializeField] private StatusDefinitionSO _status;
        [SerializeField, Min(1)] private int _threshold = 3;
        [SerializeField, Min(0)] private int _bonus = 6;

        public StatusDefinitionSO Status => _status;

        public int Threshold => _threshold;

        public int Bonus => _bonus;

        public override int GetBonus(in EffectContext context)
        {
            if (_status == null || context.Actions == null || context.Source == null)
            {
                return 0;
            }

            return context.Actions.GetStatusAmount(context.Source, _status) >= _threshold ? _bonus : 0;
        }

        public override void AddCardValues(List<CardValue> values)
        {
            values.Add(CardValue.Fixed(_threshold));
            values.Add(CardValue.Fixed(_bonus));
        }
    }
}
