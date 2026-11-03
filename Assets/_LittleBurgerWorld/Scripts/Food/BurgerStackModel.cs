using System.Collections.Generic;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// Pure stacking logic for a burger (no Unity objects needed – unit tested).
    /// Heights are the sum of each ingredient's <see cref="IngredientData.stackHeight"/>.
    /// </summary>
    public sealed class BurgerStackModel
    {
        private readonly List<IngredientData> m_Items = new List<IngredientData>();

        /// <summary>Maximum layers, keeps very tall Free Play towers visually stable.</summary>
        public int MaxItems { get; set; } = 12;

        public IReadOnlyList<IngredientData> Items => m_Items;
        public int Count => m_Items.Count;
        public bool IsEmpty => m_Items.Count == 0;
        public bool IsFull => m_Items.Count >= MaxItems;

        public bool CanAdd(IngredientData data)
        {
            return data != null && data.stackable && !data.IsRaw && !IsFull;
        }

        public bool Add(IngredientData data)
        {
            if (!CanAdd(data)) return false;
            m_Items.Add(data);
            return true;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= m_Items.Count) return false;
            m_Items.RemoveAt(index);
            return true;
        }

        public void Clear() => m_Items.Clear();

        /// <summary>Height of everything below the item at <paramref name="index"/> (its resting offset).</summary>
        public float HeightBelow(int index)
        {
            float h = 0f;
            int n = index < m_Items.Count ? index : m_Items.Count;
            for (int i = 0; i < n; i++) h += m_Items[i] != null ? m_Items[i].stackHeight : 0f;
            return h;
        }

        public float TotalHeight => HeightBelow(m_Items.Count);

        /// <summary>
        /// A burger is finished when a "completes burger" ingredient (top bun) lands on at least one other layer.
        /// Free Play does not require any specific order.
        /// </summary>
        public bool IsComplete => m_Items.Count >= 2 && m_Items[m_Items.Count - 1] != null &&
                                  m_Items[m_Items.Count - 1].completesBurger;
    }
}
