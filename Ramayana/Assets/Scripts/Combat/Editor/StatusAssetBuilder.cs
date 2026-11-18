// Creates the status data under Assets/_Project/Data/Combat/Statuses: the shared decay strategies (round timer,
// owner-turn duration, halving burn), the damage rules in their reference order (Focus adds, then Moha scales what
// its holder deals to 75%, then Bhaya scales what its holder takes to 125%), and the five statuses wired to them:
// Focus, Moha, Bhaya, Dahan and Dharma. Values are set only on assets this run creates; references are filled only
// while empty, and existing data is never changed. Icons are left empty for art to fill; until then the status UI
// shows each status's short label, whose key is filled in here when a status has none.
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.EditorTools;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.Combat.EditorTools
{
    public static class StatusAssetBuilder
    {
        public const string Root = CombatAssetBuilder.Root + "/Statuses";
        public const string DecayFolder = Root + "/Decay";
        public const string RulesFolder = Root + "/DamageRules";

        public const string FocusPath = Root + "/Focus.asset";
        public const string MohaPath = Root + "/Moha.asset";
        public const string BhayaPath = Root + "/Bhaya.asset";
        public const string DahanPath = Root + "/Dahan.asset";
        public const string DharmaPath = Root + "/Dharma.asset";

        private const int FocusOrder = 0;
        private const int MohaOrder = 10;
        private const int BhayaOrder = 20;

        [MenuItem(AssetBuilderConstants.MenuRoot + "Statuses", priority = AssetBuilderConstants.FeatureMenuPriority + 10)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("Statuses", Build);
        }

        [DefaultAssetBuilder(103)]
        public static void Build(AssetBuilderReport report)
        {
            TurnTimerDecaySO turnTimer = AssetBuilderSerialization.LoadOrCreateConfigured<TurnTimerDecaySO>(
                DecayFolder + "/TurnTimerDecay.asset", report, null);
            DurationDecaySO duration = AssetBuilderSerialization.LoadOrCreateConfigured<DurationDecaySO>(
                DecayFolder + "/DurationDecay.asset", report, null);
            BurnDecaySO burn = AssetBuilderSerialization.LoadOrCreateConfigured<BurnDecaySO>(
                DecayFolder + "/BurnDecay.asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_keepPercent", 50));

            FlatOutgoingDamageRuleSO focusRule = AssetBuilderSerialization.LoadOrCreateConfigured<FlatOutgoingDamageRuleSO>(
                RulesFolder + "/FocusDamageRule.asset", report,
                so => AssetBuilderSerialization.SetInt(so, "_order", FocusOrder));
            OutgoingDamageScaleRuleSO mohaRule = AssetBuilderSerialization.LoadOrCreateConfigured<OutgoingDamageScaleRuleSO>(
                RulesFolder + "/MohaDamageRule.asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_order", MohaOrder);
                    AssetBuilderSerialization.SetInt(so, "_percent", 75);
                });
            IncomingDamageScaleRuleSO bhayaRule = AssetBuilderSerialization.LoadOrCreateConfigured<IncomingDamageScaleRuleSO>(
                RulesFolder + "/BhayaDamageRule.asset", report, so =>
                {
                    AssetBuilderSerialization.SetInt(so, "_order", BhayaOrder);
                    AssetBuilderSerialization.SetInt(so, "_percent", 125);
                });

            Status(FocusPath, "focus", new Color(1f, 0.78f, 0.3f), false, 0, 1, duration, focusRule, report);
            Status(MohaPath, "moha", new Color(0.62f, 0.45f, 0.95f), true, 3, 0, turnTimer, mohaRule, report);
            Status(BhayaPath, "bhaya", new Color(0.88f, 0.28f, 0.32f), true, 3, 0, turnTimer, bhayaRule, report);
            Status(DahanPath, "dahan", new Color(1f, 0.5f, 0.15f), true, 0, 0, burn, null, report);
            Status(DharmaPath, "dharma", new Color(0.45f, 0.78f, 1f), false, 0, 0, null, null, report);
        }

        private static void Status(string path, string id, Color color, bool isDebuff, int maxAmount, int defaultDuration,
            StatusDecaySO decay, StatusDamageRuleSO rule, AssetBuilderReport report)
        {
            StatusDefinitionSO status = AssetBuilderSerialization.LoadOrCreateConfigured<StatusDefinitionSO>(path, report, so =>
            {
                AssetBuilderSerialization.SetString(so, "_id", id);
                AssetBuilderSerialization.SetString(so, "_nameKey", "status." + id + ".name");
                AssetBuilderSerialization.SetString(so, "_descriptionKey", "status." + id + ".description");
                AssetBuilderSerialization.SetString(so, "_shortNameKey", "status." + id + ".short");
                AssetBuilderSerialization.Require(so, "_color").colorValue = color;
                AssetBuilderSerialization.Require(so, "_isDebuff").boolValue = isDebuff;
                AssetBuilderSerialization.SetInt(so, "_maxAmount", maxAmount);
                AssetBuilderSerialization.SetInt(so, "_defaultDuration", defaultDuration);
            });

            FillStringIfEmpty(status, "_shortNameKey", "status." + id + ".short", report);

            if (decay != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(status, "_decay", decay, report);
            }

            if (rule != null)
            {
                AssetBuilderUtility.WireAssetFieldIfNull(status, "_damageRule", rule, report);
            }
        }

        private static void FillStringIfEmpty(Object owner, string propertyPath, string value, AssetBuilderReport report)
        {
            if (owner == null)
            {
                return;
            }

            using var serializedObject = new SerializedObject(owner);
            SerializedProperty property = AssetBuilderSerialization.Require(serializedObject, propertyPath);
            if (!string.IsNullOrEmpty(property.stringValue))
            {
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
            report.AddWired($"{owner.name}.{propertyPath} -> {value}");
        }
    }
}
