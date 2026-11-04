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
    /// accident (going home, clearing the kitchen). A quick tap wiggles and pops up a small
    /// "hold me" tip so grown-ups learn how it works.
    /// </summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image m_Fill;
        [SerializeField] private float m_HoldSeconds = 0.8f;
        [SerializeField] private UnityEvent m_OnHoldComplete = new UnityEvent();
        [Tooltip("Optional tip shown after a quick tap (e.g. \"Hold to go home\").")]
        [SerializeField] private RectTransform m_Tip;
        [SerializeField] private float m_TipSeconds = 2.2f;

        private bool m_Holding;
        private float m_Progress;
        private Vector3 m_Rest = Vector3.one;
        private bool m_Captured;

        public UnityEvent OnHoldComplete => m_OnHoldComplete;
        public float HoldSeconds { get => m_HoldSeconds; set => m_HoldSeconds = value; }
        public bool TipVisible => m_Tip != null && m_Tip.gameObject.activeSelf;

        private void Awake()
        {
            if (!m_Captured) { m_Rest = transform.localScale; m_Captured = true; }
            SetFill(0f);
            if (m_Tip != null) m_Tip.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_Holding = true;
            Tween.Kill(transform);
            transform.localRotation = Quaternion.identity;
            Tween.Scale(transform, m_Rest * 0.92f, 0.1f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!m_Holding) return;
            m_Holding = false;
            Tween.Kill(transform);
            // Released too early: wiggle and explain.
            Tween.Value(transform, 0.45f, t =>
            {
                transform.localScale = m_Rest;
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 12f * (1f - t));
            }, Ease.Linear, () => transform.localRotation = Quaternion.identity);
            ShowTip();
        }

        private void ShowTip()
        {
            if (m_Tip == null) return;
            Tween.Kill(m_Tip);
            m_Tip.gameObject.SetActive(true);
            m_Tip.localScale = Vector3.zero;
            Tween.Scale(m_Tip, Vector3.one, 0.35f, Ease.OutBack);
            Tween.Delay(m_Tip, m_TipSeconds, () =>
                Tween.Scale(m_Tip, Vector3.zero, 0.2f, Ease.InBack, () => m_Tip.gameObject.SetActive(false)), true);
        }

        private void HideTip()
        {
            if (m_Tip == null || !m_Tip.gameObject.activeSelf) return;
            Tween.Kill(m_Tip);
            m_Tip.gameObject.SetActive(false);
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
                    HideTip();
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
