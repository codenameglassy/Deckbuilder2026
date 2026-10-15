// EditMode tests for StateMachine: transition order, deferral from Enter and Tick, Stop, and the runaway guard.
using System;
using System.Collections.Generic;
using JourneyToLanka.Core.StateMachines;
using NUnit.Framework;

namespace JourneyToLanka.Core.Tests
{
    public sealed class StateMachineTests
    {
        private sealed class ProbeContext
        {
            public List<string> Log { get; } = new List<string>();

            public StateMachine<ProbeContext> Machine { get; set; }
        }

        private sealed class ProbeState : IState<ProbeContext>
        {
            private readonly string _name;

            public ProbeState(string name)
            {
                _name = name;
            }

            public IState<ProbeContext> NextOnEnter { get; set; }

            public void Enter(ProbeContext context)
            {
                context.Log.Add("enter:" + _name);
                if (NextOnEnter != null)
                {
                    context.Machine.ChangeState(NextOnEnter);
                    context.Log.Add("enter-done:" + _name);
                }
            }

            public void Exit(ProbeContext context)
            {
                context.Log.Add("exit:" + _name);
            }
        }

        private sealed class TickProbeState : ITickableState<ProbeContext>
        {
            private readonly string _name;

            public TickProbeState(string name)
            {
                _name = name;
            }

            public IState<ProbeContext> NextOnTick { get; set; }

            public int Ticks { get; private set; }

            public float LastDelta { get; private set; }

            public void Enter(ProbeContext context)
            {
                context.Log.Add("enter:" + _name);
            }

            public void Exit(ProbeContext context)
            {
                context.Log.Add("exit:" + _name);
            }

            public void Tick(ProbeContext context, float deltaTime)
            {
                Ticks++;
                LastDelta = deltaTime;
                context.Log.Add("tick:" + _name);
                if (NextOnTick != null)
                {
                    context.Machine.ChangeState(NextOnTick);
                    context.Log.Add("tick-done:" + _name);
                }
            }
        }

        private ProbeContext _context;
        private StateMachine<ProbeContext> _machine;

        [SetUp]
        public void SetUp()
        {
            _context = new ProbeContext();
            _machine = new StateMachine<ProbeContext>(_context);
            _context.Machine = _machine;
        }

        [Test]
        public void ChangeState_FromEmpty_EntersState()
        {
            var a = new ProbeState("A");

            _machine.ChangeState(a);

            CollectionAssert.AreEqual(new[] { "enter:A" }, _context.Log);
            Assert.AreSame(a, _machine.CurrentState);
        }

        [Test]
        public void ChangeState_ExitsPreviousBeforeEnteringNext()
        {
            var a = new ProbeState("A");
            var b = new ProbeState("B");

            _machine.ChangeState(a);
            _machine.ChangeState(b);

            CollectionAssert.AreEqual(new[] { "enter:A", "exit:A", "enter:B" }, _context.Log);
        }

        [Test]
        public void ChangeState_RequestedDuringEnter_RunsAfterEnterCompletes()
        {
            var b = new ProbeState("B");
            var a = new ProbeState("A") { NextOnEnter = b };

            _machine.ChangeState(a);

            CollectionAssert.AreEqual(new[] { "enter:A", "enter-done:A", "exit:A", "enter:B" }, _context.Log);
            Assert.AreSame(b, _machine.CurrentState);
        }

        [Test]
        public void ChangeState_RequestedDuringTick_RunsAfterTickCompletes()
        {
            var b = new ProbeState("B");
            var t = new TickProbeState("T") { NextOnTick = b };

            _machine.ChangeState(t);
            _machine.Tick(0.016f);

            CollectionAssert.AreEqual(new[] { "enter:T", "tick:T", "tick-done:T", "exit:T", "enter:B" }, _context.Log);
            Assert.AreSame(b, _machine.CurrentState);
        }

        [Test]
        public void Tick_ForwardsDeltaToTickableState()
        {
            var t = new TickProbeState("T");

            _machine.ChangeState(t);
            _machine.Tick(0.5f);
            _machine.Tick(0.25f);

            Assert.AreEqual(2, t.Ticks);
            Assert.AreEqual(0.25f, t.LastDelta);
        }

        [Test]
        public void Tick_WithNonTickableState_DoesNothing()
        {
            _machine.ChangeState(new ProbeState("A"));

            Assert.DoesNotThrow(() => _machine.Tick(0.016f));
            CollectionAssert.AreEqual(new[] { "enter:A" }, _context.Log);
        }

        [Test]
        public void StateChanged_ReportsPreviousAndNext()
        {
            var a = new ProbeState("A");
            var b = new ProbeState("B");
            IState<ProbeContext> from = null;
            IState<ProbeContext> to = null;
            int calls = 0;

            _machine.ChangeState(a);
            _machine.StateChanged += (previous, next) =>
            {
                from = previous;
                to = next;
                calls++;
            };
            _machine.ChangeState(b);

            Assert.AreEqual(1, calls);
            Assert.AreSame(a, from);
            Assert.AreSame(b, to);
        }

        [Test]
        public void Stop_ExitsCurrentAndClears()
        {
            _machine.ChangeState(new ProbeState("A"));

            _machine.Stop();

            CollectionAssert.AreEqual(new[] { "enter:A", "exit:A" }, _context.Log);
            Assert.IsNull(_machine.CurrentState);
        }

        [Test]
        public void RunawayTransitions_Throw_AndMachineStaysUsable()
        {
            var a = new ProbeState("A");
            var b = new ProbeState("B") { NextOnEnter = a };
            a.NextOnEnter = b;

            Assert.Throws<InvalidOperationException>(() => _machine.ChangeState(a));

            a.NextOnEnter = null;
            b.NextOnEnter = null;
            var c = new ProbeState("C");
            _machine.ChangeState(c);

            Assert.AreSame(c, _machine.CurrentState);
        }

        [Test]
        public void ChangeState_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _machine.ChangeState(null));
        }
    }
}
