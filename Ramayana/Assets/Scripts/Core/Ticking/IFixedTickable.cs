// Receives one call per physics step from the TickRunner, in place of a MonoBehaviour FixedUpdate.
namespace JourneyToLanka.Core.Ticking
{
    public interface IFixedTickable
    {
        void FixedTick(float fixedDeltaTime);
    }
}
