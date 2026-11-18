// An invisible raycast surface over the hand that receives every mouse click and drag aimed at a hand card and
// forwards them to the hand. It stretches over the hand canvas but only accepts the pointer where the hand's hit test
// says a card is (the resting layout, including the space above a hovered card where its zoomed copy shows), so empty
// space and the enemies behind stay reachable. The hand decides which card a press belongs to the same way it decides
// hover, so overlapping fanned cards can never steal a press and the enlarged zoom copy is clickable. It draws nothing
// (an empty mesh), so it costs no fill rate. Left button only; the click that ends a drag is ignored. Keyboard and
// gamepad never go through it; they use the cards' own buttons.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/UI/Hand Input Surface")]
    public sealed class HandInputSurface : Graphic, ICanvasRaycastFilter, IPointerClickHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler
    {
        private Func<Vector2, Camera, bool> _hitTest;

        public event Action<PointerEventData> Clicked;

        public event Action<PointerEventData> DragStarted;

        public event Action<PointerEventData> Dragged;

        public event Action<PointerEventData> DragEnded;

        public static HandInputSurface Create(RectTransform parent)
        {
            RectTransform rect = UIBuildUtility.CreateRect("HandInput", parent);
            UIBuildUtility.Stretch(rect);
            rect.gameObject.AddComponent<CanvasRenderer>();
            HandInputSurface surface = rect.gameObject.AddComponent<HandInputSurface>();
            surface.raycastTarget = false;
            return surface;
        }

        public void SetHitTest(Func<Vector2, Camera, bool> hitTest)
        {
            _hitTest = hitTest;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return _hitTest != null && _hitTest(screenPoint, eventCamera);
        }

        protected override void OnDestroy()
        {
            _hitTest = null;
            Clicked = null;
            DragStarted = null;
            Dragged = null;
            DragEnded = null;
            base.OnDestroy();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
            {
                Clicked?.Invoke(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                DragStarted?.Invoke(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Dragged?.Invoke(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                DragEnded?.Invoke(eventData);
            }
        }
    }
}
