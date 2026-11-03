using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace LittleBurgerWorld.UI
{
    /// <summary>Big, bouncy, toddler-proof button: squishes on touch, boings on release.</summary>
    public sealed class SquishyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private SfxId m_Sound = SfxId.ButtonTap;
        [SerializeField] private bool m_IdleBounce;
        [SerializeField] private UnityEvent m_OnClick = new UnityEvent();

        private Vector3 m_Rest = Vector3.one;
        private bool m_Captured;
        private float m_Time;

        public UnityEvent OnClick => m_OnClick;
        public bool Interactable { get; set; } = true;
        public bool IdleBounce { get => m_IdleBounce; set => m_IdleBounce = value; }

        private void Awake() => Capture();

        private void Capture()
        {
            if (m_Captured) return;
            m_Rest = transform.localScale;
            m_Captured = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable) return;
            Capture();
            m_Pressed = true;
            Tween.Kill(transform);
            Tween.Scale(transform, m_Rest * 0.9f, 0.08f, Ease.OutQuad, null, 0f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_Pressed = false;
            m_Cooldown = 0.4f;
            Tween.Kill(transform);
            Tween.Scale(transform, m_Rest, 0.35f, Ease.OutElastic);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Interactable) return;
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(m_Sound);
            m_OnClick.Invoke();
        }

        /// <summary>Programmatic click (tests, keyboard-free automation).</summary>
        public void Click()
        {
            if (Interactable) m_OnClick.Invoke();
        }

        private void Update()
        {
            if (m_Cooldown > 0f) { m_Cooldown -= Time.unscaledDeltaTime; return; }
            if (!m_IdleBounce || m_Pressed) return;
            m_Time += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Max(0f, Mathf.Sin(m_Time * 3f)) * 0.04f;
            transform.localScale = m_Rest * s;
        }

        private bool m_Pressed;
        private float m_Cooldown;
    }
}
