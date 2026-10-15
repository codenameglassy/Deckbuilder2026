// What one hit did after modifiers: the amount that landed, how much Kavach absorbed, how much health was lost,
// and whether the hit was the killing blow. Views use it for damage numbers and "Blocked" popups.
namespace JourneyToLanka.Combat.Model
{
    public readonly struct DamageResult
    {
        public DamageResult(int amount, int blocked, int healthLost, bool killed)
        {
            Amount = amount;
            Blocked = blocked;
            HealthLost = healthLost;
            Killed = killed;
        }

        public int Amount { get; }

        public int Blocked { get; }

        public int HealthLost { get; }

        public bool Killed { get; }

        public bool FullyBlocked => Amount > 0 && HealthLost == 0 && Blocked == Amount;
    }
}
