using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>Blends a raw-looking patty into a cooked one (colour + grill marks).</summary>
    public sealed class CookableVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_Renderers;
        [SerializeField] private Color m_RawColor = new Color(0.93f, 0.55f, 0.55f);
        [SerializeField] private Color m_CookedColor = new Color(0.55f, 0.32f, 0.2f);
        [SerializeField] private GameObject m_GrillMarks;

        [Tooltip("Applied on Awake when >= 0 (e.g. 1 for always-cooked showcase burgers).")]
        [SerializeField] private float m_InitialAmount = -1f;

        private MaterialPropertyBlock m_Block;
        private float m_Amount = 1f;

        private void Awake()
        {
            if (m_InitialAmount >= 0f) SetCookAmount(m_InitialAmount);
        }

        public float Amount => m_Amount;

        public void SetCookAmount(float amount)
        {
            m_Amount = Mathf.Clamp01(amount);
            m_Block ??= new MaterialPropertyBlock();
            Color c = Color.Lerp(m_RawColor, m_CookedColor, Mathf.SmoothStep(0f, 1f, m_Amount));
            if (m_Renderers != null)
            {
                foreach (Renderer r in m_Renderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(m_Block);
                    m_Block.SetColor("_BaseColor", c);
                    r.SetPropertyBlock(m_Block);
                }
            }
            if (m_GrillMarks != null)
            {
                m_GrillMarks.SetActive(m_Amount > 0.45f);
            }
        }
    }
}
