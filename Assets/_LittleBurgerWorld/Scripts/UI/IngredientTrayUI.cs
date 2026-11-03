using System.Collections.Generic;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Food;
using UnityEngine;

namespace LittleBurgerWorld.UI
{
    /// <summary>Bottom tray of ingredient buttons, built from data (only unlocked ingredients are shown).</summary>
    public sealed class IngredientTrayUI : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Content;
        [SerializeField] private IngredientTrayButton m_ButtonPrefab;

        private readonly List<IngredientTrayButton> m_Buttons = new List<IngredientTrayButton>();

        public IReadOnlyList<IngredientTrayButton> Buttons => m_Buttons;

        private void OnEnable()
        {
            GameEvents.ItemUnlocked += OnUnlocked;
            Rebuild();
        }

        private void OnDisable() => GameEvents.ItemUnlocked -= OnUnlocked;

        private void OnUnlocked(string id)
        {
            Rebuild();
            foreach (IngredientTrayButton b in m_Buttons)
            {
                if (b.Data != null && b.Data.id == id)
                {
                    Vector3 rest = b.transform.localScale;
                    b.transform.localScale = Vector3.zero;
                    Tween.Scale(b.transform, rest, 0.6f, Ease.OutElastic, null, 0.3f);
                }
            }
        }

        public void Rebuild()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || m_ButtonPrefab == null || m_Content == null) return;
            foreach (IngredientTrayButton b in m_Buttons) if (b != null) Destroy(b.gameObject);
            m_Buttons.Clear();

            var list = new List<IngredientData>();
            foreach (IngredientData i in gm.Config.ingredients)
            {
                if (i != null && i.showInTray && gm.Rewards.IsIngredientUnlocked(i)) list.Add(i);
            }
            list.Sort((a, b) => a.trayOrder.CompareTo(b.trayOrder));
            foreach (IngredientData data in list)
            {
                IngredientTrayButton b = Instantiate(m_ButtonPrefab, m_Content);
                b.gameObject.SetActive(true);
                b.Setup(data);
                m_Buttons.Add(b);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
