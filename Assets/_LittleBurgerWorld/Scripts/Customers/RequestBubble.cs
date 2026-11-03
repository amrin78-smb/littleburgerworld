using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEngine;

namespace LittleBurgerWorld.Customers
{
    /// <summary>
    /// Thought bubble above a customer showing a picture of what they would like.
    /// Pictures only – no reading required. Always faces the camera and bobs gently.
    /// </summary>
    public sealed class RequestBubble : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer m_Background;
        [SerializeField] private SpriteRenderer[] m_Icons;
        [SerializeField] private float m_IconSpacing = 0.75f;

        private Vector3 m_RestScale;
        private Vector3 m_RestLocalPos;
        private bool m_Visible;
        private bool m_Captured;

        public bool IsVisible => m_Visible;
        public int IconCount { get; private set; }

        private void Awake() => Capture();

        private void Capture()
        {
            if (m_Captured) return;
            m_RestScale = transform.localScale;
            m_RestLocalPos = transform.localPosition;
            m_Captured = true;
            if (!m_Visible) gameObject.SetActive(false);
        }

        public void Show(IList<Sprite> icons)
        {
            Capture();
            int n = 0;
            for (int i = 0; i < m_Icons.Length; i++)
            {
                Sprite s = icons != null && i < icons.Count ? icons[i] : null;
                m_Icons[i].sprite = s;
                m_Icons[i].gameObject.SetActive(s != null);
                if (s != null) n++;
            }
            IconCount = n;
            float start = -(n - 1) * m_IconSpacing * 0.5f;
            int k = 0;
            for (int i = 0; i < m_Icons.Length; i++)
            {
                if (!m_Icons[i].gameObject.activeSelf) continue;
                m_Icons[i].transform.localPosition = new Vector3(start + k * m_IconSpacing, 0.05f, -0.02f);
                k++;
            }
            if (m_Background != null)
            {
                Vector2 size = m_Background.size;
                size.x = Mathf.Max(1.1f, n * m_IconSpacing + 0.45f);
                m_Background.size = size;
            }
            m_Visible = true;
            gameObject.SetActive(true);
            Tween.Kill(transform);
            transform.localScale = Vector3.zero;
            Tween.Scale(transform, m_RestScale, 0.4f, Ease.OutBack);
        }

        public void Hide()
        {
            if (!m_Visible) return;
            m_Visible = false;
            Tween.Kill(transform);
            Tween.Scale(transform, Vector3.zero, 0.2f, Ease.InBack, () => { if (this != null) gameObject.SetActive(false); });
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            transform.localPosition = m_RestLocalPos + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * 0.06f);
        }
    }
}
