// A single player or AI action, run through a CommandProcessor.
// CanExecute is checked when the command actually runs, not when it is submitted,
// because commands that ran earlier in the same batch may have changed the state.
namespace JourneyToLanka.Core.Commands
{
    public interface ICommand
    {
        bool CanExecute();

        void Execute();
    }
}
