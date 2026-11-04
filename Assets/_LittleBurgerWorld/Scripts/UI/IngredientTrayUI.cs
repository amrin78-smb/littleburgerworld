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
            FitToWidth();
        }

        /// <summary>Shrinks the row uniformly when many ingredients are unlocked so all stay on screen
        /// (narrow 4:3 tablets show smaller but still large buttons; phones are wide enough).</summary>
        public void FitToWidth()
        {
            if (m_Content == null) return;
            var layout = m_Content.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            float spacing = layout != null ? layout.spacing : 0f;
            float total = 0f;
            foreach (IngredientTrayButton b in m_Buttons)
                if (b != null) total += ((RectTransform)b.transform).sizeDelta.x;
            total += Mathf.Max(0, m_Buttons.Count - 1) * spacing;
            float available = ((RectTransform)transform).rect.width - 2f * k_SideMargin;
            float scale = total > 1f && available > 1f ? Mathf.Min(1f, available / total) : 1f;
            // Size the row's box to its content so the layout stays centred, then shrink it uniformly.
            m_Content.sizeDelta = new Vector2(Mathf.Max(total, 1f), m_Content.sizeDelta.y);
            m_Content.localScale = new Vector3(scale, scale, 1f);
            Scale = scale;
        }

        /// <summary>Current tray scale (1 = full size).</summary>
        public float Scale { get; private set; } = 1f;

        private const float k_SideMargin = 70f;

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && m_Buttons.Count > 0) FitToWidth();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
