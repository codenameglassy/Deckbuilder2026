// Installs the scene loader as an application-lifetime service. Lives on the Boot object and is listed on the
// Bootstrapper after the Tick Runner.
using JourneyToLanka.Core.Services;
using UnityEngine;

namespace JourneyToLanka.Scenes
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Core/Scene Loader Installer")]
    public sealed class SceneLoaderInstaller : ServiceInstaller
    {
        private readonly SceneLoader _loader = new SceneLoader();

        public override void Install(ServiceRegistry registry)
        {
            registry.Register<ISceneLoader>(_loader);
        }

        public override void Uninstall(ServiceRegistry registry)
        {
            registry.Unregister<ISceneLoader>(_loader);
        }
    }
}
