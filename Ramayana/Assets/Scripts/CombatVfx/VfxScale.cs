// How much bigger, longer and brighter one effect plays than its look's base values: One for an ordinary hit, larger
// for heavy hits and killing blows (CombatVfxStyleSO.GetHitScale). Immutable.
namespace JourneyToLanka.CombatVfx
{
    public readonly struct VfxScale
    {
        public VfxScale(float size, float duration, float intensity)
        {
            Size = size;
            Duration = duration;
            Intensity = intensity;
        }

        public static VfxScale One => new VfxScale(1f, 1f, 1f);

        public float Size { get; }

        public float Duration { get; }

        public float Intensity { get; }
    }
}
