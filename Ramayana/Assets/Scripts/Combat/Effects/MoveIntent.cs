// Summary of what a move will do, built by its effects for the intent display: damage per hit, hit count and total
// damage after modifiers, Kavach gained, and the statuses it applies, split into buffs and debuffs with the first of
// each kept so the display can show its icon. A move with one multi-hit damage effect shows as "4x2"; several damage
// effects show their total. Plain data, built fresh each time it is asked for.
using JourneyToLanka.Combat.Statuses;

namespace JourneyToLanka.Combat.Effects
{
    public struct MoveIntent
    {
        public int DamagePerHit { get; private set; }

        public int Hits { get; private set; }

        public int TotalDamage { get; private set; }

        public int DamageEffectCount { get; private set; }

        public int Block { get; private set; }

        public int BuffCount { get; private set; }

        public int DebuffCount { get; private set; }

        public StatusDefinitionSO FirstBuff { get; private set; }

        public int FirstBuffAmount { get; private set; }

        public StatusDefinitionSO FirstDebuff { get; private set; }

        public int FirstDebuffAmount { get; private set; }

        public bool HasAttack => DamageEffectCount > 0;

        public bool HasBlock => Block > 0;

        public bool HasBuff => BuffCount > 0;

        public bool HasDebuff => DebuffCount > 0;

        public bool IsEmpty => !HasAttack && !HasBlock && !HasBuff && !HasDebuff;

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

        public void AddStatus(StatusDefinitionSO status, int amount)
        {
            if (status == null || amount <= 0)
            {
                return;
            }

            if (status.IsDebuff)
            {
                if (DebuffCount == 0)
                {
                    FirstDebuff = status;
                    FirstDebuffAmount = amount;
                }

                DebuffCount++;
            }
            else
            {
                if (BuffCount == 0)
                {
                    FirstBuff = status;
                    FirstBuffAmount = amount;
                }

                BuffCount++;
            }
        }
    }
}
