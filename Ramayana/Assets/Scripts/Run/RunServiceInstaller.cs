// Installs the run service as an application-lifetime service on the Boot object.
using JourneyToLanka.Core.Services;
using UnityEngine;

namespace JourneyToLanka.Run
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Core/Run Service Installer")]
    public sealed class RunServiceInstaller : ServiceInstaller
    {
        private readonly RunService _service = new RunService();

        public override void Install(ServiceRegistry registry)
        {
            registry.Register<IRunService>(_service);
        }

        public override void Uninstall(ServiceRegistry registry)
        {
            registry.Unregister<IRunService>(_service);
        }
    }
}
