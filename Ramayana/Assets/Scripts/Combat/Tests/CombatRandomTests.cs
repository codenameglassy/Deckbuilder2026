// EditMode tests for CombatRandom: determinism, a pinned sequence that guards saved runs and replays against
// algorithm changes, inclusive bounds, state save and restore, and the Fisher-Yates shuffle.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;
using NUnit.Framework;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var first = new CombatRandom(2026);
            var second = new CombatRandom(2026);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(first.NextUInt(), second.NextUInt(), $"Roll {i} differs.");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var first = new CombatRandom(1);
            var second = new CombatRandom(2);
            int same = 0;

            for (int i = 0; i < 20; i++)
            {
                if (first.NextUInt() == second.NextUInt())
                {
                    same++;
                }
            }

            Assert.Less(same, 20);
        }

        [Test]
        public void KnownSeeds_MatchPinnedSequences()
        {
            CollectionAssert.AreEqual(new[] { 64, 76, 52, 86, 76, 75, 95, 10 }, Roll(new CombatRandom(12345), 8));
            CollectionAssert.AreEqual(new[] { 29, 11, 81, 16, 18, 48, 7, 8 }, Roll(new CombatRandom(0), 8));
        }

        [Test]
        public void Range_StaysInsideBounds_AndHitsBothEnds()
        {
            var random = new CombatRandom(7);
            bool sawMin = false;
            bool sawMax = false;

            for (int i = 0; i < 2000; i++)
            {
                int value = random.Range(30, 34);
                Assert.That(value, Is.InRange(30, 34));
                sawMin |= value == 30;
                sawMax |= value == 34;
            }

            Assert.IsTrue(sawMin, "Never rolled the minimum.");
            Assert.IsTrue(sawMax, "Never rolled the maximum.");
        }

        [Test]
        public void Range_EdgeCases()
        {
            var random = new CombatRandom(3);

            Assert.AreEqual(5, random.Range(5, 5));
            Assert.DoesNotThrow(() => random.Range(int.MinValue, int.MaxValue));
            Assert.Throws<ArgumentException>(() => random.Range(10, 9));
        }

        [Test]
        public void FromState_ResumesTheSameSequence()
        {
            var original = new CombatRandom(99);
            Roll(original, 10);
            ulong saved = original.State;
            int[] expected = Roll(original, 5);

            CollectionAssert.AreEqual(expected, Roll(CombatRandom.FromState(saved), 5));
        }

        [Test]
        public void Shuffle_IsDeterministicAndKeepsEveryElement()
        {
            List<int> first = Sequence(20);
            List<int> second = Sequence(20);

            new CombatRandom(42).Shuffle(first);
            new CombatRandom(42).Shuffle(second);

            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AreEquivalent(Sequence(20), first);
            CollectionAssert.AreNotEqual(Sequence(20), first);
        }

        private static int[] Roll(CombatRandom random, int count)
        {
            var values = new int[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = random.Range(0, 99);
            }

            return values;
        }

        private static List<int> Sequence(int count)
        {
            var list = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(i);
            }

            return list;
        }
    }
}
