// Per-spawn values a look writes into its material along with its own: the hit's intensity multiplier, a seed that
// varies the effect (rotation, ray lengths, spark paths), and the world direction the blow travelled, from attacker
// to target (zero when nothing struck, such as a burn). Immutable.
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    public readonly struct VfxSpawnContext
    {
        public VfxSpawnContext(float intensity, float seed, Vector3 direction)
        {
            Intensity = intensity;
            Seed = seed;
            Direction = direction;
        }

        public float Intensity { get; }

        public float Seed { get; }

        public Vector3 Direction { get; }

        public bool HasDirection => Direction.sqrMagnitude > 1e-6f;

        // Normalized direction in xyz, with w set to 1 when there is one and 0 when there is none, as shaders read it.
        public Vector4 ShaderDirection
        {
            get
            {
                if (!HasDirection)
                {
                    return Vector4.zero;
                }

                Vector3 normalized = Direction.normalized;
                return new Vector4(normalized.x, normalized.y, normalized.z, 1f);
            }
        }
    }
}
