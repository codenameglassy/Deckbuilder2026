// Plain C# implementation of ITickService holding one TickList per lane. Driven by TickRunner.
using System;

namespace JourneyToLanka.Core.Ticking
{
    public sealed class TickSystem : ITickService
    {
        private static readonly Action<ITickable, float> s_tick = InvokeTick;
        private static readonly Action<ILateTickable, float> s_lateTick = InvokeLateTick;
        private static readonly Action<IFixedTickable, float> s_fixedTick = InvokeFixedTick;

        private readonly TickList<ITickable> _tickables = new TickList<ITickable>();
        private readonly TickList<ILateTickable> _lateTickables = new TickList<ILateTickable>();
        private readonly TickList<IFixedTickable> _fixedTickables = new TickList<IFixedTickable>();

        public void AddTickable(ITickable tickable)
        {
            _tickables.Add(tickable);
        }

        public void RemoveTickable(ITickable tickable)
        {
            _tickables.Remove(tickable);
        }

        public void AddLateTickable(ILateTickable tickable)
        {
            _lateTickables.Add(tickable);
        }

        public void RemoveLateTickable(ILateTickable tickable)
        {
            _lateTickables.Remove(tickable);
        }

        public void AddFixedTickable(IFixedTickable tickable)
        {
            _fixedTickables.Add(tickable);
        }

        public void RemoveFixedTickable(IFixedTickable tickable)
        {
            _fixedTickables.Remove(tickable);
        }

        public void Tick(float deltaTime)
        {
            _tickables.Run(s_tick, deltaTime);
        }

        public void LateTick(float deltaTime)
        {
            _lateTickables.Run(s_lateTick, deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            _fixedTickables.Run(s_fixedTick, fixedDeltaTime);
        }

        private static void InvokeTick(ITickable tickable, float deltaTime)
        {
            tickable.Tick(deltaTime);
        }

        private static void InvokeLateTick(ILateTickable tickable, float deltaTime)
        {
            tickable.LateTick(deltaTime);
        }

        private static void InvokeFixedTick(IFixedTickable tickable, float fixedDeltaTime)
        {
            tickable.FixedTick(fixedDeltaTime);
        }
    }
}
