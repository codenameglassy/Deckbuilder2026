// Rama's special arrows: a fixed number of slots (three in the reference build) holding arrows in the order they were
// gained. The run owns one quiver for its whole length, so arrows carry between fights; a fight fires from the same
// quiver, so a fired arrow is gone from the run too. TryAdd leaves a new arrow behind when every slot is full, and
// RemoveAt closes the gap. Changed fires on every change so the HUD refreshes only then. Allocates nothing after
// construction.
using System;
using System.Collections.Generic;

namespace JourneyToLanka.Combat.Arrows
{
    public sealed class Quiver
    {
        private readonly List<ArrowDefinitionSO> _arrows;

        public Quiver(int capacity)
            : this(capacity, null)
        {
        }

        public Quiver(int capacity, IReadOnlyList<ArrowDefinitionSO> startingArrows)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A quiver cannot have negative capacity.");
            }

            Capacity = capacity;
            _arrows = new List<ArrowDefinitionSO>(capacity);
            if (startingArrows == null)
            {
                return;
            }

            for (int i = 0; i < startingArrows.Count && _arrows.Count < capacity; i++)
            {
                if (startingArrows[i] != null)
                {
                    _arrows.Add(startingArrows[i]);
                }
            }
        }

        public event Action<Quiver> Changed;

        public int Capacity { get; }

        public int Count => _arrows.Count;

        public bool IsFull => _arrows.Count >= Capacity;

        public ArrowDefinitionSO this[int slot] => _arrows[slot];

        public bool HasArrow(int slot)
        {
            return slot >= 0 && slot < _arrows.Count;
        }

        public bool TryAdd(ArrowDefinitionSO arrow)
        {
            if (arrow == null || IsFull)
            {
                return false;
            }

            _arrows.Add(arrow);
            Changed?.Invoke(this);
            return true;
        }

        public ArrowDefinitionSO RemoveAt(int slot)
        {
            if (!HasArrow(slot))
            {
                return null;
            }

            ArrowDefinitionSO arrow = _arrows[slot];
            _arrows.RemoveAt(slot);
            Changed?.Invoke(this);
            return arrow;
        }
    }
}
