// Read-only access point for application-lifetime services (ticking, audio, save, scene loading, input, platform).
// Only the Bootstrapper's installers register. Resolve once in Awake or OnEnable and cache; never resolve per frame.
using UnityEngine;

namespace JourneyToLanka.Core.Services
{
    public static class ServiceLocator
    {
        private static readonly ServiceRegistry s_registry = new ServiceRegistry();

        internal static ServiceRegistry Registry => s_registry;

        public static T Get<T>() where T : class
        {
            return s_registry.Get<T>();
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            return s_registry.TryGet(out service);
        }

        internal static void Clear()
        {
            s_registry.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            s_registry.Clear();
        }
    }
}
