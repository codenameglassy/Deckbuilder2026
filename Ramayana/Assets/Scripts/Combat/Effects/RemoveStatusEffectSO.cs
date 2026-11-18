// Clears every stack of one status from the actor (the Arrow of Garuda clears Rama's Moha). It spends the status
// through the actions, so the change is announced and previewed like any other. No card value of its own.
using JourneyToLanka.Combat.Statuses;
using UnityEngine;

namespace JourneyToLanka.Combat.Effects
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Effects/Remove Status", fileName = "RemoveStatusEffect")]
    public sealed class RemoveStatusEffectSO : CombatEffectSO
    {
        [SerializeField] private StatusDefinitionSO _status;

        public StatusDefinitionSO Status => _status;

        public override void Apply(in EffectContext context)
        {
            if (_status == null)
            {
                Debug.LogError($"{name}: no status assigned, so it clears nothing.", this);
                return;
            }

            ICombatant source = context.Source;
            if (source == null || !source.IsAlive)
            {
                return;
            }

            int amount = context.Actions.GetStatusAmount(source, _status);
            if (amount > 0)
            {
                context.Actions.ApplyStatus(source, _status, -amount, 0);
            }
        }
    }
}
