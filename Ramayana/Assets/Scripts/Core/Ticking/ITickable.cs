// Receives one call per rendered frame from the TickRunner, in place of a MonoBehaviour Update.
namespace JourneyToLanka.Core.Ticking
{
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
