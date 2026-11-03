using System;
using System.Collections.Generic;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>Picture cards for the guided activities (thumbnail + earned stars). No reading needed.</summary>
    public sealed class ActivitySelectUI : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Grid;
        [SerializeField] private ActivityCard m_CardPrefab;

        private readonly List<ActivityCard> m_Cards = new List<ActivityCard>();

        public event Action<ActivityData> Selected;
        public IReadOnlyList<ActivityCard> Cards => m_Cards;

        public void Show()
        {
            gameObject.SetActive(true);
            Rebuild();
            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                Tween.Value(this, 0.25f, k => group.alpha = k);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        public void Rebuild()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || m_CardPrefab == null) return;
            foreach (ActivityCard c in m_Cards) if (c != null) Destroy(c.gameObject);
            m_Cards.Clear();

            SaveData save = gm.Save.Data;
            bool nextMarked = false;
            int i = 0;
            foreach (ActivityData a in gm.Config.activities)
            {
                if (a == null) continue;
                ActivityCard card = Instantiate(m_CardPrefab, m_Grid);
                card.gameObject.SetActive(true);
                ActivityProgress p = save.GetActivity(a.id);
                bool isNext = !nextMarked && (p == null || p.timesCompleted == 0);
                if (isNext) nextMarked = true;
                card.Setup(a, p != null ? p.stars : 0, isNext, i);
                ActivityData captured = a;
                card.Button.OnClick.AddListener(() => Selected?.Invoke(captured));
                m_Cards.Add(card);
                i++;
            }
        }
    }
}
