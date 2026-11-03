using System;
using UnityEngine;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// The animated helper hand: taps on things or drags from one place to another, on a loop,
    /// so children understand what to do without reading. Targets are re-evaluated every frame,
    /// so the hand follows moving objects (e.g. a customer walking in).
    /// </summary>
    public sealed class HintHand : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Hand;
        [SerializeField] private RectTransform m_Ripple;
        [SerializeField] private CanvasGroup m_Group;
        [SerializeField] private RectTransform m_CanvasRect;

        private Func<Vector2> m_From;
        private Func<Vector2> m_To;
        private bool m_Drag;
        private bool m_Active;
        private float m_T;

        public bool IsShowing => m_Active;

        private void Awake()
        {
            if (m_CanvasRect == null) m_CanvasRect = transform.root as RectTransform;
            SetAlpha(0f);
        }

        public void ShowTap(Func<Vector2> target)
        {
            m_From = target;
            m_To = null;
            m_Drag = false;
            Begin();
        }

        public void ShowDrag(Func<Vector2> from, Func<Vector2> to)
        {
            m_From = from;
            m_To = to;
            m_Drag = true;
            Begin();
        }

        private void Begin()
        {
            m_Active = true;
            m_T = 0f;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            m_Active = false;
            SetAlpha(0f);
        }

        private void Update()
        {
            if (!m_Active || m_From == null) return;
            m_T += Time.unscaledDeltaTime;
            Vector2 a = m_From();
            if (a.x < -9000f) { SetAlpha(0f); return; }

            float scale = 1f;
            float alpha = 1f;
            float ripple = -1f;
            Vector2 p;

            if (!m_Drag)
            {
                const float cycle = 1.4f;
                float t = m_T % cycle;
                Vector2 hover = a + new Vector2(30f, -40f);
                if (t < 0.35f)
                {
                    float k = t / 0.35f;
                    p = Vector2.Lerp(hover, a, Smooth(k));
                    alpha = Mathf.Clamp01(k * 2f);
                }
                else if (t < 0.55f)
                {
                    p = a;
                    scale = 0.85f;
                    ripple = (t - 0.35f) / 0.2f;
                }
                else
                {
                    float k = (t - 0.55f) / (cycle - 0.55f);
                    p = Vector2.Lerp(a, hover, Smooth(k));
                    alpha = 1f - k;
                    ripple = 1f + k;
                }
            }
            else
            {
                Vector2 b = m_To != null ? m_To() : a;
                if (b.x < -9000f) b = a;
                const float cycle = 2.4f;
                float t = m_T % cycle;
                if (t < 0.3f) { p = a; alpha = t / 0.3f; }
                else if (t < 0.45f) { p = a; scale = 0.85f; }
                else if (t < 1.65f) { p = Vector2.Lerp(a, b, Smooth((t - 0.45f) / 1.2f)); scale = 0.85f; }
                else if (t < 1.85f) { p = b; scale = 0.85f; ripple = (t - 1.65f) / 0.2f; }
                else { p = b; float k = (t - 1.85f) / (cycle - 1.85f); alpha = 1f - k; ripple = 1f + k; }
            }

            Place(m_Hand, p);
            m_Hand.localScale = Vector3.one * scale;
            SetAlpha(alpha);
            if (m_Ripple != null)
            {
                bool show = ripple >= 0f && ripple <= 2f;
                m_Ripple.gameObject.SetActive(show);
                if (show)
                {
                    Place(m_Ripple, m_Drag && ripple >= 0f ? (m_To != null ? m_To() : a) : a);
                    float r = Mathf.Lerp(0.4f, 1.6f, ripple / 2f);
                    m_Ripple.localScale = Vector3.one * r;
                    var g = m_Ripple.GetComponent<UnityEngine.UI.Graphic>();
                    if (g != null) { Color c = g.color; c.a = (1f - ripple / 2f) * 0.8f; g.color = c; }
                }
            }
        }

        private static float Smooth(float k) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));

        private void Place(RectTransform rt, Vector2 screen)
        {
            if (rt == null || m_CanvasRect == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CanvasRect, screen, null, out Vector2 local))
            {
                rt.anchoredPosition = local;
            }
        }

        private void SetAlpha(float a)
        {
            if (m_Group != null) m_Group.alpha = a;
            if (a <= 0f && m_Ripple != null) m_Ripple.gameObject.SetActive(false);
        }
    }
}
