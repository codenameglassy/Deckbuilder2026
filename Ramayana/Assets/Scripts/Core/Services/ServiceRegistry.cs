// Type-keyed container of application-lifetime services. Plain C# so it is testable in EditMode.
// Register by interface with an explicit type argument: registry.Register<ISaveService>(instance).
using System;
using System.Collections.Generic;

namespace JourneyToLanka.Core.Services
{
    public sealed class ServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type key = typeof(T);
            if (_services.TryGetValue(key, out object existing) && !ReferenceEquals(existing, service))
            {
                throw new InvalidOperationException($"A different service is already registered for {key.Name}.");
            }

            _services[key] = service;
        }

        public void Unregister<T>(T service) where T : class
        {
            Type key = typeof(T);
            if (_services.TryGetValue(key, out object existing) && ReferenceEquals(existing, service))
            {
                _services.Remove(key);
            }
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        public T Get<T>() where T : class
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"No service registered for {typeof(T).Name}. Is the Boot scene loaded and its installer listed on the Bootstrapper?");
        }

        public void Clear()
        {
            _services.Clear();
        }
    }
}
