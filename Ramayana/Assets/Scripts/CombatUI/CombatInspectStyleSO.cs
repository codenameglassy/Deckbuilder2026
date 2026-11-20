// Every visual value for the combat inspect widgets: the pile viewer (backdrop, panel, title and count, grid, empty
// text, Close button, fade) and the fighter info panel (how far above the fighter it sits, its screen offset and edge
// margin, size, colours, text sizes, and the size and colour of each status's description). Text uses the Combat HUD
// style's font so every combat widget shares one typeface. Sprites are optional; without one an element draws as a flat
// colour. Read-only at runtime. In the Editor, changing a value raises Changed so the widgets re-apply it in Play mode
// without rebuilding.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatUI
{
    [CreateAssetMenu(menuName = "Journey to Lanka/UI/Combat Inspect Style", fileName = "CombatInspectStyle")]
    public sealed class CombatInspectStyleSO : ScriptableObject
    {
        [Header("Pile Viewer")]
        [SerializeField] private Color _viewerBackdropColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Sprite _viewerPanelSprite;
        [SerializeField] private Color _viewerPanelColor = new Color(0.08f, 0.07f, 0.06f, 0.96f);
        [SerializeField] private Vector2 _viewerPanelSize = new Vector2(1560f, 900f);
        [SerializeField, Min(0f)] private float _viewerPadding = 32f;
        [SerializeField, Min(1f)] private float _viewerHeaderHeight = 72f;
        [SerializeField, Min(1f)] private float _viewerTitleFontSize = 44f;
        [SerializeField] private Color _viewerTitleColor = new Color(0.98f, 0.85f, 0.45f);
        [SerializeField, Min(1f)] private float _viewerCountFontSize = 36f;
        [SerializeField] private Color _viewerCountColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField, Min(1)] private int _viewerColumns = 6;
        [SerializeField] private Vector2 _viewerSpacing = new Vector2(24f, 24f);
        [SerializeField, Min(1f)] private float _viewerScrollSensitivity = 40f;
        [SerializeField, Min(1f)] private float _viewerEmptyFontSize = 32f;
        [SerializeField] private Color _viewerEmptyColor = new Color(0.75f, 0.7f, 0.62f);
        [SerializeField] private Vector2 _viewerCloseSize = new Vector2(220f, 64f);
        [SerializeField] private Sprite _viewerCloseSprite;
        [SerializeField] private Color _viewerCloseColor = new Color(0.55f, 0.2f, 0.12f);
        [SerializeField] private Color _viewerCloseTextColor = new Color(0.98f, 0.94f, 0.85f);
        [SerializeField, Min(1f)] private float _viewerCloseFontSize = 28f;
        [SerializeField, Min(0.01f)] private float _viewerFadeDuration = 0.15f;

        [Header("Enemy Info")]
        [SerializeField] private float _infoWorldLift = 1.7f;
        [SerializeField] private Vector2 _infoScreenOffset = new Vector2(0f, 8f);
        [SerializeField, Min(0f)] private float _infoScreenMargin = 16f;
        [SerializeField, Min(1f)] private float _infoWidth = 260f;
        [SerializeField, Min(0f)] private float _infoPadding = 12f;
        [SerializeField, Min(0f)] private float _infoSpacing = 2f;
        [SerializeField] private Sprite _infoBackgroundSprite;
        [SerializeField] private Color _infoBackgroundColor = new Color(0.08f, 0.07f, 0.06f, 0.92f);
        [SerializeField, Min(1f)] private float _infoNameFontSize = 28f;
        [SerializeField] private Color _infoNameColor = new Color(0.98f, 0.85f, 0.45f);
        [SerializeField, Min(1f)] private float _infoRowFontSize = 24f;
        [SerializeField, Min(1f)] private float _infoRowHeight = 32f;
        [SerializeField] private Color _infoTextColor = new Color(0.96f, 0.91f, 0.8f);
        [SerializeField] private Color _infoHealthColor = new Color(0.95f, 0.4f, 0.35f);
        [SerializeField] private Color _infoKavachColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Color _infoAttackColor = new Color(1f, 0.45f, 0.35f);
        [SerializeField] private Color _infoDefendColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField, Min(1f)] private float _infoStatusDescriptionFontSize = 18f;
        [SerializeField] private Color _infoStatusDescriptionColor = new Color(0.82f, 0.78f, 0.7f);

        public event Action Changed;

        public Color ViewerBackdropColor => _viewerBackdropColor;
        public Sprite ViewerPanelSprite => _viewerPanelSprite;
        public Color ViewerPanelColor => _viewerPanelColor;
        public Vector2 ViewerPanelSize => _viewerPanelSize;
        public float ViewerPadding => _viewerPadding;
        public float ViewerHeaderHeight => _viewerHeaderHeight;
        public float ViewerTitleFontSize => _viewerTitleFontSize;
        public Color ViewerTitleColor => _viewerTitleColor;
        public float ViewerCountFontSize => _viewerCountFontSize;
        public Color ViewerCountColor => _viewerCountColor;
        public int ViewerColumns => _viewerColumns;
        public Vector2 ViewerSpacing => _viewerSpacing;
        public float ViewerScrollSensitivity => _viewerScrollSensitivity;
        public float ViewerEmptyFontSize => _viewerEmptyFontSize;
        public Color ViewerEmptyColor => _viewerEmptyColor;
        public Vector2 ViewerCloseSize => _viewerCloseSize;
        public Sprite ViewerCloseSprite => _viewerCloseSprite;
        public Color ViewerCloseColor => _viewerCloseColor;
        public Color ViewerCloseTextColor => _viewerCloseTextColor;
        public float ViewerCloseFontSize => _viewerCloseFontSize;
        public float ViewerFadeDuration => _viewerFadeDuration;

        public float InfoWorldLift => _infoWorldLift;
        public Vector2 InfoScreenOffset => _infoScreenOffset;
        public float InfoScreenMargin => _infoScreenMargin;
        public float InfoWidth => _infoWidth;
        public float InfoPadding => _infoPadding;
        public float InfoSpacing => _infoSpacing;
        public Sprite InfoBackgroundSprite => _infoBackgroundSprite;
        public Color InfoBackgroundColor => _infoBackgroundColor;
        public float InfoNameFontSize => _infoNameFontSize;
        public Color InfoNameColor => _infoNameColor;
        public float InfoRowFontSize => _infoRowFontSize;
        public float InfoRowHeight => _infoRowHeight;
        public Color InfoTextColor => _infoTextColor;
        public Color InfoHealthColor => _infoHealthColor;
        public Color InfoKavachColor => _infoKavachColor;
        public Color InfoAttackColor => _infoAttackColor;
        public Color InfoDefendColor => _infoDefendColor;
        public float InfoStatusDescriptionFontSize => _infoStatusDescriptionFontSize;
        public Color InfoStatusDescriptionColor => _infoStatusDescriptionColor;

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
