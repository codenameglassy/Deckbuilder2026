// One pooled copy of an accent particle prefab, played by PrefabVfxSpawner. The spawner builds one template per prefab
// and calls Initialize on it, which makes every particle system in the hierarchy safe to pool (no play on awake, no
// stop action that would disable or destroy it, game time, hierarchy scaling so the copy scales as a whole), records
// the systems with no particle system above them (the ones played, stopped and cleared with their children, so
// sub-emitters are never started on their own) and records its trail renderers. Those settings are serialized, so
// every pooled copy carries them; each copy reads its own authored start colours once when it is created.
// Play places, tints, clears and restarts it in place; with an infinite max duration it loops until StopLoop (a
// status aura), and MoveTo keeps it on a moving fighter. PlayFlight does the same at the launch point and then moves it
// along a MissileArc to the landing point over the flight, its forward axis along the path (Polygon Arsenal missiles
// travel along their forward axis), and stops it on arrival. Trails are cleared at every start so a reused copy never
// draws a streak from where it last played, and stop emitting when it stops. Advance stops emission after the look's
// max duration (or on arrival) and reports done once no particle is left alive, or after the spawner's linger limit
// as a safety. Particle systems simulate on scaled time, so accents freeze with hit-stop like every other effect.
using System;
using JourneyToLanka.Core.Pooling;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PrefabVfxInstance : MonoBehaviour, IPoolable
    {
        [SerializeField] private GameObject _source;
        [SerializeField] private ParticleSystem[] _systems = Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _roots = Array.Empty<ParticleSystem>();
        [SerializeField] private TrailRenderer[] _trails = Array.Empty<TrailRenderer>();
        [SerializeField] private Quaternion _baseRotation = Quaternion.identity;
        [SerializeField] private Vector3 _baseScale = Vector3.one;

        private ParticleSystem.MinMaxGradient[] _authoredColors = Array.Empty<ParticleSystem.MinMaxGradient>();
        private Transform _transform;
        private Color _appliedTint = Color.white;
        private float _elapsed;
        private float _maxDuration = 1f;
        private float _lingerLimit = 1f;
        private bool _stopped;
        private bool _flying;
        private Vector3 _from;
        private Vector3 _to;
        private float _arc;

        public GameObject Source => _source;

        public Quaternion BaseRotation => _baseRotation;

        public int PlayOrder { get; private set; }

        internal void Initialize(GameObject source)
        {
            Transform root = transform;
            _source = source;
            _baseRotation = root.localRotation;
            _baseScale = root.localScale;
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _trails = GetComponentsInChildren<TrailRenderer>(true);

            int rootCount = 0;
            var roots = new ParticleSystem[_systems.Length];
            for (int i = 0; i < _systems.Length; i++)
            {
                ParticleSystem system = _systems[i];
                ParticleSystem.MainModule main = system.main;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.useUnscaledTime = false;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;

                if (!HasSystemAbove(system.transform, root))
                {
                    roots[rootCount++] = system;
                }
            }

            _roots = new ParticleSystem[rootCount];
            Array.Copy(roots, _roots, rootCount);
        }

        private void Awake()
        {
            _transform = transform;
            _authoredColors = new ParticleSystem.MinMaxGradient[_systems.Length];
            for (int i = 0; i < _systems.Length; i++)
            {
                _authoredColors[i] = _systems[i].main.startColor;
            }
        }

        internal void Play(Vector3 position, Quaternion rotation, float scale, Color tint, float maxDuration,
            float lingerLimit, int playOrder)
        {
            _flying = false;
            Begin(position, rotation, scale, tint, maxDuration, lingerLimit, playOrder);
        }

        internal void PlayFlight(Vector3 from, Vector3 to, float arc, float duration, float scale, Color tint,
            float lingerLimit, int playOrder)
        {
            _from = from;
            _to = to;
            _arc = arc;
            _flying = true;
            Begin(from, Facing(MissileArc.Direction(from, to, arc, 0f)), scale, tint, duration, lingerLimit, playOrder);
        }

        internal void MoveTo(Vector3 position)
        {
            _transform.position = position;
        }

        // Stops a looping play: emission ends now and the linger limit counts from here.
        internal void StopLoop()
        {
            if (!_stopped)
            {
                _maxDuration = _elapsed;
                StopEmitting();
            }
        }

        internal bool Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_flying)
            {
                float progress = _elapsed / _maxDuration;
                _transform.SetPositionAndRotation(
                    MissileArc.Position(_from, _to, _arc, progress),
                    Facing(MissileArc.Direction(_from, _to, _arc, progress)));
            }

            if (!_stopped && _elapsed >= _maxDuration)
            {
                StopEmitting();
            }

            if (_elapsed >= _maxDuration + _lingerLimit)
            {
                return true;
            }

            for (int i = 0; i < _roots.Length; i++)
            {
                if (_roots[i].IsAlive(true))
                {
                    return false;
                }
            }

            return true;
        }

        public void OnTakenFromPool()
        {
            _elapsed = 0f;
            _stopped = false;
            _flying = false;
        }

        public void OnReturnedToPool()
        {
            for (int i = 0; i < _roots.Length; i++)
            {
                _roots[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            for (int i = 0; i < _trails.Length; i++)
            {
                _trails[i].emitting = false;
                _trails[i].Clear();
            }

            _elapsed = 0f;
            _stopped = false;
            _flying = false;
            PlayOrder = 0;
        }

        private void Begin(Vector3 position, Quaternion rotation, float scale, Color tint, float maxDuration,
            float lingerLimit, int playOrder)
        {
            _transform.SetPositionAndRotation(position, rotation);
            _transform.localScale = _baseScale * scale;
            ApplyTint(tint);

            _elapsed = 0f;
            _maxDuration = Mathf.Max(0.05f, maxDuration);
            _lingerLimit = Mathf.Max(0f, lingerLimit);
            _stopped = false;
            PlayOrder = playOrder;

            for (int i = 0; i < _trails.Length; i++)
            {
                _trails[i].Clear();
                _trails[i].emitting = true;
            }

            for (int i = 0; i < _roots.Length; i++)
            {
                _roots[i].Clear(true);
                _roots[i].Play(true);
            }
        }

        private void StopEmitting()
        {
            _stopped = true;
            _flying = false;
            for (int i = 0; i < _roots.Length; i++)
            {
                _roots[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            for (int i = 0; i < _trails.Length; i++)
            {
                _trails[i].emitting = false;
            }
        }

        private Quaternion Facing(Vector3 direction)
        {
            return direction.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(direction) : _baseRotation;
        }

        private void ApplyTint(Color tint)
        {
            if (tint == _appliedTint)
            {
                return;
            }

            _appliedTint = tint;
            bool authored = tint == Color.white;
            for (int i = 0; i < _systems.Length; i++)
            {
                ParticleSystem.MainModule main = _systems[i].main;
                main.startColor = authored ? _authoredColors[i] : Tinted(_authoredColors[i], tint);
            }
        }

        private static ParticleSystem.MinMaxGradient Tinted(in ParticleSystem.MinMaxGradient authored, Color tint)
        {
            if (authored.mode == ParticleSystemGradientMode.Color)
            {
                return new ParticleSystem.MinMaxGradient(authored.color * tint);
            }

            if (authored.mode == ParticleSystemGradientMode.TwoColors)
            {
                return new ParticleSystem.MinMaxGradient(authored.colorMin * tint, authored.colorMax * tint);
            }

            return authored;
        }

        private static bool HasSystemAbove(Transform child, Transform root)
        {
            if (child == root)
            {
                return false;
            }

            Transform current = child.parent;
            while (current != null)
            {
                if (current.TryGetComponent(out ParticleSystem _))
                {
                    return true;
                }

                if (current == root)
                {
                    return false;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
