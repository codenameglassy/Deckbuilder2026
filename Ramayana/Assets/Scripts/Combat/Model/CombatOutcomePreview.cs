// A dry run of a card: an ICombatActions that simulates instead of acting. CombatModel.PreviewPlay runs the card's
// real effect strategies against it, so every hit goes through the resolver's damage modifiers and then Kavach,
// one hit at a time, exactly as play resolves, while the real fighters are never touched. Hits on a fighter the
// simulation has already brought to zero are ignored. Card-pile actions such as draws are not offered, so those
// effects do nothing here. Any future action added to ICombatActions must be simulated here too, which the compiler
// enforces. Results are read per fighter with TryGet. Reused between previews: Begin clears it without allocating.
// Statuses are simulated on copies: the first status a card applies to a fighter copies that fighter's statuses,
// and while the preview calculates damage it hands the copies to the status damage modifier as its overlay, so a
// Bhaya applied before a hit raises that hit. TryGetStatuses reads the copies, for previews that show what a card
// would apply. As an IDamagePreview it previews damage with the copies too, so the threat panel can show enemy
// attacks as they would be after the card (a Moha it applies weakens them). Heals raise the simulated health up to
// the maximum, unmodified damage meets Kavach without modifiers, spending a status (Brahmastra's Dharma) changes only
// the copy, and GetStatusAmount reads the copy when there is one. Retaining Kavach changes nothing within the card;
// removing it empties the simulated Kavach. A status gained runs the copy's triggers against this preview, so a
// Devotion's Kavach shows before the card is played.
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CombatOutcomePreview : ICombatActions, IDamagePreview, IStatusOverlay
    {
        private readonly List<CombatantOutcome> _outcomes = new List<CombatantOutcome>(8);
        private readonly List<Combatant> _statusOwners = new List<Combatant>(4);
        private readonly List<StatusSet> _statusCopies = new List<StatusSet>(4);
        private readonly List<StatusSet> _freeStatusCopies = new List<StatusSet>(4);
        private CombatResolver _resolver;
        private StatusDamageModifier _statusModifier;

        public int Count => _outcomes.Count;

        public bool CanPreviewDamage => _resolver != null;

        public void Begin(CombatResolver resolver)
        {
            Begin(resolver, null);
        }

        public void Begin(CombatResolver resolver, StatusDamageModifier statusModifier)
        {
            _resolver = resolver;
            _statusModifier = statusModifier;
            _outcomes.Clear();

            for (int i = 0; i < _statusCopies.Count; i++)
            {
                _freeStatusCopies.Add(_statusCopies[i]);
            }

            _statusCopies.Clear();
            _statusOwners.Clear();
        }

        public CombatantOutcome Get(int index)
        {
            return _outcomes[index];
        }

        public bool TryGet(Combatant combatant, out CombatantOutcome outcome)
        {
            int index = IndexOf(combatant);
            if (index < 0)
            {
                outcome = default;
                return false;
            }

            outcome = _outcomes[index];
            return true;
        }

        public bool TryGetStatuses(ICombatant combatant, out StatusSet statuses)
        {
            for (int i = 0; i < _statusOwners.Count; i++)
            {
                if (_statusOwners[i] == combatant)
                {
                    statuses = _statusCopies[i];
                    return true;
                }
            }

            statuses = null;
            return false;
        }

        public int PreviewDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            return CalculateDamage(source, target, baseAmount);
        }

        public void DealDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            if (!(target is Combatant combatant))
            {
                return;
            }

            int index = IndexOrAdd(combatant);
            CombatantOutcome current = _outcomes[index];
            if (current.EndHealth <= 0 || baseAmount < 0)
            {
                return;
            }

            ApplyHit(index, CalculateDamage(source, target, baseAmount));
        }

        public void DealUnmodifiedDamage(ICombatant source, ICombatant target, int amount)
        {
            if (!(target is Combatant combatant))
            {
                return;
            }

            int index = IndexOrAdd(combatant);
            if (_outcomes[index].EndHealth <= 0 || amount < 0)
            {
                return;
            }

            ApplyHit(index, amount);
        }

        public void RetainBlock(ICombatant target)
        {
        }

        public void RemoveBlock(ICombatant target)
        {
            if (!(target is Combatant combatant))
            {
                return;
            }

            int index = IndexOrAdd(combatant);
            CombatantOutcome current = _outcomes[index];
            if (current.EndHealth <= 0 || current.EndBlock == 0)
            {
                return;
            }

            _outcomes[index] = new CombatantOutcome(combatant, current.StartHealth, current.EndHealth,
                current.StartBlock, 0, current.BlockGained);
        }

        public void Heal(ICombatant target, int amount)
        {
            if (!(target is Combatant combatant) || amount <= 0)
            {
                return;
            }

            int index = IndexOrAdd(combatant);
            CombatantOutcome current = _outcomes[index];
            if (current.EndHealth <= 0)
            {
                return;
            }

            int endHealth = current.EndHealth + amount;
            if (endHealth > combatant.MaxHealth)
            {
                endHealth = combatant.MaxHealth;
            }

            _outcomes[index] = new CombatantOutcome(combatant, current.StartHealth, endHealth,
                current.StartBlock, current.EndBlock, current.BlockGained);
        }

        public int GetStatusAmount(ICombatant target, StatusDefinitionSO status)
        {
            if (TryGetStatuses(target, out StatusSet statuses))
            {
                return statuses.GetAmount(status);
            }

            return target is Combatant combatant ? combatant.Statuses.GetAmount(status) : 0;
        }

        public int GetMaxHealth(ICombatant target)
        {
            return target is Combatant combatant ? combatant.MaxHealth : 0;
        }

        public void GainBlock(ICombatant target, int amount)
        {
            if (!(target is Combatant combatant) || amount <= 0)
            {
                return;
            }

            int index = IndexOrAdd(combatant);
            CombatantOutcome current = _outcomes[index];
            if (current.EndHealth <= 0)
            {
                return;
            }

            _outcomes[index] = new CombatantOutcome(combatant, current.StartHealth, current.EndHealth,
                current.StartBlock, current.EndBlock + amount, current.BlockGained + amount);
        }

        public void ApplyStatus(ICombatant target, StatusDefinitionSO status, int amount, int duration)
        {
            if (!(target is Combatant combatant) || status == null || amount == 0)
            {
                return;
            }

            if (TryGet(combatant, out CombatantOutcome current) && current.EndHealth <= 0)
            {
                return;
            }

            if (!combatant.IsAlive)
            {
                return;
            }

            if (!TryGetStatuses(combatant, out StatusSet statuses))
            {
                statuses = TakeStatusCopy();
                statuses.CopyFrom(combatant.Statuses);
                _statusOwners.Add(combatant);
                _statusCopies.Add(statuses);
            }

            statuses.Apply(status, amount, duration);
            if (amount > 0)
            {
                statuses.TriggerStatusGained(status, amount, new StatusTickContext(this, combatant));
            }
        }

        public void LoseHealth(ICombatant target, int amount)
        {
            if (!(target is Combatant combatant) || amount <= 0)
            {
                return;
            }

            int index = IndexOrAdd(combatant);
            CombatantOutcome current = _outcomes[index];
            if (current.EndHealth <= 0)
            {
                return;
            }

            int healthLost = amount < current.EndHealth ? amount : current.EndHealth;
            _outcomes[index] = new CombatantOutcome(combatant, current.StartHealth, current.EndHealth - healthLost,
                current.StartBlock, current.EndBlock, current.BlockGained);
        }

        private void ApplyHit(int index, int amount)
        {
            CombatantOutcome current = _outcomes[index];
            int blocked = amount < current.EndBlock ? amount : current.EndBlock;
            int healthLost = amount - blocked;
            if (healthLost > current.EndHealth)
            {
                healthLost = current.EndHealth;
            }

            _outcomes[index] = new CombatantOutcome(current.Combatant, current.StartHealth, current.EndHealth - healthLost,
                current.StartBlock, current.EndBlock - blocked, current.BlockGained);
        }

        private int CalculateDamage(ICombatant source, ICombatant target, int baseAmount)
        {
            if (_resolver == null)
            {
                return baseAmount;
            }

            if (_statusModifier == null)
            {
                return _resolver.CalculateDamage(source, target, baseAmount);
            }

            IStatusOverlay previous = _statusModifier.Overlay;
            _statusModifier.Overlay = this;
            try
            {
                return _resolver.CalculateDamage(source, target, baseAmount);
            }
            finally
            {
                _statusModifier.Overlay = previous;
            }
        }

        private StatusSet TakeStatusCopy()
        {
            int last = _freeStatusCopies.Count - 1;
            if (last < 0)
            {
                return new StatusSet();
            }

            StatusSet copy = _freeStatusCopies[last];
            _freeStatusCopies.RemoveAt(last);
            return copy;
        }

        private int IndexOrAdd(Combatant combatant)
        {
            int index = IndexOf(combatant);
            if (index >= 0)
            {
                return index;
            }

            _outcomes.Add(new CombatantOutcome(combatant, combatant.Health, combatant.Health, combatant.Block, combatant.Block, 0));
            return _outcomes.Count - 1;
        }

        private int IndexOf(Combatant combatant)
        {
            for (int i = 0; i < _outcomes.Count; i++)
            {
                if (_outcomes[i].Combatant == combatant)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
