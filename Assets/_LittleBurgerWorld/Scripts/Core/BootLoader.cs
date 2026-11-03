using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>Boot scene: services are created by <see cref="GameManager"/>; show the splash briefly, then go to the menu.</summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private float m_SplashSeconds = 1.2f;
        [SerializeField] private RectTransform m_Logo;

        private float m_Timer;
        private bool m_Done;

        private void Start()
        {
            if (m_Logo != null)
            {
                m_Logo.localScale = Vector3.zero;
                Tween.Scale(m_Logo, Vector3.one, 0.6f, Ease.OutBack);
            }
        }

        private void Update()
        {
            if (m_Done) return;
            m_Timer += Time.unscaledDeltaTime;
            if (m_Timer < m_SplashSeconds) return;
            m_Done = true;
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.LoadMainMenu();
        }
    }
}
