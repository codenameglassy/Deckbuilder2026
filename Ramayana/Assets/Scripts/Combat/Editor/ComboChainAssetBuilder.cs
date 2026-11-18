// Creates the two combo chain kinds under Assets/_Project/Data/Combat/Chains: the Bow Chain (scales damage, saffron)
// and the Ring Chain (scales Kavach, blue), each with the ladder 100, 100, 150, 200, 300 percent, and links the cards
// that build them: Kodanda Strike and Kodanda Strike+ to the Bow Chain, Vajra Guard and Vajra Guard+ to the Ring
// Chain. Values are set only on assets this run creates, and a card's chain is filled only while it is empty, so a
// chain edited by hand is never changed. Runs after the Combat builder, which creates the four cards.
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class ComboChainAssetBuilder
    {
        public const string ChainsFolder = CombatAssetBuilder.Root + "/Chains";
        public const string BowChainPath = ChainsFolder + "/BowChain.asset";
        public const string RingChainPath = ChainsFolder + "/RingChain.asset";

        private static readonly int[] DefaultLadder = { 100, 100, 150, 200, 300 };
        private static readonly Color BowColor = new Color(1f, 0.62f, 0.2f, 1f);
        private static readonly Color RingColor = new Color(0.42f, 0.74f, 1f, 1f);

        private static readonly string[] BowCards = { "KodandaStrike", "KodandaStrikePlus" };
        private static readonly string[] RingCards = { "VajraGuard", "VajraGuardPlus" };

        [MenuItem(AssetBuilderConstants.MenuRoot + "Combo Chains", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Combo Chains", Build);
        }

        [DefaultAssetBuilder(118)]
        public static void Build(AssetBuilderReport report)
        {
            AssetBuilderUtility.EnsureFolder(ChainsFolder, report);

            ChainKindSO bow = Chain(BowChainPath, "bow", BowColor, true, false, report);
            ChainKindSO ring = Chain(RingChainPath, "ring", RingColor, false, true, report);

            LinkCards(BowCards, bow, report);
            LinkCards(RingCards, ring, report);
        }

        private static ChainKindSO Chain(string path, string id, Color color, bool scalesDamage, bool scalesKavach,
            AssetBuilderReport report)
        {
            return AssetBuilderSerialization.LoadOrCreateConfigured<ChainKindSO>(path, report, so =>
            {
                AssetBuilderSerialization.SetString(so, "_id", id);
                AssetBuilderSerialization.SetString(so, "_nameKey", "chain." + id + ".name");
                AssetBuilderSerialization.SetString(so, "_hintKey", "chain." + id + ".hint");
                AssetBuilderSerialization.Require(so, "_color").colorValue = color;
                AssetBuilderSerialization.Require(so, "_scalesDamage").boolValue = scalesDamage;
                AssetBuilderSerialization.Require(so, "_scalesKavach").boolValue = scalesKavach;

                SerializedProperty ladder = AssetBuilderSerialization.Require(so, "_multiplierPercents");
                ladder.arraySize = DefaultLadder.Length;
                for (int i = 0; i < DefaultLadder.Length; i++)
                {
                    ladder.GetArrayElementAtIndex(i).intValue = DefaultLadder[i];
                }
            });
        }

        private static void LinkCards(string[] cardFiles, ChainKindSO chain, AssetBuilderReport report)
        {
            if (chain == null)
            {
                report.AddSkipped($"Chain links for {string.Join(", ", cardFiles)} (chain asset missing)");
                return;
            }

            for (int i = 0; i < cardFiles.Length; i++)
            {
                string path = CombatAssetBuilder.CardsFolder + "/" + cardFiles[i] + ".asset";
                var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card == null)
                {
                    report.AddSkipped($"{path} (missing, run the Combat builder first)");
                    continue;
                }

                AssetBuilderUtility.WireAssetFieldIfNull(card, "_chain", chain, report);
            }
        }
    }
}
