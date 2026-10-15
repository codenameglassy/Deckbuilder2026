// One enemy in a fight: its definition, field slot, Combatant (health rolled inclusively from the definition's range)
// and its place in the move pattern. On its turn the enemy drops its own Kavach, performs the current move against
// the player, then advances the pattern. IntentChanged fires when the next move changes so intent icons can refresh.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;

namespace JourneyToLanka.Combat.Model
{
    public sealed class EnemyCombatant
    {
        public EnemyCombatant(EnemyDefinitionSO definition, int slotIndex, CombatRandom random)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            Definition = definition;
            SlotIndex = slotIndex;

            int health = random.Range(definition.MinHealth, Math.Max(definition.MinHealth, definition.MaxHealth));
            Fighter = new Combatant(definition.NameKey, health, health);

            int moveCount = definition.MoveCount;
            PatternIndex = moveCount > 0 ? definition.StartMoveIndex % moveCount : 0;
        }

        public event Action<EnemyCombatant> IntentChanged;

        public EnemyDefinitionSO Definition { get; }

        public int SlotIndex { get; }

        public Combatant Fighter { get; }

        public int PatternIndex { get; private set; }

        public bool IsAlive => Fighter.IsAlive;

        public EnemyMoveSO CurrentMove => Definition.GetMove(PatternIndex);

        public bool TakeTurn(ICombatActions actions, ICombatant player, IReadOnlyList<ICombatant> opponents)
        {
            if (!IsAlive)
            {
                return false;
            }

            Fighter.ClearBlock();

            EnemyMoveSO move = CurrentMove;
            if (move != null)
            {
                move.ApplyEffects(new EffectContext(actions, Fighter, player, opponents));
            }

            AdvanceMove();
            return true;
        }

        private void AdvanceMove()
        {
            int moveCount = Definition.MoveCount;
            if (moveCount == 0)
            {
                return;
            }

            PatternIndex = (PatternIndex + 1) % moveCount;
            IntentChanged?.Invoke(this);
        }
    }
}
