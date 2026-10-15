// EditMode tests for TickList: ordering, duplicates, and safe add, remove and exception handling during a run.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JourneyToLanka.Core.Ticking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JourneyToLanka.Core.Tests
{
    public sealed class TickListTests
    {
        private sealed class Probe
        {
            public Action OnTick;
            public int Calls;
        }

        private static readonly Action<Probe, float> s_invoke = Invoke;

        private TickList<Probe> _list;

        [SetUp]
        public void SetUp()
        {
            _list = new TickList<Probe>();
        }

        [Test]
        public void Run_InvokesInRegistrationOrder()
        {
            var order = new List<int>();
            _list.Add(new Probe { OnTick = () => order.Add(1) });
            _list.Add(new Probe { OnTick = () => order.Add(2) });
            _list.Add(new Probe { OnTick = () => order.Add(3) });

            _list.Run(s_invoke, 0f);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
        }

        [Test]
        public void Add_Duplicate_ReturnsFalseAndRunsOnce()
        {
            var probe = new Probe();

            Assert.IsTrue(_list.Add(probe));
            Assert.IsFalse(_list.Add(probe));
            _list.Run(s_invoke, 0f);

            Assert.AreEqual(1, probe.Calls);
        }

        [Test]
        public void Remove_DuringRun_SkipsRemovedItem()
        {
            var removed = new Probe();
            var remover = new Probe { OnTick = () => _list.Remove(removed) };
            _list.Add(remover);
            _list.Add(removed);

            _list.Run(s_invoke, 0f);
            _list.Run(s_invoke, 0f);

            Assert.AreEqual(2, remover.Calls);
            Assert.AreEqual(0, removed.Calls);
        }

        [Test]
        public void Add_DuringRun_StartsNextRun()
        {
            var added = new Probe();
            var adder = new Probe { OnTick = () => _list.Add(added) };
            _list.Add(adder);

            _list.Run(s_invoke, 0f);
            Assert.AreEqual(0, added.Calls);

            _list.Run(s_invoke, 0f);
            Assert.AreEqual(1, added.Calls);
        }

        [Test]
        public void Run_WhenItemThrows_ContinuesWithOthers()
        {
            var thrower = new Probe { OnTick = () => throw new InvalidOperationException("boom") };
            var survivor = new Probe();
            _list.Add(thrower);
            _list.Add(survivor);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            _list.Run(s_invoke, 0f);

            Assert.AreEqual(1, survivor.Calls);
        }

        private static void Invoke(Probe probe, float deltaTime)
        {
            probe.Calls++;
            probe.OnTick?.Invoke();
        }
    }
}
