// One particle prefab played at a fighter as an accent over the hand-written hit effects, such as a Polygon Arsenal
// impact, death or shield burst: the prefab, where on the fighter it plays (a height between the bottom and the top of
// the fighter's collider, optionally brought out to the camera-facing surface of its bounds as hit effects are, plus a
// world offset), its size (optionally grown by heavy hits and killing blows), whether its forward axis turns along the
// blow, a tint for start colours authored as one or two colours, and how long it may emit before it is stopped, so a
// looping prefab used as a one-shot still ends. An empty prefab plays nothing. A serializable value inside shared
// profiles, read-only at runtime and read on every spawn, so Play mode edits show on the next one.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public sealed class PrefabVfxLook
    {
        [Tooltip("A particle prefab, such as one from Polygon Arsenal. Empty plays nothing.")]
        [SerializeField] private GameObject _prefab;
        [Tooltip("Where it plays: 0 at the bottom of the fighter's collider, 1 at the top.")]
        [SerializeField, Range(0f, 1.5f)] private float _height = 0.65f;
        [Tooltip("Bring it out to the camera-facing surface of the fighter's bounds, as hit effects are.")]
        [SerializeField] private bool _onCameraSurface = true;
        [Tooltip("World units added after placement.")]
        [SerializeField] private Vector3 _offset;
        [Tooltip("Uniform scale over the prefab's own.")]
        [SerializeField, Min(0.01f)] private float _scale = 1f;
        [Tooltip("Grow with heavy hits and killing blows, as the hit effects do.")]
        [SerializeField] private bool _scaleWithHit;
        [Tooltip("Turn the prefab's forward axis along the blow when the hit has an attacker. Polygon Arsenal effects rise along their forward axis.")]
        [SerializeField] private bool _alignToBlow;
        [Tooltip("Multiplies start colours authored as one or two colours; gradient start colours keep their authored look. White leaves the prefab unchanged.")]
        [SerializeField] private Color _tint = Color.white;
        [Tooltip("Seconds it may emit before it is stopped; particles already out finish their lives.")]
        [SerializeField, Min(0.05f)] private float _maxDuration = 3f;

        public GameObject Prefab => _prefab;

        public bool IsEnabled => _prefab != null;

        public float Height => _height;

        public bool OnCameraSurface => _onCameraSurface;

        public Vector3 Offset => _offset;

        public bool AlignToBlow => _alignToBlow;

        public Color Tint => _tint;

        public float MaxDuration => _maxDuration;

        public float ScaleFor(in VfxScale hitScale)
        {
            return _scaleWithHit ? _scale * hitScale.Size : _scale;
        }
    }
}
