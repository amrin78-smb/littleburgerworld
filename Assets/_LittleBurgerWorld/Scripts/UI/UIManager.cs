using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// Restaurant HUD: home, mode badge, picture prompt bubble, ingredient tray, hint hand,
    /// activity picker, celebration and sticker toasts. Shows only what the current mode needs.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private HintHand m_Hand;
        [SerializeField] private RectTransform m_PromptBubble;
        [SerializeField] private Image m_PromptIcon;
        [Tooltip("Instruction line beside the picture (optional).")]
        [SerializeField] private Text m_PromptText;
        [SerializeField] private float m_PromptWidthWithText = 800f;
        [SerializeField] private IngredientTrayUI m_Tray;
        [SerializeField] private ActivitySelectUI m_ActivitySelect;
        [SerializeField] private CelebrationUI m_Celebration;
        [SerializeField] private HoldButton m_Home;
        [SerializeField] private HoldButton m_ResetKitchen;
        [SerializeField] private SquishyButton m_Replay;
        [SerializeField] private SquishyButton m_ActivityList;
        [SerializeField] private Image m_ModeBadge;
        [SerializeField] private Sprite m_GuidedBadge;
        [SerializeField] private Sprite m_FreePlayBadge;

        private Sprite m_CurrentPrompt;
        private string m_CurrentText;

        public HintHand Hand => m_Hand;
        public IngredientTrayUI Tray => m_Tray;
        public ActivitySelectUI ActivitySelect => m_ActivitySelect;
        public CelebrationUI Celebration => m_Celebration;
        public HoldButton Home => m_Home;
        public HoldButton ResetKitchen => m_ResetKitchen;
        public SquishyButton Replay => m_Replay;
        public SquishyButton ActivityList => m_ActivityList;
        public bool PromptVisible => m_PromptBubble != null && m_PromptBubble.gameObject.activeSelf;
        /// <summary>Instruction text currently shown in the prompt bubble (empty when picture only).</summary>
        public string PromptText => m_PromptText != null && m_PromptText.gameObject.activeSelf ? m_PromptText.text : string.Empty;

        private void Awake()
        {
            if (m_PromptBubble != null) m_PromptBubble.gameObject.SetActive(false);
            if (m_Celebration != null) m_Celebration.gameObject.SetActive(false);
            if (m_ActivitySelect != null) m_ActivitySelect.gameObject.SetActive(false);
        }

        public void SetMode(GameMode mode)
        {
            bool guided = mode == GameMode.Guided;
            if (m_ModeBadge != null) m_ModeBadge.sprite = guided ? m_GuidedBadge : m_FreePlayBadge;
            if (m_ResetKitchen != null) m_ResetKitchen.gameObject.SetActive(!guided);
            SetActivityControls(false);
        }

        /// <summary>Guided Play: replay + back-to-activities buttons while an activity runs.</summary>
        public void SetActivityControls(bool visible)
        {
            if (m_Replay != null) m_Replay.gameObject.SetActive(visible);
            if (m_ActivityList != null) m_ActivityList.gameObject.SetActive(visible);
        }

        public void SetPrompt(Sprite icon) => SetPrompt(icon, null);

        /// <summary>Picture prompt with an optional short instruction beside it. Null icon hides the bubble.</summary>
        public void SetPrompt(Sprite icon, string text)
        {
            if (m_PromptBubble == null) return;
            if (icon == null) text = null;
            bool shown = m_PromptBubble.gameObject.activeSelf;
            if (icon == m_CurrentPrompt && text == m_CurrentText && (icon != null) == shown) return;
            m_CurrentPrompt = icon;
            m_CurrentText = text;
            Tween.Kill(m_PromptBubble);
            if (icon == null)
            {
                Tween.Scale(m_PromptBubble, Vector3.zero, 0.15f, Ease.InBack, () =>
                {
                    if (m_CurrentPrompt == null) m_PromptBubble.gameObject.SetActive(false);
                });
                return;
            }
            m_PromptIcon.sprite = icon;
            m_PromptIcon.preserveAspect = true;
            LayoutPrompt(text);
            m_PromptBubble.gameObject.SetActive(true);
            m_PromptBubble.localScale = Vector3.zero;
            Tween.Scale(m_PromptBubble, Vector3.one, 0.45f, Ease.OutBack);
        }

        private void LayoutPrompt(string text)
        {
            bool hasText = m_PromptText != null && !string.IsNullOrEmpty(text);
            float height = m_PromptBubble.sizeDelta.y;
            float width = hasText ? m_PromptWidthWithText : height;
            m_PromptBubble.sizeDelta = new Vector2(width, height);
            RectTransform icon = m_PromptIcon.rectTransform;
            icon.anchoredPosition = hasText ? new Vector2(-width * 0.5f + height * 0.5f, 0f) : Vector2.zero;
            if (m_PromptText == null) return;
            m_PromptText.gameObject.SetActive(hasText);
            if (!hasText) return;
            m_PromptText.text = text;
            RectTransform t = m_PromptText.rectTransform;
            float left = height - 10f, right = 36f;
            t.sizeDelta = new Vector2(width - left - right, height - 30f);
            t.anchoredPosition = new Vector2((left - right) * 0.5f, 0f);
        }

        public void FlashStepSuccess()
        {
            if (m_PromptBubble == null || !m_PromptBubble.gameObject.activeSelf) return;
            Tween.Squash(m_PromptBubble, Vector3.one, 0.2f, 0.3f);
        }

        public void ShowActivitySelect()
        {
            m_Hand?.Hide();
            SetPrompt(null);
            SetActivityControls(false);
            if (m_ActivitySelect != null) m_ActivitySelect.Show();
        }

        public void HideActivitySelect()
        {
            if (m_ActivitySelect != null) m_ActivitySelect.Hide();
        }

        public void ShowCelebration(int stars, Sprite portrait, System.Action onClosed)
        {
            m_Hand?.Hide();
            SetPrompt(null);
            if (m_Celebration != null) m_Celebration.Show(stars, portrait, onClosed);
            else onClosed?.Invoke();
        }
    }
}
