// The state of one run in plain C#: Rama's health, deck and quiver of special arrows between fights (each fight fires
// from this same quiver, so used arrows are gone for the rest of the run), which encounter is next, how the run
// stands, and a seeded random source that hands each fight its own seed, so a whole run replays from one seed.
// A won fight that does not end the run rolls a card reward, as in the reference build: for each choice it rolls a
// rarity with the won encounter's odds (or the run's default), offers a random card of that rarity not yet offered,
// and falls back to any card not yet offered when that rarity has none left; without odds every card is equally
// likely. The choices are distinct cards from the run's pool, drawn with the run's random source, and held as the
// pending reward until ResolveReward takes one (or skips with -1). After the card roll it rolls for a special arrow
// with the run's drop table and its running chance, as in the reference build; an arrow that drops goes straight into
// the quiver, or is left behind when the quiver is full, and LastArrowDrop and LastArrowKept tell the reward screen
// which happened until the next fight's result.
// It changes only through ApplyCombatResult, ResolveReward and AddCard, and its quiver through the fights that fire
// from it. Saving will serialize this state later.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;

namespace JourneyToLanka.Run
{
    public sealed class RunState
    {
        private readonly List<CardDefinitionSO> _deck;
        private readonly CombatRandom _random;
        private readonly List<CardDefinitionSO> _pendingReward = new List<CardDefinitionSO>(4);
        private readonly List<CardDefinitionSO> _rewardPool = new List<CardDefinitionSO>(32);
        private readonly List<CardDefinitionSO> _rewardCandidates = new List<CardDefinitionSO>(32);
        private int _arrowChance;

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
            Quiver = player.CreateQuiver();
            _arrowChance = definition.ArrowDrops != null ? definition.ArrowDrops.StartChance : 0;
        }

        public RunDefinitionSO Definition { get; }

        public int Seed { get; }

        public int MaxHealth { get; }

        public int Health { get; private set; }

        public IReadOnlyList<CardDefinitionSO> Deck => _deck;

        public Quiver Quiver { get; }

        public int ArrowChance => _arrowChance;

        public ArrowDefinitionSO LastArrowDrop { get; private set; }

        public bool LastArrowKept { get; private set; }

        public int EncounterIndex { get; private set; }

        public int EncounterCount => Definition.Encounters.Count;

        public RunOutcome Outcome { get; private set; }

        public bool IsFinished => Outcome != RunOutcome.InProgress;

        public IReadOnlyList<CardDefinitionSO> PendingReward => _pendingReward;

        public bool HasPendingReward => _pendingReward.Count > 0;

        public CombatEncounterSO CurrentEncounter => IsFinished ? null : Definition.Encounters[EncounterIndex];

        public PlayerSetup CreatePlayerSetup()
        {
            return new PlayerSetup(Definition.Player.NameKey, MaxHealth, Health, _deck, Quiver);
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
            LastArrowDrop = null;
            LastArrowKept = false;

            if (!result.Victory || Health == 0)
            {
                Outcome = RunOutcome.Lost;
                return;
            }

            CombatEncounterSO won = Definition.Encounters[EncounterIndex];
            EncounterIndex++;
            if (EncounterIndex >= EncounterCount)
            {
                Outcome = RunOutcome.Won;
                return;
            }

            RollReward(won);
            RollArrowDrop();
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

        private void RollArrowDrop()
        {
            ArrowDropTableSO table = Definition.ArrowDrops;
            if (table == null)
            {
                return;
            }

            bool dropped = _random.Range(0, 99) < _arrowChance;
            _arrowChance = table.NextChance(_arrowChance, dropped);
            if (!dropped)
            {
                return;
            }

            LastArrowDrop = table.Roll(_random);
            LastArrowKept = Quiver.TryAdd(LastArrowDrop);
        }

        private void RollReward(CombatEncounterSO won)
        {
            _pendingReward.Clear();
            CardRewardPoolSO pool = Definition.RewardPool;
            if (pool == null)
            {
                return;
            }

            _rewardPool.Clear();
            for (int i = 0; i < pool.Cards.Count; i++)
            {
                CardDefinitionSO card = pool.Cards[i];
                if (card != null && !_rewardPool.Contains(card))
                {
                    _rewardPool.Add(card);
                }
            }

            CardRewardOddsSO odds = won != null && won.RewardOdds != null ? won.RewardOdds : Definition.RewardOdds;
            int count = Math.Min(Definition.RewardChoices, _rewardPool.Count);
            for (int i = 0; i < count; i++)
            {
                _rewardCandidates.Clear();
                if (odds != null)
                {
                    CollectRewardCandidates(odds.Roll(_random), true);
                }

                if (_rewardCandidates.Count == 0)
                {
                    CollectRewardCandidates(CardRarity.None, false);
                }

                _pendingReward.Add(_rewardCandidates[_random.Range(0, _rewardCandidates.Count - 1)]);
            }
        }

        private void CollectRewardCandidates(CardRarity rarity, bool matchRarity)
        {
            for (int i = 0; i < _rewardPool.Count; i++)
            {
                CardDefinitionSO card = _rewardPool[i];
                if (!_pendingReward.Contains(card) && (!matchRarity || card.Rarity == rarity))
                {
                    _rewardCandidates.Add(card);
                }
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
