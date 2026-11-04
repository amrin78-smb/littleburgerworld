using UnityEngine;
using UnityEngine.EventSystems;

namespace LittleBurgerWorld.UI
{
    /// <summary>Forwards finger rubs and taps on the plate to <see cref="WashingUpUI"/>.</summary>
    public sealed class ScrubSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public WashingUpUI Owner { get; set; }

        public void OnPointerDown(PointerEventData e) { if (Owner != null) Owner.Scrub(e.position, true); }
        public void OnDrag(PointerEventData e) { if (Owner != null) Owner.Scrub(e.position, false); }
        public void OnPointerUp(PointerEventData e) { if (Owner != null) Owner.ParkSponge(); }
    }
}
