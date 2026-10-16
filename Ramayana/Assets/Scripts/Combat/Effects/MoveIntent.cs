// Summary of what a move will do, built by its effects for the intent display: damage per hit, hit count and total
// damage after modifiers, and Kavach gained. A move with one multi-hit damage effect shows as "4x2"; several damage
// effects show their total. Plain data, built fresh each time it is asked for.
namespace JourneyToLanka.Combat.Effects
{
    public struct MoveIntent
    {
        public int DamagePerHit { get; private set; }

        public int Hits { get; private set; }

        public int TotalDamage { get; private set; }

        public int DamageEffectCount { get; private set; }

        public int Block { get; private set; }

        public bool HasAttack => DamageEffectCount > 0;

        public bool HasBlock => Block > 0;

        public bool IsEmpty => !HasAttack && !HasBlock;

        public bool ShowsHits => DamageEffectCount == 1 && Hits > 1;

        public void AddAttack(int damagePerHit, int hits)
        {
            DamagePerHit = damagePerHit;
            Hits = hits;
            TotalDamage += damagePerHit * hits;
            DamageEffectCount++;
        }

        public void AddBlock(int amount)
        {
            if (amount > 0)
            {
                Block += amount;
            }
        }
    }
}
