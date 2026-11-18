// Shakes the combat camera around its resting local position with DOTween core (DOShakePosition, game time, so a
// hit-stop holds the frame before the shake plays out). A new shake replaces the current one, and stopping always
// puts the camera back at rest. The scene camera is a plain Camera today; when Cinemachine arrives, this component is
// the one place to swap for an impulse-based version.
using DG.Tweening;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Combat Camera Shake")]
    public sealed class CombatCameraShake : MonoBehaviour
    {
        private const float Randomness = 90f;

        private Transform _transform;
        private Vector3 _restPosition;
        private TweenCallback _clearShake;
        private Tween _shake;

        private void Awake()
        {
            _transform = transform;
            _restPosition = _transform.localPosition;
            _clearShake = () => _shake = null;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }

        public void Shake(float strength, float duration, int vibrato)
        {
            if (strength <= 0f || duration <= 0f || !isActiveAndEnabled)
            {
                return;
            }

            Stop();
            _shake = _transform.DOShakePosition(duration, strength, vibrato, Randomness, false, true)
                .OnComplete(_clearShake);
        }

        public void Stop()
        {
            if (_shake != null)
            {
                Tween shake = _shake;
                _shake = null;
                shake.Kill();
            }

            if (_transform != null)
            {
                _transform.localPosition = _restPosition;
            }
        }
    }
}
