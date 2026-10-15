// Receives one call per rendered frame after all Ticks, in place of a MonoBehaviour LateUpdate.
namespace JourneyToLanka.Core.Ticking
{
    public interface ILateTickable
    {
        void LateTick(float deltaTime);
    }
}
