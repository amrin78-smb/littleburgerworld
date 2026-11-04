using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>Toy shelf: every toy found in a surprise capsule, the rest as soft silhouettes to discover.</summary>
    public sealed class ToyShelfUI : MonoBehaviour
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

        public int SlotCount => m_Slots.Count;

        private void Rebuild()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || m_SlotPrefab == null) return;
            foreach (Image s in m_Slots) if (s != null) Destroy(s.gameObject);
            m_Slots.Clear();
            int i = 0;
            foreach (Sprite toy in gm.Config.toys)
            {
                if (toy == null) continue;
                Image slot = Instantiate(m_SlotPrefab, m_Grid);
                slot.gameObject.SetActive(true);
                Image icon = slot.transform.childCount > 0 ? slot.transform.GetChild(0).GetComponent<Image>() : slot;
                icon.sprite = toy;
                icon.preserveAspect = true;
                bool found = gm.Rewards.HasToy(toy.name);
                icon.color = found ? Color.white : new Color(0.35f, 0.25f, 0.2f, 0.18f);
                Vector3 rest = slot.transform.localScale;
                slot.transform.localScale = Vector3.zero;
                Tween.Scale(slot.transform, rest, 0.3f, Ease.OutBack, null, 0.02f * i++);
                if (found)
                {
                    // Found toys do a little happy wobble when tapped.
                    var b = slot.gameObject.AddComponent<SquishyButton>();
                    b.OnClick.AddListener(() => Tween.Squash(slot.transform, rest, 0.3f, 0.4f));
                }
                m_Slots.Add(slot);
            }
        }
    }
}
