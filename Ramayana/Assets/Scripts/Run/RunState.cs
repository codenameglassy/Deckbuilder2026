// The state of one run in plain C#: Rama's health and deck between fights, which encounter is next, how the run
// stands, and a seeded random source that hands each fight its own seed, so a whole run replays from one seed.
// A won fight that does not end the run rolls a card reward: distinct cards from the run's pool, drawn with the run's
// random source, held as the pending reward until ResolveReward takes one (or skips with -1).
// It changes only through ApplyCombatResult, ResolveReward and AddCard. Saving will serialize this state later.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;

namespace JourneyToLanka.Run
{
    public sealed class RunState
    {
        private readonly List<CardDefinitionSO> _deck;
        private readonly CombatRandom _random;
        private readonly List<CardDefinitionSO> _pendingReward = new List<CardDefinitionSO>(4);

        public RunState(RunDefinitionSO definition, int seed)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.Player == null || definition.Player.StartingDeck == null)
            {
                throw new ArgumentException($"{definition.name} needs a player with a starting deck.", nameof(definition));
            }

            if (definition.Encounters.Count == 0)
            {
                throw new ArgumentException($"{definition.name} has no encounters.", nameof(definition));
            }

            Definition = definition;
            Seed = seed;
            _random = new CombatRandom(seed);

            PlayerDefinitionSO player = definition.Player;
            MaxHealth = player.MaxHealth;
            Health = player.MaxHealth;
            _deck = new List<CardDefinitionSO>(player.StartingDeck.TotalCount + 16);
            PlayerSetup.ExpandDeck(player.StartingDeck, _deck);
        }

        public RunDefinitionSO Definition { get; }

        public int Seed { get; }

        public int MaxHealth { get; }

        public int Health { get; private set; }

        public IReadOnlyList<CardDefinitionSO> Deck => _deck;

        public int EncounterIndex { get; private set; }

        public int EncounterCount => Definition.Encounters.Count;

        public RunOutcome Outcome { get; private set; }

        public bool IsFinished => Outcome != RunOutcome.InProgress;

        public IReadOnlyList<CardDefinitionSO> PendingReward => _pendingReward;

        public bool HasPendingReward => _pendingReward.Count > 0;

        public CombatEncounterSO CurrentEncounter => IsFinished ? null : Definition.Encounters[EncounterIndex];

        public PlayerSetup CreatePlayerSetup()
        {
            return new PlayerSetup(Definition.Player.NameKey, MaxHealth, Health, _deck);
        }

        public int NextCombatSeed()
        {
            return unchecked((int)_random.NextUInt());
        }

        public void ApplyCombatResult(CombatResult result)
        {
            if (IsFinished)
            {
                return;
            }

            Health = Math.Clamp(result.PlayerHealth, 0, MaxHealth);

            if (!result.Victory || Health == 0)
            {
                Outcome = RunOutcome.Lost;
                return;
            }

            EncounterIndex++;
            if (EncounterIndex >= EncounterCount)
            {
                Outcome = RunOutcome.Won;
                return;
            }

            RollReward();
        }

        public CardDefinitionSO ResolveReward(int choiceIndex)
        {
            CardDefinitionSO chosen = choiceIndex >= 0 && choiceIndex < _pendingReward.Count
                ? _pendingReward[choiceIndex]
                : null;

            _pendingReward.Clear();
            if (chosen != null)
            {
                _deck.Add(chosen);
            }

            return chosen;
        }

        private void RollReward()
        {
            _pendingReward.Clear();
            CardRewardPoolSO pool = Definition.RewardPool;
            if (pool == null)
            {
                return;
            }

            var candidates = new List<CardDefinitionSO>(pool.Cards.Count);
            for (int i = 0; i < pool.Cards.Count; i++)
            {
                CardDefinitionSO card = pool.Cards[i];
                if (card != null && !candidates.Contains(card))
                {
                    candidates.Add(card);
                }
            }

            _random.Shuffle(candidates);
            int count = Math.Min(Definition.RewardChoices, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                _pendingReward.Add(candidates[i]);
            }
        }

        public void AddCard(CardDefinitionSO card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _deck.Add(card);
        }
    }
}
