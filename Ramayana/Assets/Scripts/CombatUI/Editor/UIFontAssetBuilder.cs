// Wires the UI font into every empty font field on the UI style assets: the Combat HUD style (which the chain bar,
// quiver, inspect panels and cards also read), the card reward style, the combatant view style, and both fonts on the
// combat feedback style (damage numbers and enemy intents). While a field is empty its views keep the TMP Settings
// default font, so filling them is what switches the game's text. Only empty fields are filled; to change a font that
// is already assigned, use Tools/Journey to Lanka/UI Fonts/Apply Selected Font to UI Styles. Runs after every UI style
// builder.
using JourneyToLanka.CombatScene.EditorTools;
using JourneyToLanka.Core.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatUI.EditorTools
{
    public static class UIFontAssetBuilder
    {
        public const string FontPath = "Assets/Arts/Cozy Fonts/bunny-snaps/BunnySnaps SDF.asset";

        public static readonly (string AssetPath, string FieldName)[] FontFields =
        {
            (CombatUIAssetBuilder.StylePath, "_font"),
            (CardRewardAssetBuilder.StylePath, "_font"),
            (CombatSceneAssetBuilder.ViewStylePath, "_font"),
            (CombatFeedbackAssetBuilder.FeedbackStylePath, "_numberFont"),
            (CombatFeedbackAssetBuilder.FeedbackStylePath, "_intentFont"),
        };

        [MenuItem(AssetBuilderConstants.MenuRoot + "UI Fonts", priority = AssetBuilderConstants.FeatureMenuPriority + 40)]
        private static void BuildFromMenu()
        {
            AssetBuilderRunner.Run("UI Fonts", Build);
        }

        [DefaultAssetBuilder(190)]
        public static void Build(AssetBuilderReport report)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                report.AddSkipped($"{FontPath} (no TMP font asset at this path)");
                return;
            }

            for (int i = 0; i < FontFields.Length; i++)
            {
                string assetPath = FontFields[i].AssetPath;
                Object style = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (style == null)
                {
                    report.AddSkipped($"{assetPath} (style asset is missing, run its builder first)");
                    continue;
                }

                AssetBuilderUtility.WireAssetFieldIfNull(style, FontFields[i].FieldName, font, report);
            }
        }
    }
}
