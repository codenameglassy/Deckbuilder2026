// One pooled billboard effect: the shared VFX quad drawn with this instance's own copy of a builder-made material, so
// every instance animates its progress while staying SRP Batcher compatible (a MaterialPropertyBlock would take the
// renderer out of the batcher). The copy is made once, when the pool instantiates this object, never per play, and
// is destroyed with it; it keeps the source's shader and keywords, so it renders in player builds exactly as the
// source asset does. The shader turns the quad to face the camera, so the transform carries only position and
// uniform scale. QuadVfxSpawner advances every active instance from one tick on game time, so it freezes with
// hit-stop.
using JourneyToLanka.Core.Pooling;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    public sealed class QuadVfx : MonoBehaviour, IPoolable
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [SerializeField] private Material _sourceMaterial;
        [SerializeField] private MeshRenderer _renderer;

        private Material _material;
        private Transform _transform;
        private float _elapsed;
        private float _duration = 1f;

        public Material SourceMaterial => _sourceMaterial;

        public Material Material => _material;

        public int PlayOrder { get; private set; }

        internal void Initialize(MeshRenderer meshRenderer, Material sourceMaterial)
        {
            _renderer = meshRenderer;
            _sourceMaterial = sourceMaterial;
        }

        private void Awake()
        {
            _transform = transform;
            if (_sourceMaterial != null && _renderer != null)
            {
                _material = new Material(_sourceMaterial);
                _renderer.sharedMaterial = _material;
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        internal void Play(Vector3 position, float size, float duration, int playOrder)
        {
            _transform.position = position;
            _transform.localScale = new Vector3(size, size, size);
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            PlayOrder = playOrder;
            _material.SetFloat(ProgressId, 0f);
        }

        internal bool Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);
            _material.SetFloat(ProgressId, progress);
            return progress >= 1f;
        }

        public void OnTakenFromPool()
        {
            _elapsed = 0f;
        }

        public void OnReturnedToPool()
        {
            _elapsed = 0f;
            PlayOrder = 0;
        }
    }
}
