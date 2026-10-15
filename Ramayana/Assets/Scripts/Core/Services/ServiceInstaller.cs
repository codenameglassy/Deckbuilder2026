// Base for components in the Boot scene that create and register one application-lifetime service.
// Install runs in Bootstrapper.Awake in list order. Uninstall runs in reverse order on shutdown and
// must not depend on other services, since they may already be gone.
using UnityEngine;

namespace JourneyToLanka.Core.Services
{
    public abstract class ServiceInstaller : MonoBehaviour
    {
        public abstract void Install(ServiceRegistry registry);

        public abstract void Uninstall(ServiceRegistry registry);
    }
}
