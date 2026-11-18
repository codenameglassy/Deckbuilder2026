// A short freeze when a hit lands: drops Time.timeScale for a few frames and restores it on unscaled time. It is the
// only combat code that writes Time.timeScale. Overlapping requests extend the current stop instead of stacking, the
// scale found when the stop began is restored when it ends or when this component is disabled, and without a tick
// service it does nothing rather than risk staying slowed. The flow, lunges and health bars run on game time and
// freeze with it; HUD tweens run on unscaled time and keep moving. A pause menu must go through this owner later.
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Hit Stop")]
    public sealed class HitStop : MonoBehaviour, ITickable
    {
        private ITickService _ticks;
        private bool _active;
        private float _endTime;
        private float _restoreScale = 1f;

        public bool IsActive => _active;

        private void Start()
        {
            ServiceLocator.TryGet(out _ticks);
        }

        private void OnDisable()
        {
            End();
        }

        public void Trigger(float duration, float timeScale)
        {
            if (duration <= 0f || _ticks == null || !isActiveAndEnabled)
            {
                return;
            }

            float endTime = Time.unscaledTime + duration;
            if (_active)
            {
                _endTime = Mathf.Max(_endTime, endTime);
                Time.timeScale = Mathf.Min(Time.timeScale, timeScale);
                return;
            }

            _active = true;
            _endTime = endTime;
            _restoreScale = Time.timeScale;
            Time.timeScale = Mathf.Min(_restoreScale, timeScale);
            _ticks.AddTickable(this);
        }

        public void Tick(float deltaTime)
        {
            if (Time.unscaledTime >= _endTime)
            {
                End();
            }
        }

        private void End()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            Time.timeScale = _restoreScale;
            _ticks?.RemoveTickable(this);
        }
    }
}
