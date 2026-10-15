// Rama's Prana for the current turn. It refills to the per-turn amount at the start of each player turn
// (unspent Prana does not carry over) and cards spend it. Changed fires whenever the current value moves.
using System;

namespace JourneyToLanka.Combat.Model
{
    public sealed class PranaPool
    {
        public PranaPool(int perTurn)
        {
            if (perTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(perTurn), perTurn, "Prana per turn cannot be negative.");
            }

            PerTurn = perTurn;
        }

        public event Action<PranaPool> Changed;

        public int PerTurn { get; }

        public int Current { get; private set; }

        public void Refill()
        {
            Set(PerTurn);
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && amount <= Current;
        }

        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount))
            {
                return false;
            }

            Set(Current - amount);
            return true;
        }

        public void Gain(int amount)
        {
            if (amount > 0)
            {
                Set(Current + amount);
            }
        }

        private void Set(int value)
        {
            if (value == Current)
            {
                return;
            }

            Current = value;
            Changed?.Invoke(this);
        }
    }
}
