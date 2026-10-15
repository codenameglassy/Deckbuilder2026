// Entry point of the persistent Boot scene. Installs application-lifetime services in list order, raises
// BootCompleted only if every installer succeeded, and uninstalls in reverse order on shutdown.
// The Boot scene is never unloaded; every other scene loads additively.
using System;
using JourneyToLanka.Core.Events;
using UnityEngine;

namespace JourneyToLanka.Core.Services
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Core/Bootstrapper")]
    public sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private ServiceInstaller[] _installers = Array.Empty<ServiceInstaller>();
        [SerializeField] private VoidEventChannelSO _bootCompleted;

        private bool _bootSucceeded;

        private void Awake()
        {
            ServiceRegistry registry = ServiceLocator.Registry;
            _bootSucceeded = true;

            for (int i = 0; i < _installers.Length; i++)
            {
                ServiceInstaller installer = _installers[i];
                if (installer == null)
                {
                    Debug.LogError($"Bootstrapper: installer slot {i} is empty.", this);
                    _bootSucceeded = false;
                    continue;
                }

                try
                {
                    installer.Install(registry);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, installer);
                    _bootSucceeded = false;
                }
            }
        }

        private void Start()
        {
            if (!_bootSucceeded)
            {
                Debug.LogError("Bootstrapper: boot failed, BootCompleted was not raised. See the errors above.", this);
                return;
            }

            if (_bootCompleted != null)
            {
                _bootCompleted.Raise();
            }
        }

        private void OnDestroy()
        {
            ServiceRegistry registry = ServiceLocator.Registry;

            for (int i = _installers.Length - 1; i >= 0; i--)
            {
                ServiceInstaller installer = _installers[i];
                if (installer == null)
                {
                    continue;
                }

                try
                {
                    installer.Uninstall(registry);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            ServiceLocator.Clear();
        }
    }
}
