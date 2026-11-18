// Creates the definition assets the combat model tests need (cards, effects, statuses, enemies, decks, encounters,
// rules) in memory, configured through SerializedObject, and destroys them all in one call from a test's TearDown.
// Fields are set in the order a designer fills them, because OnValidate runs on each applied change and clamps
// against fields set earlier (an enemy's start move index is clamped to its move count, so moves go first).
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Statuses;
using UnityEditor;
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

        public ArrowDefinitionSO Arrow(params CombatEffectSO[] effects)
        {
            ArrowDefinitionSO arrow = Create<ArrowDefinitionSO>();
            SerializedTestUtility.SetObjectArray(arrow, "_effects", effects);
            return arrow;
        }

        public ArrowDefinitionSO Arrow(ArrowTier tier, params CombatEffectSO[] effects)
        {
            ArrowDefinitionSO arrow = Arrow(effects);
            using var serializedObject = new SerializedObject(arrow);
            serializedObject.FindProperty("_tier").enumValueIndex = (int)tier;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return arrow;
        }

        public ArrowDropTableSO DropTable(int startChance, int step, int minChance, int maxChance, int common, int uncommon,
            int rare, params ArrowDefinitionSO[] arrows)
        {
            ArrowDropTableSO table = Create<ArrowDropTableSO>();
            using (var serializedObject = new SerializedObject(table))
            {
                serializedObject.FindProperty("_startChance").intValue = startChance;
                serializedObject.FindProperty("_chanceStep").intValue = step;
                serializedObject.FindProperty("_minChance").intValue = minChance;
                serializedObject.FindProperty("_maxChance").intValue = maxChance;
                serializedObject.FindProperty("_commonWeight").intValue = common;
                serializedObject.FindProperty("_uncommonWeight").intValue = uncommon;
                serializedObject.FindProperty("_rareWeight").intValue = rare;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SerializedTestUtility.SetObjectArray(table, "_arrows", arrows);
            return table;
        }

        public DamageEffectSO DamageAll(int amount)
        {
            DamageEffectSO effect = Create<DamageEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            SerializedTestUtility.SetInt(effect, "_hits", 1);
            SerializedTestUtility.SetObject(effect, "_targets", Create<AllOpponentsTargetRuleSO>());
            return effect;
        }

        public DrawCardsEffectSO Draw(int count)
        {
            DrawCardsEffectSO effect = Create<DrawCardsEffectSO>();
            SerializedTestUtility.SetInt(effect, "_count", count);
            return effect;
        }

        public GainPranaEffectSO GainPrana(int amount)
        {
            GainPranaEffectSO effect = Create<GainPranaEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            return effect;
        }

        public HealPercentEffectSO HealPercent(int percent)
        {
            HealPercentEffectSO effect = Create<HealPercentEffectSO>();
            SerializedTestUtility.SetInt(effect, "_percent", percent);
            return effect;
        }

        public RemoveStatusEffectSO RemoveStatus(StatusDefinitionSO status)
        {
            RemoveStatusEffectSO effect = Create<RemoveStatusEffectSO>();
            SerializedTestUtility.SetObject(effect, "_status", status);
            return effect;
        }

        public BlockEffectSO RetainedBlock(int amount)
        {
            BlockEffectSO effect = Block(amount);
            SetBool(effect, "_retain", true);
            return effect;
        }

        public HealEffectSO Heal(int amount)
        {
            HealEffectSO effect = Create<HealEffectSO>();
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            return effect;
        }

        public DamageEffectSO DamageWithStatusBonus(int amount, StatusDefinitionSO status, int threshold, int bonus)
        {
            StatusThresholdDamageBonusSO bonusRule = Create<StatusThresholdDamageBonusSO>();
            SerializedTestUtility.SetObject(bonusRule, "_status", status);
            SerializedTestUtility.SetInt(bonusRule, "_threshold", threshold);
            SerializedTestUtility.SetInt(bonusRule, "_bonus", bonus);
            DamageEffectSO effect = Damage(amount);
            SerializedTestUtility.SetObject(effect, "_bonus", bonusRule);
            return effect;
        }

        public SpendStatusDamageEffectSO SpendStatusDamage(StatusDefinitionSO status, int baseDamage, int damagePerStack)
        {
            SpendStatusDamageEffectSO effect = Create<SpendStatusDamageEffectSO>();
            SerializedTestUtility.SetObject(effect, "_status", status);
            SerializedTestUtility.SetInt(effect, "_baseDamage", baseDamage);
            SerializedTestUtility.SetInt(effect, "_damagePerStack", damagePerStack);
            SerializedTestUtility.SetObject(effect, "_targets", Create<ChosenTargetRuleSO>());
            return effect;
        }

        public CardDefinitionSO ExhaustingCard(int cost, params CombatEffectSO[] effects)
        {
            CardDefinitionSO card = Card(cost, effects);
            SetBool(card, "_exhausts", true);
            return card;
        }

        public ApplyStatusEffectSO ApplyStatus(StatusDefinitionSO status, int amount, int duration, bool toChosenTarget)
        {
            ApplyStatusEffectSO effect = Create<ApplyStatusEffectSO>();
            SerializedTestUtility.SetObject(effect, "_status", status);
            SerializedTestUtility.SetInt(effect, "_amount", amount);
            SerializedTestUtility.SetInt(effect, "_duration", duration);
            if (toChosenTarget)
            {
                SerializedTestUtility.SetObject(effect, "_targets", Create<ChosenTargetRuleSO>());
            }

            return effect;
        }

        public StatusDefinitionSO Status(string fileName, int maxAmount, bool isDebuff, StatusDecaySO decay, StatusDamageRuleSO rule)
        {
            StatusDefinitionSO status = Create<StatusDefinitionSO>();
            status.name = fileName;
            using var serializedObject = new SerializedObject(status);
            serializedObject.FindProperty("_id").stringValue = fileName;
            serializedObject.FindProperty("_isDebuff").boolValue = isDebuff;
            serializedObject.FindProperty("_maxAmount").intValue = maxAmount;
            serializedObject.FindProperty("_decay").objectReferenceValue = decay;
            serializedObject.FindProperty("_damageRule").objectReferenceValue = rule;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return status;
        }

        public StatusDefinitionSO Power(string fileName, int maxAmount, StatusDamageRuleSO rule, StatusTriggerSO trigger)
        {
            StatusDefinitionSO status = Status(fileName, maxAmount, false, null, rule);
            using var serializedObject = new SerializedObject(status);
            serializedObject.FindProperty("_trigger").objectReferenceValue = trigger;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return status;
        }

        public TurnStartGainStatusTriggerSO TurnStartGain(StatusDefinitionSO status, int amount)
        {
            TurnStartGainStatusTriggerSO trigger = Create<TurnStartGainStatusTriggerSO>();
            SerializedTestUtility.SetObject(trigger, "_status", status);
            SerializedTestUtility.SetInt(trigger, "_amount", amount);
            return trigger;
        }

        public StatusGainedBlockTriggerSO GainedBlock(StatusDefinitionSO status, int blockPerStack)
        {
            StatusGainedBlockTriggerSO trigger = Create<StatusGainedBlockTriggerSO>();
            SerializedTestUtility.SetObject(trigger, "_status", status);
            SerializedTestUtility.SetInt(trigger, "_blockPerStack", blockPerStack);
            return trigger;
        }

        public RemoveBlockEffectSO RemoveBlock()
        {
            RemoveBlockEffectSO effect = Create<RemoveBlockEffectSO>();
            SerializedTestUtility.SetObject(effect, "_targets", Create<ChosenTargetRuleSO>());
            return effect;
        }

        public BurnDecaySO BurnDecay(int keepPercent)
        {
            BurnDecaySO decay = Create<BurnDecaySO>();
            SerializedTestUtility.SetInt(decay, "_keepPercent", keepPercent);
            return decay;
        }

        public FlatOutgoingDamageRuleSO FlatOutgoing(int order)
        {
            FlatOutgoingDamageRuleSO rule = Create<FlatOutgoingDamageRuleSO>();
            SerializedTestUtility.SetInt(rule, "_order", order);
            return rule;
        }

        public OutgoingDamageScaleRuleSO OutgoingScale(int order, int percent)
        {
            OutgoingDamageScaleRuleSO rule = Create<OutgoingDamageScaleRuleSO>();
            SerializedTestUtility.SetInt(rule, "_order", order);
            SerializedTestUtility.SetInt(rule, "_percent", percent);
            return rule;
        }

        public IncomingDamageScaleRuleSO IncomingScale(int order, int percent)
        {
            IncomingDamageScaleRuleSO rule = Create<IncomingDamageScaleRuleSO>();
            SerializedTestUtility.SetInt(rule, "_order", order);
            SerializedTestUtility.SetInt(rule, "_percent", percent);
            return rule;
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

        private static void SetBool(Object target, string propertyPath, bool value)
        {
            using var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyPath).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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
