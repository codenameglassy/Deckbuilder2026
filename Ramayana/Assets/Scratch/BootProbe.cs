// TEMPORARY verification probe for Foundation Step 2. Delete after verifying.
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

public sealed class BootProbe : MonoBehaviour, ITickable
{
    [SerializeField] private VoidEventChannelSO _bootCompleted;

    private ITickService _ticks;
    private int _frames;

    private void OnEnable()
    {
        if (_bootCompleted != null)
        {
            _bootCompleted.Register(HandleBootCompleted);
        }
    }

    private void OnDisable()
    {
        if (_bootCompleted != null)
        {
            _bootCompleted.Unregister(HandleBootCompleted);
        }

        _ticks?.RemoveTickable(this);
    }

    private void HandleBootCompleted()
    {
        _ticks = ServiceLocator.Get<ITickService>();
        _ticks.AddTickable(this);
        Debug.Log("BootProbe: BootCompleted received, ITickService resolved.");
    }

    public void Tick(float deltaTime)
    {
        _frames++;
        if (_frames == 60)
        {
            Debug.Log("BootProbe: 60 ticks received.");
            _ticks.RemoveTickable(this);
        }
    }
}
