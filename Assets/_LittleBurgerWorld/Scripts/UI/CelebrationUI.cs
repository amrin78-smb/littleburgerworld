using System;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// End-of-activity party: big bouncing stars, a cheering mascot portrait and floating balloons.
    /// Closes by itself (or on tap) – never asks the child to read anything.
    /// </summary>
    public sealed class CelebrationUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup m_Group;
        [SerializeField] private RectTransform m_Panel;
        [SerializeField] private Image[] m_Stars;
        [SerializeField] private Image m_Portrait;
        [SerializeField] private float m_AutoClose = 4f;

        private Action m_OnClosed;
        private float m_Timer;
        private bool m_Open;

        public bool IsOpen => m_Open;

        public void Show(int stars, Sprite portrait, Action onClosed)
        {
            m_OnClosed = onClosed;
            m_Open = true;
            m_Timer = 0f;
            gameObject.SetActive(true);
            if (m_Group != null) { m_Group.alpha = 0f; Tween.Value(this, 0.25f, k => m_Group.alpha = k); }
            if (m_Panel != null)
            {
                m_Panel.localScale = Vector3.zero;
                Tween.Scale(m_Panel, Vector3.one, 0.5f, Ease.OutBack);
            }
            if (m_Portrait != null && portrait != null) m_Portrait.sprite = portrait;
            for (int i = 0; i < m_Stars.Length; i++)
            {
                Image s = m_Stars[i];
                if (s == null) continue;
                bool on = i < Mathf.Max(1, stars);
                s.color = on ? Color.white : new Color(1f, 1f, 1f, 0.25f);
                s.transform.localScale = Vector3.zero;
                int idx = i;
                Tween.Scale(s.transform, Vector3.one * (on ? 1f : 0.8f), 0.45f, Ease.OutElastic, () =>
                {
                    if (on)
                    {
                        GameManager gm = GameManager.Instance;
                        if (gm != null) gm.Audio.Play(SfxId.Star, 1f, 1f + idx * 0.12f);
                    }
                }, 0.35f + i * 0.25f);
            }
            BalloonCelebration.Launch(7);
            Camera cam = Camera.main;
            if (cam != null) FxManager.Play(FxType.Confetti, cam.transform.position + cam.transform.forward * 20f, 60);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_Timer > 1.2f) Close();
        }

        private void Update()
        {
            if (!m_Open) return;
            m_Timer += Time.unscaledDeltaTime;
            if (m_Portrait != null)
            {
                float b = Mathf.Abs(Mathf.Sin(m_Timer * 5f)) * 18f;
                m_Portrait.rectTransform.anchoredPosition = new Vector2(m_Portrait.rectTransform.anchoredPosition.x, b);
            }
            if (m_Timer >= m_AutoClose) Close();
        }

        public void Close()
        {
            if (!m_Open) return;
            m_Open = false;
            Action cb = m_OnClosed;
            m_OnClosed = null;
            if (m_Group != null)
            {
                Tween.Value(this, 0.25f, k => m_Group.alpha = 1f - k, Ease.OutQuad, () => gameObject.SetActive(false));
            }
            else gameObject.SetActive(false);
            cb?.Invoke();
        }
    }
}
