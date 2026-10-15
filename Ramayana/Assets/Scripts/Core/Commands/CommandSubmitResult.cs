// What CommandProcessor.Submit did with the submitted command.
// Queued means it was submitted during another command and will run, or be rejected, once that one finishes.
namespace JourneyToLanka.Core.Commands
{
    public enum CommandSubmitResult
    {
        Executed,
        Rejected,
        Queued,
        Failed
    }
}
