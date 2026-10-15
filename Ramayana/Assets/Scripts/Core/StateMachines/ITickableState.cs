// Optional extension for states that need per-frame work such as timers or pacing.
// The owner forwards frames through StateMachine.Tick, usually from its own ITickable.
namespace JourneyToLanka.Core.StateMachines
{
    public interface ITickableState<in TContext> : IState<TContext>
    {
        void Tick(TContext context, float deltaTime);
    }
}
