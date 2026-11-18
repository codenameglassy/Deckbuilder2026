// The whole state of one fight in plain C#: Rama's Combatant, the enemies in field order, the deck, Prana and the
// resolver. It implements the turn rules (refill Prana, draw, Kavach clears from turn 2, discard the hand at end of
// turn), card play validation and resolution, and one enemy action at a time. It holds no timing: the combat state
// machine decides when each step runs, and views subscribe to the events of the parts they show.
// Events raised inside the constructor have no listeners, so views read initial state after construction.
// A card marked Exhausts goes to the exhaust pile after it resolves instead of the discard pile, and Kavach marked
// to be retained survives the next turn start once.
// Rama comes in as a PlayerSetup, so a run can pass in carried health and its current deck.
// Special arrows fire from Rama's quiver (the run's own, so a fired arrow is gone for good): any time on his turn,
// for no Prana, one at a time through CheckArrow and TryFireArrow, and PreviewArrow dry-runs one like a card.
// PreviewPlay dry-runs a card into a CombatOutcomePreview (no Prana spent, no card moved, no fighter changed), so
// the HUD can show what it would do before it is played.
// At the start of Rama's turn, after his Kavach clears and Prana refills and before he draws, his status triggers run
// (Vow of the Exile gains Dharma, which Devotion turns into Kavach), as in the reference build.
// Statuses follow the reference build's timing: Rama's statuses tick at the end of his turn (his Focus ends), each
// enemy's after its own action (an enemy's Focus lasts through its next action), and every fighter's at the end of
// the round, after all enemies have acted (Dahan burns, Bhaya and Moha count down). All statuses reach damage
// through one StatusDamageModifier, added first, and any status change raises the resolver's ModifiersChanged.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Statuses;

namespace JourneyToLanka.Combat.Model
{
    public sealed class CombatModel
    {
        private readonly List<EnemyCombatant> _enemies;
        private readonly List<ICombatant> _enemyFighters;
        private readonly ICombatant[] _playerSide;
        private readonly int _cardsPerTurn;
        private readonly StatusDamageModifier _statusModifier = new StatusDamageModifier();
        private readonly Action<StatusSet> _statusesChanged;

        public CombatModel(CombatRulesSO rules, PlayerDefinitionSO player, int playerHealth, CombatEncounterSO encounter, CombatRandom random)
            : this(rules, PlayerSetup.FromDefinition(player, playerHealth), encounter, random)
        {
        }

        public CombatModel(CombatRulesSO rules, in PlayerSetup player, CombatEncounterSO encounter, CombatRandom random)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (player.Deck == null)
            {
                throw new ArgumentException("The player setup has no deck.", nameof(player));
            }

            Random = random;
            _cardsPerTurn = rules.CardsPerTurn;

