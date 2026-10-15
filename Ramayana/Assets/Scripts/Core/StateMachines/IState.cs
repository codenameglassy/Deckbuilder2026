// One discrete mode of a StateMachine. Enter and Exit run once per transition.
// States are plain C# objects created once by their owner and reused, so Enter must reset any per-visit data.
namespace JourneyToLanka.Core.StateMachines
{
    public interface IState<in TContext>
    {
        void Enter(TContext context);

        void Exit(TContext context);
    }
}
