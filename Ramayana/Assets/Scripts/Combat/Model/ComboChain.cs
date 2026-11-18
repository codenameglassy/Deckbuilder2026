// The live combo chain of one fight in plain C#: which chain kind is running and how many of its cards were played
// in a row. A card builds the chain when its definition names a chain kind and it costs at least 1 Prana; every
// other card, zero-cost cards included, breaks it. A card of a different kind breaks the running chain and starts
// its own at 1. When a card reaches the ladder's last step (the finisher) the chain starts again at 1 after it.
// Special arrows and ally effects never reach it, so they neither build nor break it. The combat model calls
// Register as each card is played, Reset at the end of the player's turn, and the Peek methods for previews.
// Events, all raised after the state has changed: Advanced (kind, step, percent) for every card that builds a
// chain, the finisher included; Broken (kind, length) when a running chain of any length is broken by another card
// (views decide which lengths are worth announcing); Changed after any change, Reset included, so views redraw
// from Kind and Count. Reset is not a break and raises no Broken.
using System;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Model
{
    public sealed class ComboChain
    {
        // Kind, step reached (1-based), multiplier percent applied to that card.
        public event Action<ChainKindSO, int, int> Advanced;

        // Kind, number of its cards that were in a row when another card broke it.
        public event Action<ChainKindSO, int> Broken;

        public event Action Changed;

        // The running chain, or null when none is running.
        public ChainKindSO Kind { get; private set; }

        // Cards of Kind played in a row so far; 0 when no chain is running.
        public int Count { get; private set; }

        // The step the card would reach if played now: 1 or more when it builds a chain, 0 when it breaks it.
        public int PeekStep(CardDefinitionSO card)
        {
            ChainKindSO kind = KindOf(card);
            if (kind == null)
            {
                return 0;
            }

            return kind == Kind ? Count + 1 : 1;
        }

        // The multiplier percent the card would get if played now (100 for a card that breaks the chain).
        public int Peek(CardDefinitionSO card)
        {
            int step = PeekStep(card);
            return step > 0 ? card.Chain.GetPercent(step) : ChainScale.NeutralPercent;
        }

        public ChainScale PeekScale(CardDefinitionSO card)
        {
            int step = PeekStep(card);
            return step > 0 ? card.Chain.GetScale(step) : ChainScale.None;
        }

        // Records the card as played and returns the scale it resolves with.
        public ChainScale Register(CardDefinitionSO card)
        {
            ChainKindSO kind = KindOf(card);
            if (kind == null)
            {
                if (BreakRunningChain())
                {
                    Changed?.Invoke();
                }

                return ChainScale.None;
            }

            if (kind != Kind)
            {
                BreakRunningChain();
            }

            int step = Count + 1;
            int percent = kind.GetPercent(step);
            ChainScale scale = kind.GetScale(step);

            if (kind.IsFinisher(step))
            {
                Kind = null;
                Count = 0;
            }
            else
            {
                Kind = kind;
                Count = step;
            }

            Advanced?.Invoke(kind, step, percent);
            Changed?.Invoke();
            return scale;
        }

        public void Reset()
        {
            if (Count == 0 && Kind == null)
            {
                return;
            }

            Kind = null;
            Count = 0;
            Changed?.Invoke();
        }

        private bool BreakRunningChain()
        {
            if (Count == 0)
            {
                return false;
            }

            ChainKindSO kind = Kind;
            int length = Count;
            Kind = null;
            Count = 0;
            Broken?.Invoke(kind, length);
            return true;
        }

        private static ChainKindSO KindOf(CardDefinitionSO card)
        {
            return card != null && card.Cost > 0 ? card.Chain : null;
        }
    }
}
