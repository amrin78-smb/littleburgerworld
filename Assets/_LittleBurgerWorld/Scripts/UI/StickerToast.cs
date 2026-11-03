using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Rewards;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>Pops a newly earned sticker in from the corner for a moment.</summary>
    public sealed class StickerToast : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Panel;
        [SerializeField] private Image m_Sticker;

        private float m_Timer = -1f;
        private Vector2 m_Shown;
        private Vector2 m_Hidden;

        private void Awake()
        {
            if (m_Panel != null)
            {
                m_Shown = m_Panel.anchoredPosition;
                m_Hidden = m_Shown + new Vector2(0f, 420f);
                m_Panel.anchoredPosition = m_Hidden;
            }
        }

        private void OnEnable() => GameEvents.StickerEarned += OnEarned;
        private void OnDisable() => GameEvents.StickerEarned -= OnEarned;

        private void OnEarned(string id)
        {
            GameManager gm = GameManager.Instance;
            StickerData s = gm != null ? gm.Config.FindSticker(id) : null;
            if (s == null || m_Panel == null) return;
            m_Sticker.sprite = s.sprite;
            Tween.Kill(m_Panel);
            Tween.Value(m_Panel, 0.45f, k => m_Panel.anchoredPosition = Vector2.LerpUnclamped(m_Hidden, m_Shown, k), Ease.OutBack);
            Tween.Value(m_Sticker.transform, 1.2f, k =>
                m_Sticker.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 3f) * 12f * (1f - k)), Ease.Linear, null, 0.3f);
            gm.Audio.Play(SfxId.Sparkle);
            m_Timer = 2.8f;
        }

        private void Update()
        {
            if (m_Timer < 0f) return;
            m_Timer -= Time.unscaledDeltaTime;
            if (m_Timer < 0f && m_Panel != null)
            {
                Tween.Value(m_Panel, 0.3f, k => m_Panel.anchoredPosition = Vector2.Lerp(m_Shown, m_Hidden, k), Ease.InBack);
            }
        }
    }
}
