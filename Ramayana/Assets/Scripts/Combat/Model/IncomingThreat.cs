// What the living enemies' next moves will do to Rama, as the threat panel shows it: the total incoming attack
// damage (previewed through the resolver, so damage modifiers apply), how much of it his current Kavach absorbs,
// and the health he will lose. Kavach absorbs hits in order until it runs out, so across every hit the absorbed
// share is min(Kavach, incoming). Plain data, computed on demand; nothing is cached on the model.
// Given a card's previewed outcome, it shows the threat as it would be after that card: enemies the card would kill
// no longer attack, Rama's Kavach is the Kavach he would end with, and attacks are previewed through the outcome so
// statuses the card applies count (a Moha on an enemy weakens its attack, a Focus Rama gains does not touch it).
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Model
{
    public readonly struct IncomingThreat
    {
        public IncomingThreat(int incoming, int block, int health)
        {
            Incoming = Math.Max(0, incoming);
            Blocked = Math.Min(Math.Max(0, block), Incoming);
            Taken = Incoming - Blocked;
            IsLethal = Taken > 0 && Taken >= health;
        }

        public int Incoming { get; }

        public int Blocked { get; }

        public int Taken { get; }

        public bool IsLethal { get; }

        public bool HasAttack => Incoming > 0;

        public static IncomingThreat Calculate(CombatModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return Calculate(model.Enemies, model.Resolver, model.Player, null);
        }

        public static IncomingThreat Calculate(CombatModel model, CombatOutcomePreview outcome)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return Calculate(model.Enemies, model.Resolver, model.Player, outcome);
        }

        public static IncomingThreat Calculate(IReadOnlyList<EnemyCombatant> enemies, IDamagePreview preview, Combatant player)
        {
            return Calculate(enemies, preview, player, null);
        }

        public static IncomingThreat Calculate(IReadOnlyList<EnemyCombatant> enemies, IDamagePreview preview, Combatant player,
            CombatOutcomePreview outcome)
        {
            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            IDamagePreview intentPreview = outcome != null && outcome.CanPreviewDamage ? outcome : preview;
            int incoming = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyCombatant enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (outcome != null && outcome.TryGet(enemy.Fighter, out CombatantOutcome enemyOutcome) && enemyOutcome.IsLethal)
                {
                    continue;
                }

                MoveIntent intent = enemy.GetIntent(intentPreview, player);
                if (intent.HasAttack)
                {
                    incoming += intent.TotalDamage;
                }
            }

            int block = player.Block;
            if (outcome != null && outcome.TryGet(player, out CombatantOutcome playerOutcome))
            {
                block = playerOutcome.EndBlock;
            }

            return new IncomingThreat(incoming, block, player.Health);
        }
    }
}
