// One hit ready to be shown: the slot of the fighter it landed on (HitSource.PlayerSlot for Rama, 0 and up for enemy
// field slots), what the hit did after modifiers, and its resolved source. Produced by CombatHitBuffer.Drain.
// Immutable.
using JourneyToLanka.Combat.Model;

namespace JourneyToLanka.CombatVfx
{
    public readonly struct HitRecord
    {
        public HitRecord(int targetSlot, in DamageResult result, in HitSource source)
        {
            TargetSlot = targetSlot;
            Result = result;
            Source = source;
        }

        public int TargetSlot { get; }

        public DamageResult Result { get; }

        public HitSource Source { get; }
    }
}
