// Gives every chapter one card its rarity from the reference build (Common, Rare or Epic, and Divine for Brahmastra,
// which reward rolls never offer), on the base card and its upgraded form alike, and creates the card reward odds:
// RewardOdds_Normal (60 Common, 33 Rare, 7 Epic) and RewardOdds_Elite (45, 42, 13). The elite odds go on the
// Maya-Marich encounter. A rarity is written only while the card's rarity is still None and a reference only while
// it is empty, so cards and encounters edited by hand are never changed. Runs after every card and encounter builder.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class CardRarityAssetBuilder
    {
        public const string NormalOddsPath = RewardCardsAssetBuilder.RewardsFolder + "/RewardOdds_Normal.asset";
        public const string EliteOddsPath = RewardCardsAssetBuilder.RewardsFolder + "/RewardOdds_Elite.asset";

        private const string UpgradedSuffix = "Plus";

        private static readonly string[] CardFiles =
        {
            "KodandaStrike",
            "VajraGuard",
            "TwinShafts",
            "VowOfDharma",
            "PiercingVow",
            "Meditation",
            "BreakTheIllusion",
            "LakshmanasWatch",
            "TerrorOfTheBow",
            "StormOfArrows",
            "WindforceVolley",
            "Agneyastra",
            "GaleArrow",
            "Vayavyastra",
            "AncientEndurance",
            "SagesCounsel",
            "SanjeevaniLeaf",
            "Tapasya",
            "BlessingOfAgni",
            "Devotion",
            "VowOfTheExile",
            "Brahmastra",
        };

        private static readonly CardRarity[] CardRarities =
        {
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Common,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Epic,
            CardRarity.Epic,
            CardRarity.Epic,
            CardRarity.Epic,
            CardRarity.Divine,
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Card Rarity", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Card Rarity", Build);
        }

        [DefaultAssetBuilder(117)]
        public static void Build(AssetBuilderReport report)
        {
            for (int i = 0; i < CardFiles.Length; i++)
            {
                string file = CardFiles[i];
                CardRarity rarity = CardRarities[i];
                string path = CombatAssetBuilder.CardsFolder + "/" + file + ".asset";
                var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card == null)
                {
                    report.AddSkipped($"{path} rarity (card missing, run the card builders first)");
                    continue;
                }

                SetRarityIfUnset(card, rarity, report);
                var upgraded = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(
                    CombatAssetBuilder.CardsFolder + "/" + file + UpgradedSuffix + ".asset");
                if (upgraded != null)
                {
                    SetRarityIfUnset(upgraded, rarity, report);
                }
            }

            AssetBuilderSerialization.LoadOrCreateConfigured<CardRewardOddsSO>(NormalOddsPath, report,
                so => SetWeights(so, 60, 33, 7));
            CardRewardOddsSO elite = AssetBuilderSerialization.LoadOrCreateConfigured<CardRewardOddsSO>(EliteOddsPath, report,
                so => SetWeights(so, 45, 42, 13));

            var maya = AssetDatabase.LoadAssetAtPath<CombatEncounterSO>(StatusEnemiesAssetBuilder.MayaMarichEncounterPath);
            if (maya != null && elite != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(maya, "_rewardOdds", elite, report);
            }
            else
            {
                report.AddSkipped($"{StatusEnemiesAssetBuilder.MayaMarichEncounterPath} reward odds (encounter missing, run Status Enemies first)");
            }
        }

        private static void SetWeights(SerializedObject odds, int common, int rare, int epic)
        {
            AssetBuilderSerialization.SetInt(odds, "_commonWeight", common);
            AssetBuilderSerialization.SetInt(odds, "_rareWeight", rare);
            AssetBuilderSerialization.SetInt(odds, "_epicWeight", epic);
        }

        private static void SetRarityIfUnset(CardDefinitionSO card, CardRarity rarity, AssetBuilderReport report)
        {
            using var serializedObject = new SerializedObject(card);
            SerializedProperty property = AssetBuilderSerialization.Require(serializedObject, "_rarity");
            if (property.enumValueIndex != (int)CardRarity.None)
            {
                return;
            }

            property.enumValueIndex = (int)rarity;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);
            report.AddWired($"{card.name}._rarity -> {rarity}");
        }
    }
}
