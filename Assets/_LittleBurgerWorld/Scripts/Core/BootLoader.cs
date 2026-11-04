using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>Boot scene: services are created by <see cref="GameManager"/>; show the splash briefly, then go to the menu.</summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private float m_SplashSeconds = 2.2f;
        [SerializeField] private RectTransform m_Logo;
        [SerializeField] private RectTransform m_Title;

        private float m_Timer;
        private bool m_Done;

        private void Start()
        {
            if (m_Logo != null)
            {
                m_Logo.localScale = Vector3.zero;
                Tween.Scale(m_Logo, Vector3.one, 0.6f, Ease.OutBack);
            }
            if (m_Title != null)
            {
                m_Title.localScale = Vector3.zero;
                Tween.Scale(m_Title, Vector3.one, 0.5f, Ease.OutBack, null, 0.35f);
            }
        }

        private void Update()
        {
            if (m_Done) return;
            // Clamp: the first frame after a slow scene load can last over a second on budget
            // phones, which used to skip the title screen entirely.
            m_Timer += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (m_Timer < m_SplashSeconds) return;
            m_Done = true;
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.LoadMainMenu();
        }
    }
}
