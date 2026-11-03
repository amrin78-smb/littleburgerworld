using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>One activity card.</summary>
    public sealed class ActivityCard : MonoBehaviour
    {
        [SerializeField] private Image m_Background;
        [SerializeField] private Image m_Thumbnail;
        [SerializeField] private Image[] m_Stars;
        [SerializeField] private Sprite m_StarOn;
        [SerializeField] private Sprite m_StarOff;
        [SerializeField] private SquishyButton m_Button;

        public SquishyButton Button => m_Button;
        public ActivityData Activity { get; private set; }

        public void Setup(ActivityData activity, int stars, bool highlight, int index)
        {
            Activity = activity;
            name = "Card_" + activity.id;
            if (m_Background != null) m_Background.color = activity.cardColor;
            if (m_Thumbnail != null) { m_Thumbnail.sprite = activity.thumbnail; m_Thumbnail.preserveAspect = true; }
            for (int i = 0; i < m_Stars.Length; i++)
            {
                if (m_Stars[i] == null) continue;
                m_Stars[i].sprite = i < stars ? m_StarOn : m_StarOff;
                m_Stars[i].color = i < stars ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }
            if (m_Button != null) m_Button.IdleBounce = highlight;
            Vector3 rest = transform.localScale;
            transform.localScale = Vector3.zero;
            Tween.Scale(transform, rest, 0.4f, Ease.OutBack, null, 0.05f * index);
        }
    }
}
