using UnityEngine;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>Visual parts of a milkshake cup: rising liquid, whipped cream and a straw.</summary>
    public sealed class MilkshakeVisual : MonoBehaviour
    {
        [SerializeField] private Transform m_Liquid;
        [SerializeField] private Renderer m_LiquidRenderer;
        [SerializeField] private GameObject m_Cream;
        [SerializeField] private Renderer m_CreamRenderer;
        [SerializeField] private GameObject m_Straw;
        [SerializeField] private float m_FullHeight = 1f;

        private MaterialPropertyBlock m_Block;

        public GameObject Cream => m_Cream;
        public GameObject Straw => m_Straw;
        public float Fill { get; private set; }

        public void ResetVisual()
        {
            SetFill(0f);
            if (m_Cream != null) m_Cream.SetActive(false);
            if (m_Straw != null) m_Straw.SetActive(false);
        }

        public void SetFill(float amount)
        {
            Fill = Mathf.Clamp01(amount);
            if (m_Liquid == null) return;
            m_Liquid.gameObject.SetActive(Fill > 0.01f);
            Vector3 s = m_Liquid.localScale;
            s.y = Mathf.Max(0.001f, Fill * m_FullHeight);
            m_Liquid.localScale = s;
        }

        public void SetColors(Color liquid, Color cream)
        {
            m_Block ??= new MaterialPropertyBlock();
            Apply(m_LiquidRenderer, liquid);
            Apply(m_CreamRenderer, cream);
        }

        private void Apply(Renderer r, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(m_Block);
            m_Block.SetColor("_BaseColor", c);
            r.SetPropertyBlock(m_Block);
        }
    }
}
