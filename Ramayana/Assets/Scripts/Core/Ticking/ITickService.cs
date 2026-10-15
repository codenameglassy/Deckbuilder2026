// Registration surface of the tick service. Add in OnEnable, remove in OnDisable, same as channel listeners.
// Separate method names per lane so a class implementing several lanes never hits an ambiguous overload.
namespace JourneyToLanka.Core.Ticking
{
    public interface ITickService
    {
        void AddTickable(ITickable tickable);
        void RemoveTickable(ITickable tickable);
        void AddLateTickable(ILateTickable tickable);
        void RemoveLateTickable(ILateTickable tickable);
        void AddFixedTickable(IFixedTickable tickable);
        void RemoveFixedTickable(IFixedTickable tickable);
    }
}
