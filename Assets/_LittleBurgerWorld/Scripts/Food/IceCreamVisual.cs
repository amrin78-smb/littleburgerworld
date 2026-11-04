using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// Look of an ice cream cone: up to three tinted scoops, sprinkles and a cherry that always sit on
    /// the top scoop. Scoop colours use a property block, so one material serves every flavour.
    /// </summary>
    public sealed class IceCreamVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_Scoops = new Renderer[0];
        [SerializeField] private Transform m_Sprinkles;
        [SerializeField] private Transform m_Cherry;
        [SerializeField] private float m_ToppingLift = 0.17f;

        private int m_Count;
        private MaterialPropertyBlock m_Block;

        public int ScoopCount => m_Count;
        public int MaxScoops => m_Scoops.Length;
        public bool HasSprinkles => m_Sprinkles != null && m_Sprinkles.gameObject.activeSelf;
        public bool HasCherry => m_Cherry != null && m_Cherry.gameObject.activeSelf;
        public Transform Sprinkles => m_Sprinkles;
        public Transform Cherry => m_Cherry;

        public void ResetVisual()
        {
            m_Count = 0;
            foreach (Renderer r in m_Scoops) if (r != null) r.gameObject.SetActive(false);
            if (m_Sprinkles != null) m_Sprinkles.gameObject.SetActive(false);
            if (m_Cherry != null) m_Cherry.gameObject.SetActive(false);
        }

        /// <summary>Shows the next scoop in the given colour. Returns its transform (null when full).</summary>
        public Transform AddScoop(Color color)
        {
            if (m_Count >= m_Scoops.Length || m_Scoops[m_Count] == null) return null;
            Renderer r = m_Scoops[m_Count];
            m_Block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(m_Block);
            m_Block.SetColor("_BaseColor", color);
            r.SetPropertyBlock(m_Block);
            r.gameObject.SetActive(true);
            m_Count++;
            PlaceToppings();
            return r.transform;
        }

        public void ShowSprinkles(bool on)
        {
            if (m_Sprinkles == null) return;
            m_Sprinkles.gameObject.SetActive(on);
            PlaceToppings();
        }

        public void ShowCherry(bool on)
        {
            if (m_Cherry == null) return;
            m_Cherry.gameObject.SetActive(on);
            PlaceToppings();
        }

        /// <summary>Keeps sprinkles and the cherry on top of the highest scoop.</summary>
        private void PlaceToppings()
        {
            if (m_Count == 0) return;
            Vector3 top = m_Scoops[m_Count - 1].transform.localPosition;
            if (m_Sprinkles != null) m_Sprinkles.localPosition = top;
            if (m_Cherry != null) m_Cherry.localPosition = top + Vector3.up * m_ToppingLift;
        }
    }
}
