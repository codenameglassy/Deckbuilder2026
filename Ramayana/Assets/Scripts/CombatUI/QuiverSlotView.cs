// One slot of the quiver on the combat HUD. It sits on the slot's Button and only reports what the pointer and the
// selection do to it (enter, exit, select, deselect), so the quiver panel can work out which arrow the player is
// considering. Index is the slot number, set once when the panel builds it. It holds no state of its own.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    public sealed class QuiverSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public event Action<QuiverSlotView> PointerEntered;

        public event Action<QuiverSlotView> PointerExited;

        public event Action<QuiverSlotView> Selected;

        public event Action<QuiverSlotView> Deselected;

        public int Index { get; private set; }

        internal void Initialize(int index)
        {
            Index = index;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExited?.Invoke(this);
        }

        public void OnSelect(BaseEventData eventData)
        {
            Selected?.Invoke(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            Deselected?.Invoke(this);
        }

        private void OnDestroy()
        {
            PointerEntered = null;
            PointerExited = null;
            Selected = null;
            Deselected = null;
        }
    }
}
