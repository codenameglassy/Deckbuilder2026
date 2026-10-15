// The whole state of one fight in plain C#: Rama's Combatant, the enemies in field order, the deck, Prana and the
// resolver. It implements the turn rules (refill Prana, draw, Kavach clears from turn 2, discard the hand at end of
// turn), card play validation and resolution, and one enemy action at a time. It holds no timing: the combat state
// machine decides when each step runs, and views subscribe to the events of the parts they show.
// Events raised inside the constructor have no listeners, so views read initial state after construction.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CombatModel
    {
        private readonly List<EnemyCombatant> _enemies;
        private readonly List<ICombatant> _enemyFighters;
        private readonly ICombatant[] _playerSide;
        private readonly int _cardsPerTurn;

        public CombatModel(CombatRulesSO rules, PlayerDefinitionSO player, int playerHealth, CombatEncounterSO encounter, CombatRandom random)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (player.StartingDeck == null)
            {
                throw new ArgumentException($"{player.name} has no starting deck.", nameof(player));
            }

            Random = random;
            _cardsPerTurn = rules.CardsPerTurn;

            Player = new Combatant(player.NameKey, player.MaxHealth, playerHealth);
            _playerSide = new ICombatant[] { Player };

            var definitions = encounter.Enemies;
            _enemies = new List<EnemyCombatant>(definitions.Count);
            _enemyFighters = new List<ICombatant>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] == null)
                {
                    continue;
                }

                var enemy = new EnemyCombatant(definitions[i], _enemies.Count, random);
                _enemies.Add(enemy);
                _enemyFighters.Add(enemy.Fighter);
            }

            if (_enemies.Count == 0)
            {
                throw new ArgumentException($"{encounter.name} has no enemies.", nameof(encounter));
            }

            Resolver = new CombatResolver();
            Prana = new PranaPool(rules.PranaPerTurn);
            Deck = new CombatDeck(random, rules.MaxHandSize);
            Deck.AddStartingDeck(player.StartingDeck);
            Deck.ShuffleDrawPile();
        }

        public event Action<int> PlayerTurnStarted;

        // Card, the chosen target (null for untargeted cards).
        public event Action<CardInstance, ICombatant> CardPlayed;

        public CombatRandom Random { get; }

        public Combatant Player { get; }

        public IReadOnlyList<EnemyCombatant> Enemies => _enemies;

        public IReadOnlyList<ICombatant> EnemyFighters => _enemyFighters;

        public CombatDeck Deck { get; }

        public PranaPool Prana { get; }

        public CombatResolver Resolver { get; }

        public int TurnNumber { get; private set; }

        public bool IsPlayerDefeated => !Player.IsAlive;

        public bool AreEnemiesDefeated
        {
            get
            {
                for (int i = 0; i < _enemies.Count; i++)
                {
                    if (_enemies[i].IsAlive)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsOver => IsPlayerDefeated || AreEnemiesDefeated;

        public void StartPlayerTurn()
        {
            TurnNumber++;
            if (TurnNumber > 1)
            {
                Player.ClearBlock();
            }

            Prana.Refill();
            Deck.Draw(_cardsPerTurn);
            PlayerTurnStarted?.Invoke(TurnNumber);
        }

        public void EndPlayerTurn()
        {
            Deck.DiscardHand();
        }

        public int FirstLivingEnemyIndex()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive)
                {
                    return i;
                }
            }

            return -1;
        }

        public CardPlayCheck CheckPlay(int instanceId, int targetIndex)
        {
            if (IsOver)
            {
                return CardPlayCheck.CombatOver;
            }

            if (!Deck.TryGetFromHand(instanceId, out CardInstance card))
            {
                return CardPlayCheck.NotInHand;
            }

            if (!Prana.CanSpend(card.Cost))
            {
                return CardPlayCheck.NotEnoughPrana;
            }

            if (card.RequiresChosenTarget && !IsLivingEnemy(targetIndex))
            {
                return CardPlayCheck.InvalidTarget;
            }

            return CardPlayCheck.Playable;
        }

        public bool TryPlayCard(int instanceId, int targetIndex)
        {
            if (CheckPlay(instanceId, targetIndex) != CardPlayCheck.Playable)
            {
                return false;
            }

            Deck.TryGetFromHand(instanceId, out CardInstance card);
            ICombatant target = card.RequiresChosenTarget ? _enemyFighters[targetIndex] : null;

            Prana.TrySpend(card.Cost);
            Deck.BeginPlay(card);
            try
            {
                card.Definition.ApplyEffects(new EffectContext(Resolver, Player, target, _enemyFighters));
            }
            finally
            {
                Deck.FinishPlay(card);
            }

            CardPlayed?.Invoke(card, target);
            return true;
        }

        public bool EnemyTakeTurn(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= _enemies.Count || IsOver)
            {
                return false;
            }

            return _enemies[enemyIndex].TakeTurn(Resolver, Player, _playerSide);
        }

        private bool IsLivingEnemy(int index)
        {
            return index >= 0 && index < _enemies.Count && _enemies[index].IsAlive;
        }
    }
}