            Player = new Combatant(player.NameKey, player.MaxHealth, player.Health);
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
            Resolver.AddModifier(_statusModifier);
            _statusesChanged = HandleStatusesChanged;
            Player.Statuses.Changed += _statusesChanged;
            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].Fighter.Statuses.Changed += _statusesChanged;
            }

            Prana = new PranaPool(rules.PranaPerTurn);
            Deck = new CombatDeck(random, rules.MaxHandSize);
            Deck.AddCards(player.Deck);
            Deck.ShuffleDrawPile();
            Resolver.AttachDeck(Deck);
            Resolver.AttachPrana(Prana);
            Quiver = player.Quiver ?? new Quiver(0);
        }

        public event Action<int> PlayerTurnStarted;

        // Card, the chosen target (null for untargeted cards).
        public event Action<CardInstance, ICombatant> CardPlayed;

        // Arrow, the chosen target (null for untargeted arrows).
        public event Action<ArrowDefinitionSO, ICombatant> ArrowFired;

        // Raised after the end-of-round status ticks, before the next player turn starts.
        public event Action RoundEnded;

        public CombatRandom Random { get; }

        public Combatant Player { get; }

        public IReadOnlyList<EnemyCombatant> Enemies => _enemies;

        public IReadOnlyList<ICombatant> EnemyFighters => _enemyFighters;

        public CombatDeck Deck { get; }

        public PranaPool Prana { get; }

        public Quiver Quiver { get; }

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
                Player.ClearBlockAtTurnStart();
            }

            Prana.Refill();
            Player.Statuses.TriggerOwnerTurnStart(new StatusTickContext(Resolver, Player));
            Deck.Draw(_cardsPerTurn);
            PlayerTurnStarted?.Invoke(TurnNumber);
        }

        public void EndPlayerTurn()
        {
            Deck.DiscardHand();
            Player.Statuses.TickOwnerTurnEnd();
        }

        public void EndRound()
        {
            if (IsOver)
            {
                return;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                Combatant fighter = _enemies[i].Fighter;
                if (fighter.IsAlive)
                {
                    fighter.Statuses.TickRoundEnd(new StatusTickContext(Resolver, fighter));
                }
            }

            if (Player.IsAlive)
            {
                Player.Statuses.TickRoundEnd(new StatusTickContext(Resolver, Player));
            }

            RoundEnded?.Invoke();
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
                if (card.Definition.Exhausts)
                {
                    Deck.ExhaustFromPlay(card);
                }
                else
                {
                    Deck.FinishPlay(card);
                }
            }

            CardPlayed?.Invoke(card, target);
            return true;
        }

        public void PreviewPlay(CardInstance card, int targetIndex, CombatOutcomePreview outcome)
        {
            if (outcome == null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            outcome.Begin(Resolver, _statusModifier);
            if (card == null || IsOver)
            {
                return;
            }

            ICombatant target = null;
            if (card.RequiresChosenTarget)
            {
                if (!IsLivingEnemy(targetIndex))
                {
                    return;
                }

                target = _enemyFighters[targetIndex];
            }

            card.Definition.ApplyEffects(new EffectContext(outcome, Player, target, _enemyFighters));
        }

        public ArrowUseCheck CheckArrow(int slot, int targetIndex)
        {
            if (IsOver)
            {
                return ArrowUseCheck.CombatOver;
            }

            if (!Quiver.HasArrow(slot))
            {
                return ArrowUseCheck.NoArrow;
            }

            if (Quiver[slot].RequiresChosenTarget && !IsLivingEnemy(targetIndex))
            {
                return ArrowUseCheck.InvalidTarget;
            }

            return ArrowUseCheck.Usable;
        }

        public bool TryFireArrow(int slot, int targetIndex)
        {
            if (CheckArrow(slot, targetIndex) != ArrowUseCheck.Usable)
            {
                return false;
            }

            ArrowDefinitionSO arrow = Quiver.RemoveAt(slot);
            ICombatant target = arrow.RequiresChosenTarget ? _enemyFighters[targetIndex] : null;
            arrow.ApplyEffects(new EffectContext(Resolver, Player, target, _enemyFighters));
            ArrowFired?.Invoke(arrow, target);
            return true;
        }

        public void PreviewArrow(int slot, int targetIndex, CombatOutcomePreview outcome)
        {
            if (outcome == null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            outcome.Begin(Resolver, _statusModifier);
            if (IsOver || !Quiver.HasArrow(slot))
            {
                return;
            }

            ArrowDefinitionSO arrow = Quiver[slot];
            ICombatant target = null;
            if (arrow.RequiresChosenTarget)
            {
                if (!IsLivingEnemy(targetIndex))
                {
                    return;
                }

                target = _enemyFighters[targetIndex];
            }

            arrow.ApplyEffects(new EffectContext(outcome, Player, target, _enemyFighters));
        }

        public bool EnemyTakeTurn(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= _enemies.Count || IsOver)
            {
                return false;
            }

            EnemyCombatant enemy = _enemies[enemyIndex];
            if (!enemy.TakeTurn(Resolver, Player, _playerSide))
            {
                return false;
            }

            enemy.Fighter.Statuses.TickOwnerTurnEnd();
            return true;
        }

        private void HandleStatusesChanged(StatusSet statuses)
        {
            Resolver.NotifyModifiersChanged();
        }

        private bool IsLivingEnemy(int index)
        {
            return index >= 0 && index < _enemies.Count && _enemies[index].IsAlive;
        }
    }
}
