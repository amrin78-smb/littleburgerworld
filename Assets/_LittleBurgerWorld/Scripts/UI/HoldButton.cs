using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// Press-and-hold button with a filling ring. Used for actions a toddler should not trigger by
    /// accident (going home, clearing the kitchen). A quick tap just wiggles and shows the ring.
    /// </summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image m_Fill;
        [SerializeField] private float m_HoldSeconds = 0.8f;
        [SerializeField] private UnityEvent m_OnHoldComplete = new UnityEvent();

        private bool m_Holding;
        private float m_Progress;
        private Vector3 m_Rest = Vector3.one;
        private bool m_Captured;

        public UnityEvent OnHoldComplete => m_OnHoldComplete;
        public float HoldSeconds { get => m_HoldSeconds; set => m_HoldSeconds = value; }

        private void Awake()
        {
            if (!m_Captured) { m_Rest = transform.localScale; m_Captured = true; }
            SetFill(0f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_Holding = true;
            Tween.Kill(transform);
            Tween.Scale(transform, m_Rest * 0.92f, 0.1f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!m_Holding) return;
            m_Holding = false;
            Tween.Kill(transform);
            Tween.Scale(transform, m_Rest, 0.3f, Ease.OutElastic);
        }

        private void Update()
        {
            if (m_Holding)
            {
                m_Progress += Time.unscaledDeltaTime / Mathf.Max(0.05f, m_HoldSeconds);
                if (m_Progress >= 1f)
                {
                    m_Holding = false;
                    m_Progress = 0f;
                    SetFill(0f);
                    transform.localScale = m_Rest;
                    GameManager gm = GameManager.Instance;
                    if (gm != null) gm.Audio.Play(SfxId.ButtonTap);
                    m_OnHoldComplete.Invoke();
                    return;
                }
            }
            else if (m_Progress > 0f)
            {
                m_Progress = Mathf.Max(0f, m_Progress - Time.unscaledDeltaTime * 2f);
            }
            SetFill(m_Progress);
        }

        private void SetFill(float v)
        {
            if (m_Fill != null) m_Fill.fillAmount = v;
        }

        /// <summary>Programmatic activation (tests).</summary>
        public void Trigger() => m_OnHoldComplete.Invoke();
    }
}
