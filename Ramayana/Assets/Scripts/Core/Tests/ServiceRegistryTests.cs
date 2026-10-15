// EditMode tests for ServiceRegistry: registration, ownership-checked removal, and missing-service errors.
using System;
using JourneyToLanka.Core.Services;
using NUnit.Framework;

namespace JourneyToLanka.Core.Tests
{
    public sealed class ServiceRegistryTests
    {
        private interface IFakeService
        {
        }

        private sealed class FakeService : IFakeService
        {
        }

        private ServiceRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new ServiceRegistry();
        }

        [Test]
        public void Register_ThenGet_ReturnsSameInstance()
        {
            var service = new FakeService();
            _registry.Register<IFakeService>(service);

            Assert.AreSame(service, _registry.Get<IFakeService>());
        }

        [Test]
        public void Register_DifferentInstanceForSameType_Throws()
        {
            _registry.Register<IFakeService>(new FakeService());

            Assert.Throws<InvalidOperationException>(() => _registry.Register<IFakeService>(new FakeService()));
        }

        [Test]
        public void Register_SameInstanceTwice_DoesNotThrow()
        {
            var service = new FakeService();
            _registry.Register<IFakeService>(service);

            Assert.DoesNotThrow(() => _registry.Register<IFakeService>(service));
        }

        [Test]
        public void Unregister_ByNonOwner_KeepsService()
        {
            var service = new FakeService();
            _registry.Register<IFakeService>(service);

            _registry.Unregister<IFakeService>(new FakeService());

            Assert.AreSame(service, _registry.Get<IFakeService>());
        }

        [Test]
        public void Get_WhenMissing_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _registry.Get<IFakeService>());
        }

        [Test]
        public void TryGet_WhenMissing_ReturnsFalse()
        {
            bool found = _registry.TryGet(out IFakeService service);

            Assert.IsFalse(found);
            Assert.IsNull(service);
        }
    }
}
