// EditMode tests for the event channel contract: delivery, idempotent registration, unregistration.
using JourneyToLanka.Core.Events;
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.Core.Tests
{
    public sealed class EventChannelTests
    {
        private IntEventChannelSO _intChannel;
        private VoidEventChannelSO _voidChannel;

        [SetUp]
        public void SetUp()
        {
            _intChannel = ScriptableObject.CreateInstance<IntEventChannelSO>();
            _voidChannel = ScriptableObject.CreateInstance<VoidEventChannelSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_intChannel);
            Object.DestroyImmediate(_voidChannel);
        }

        [Test]
        public void Raise_DeliversPayloadToListener()
        {
            int received = 0;
            void Listener(int value) => received = value;

            _intChannel.Register(Listener);
            _intChannel.Raise(7);

            Assert.AreEqual(7, received);
        }

        [Test]
        public void Register_Twice_DeliversOnce()
        {
            int calls = 0;
            void Listener(int value) => calls++;

            _intChannel.Register(Listener);
            _intChannel.Register(Listener);
            _intChannel.Raise(1);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Unregister_StopsDelivery()
        {
            int calls = 0;
            void Listener(int value) => calls++;

            _intChannel.Register(Listener);
            _intChannel.Unregister(Listener);
            _intChannel.Raise(1);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Raise_WithoutListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _intChannel.Raise(1);
                _voidChannel.Raise();
            });
        }

        [Test]
        public void VoidChannel_Register_Twice_DeliversOnce()
        {
            int calls = 0;
            void Listener() => calls++;

            _voidChannel.Register(Listener);
            _voidChannel.Register(Listener);
            _voidChannel.Raise();

            Assert.AreEqual(1, calls);
        }
    }
}
