// Creates the combat slice data under Assets/_Project/Data/Combat: card types, targeting rules, effects,
// Kodanda Strike and Vajra Guard with their upgraded forms, the Nishachar Scout and its three moves,
// the first encounter, the slice starting deck (5 Strike, 5 Guard), the combat rules and the player definition.
// Values come from the reference build's balance pass. They are set only on assets this run creates;
// references and lists are filled only while empty, and existing data is never changed.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class CombatAssetBuilder
    {
        public const string Root = AssetBuilderConstants.DataRoot + "/Combat";
        public const string CardTypesFolder = Root + "/CardTypes";
        public const string TargetingFolder = Root + "/Targeting";
        public const string EffectsFolder = Root + "/Effects";
        public const string CardsFolder = Root + "/Cards";
        public const string EnemiesFolder = Root + "/Enemies";
        public const string EnemyMovesFolder = EnemiesFolder + "/Moves";
        public const string EncountersFolder = Root + "/Encounters";
        public const string DecksFolder = Root + "/Decks";

        public const string CombatRulesPath = Root + "/CombatRules.asset";
        public const string PlayerPath = Root + "/Player.asset";
        public const string StartingDeckPath = DecksFolder + "/StartingDeck_Slice.asset";
        public const string ScoutEncounterPath = EncountersFolder + "/Encounter_Scout.asset";

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combat", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combat", Build);
        }

        [DefaultAssetBuilder(100)]
        public static void Build(AssetBuilderReport report)
        {
            CardTypeSO attackType = CardType("Attack", "attack", report);
            CardTypeSO skillType = CardType("Skill", "skill", report);

            ChosenTargetRuleSO chosenTarget = AssetBuilderSerialization.LoadOrCreateConfigured<ChosenTargetRuleSO>(
                TargetingFolder + "/ChosenTarget.asset", report, null);
            AssetBuilderSerialization.LoadOrCreateConfigured<AllOpponentsTargetRuleSO>(
                TargetingFolder + "/AllOpponents.asset", report, null);

            DamageEffectSO damage6 = Damage("Damage_6", 6, 1, chosenTarget, report);
            DamageEffectSO damage8 = Damage("Damage_8", 8, 1, chosenTarget, report);
            DamageEffectSO damage7 = Damage("Damage_7", 7, 1, chosenTarget, report);
            DamageEffectSO damage4x2 = Damage("Damage_4x2", 4, 2, chosenTarget, report);
            BlockEffectSO block5 = Block("Block_5", 5, report);
            BlockEffectSO block8 = Block("Block_8", 8, report);
            BlockEffectSO block6 = Block("Block_6", 6, report);

            CardDefinitionSO strikePlus = Card("KodandaStrikePlus", "strike_plus", 1, attackType, null, report, damage8);
            CardDefinitionSO strike = Card("KodandaStrike", "strike", 1, attackType, strikePlus, report, damage6);
            CardDefinitionSO guardPlus = Card("VajraGuardPlus", "guard_plus", 1, skillType, null, report, block8);
            CardDefinitionSO guard = Card("VajraGuard", "guard", 1, skillType, guardPlus, report, block5);

            EnemyMoveSO scoutAttack7 = Move("NishacharScout_Attack7", "scout_attack_7", report, damage7);
            EnemyMoveSO scoutAttack4x2 = Move("NishacharScout_Attack4x2", "scout_attack_4x2", report, damage4x2);
            EnemyMoveSO scoutDefend6 = Move("NishacharScout_Defend6", "scout_defend_6", report, block6);
            EnemyDefinitionSO scout = Enemy("NishacharScout", "scout", 30, 34, 0, report,
                scoutAttack7, scoutAttack4x2, scoutDefend6);

            CombatEncounterSO encounter = AssetBuilderSerialization.LoadOrCreateConfigured<CombatEncounterSO>(
                ScoutEncounterPath, report,
                so => AssetBuilderSerialization.SetString(so, "_id", "encounter_scout"));
            AssetBuilderSerialization.FillObjectArrayIfEmpty(encounter, "_enemies", new UnityEngine.Object[] { scout }, report);

            StartingDeckSO deck = AssetBuilderSerialization.LoadOrCreateConfigured<StartingDeckSO>(StartingDeckPath, report, null);
            FillDeck(deck, report, strike, 5, guard, 5);

            AssetBuilderSerialization.LoadOrCreateConfigured<CombatRulesSO>(CombatRulesPath, report, so =>
            {
                AssetBuilderSerialization.SetInt(so, "_pranaPerTurn", 3);
                AssetBuilderSerialization.SetInt(so, "_cardsPerTurn", 5);
                AssetBuilderSerialization.SetInt(so, "_maxHandSize", 10);
            });

            PlayerDefinitionSO player = AssetBuilderSerialization.LoadOrCreateConfigured<PlayerDefinitionSO>(PlayerPath, report, so =>
            {
                AssetBuilderSerialization.SetString(so, "_nameKey", "character.rama.name");
                AssetBuilderSerialization.SetInt(so, "_maxHealth", 78);
            });
            AssetBuilderUtility.WireAssetFieldIfNull(player, "_startingDeck", deck, report);
        }

        private static CardTypeSO CardType(string fileName, string id, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<CardTypeSO>(CardTypesFolder + "/" + fileName + ".asset", report, so =>
            {
                AssetBuilderSerialization.SetString(so, "_id", id);
                AssetBuilderSerialization.SetString(so, "_nameKey", "card.type." + id);
            });
        }

        private static DamageEffectSO Damage(string fileName, int amount, int hits, TargetRuleSO targets, AssetBuilderReport report)
        {
            DamageEffectSO effect = AssetBuilderSerialization.LoadOrCreateConfigured<DamageEffectSO>(
                EffectsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_amount", amount);
                    AssetBuilderSerialization.SetInt(so, "_hits", hits);
                });
            AssetBuilderUtility.WireAssetFieldIfNull(effect, "_targets", targets, report);
            return effect;
        }

        private static BlockEffectSO Block(string fileName, int amount, AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<BlockEffectSO>(
                EffectsFolder + "/" + fileName + ".asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_amount", amount));
        }

        private static CardDefinitionSO Card(string fileName, string id, int cost, CardTypeSO type,
            CardDefinitionSO upgradedForm, AssetBuilderReport report, params CombatEffectSO[] effects)
        {
            CardDefinitionSO card = AssetBuilderSerialization.LoadOrCreateConfigured<CardDefinitionSO>(
                CardsFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetString(so, "_id", id);
                    AssetBuilderSerialization.SetString(so, "_nameKey", "card." + id + ".name");
                    AssetBuilderSerialization.SetString(so, "_descriptionKey", "card." + id + ".description");
                    AssetBuilderSerialization.SetInt(so, "_cost", cost);
                });

            AssetBuilderUtility.WireAssetFieldIfNull(card, "_type", type, report);
            if (upgradedForm != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(card, "_upgradedForm", upgradedForm, report);
            }

            AssetBuilderSerialization.FillObjectArrayIfEmpty(card, "_effects", effects, report);
            return card;
        }

        private static EnemyMoveSO Move(string fileName, string id, AssetBuilderReport report, params CombatEffectSO[] effects)
        {
            EnemyMoveSO move = AssetBuilderSerialization.LoadOrCreateConfigured<EnemyMoveSO>(
                EnemyMovesFolder + "/" + fileName + ".asset", report,
                so => AssetBuilderSerialization.SetString(so, "_id", id));
            AssetBuilderSerialization.FillObjectArrayIfEmpty(move, "_effects", effects, report);
            return move;
        }

        private static EnemyDefinitionSO Enemy(string fileName, string id, int minHealth, int maxHealth, int startMoveIndex,
            AssetBuilderReport report, params EnemyMoveSO[] moves)
        {
            EnemyDefinitionSO enemy = AssetBuilderSerialization.LoadOrCreateConfigured<EnemyDefinitionSO>(
                EnemiesFolder + "/" + fileName + ".asset", report, so =>
                {
                    AssetBuilderSerialization.SetString(so, "_id", id);
                    AssetBuilderSerialization.SetString(so, "_nameKey", "enemy." + id + ".name");
                    AssetBuilderSerialization.SetInt(so, "_minHealth", minHealth);
                    AssetBuilderSerialization.SetInt(so, "_maxHealth", maxHealth);
                    AssetBuilderSerialization.SetInt(so, "_startMoveIndex", startMoveIndex);
                });
            AssetBuilderSerialization.FillObjectArrayIfEmpty(enemy, "_moves", moves, report);
            return enemy;
        }

        private static void FillDeck(StartingDeckSO deck, AssetBuilderReport report,
            CardDefinitionSO firstCard, int firstCount, CardDefinitionSO secondCard, int secondCount)
        {
            if (firstCard == null || secondCard == null)
            {
                report.AddSkipped($"{StartingDeckPath} entries (a card definition is missing)");
                return;
            }

            CardDefinitionSO[] cards = { firstCard, secondCard };
            int[] counts = { firstCount, secondCount };

            AssetBuilderSerialization.FillArrayIfEmpty(deck, "_entries", cards.Length, (element, index) =>
            {
                AssetBuilderSerialization.RequireRelative(element, "_card").objectReferenceValue = cards[index];
                AssetBuilderSerialization.RequireRelative(element, "_count").intValue = counts[index];
            }, report);
        }
    }
}
