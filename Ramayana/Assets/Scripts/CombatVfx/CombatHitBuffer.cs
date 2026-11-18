// Collects the hits of one frame so each can be shown with its source, which the combat model announces only after
// the damage: a card's or arrow's hits land before CardPlayed or ArrowFired, and a round's burns before RoundEnded.
// Record stores a hit untagged. TagUntagged gives every hit recorded since the last drain and still untagged the
// source just announced. SetFallback holds a source for hits nobody tags (an enemy's action, announced one windup
// before its damage) until ClearFallback. Drain hands the frame's hits over in order, resolving untagged ones to the
// fallback (or HitSource.None without one), and empties the buffer. Plain C# with no Unity calls, covered by EditMode
// tests; it allocates nothing once its list has grown to the busiest frame.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;

namespace JourneyToLanka.CombatVfx
{
    public sealed class CombatHitBuffer
    {
        private readonly List<Entry> _entries;
        private HitSource _fallback;
        private bool _hasFallback;

        public CombatHitBuffer(int capacity = 16)
        {
            _entries = new List<Entry>(Math.Max(1, capacity));
        }

        public int Count => _entries.Count;

        public bool HasFallback => _hasFallback;

        public void Record(int targetSlot, in DamageResult result)
        {
            _entries.Add(new Entry(targetSlot, result, HitSource.None, false));
        }

        public void TagUntagged(in HitSource source)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (!entry.Tagged)
                {
                    _entries[i] = new Entry(entry.TargetSlot, entry.Result, source, true);
                }
            }
        }

        public void SetFallback(in HitSource source)
        {
            _fallback = source;
            _hasFallback = true;
        }

        public void ClearFallback()
        {
            _fallback = HitSource.None;
            _hasFallback = false;
        }

        public void Drain(List<HitRecord> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                HitSource source = entry.Tagged ? entry.Source : _hasFallback ? _fallback : HitSource.None;
                output.Add(new HitRecord(entry.TargetSlot, entry.Result, source));
            }

            _entries.Clear();
        }

        public void Clear()
        {
            _entries.Clear();
            ClearFallback();
        }

        private readonly struct Entry
        {
            public Entry(int targetSlot, in DamageResult result, in HitSource source, bool tagged)
            {
                TargetSlot = targetSlot;
                Result = result;
                Source = source;
                Tagged = tagged;
            }

            public int TargetSlot { get; }

            public DamageResult Result { get; }

            public HitSource Source { get; }

            public bool Tagged { get; }
        }
    }
}
