// Boot scene component that installs ITickService and drives every registered tickable from
// one Update, one LateUpdate and one FixedUpdate.
using JourneyToLanka.Core.Services;
using UnityEngine;

namespace JourneyToLanka.Core.Ticking
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Core/Tick Runner")]
    public sealed class TickRunner : ServiceInstaller
    {
        private readonly TickSystem _system = new TickSystem();

        public override void Install(ServiceRegistry registry)
        {
            registry.Register<ITickService>(_system);
        }

        public override void Uninstall(ServiceRegistry registry)
        {
            registry.Unregister<ITickService>(_system);
        }

        private void Update()
        {
            _system.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            _system.LateTick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _system.FixedTick(Time.fixedDeltaTime);
        }
    }
}
