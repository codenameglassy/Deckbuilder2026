// Contract for a component that lives in a ComponentPool. OnTakenFromPool must fully reset the object's state,
// because the instance was used before. OnReturnedToPool must unsubscribe every listener and stop anything still
// running (tweens, coroutines, audio), so a returned object never reacts to events while it waits in the pool.
// Implement it on the pooled component itself, the type the pool is created for.
namespace JourneyToLanka.Core.Pooling
{
    public interface IPoolable
    {
        void OnTakenFromPool();

        void OnReturnedToPool();
    }
}
