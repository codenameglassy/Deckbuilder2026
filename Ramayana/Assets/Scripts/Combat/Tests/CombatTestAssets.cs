// Creates the definition assets the combat model tests need (cards, effects, enemies, decks, encounters, rules)
// in memory, configured through SerializedObject, and destroys them all in one call from a test's TearDown.
// Fields are set in the order a designer fills them, because OnValidate runs on each applied change and clamps
// against fields set earlier (an enemy's start move index is clamped to its move count, so moves go first).
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using UnityEngine;

namespace JourneyToLanka.Combat.Tests
{
    public sealed class CombatTestAssets
    {
        private readonly List<Object> _created = new List<Object>();

        public T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        public DamageEffectSO Damage(int amount, int hits = 1)
        {
            DamageEffectSO effect = Create<DamageEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            SerializedTestUtility.SetInt(effect, "_hits", hits);
            SerializedTestUtility.SetObject(effect, "_targets", Create<ChosenTargetRuleSO>());
            return effect;
        }

        public BlockEffectSO Block(int amount)
        {
            BlockEffectSO effect = Create<BlockEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            return effect;
        }

        public CardDefinitionSO Card(int cost, params CombatEffectSO[] effects)
        {
            CardDefinitionSO card = Create<CardDefinitionSO>();
            SerializedTestUtility.SetInt(card, "_cost", cost);
            SerializedTestUtility.SetObjectArray(card, "_effects", effects);
            return card;
        }

        public EnemyMoveSO Move(params CombatEffectSO[] effects)
        {
            EnemyMoveSO move = Create<EnemyMoveSO>();
            SerializedTestUtility.SetObjectArray(move, "_effects", effects);
            return move;
        }

        public EnemyDefinitionSO Enemy(int minHealth, int maxHealth, int startMoveIndex, params EnemyMoveSO[] moves)
        {
            EnemyDefinitionSO enemy = Create<EnemyDefinitionSO>();
            SerializedTestUtility.SetInt(enemy, "_minHealth", minHealth);
            SerializedTestUtility.SetInt(enemy, "_maxHealth", maxHealth);
            SerializedTestUtility.SetObjectArray(enemy, "_moves", moves);
            SerializedTestUtility.SetInt(enemy, "_startMoveIndex", startMoveIndex);
            return enemy;
        }

        public StartingDeckSO Deck(CardDefinitionSO[] cards, int[] counts)
        {
            StartingDeckSO deck = Create<StartingDeckSO>();
            SerializedTestUtility.SetDeckEntries(deck, "_entries", cards, counts);
            return deck;
        }

        public PlayerDefinitionSO Player(int maxHealth, StartingDeckSO deck)
        {
            PlayerDefinitionSO player = Create<PlayerDefinitionSO>();
            SerializedTestUtility.SetInt(player, "_maxHealth", maxHealth);
            SerializedTestUtility.SetObject(player, "_startingDeck", deck);
            return player;
        }

        public CombatEncounterSO Encounter(params EnemyDefinitionSO[] enemies)
        {
            CombatEncounterSO encounter = Create<CombatEncounterSO>();
            SerializedTestUtility.SetObjectArray(encounter, "_enemies", enemies);
            return encounter;
        }

        public CombatRulesSO Rules(int pranaPerTurn, int cardsPerTurn, int maxHandSize)
        {
            CombatRulesSO rules = Create<CombatRulesSO>();
            SerializedTestUtility.SetInt(rules, "_pranaPerTurn", pranaPerTurn);
            SerializedTestUtility.SetInt(rules, "_cardsPerTurn", cardsPerTurn);
            SerializedTestUtility.SetInt(rules, "_maxHandSize", maxHandSize);
            return rules;
        }

        public void DestroyAll()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }
    }
}
