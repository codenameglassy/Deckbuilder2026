// Deterministic random source for combat: xorshift64* seeded through SplitMix64, so one seed gives the same shuffles,
// health rolls and outcomes on every machine and in both Mono and IL2CPP builds. Its whole state is one ulong that a
// save can store and restore. Gameplay rolls never use UnityEngine.Random or System.Random.
using System;
using System.Collections.Generic;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CombatRandom
    {
        private const ulong FallbackState = 0x9E3779B97F4A7C15UL;

        private ulong _state;

        public CombatRandom(int seed)
        {
            _state = MixSeed(unchecked((ulong)(uint)seed));
        }

        public ulong State => _state;

        public static CombatRandom FromState(ulong state)
        {
            var random = new CombatRandom(0);
            random._state = state != 0UL ? state : FallbackState;
            return random;
        }

        public uint NextUInt()
        {
            unchecked
            {
                ulong x = _state;
                x ^= x >> 12;
                x ^= x << 25;
                x ^= x >> 27;
                _state = x;
                return (uint)((x * 0x2545F4914F6CDD1DUL) >> 32);
            }
        }

        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentException(
                    $"maxInclusive ({maxInclusive}) is below minInclusive ({minInclusive}).", nameof(maxInclusive));
            }

            long width = (long)maxInclusive - minInclusive + 1L;
            if (width > uint.MaxValue)
            {
                return unchecked((int)NextUInt());
            }

            uint span = (uint)width;
            uint threshold = unchecked(0u - span) % span;
            uint roll;
            do
            {
                roll = NextUInt();
            }
            while (roll < threshold);

            return (int)(minInclusive + (long)(roll % span));
        }

        public void Shuffle<T>(IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i);
                T swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }

        private static ulong MixSeed(ulong value)
        {
            unchecked
            {
                ulong z = value + 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return z != 0UL ? z : FallbackState;
            }
        }
    }
}
