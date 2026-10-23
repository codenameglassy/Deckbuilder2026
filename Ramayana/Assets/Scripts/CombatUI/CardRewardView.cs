// The card reward screen: a dimmed overlay over the combat HUD with a title, the offered cards and a Take Nothing
// button. It opens when the run director raises CardRewardOffered, reads the offer from the active run, and answers
// on CardRewardChosen with the chosen index (or -1). It only presents and forwards the choice; the run applies it.
// Cards are pooled CardViews styled by the Combat HUD style. Keyboard, mouse and gamepad all work: left and right
// move between cards, down reaches Take Nothing, and Escape or the east face button takes nothing.
using System.Collections.Generic;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Localization;
using JourneyToLanka.Run;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [AddComponentMenu("Journey to Lanka/UI/Card Reward View")]
    public sealed class CardRewardView : MonoBehaviour
    {
        [SerializeField] private CardRewardStyleSO _style;
        [SerializeField] private CombatHudStyleSO _cardStyle;
        [SerializeField] private VoidEventChannelSO _rewardOffered;
        [SerializeField] private IntEventChannelSO _rewardChosen;

        private readonly List<CardView> _shown = new List<CardView>(4);
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _panel;
        private Image _backdrop;
        private TextMeshProUGUI _title;
        private LocalizedText _titleText;
        private RectTransform _row;
        private HorizontalLayoutGroup _rowLayout;
        private Image _skipImage;
        private Button _skip;
        private TextMeshProUGUI _skipLabel;
        private LocalizedText _skipText;
        private ComponentPool<CardView> _pool;
        private InputAction _cancelAction;
        private bool _open;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _scaler = GetComponent<CanvasScaler>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Build();

            _skip.onClick.AddListener(HandleSkipPressed);
            _cancelAction = new InputAction("RewardCancel", InputActionType.Button, "<Keyboard>/escape");
            _cancelAction.AddBinding("<Gamepad>/buttonEast");
            _cancelAction.performed += _ => HandleSkipPressed();

            ApplyStyle();
            _panel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            SubscribeStyles(true);
            if (_rewardOffered != null)
            {
                _rewardOffered.Register(HandleRewardOffered);
            }
        }

        private void OnDisable()
        {
            SubscribeStyles(false);
            if (_rewardOffered != null)
            {
                _rewardOffered.Unregister(HandleRewardOffered);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (_skip != null)
            {
                _skip.onClick.RemoveListener(HandleSkipPressed);
            }

            _cancelAction?.Dispose();
            _pool?.Dispose();
            _titleText?.Clear();
            _skipText?.Clear();
        }

        private void Build()
        {
            _panel = UIBuildUtility.CreateRect("Panel", transform);
            UIBuildUtility.Stretch(_panel);

            _backdrop = _panel.gameObject.AddComponent<Image>();
            _backdrop.raycastTarget = true;

            _title = UIBuildUtility.CreateText("Title", _panel, TextAlignmentOptions.Center);
            _titleText = new LocalizedText(_title);
            _titleText.Set(LocalizationTableNames.UI, CardRewardKeys.Title);

            _row = UIBuildUtility.CreateRect("Cards", _panel);
            _rowLayout = _row.gameObject.AddComponent<HorizontalLayoutGroup>();
            _rowLayout.childAlignment = TextAnchor.MiddleCenter;
            _rowLayout.childControlWidth = false;
            _rowLayout.childControlHeight = false;
            _rowLayout.childForceExpandWidth = false;
            _rowLayout.childForceExpandHeight = false;

            _skipImage = UIBuildUtility.CreateImage("TakeNothing", _panel, true);
            _skip = _skipImage.gameObject.AddComponent<Button>();
            _skip.targetGraphic = _skipImage;
            _skip.transition = Selectable.Transition.ColorTint;
            _skipLabel = UIBuildUtility.CreateText("Label", _skipImage.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(_skipLabel.rectTransform);
            _skipText = new LocalizedText(_skipLabel);
            _skipText.Set(LocalizationTableNames.UI, CardRewardKeys.Skip);

            RectTransform templates = UIBuildUtility.CreateRect("Templates", transform);
            templates.gameObject.SetActive(false);
            _pool = new ComponentPool<CardView>(CardViewFactory.CreateTemplate(templates), _row, 4, 8);
        }

        private void SubscribeStyles(bool subscribe)
        {
            if (_style != null)
            {
                if (subscribe)
                {
                    _style.Changed += ApplyStyle;
                }
                else
                {
                    _style.Changed -= ApplyStyle;
                }
            }

            if (_cardStyle != null)
            {
                if (subscribe)
                {
                    _cardStyle.Changed += ApplyStyle;
                }
                else
                {
                    _cardStyle.Changed -= ApplyStyle;
                }
            }
        }

        private void ApplyStyle()
        {
            if (_style == null || _cardStyle == null)
            {
                return;
            }

            _canvas.sortingOrder = _style.SortingOrder;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = _cardStyle.ReferenceResolution;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            _backdrop.color = _style.BackdropColor;

            UIBuildUtility.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), _style.TitleOffset, _style.TitleSize);
            UIBuildUtility.StyleText(_title, _style.Font, _style.TitleFontSize, _style.TitleColor);

            float rowWidth = (_cardStyle.CardSize.x + _style.CardSpacing) * 4f;
            UIBuildUtility.Place(_row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), _style.RowOffset, new Vector2(rowWidth, _cardStyle.CardSize.y));
            _row.localScale = Vector3.one * _style.CardScale;
            _rowLayout.spacing = _style.CardSpacing;

            UIBuildUtility.Place(_skipImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), _style.SkipOffset, _style.SkipSize);
            UIBuildUtility.StyleImage(_skipImage, _style.SkipSprite, _style.SkipColor);
            _skip.colors = CardView.CreateTint(_cardStyle);
            UIBuildUtility.StyleText(_skipLabel, _style.Font, _style.SkipFontSize, _style.SkipTextColor);

            for (int i = 0; i < _shown.Count; i++)
            {
                _shown[i].ApplyStyle(_cardStyle);
            }
        }

        private void HandleRewardOffered()
        {
            if (!ServiceLocator.TryGet(out IRunService runs) || !runs.HasActiveRun || !runs.Current.HasPendingReward)
            {
                if (_rewardChosen != null)
                {
                    _rewardChosen.Raise(-1);
                }

                return;
            }

            Open(runs.Current.PendingReward);
        }

        private void Open(IReadOnlyList<CardDefinitionSO> cards)
        {
            Close();
            _panel.gameObject.SetActive(true);

            for (int i = 0; i < cards.Count; i++)
            {
                CardView view = _pool.Get();
                if (_cardStyle != null)
                {
                    view.ApplyStyle(_cardStyle);
                }

                view.Bind(new CardInstance(i, cards[i]));
                view.SetPlayable(true, 1f);
                view.transform.SetSiblingIndex(i);
                view.Clicked += HandleCardClicked;
                _shown.Add(view);
            }

            WireNavigation();
            _cancelAction.Enable();
            _open = true;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(_shown.Count > 0 ? _shown[0].gameObject : _skip.gameObject);
            }
        }

        private void Close()
        {
            for (int i = _shown.Count - 1; i >= 0; i--)
            {
                CardView view = _shown[i];
                view.Clicked -= HandleCardClicked;
                _pool.Release(view);
            }

            _shown.Clear();
            _cancelAction?.Disable();
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }

            _open = false;
        }

        private void WireNavigation()
        {
            int count = _shown.Count;
            for (int i = 0; i < count; i++)
            {
                _shown[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _shown[i - 1].Button : null,
                    selectOnRight = i < count - 1 ? _shown[i + 1].Button : null,
                    selectOnDown = _skip,
                };
            }

            _skip.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = count > 0 ? _shown[count / 2].Button : null,
            };
        }

        private void HandleCardClicked(CardView view)
        {
            Choose(view.CardId);
        }

        private void HandleSkipPressed()
        {
            Choose(-1);
        }

        private void Choose(int choiceIndex)
        {
            if (!_open)
            {
                return;
            }

            Close();
            if (_rewardChosen != null)
            {
                _rewardChosen.Raise(choiceIndex);
            }
        }
    }
}
