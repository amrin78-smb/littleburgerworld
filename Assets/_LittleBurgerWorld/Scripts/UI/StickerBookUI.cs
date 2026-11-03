using System.Collections.Generic;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Rewards;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>Sticker book: earned stickers in full colour, others as soft silhouettes to discover.</summary>
    public sealed class StickerBookUI : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Grid;
        [SerializeField] private Image m_SlotPrefab;
        [SerializeField] private SquishyButton m_Close;

        private readonly List<Image> m_Slots = new List<Image>();

        private void Awake()
        {
            if (m_Close != null) m_Close.OnClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Rebuild();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Rebuild()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || m_SlotPrefab == null) return;
            foreach (Image s in m_Slots) if (s != null) Destroy(s.gameObject);
            m_Slots.Clear();
            int i = 0;
            foreach (StickerData st in gm.Config.stickers)
            {
                if (st == null) continue;
                Image slot = Instantiate(m_SlotPrefab, m_Grid);
                slot.gameObject.SetActive(true);
                Image icon = slot.transform.childCount > 0 ? slot.transform.GetChild(0).GetComponent<Image>() : slot;
                icon.sprite = st.sprite;
                icon.preserveAspect = true;
                bool earned = gm.Rewards.HasSticker(st.id);
                icon.color = earned ? Color.white : new Color(0.35f, 0.25f, 0.2f, 0.22f);
                Vector3 rest = slot.transform.localScale;
                slot.transform.localScale = Vector3.zero;
                Tween.Scale(slot.transform, rest, 0.35f, Ease.OutBack, null, 0.03f * i++);
                m_Slots.Add(slot);
            }
        }
    }
}
