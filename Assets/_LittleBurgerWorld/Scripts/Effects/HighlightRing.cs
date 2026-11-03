using LittleBurgerWorld.Core;
using UnityEngine;

namespace LittleBurgerWorld.Effects
{
    /// <summary>Soft glowing ring on the counter that says "drop it here!".</summary>
    public sealed class HighlightRing : MonoBehaviour
    {
        private static Mesh s_Mesh;
        private MeshRenderer m_Renderer;
        private MaterialPropertyBlock m_Block;
        private bool m_Visible;
        private float m_Alpha;
        private Color m_Color = new Color(1f, 0.92f, 0.45f, 1f);

        public static HighlightRing Create(Transform parent, Vector3 worldPosition, float radius)
        {
            var go = new GameObject("HighlightRing");
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition + Vector3.up * 0.02f;
            go.transform.rotation = Quaternion.identity;
            Vector3 ls = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(radius / Mathf.Max(0.001f, ls.x), 1f / Mathf.Max(0.001f, ls.y),
                radius / Mathf.Max(0.001f, ls.z));

            if (s_Mesh == null)
            {
                s_Mesh = MeshFactory.Ring(0.82f, 1f, 48, "HighlightRing");
                var colors = new Color[s_Mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                s_Mesh.colors = colors;
            }
            go.AddComponent<MeshFilter>().sharedMesh = s_Mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            GameConfig cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cfg != null && cfg.highlightRingMaterial != null) mr.sharedMaterial = cfg.highlightRingMaterial;
            var ring = go.AddComponent<HighlightRing>();
            ring.m_Renderer = mr;
            ring.SetVisible(false);
            return ring;
        }

        public void SetColor(Color c) => m_Color = c;

        public void SetVisible(bool visible)
        {
            m_Visible = visible;
            if (visible) gameObject.SetActive(true);
        }

        private void Update()
        {
            m_Alpha = Mathf.MoveTowards(m_Alpha, m_Visible ? 1f : 0f, Time.deltaTime * 6f);
            if (!m_Visible && m_Alpha <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
            float pulse = 0.75f + Mathf.Sin(Time.time * 6f) * 0.25f;
            m_Block ??= new MaterialPropertyBlock();
            Color c = m_Color;
            c.a = m_Alpha * pulse;
            m_Block.SetColor("_Color", c);
            m_Renderer.SetPropertyBlock(m_Block);
        }
    }

    /// <summary>
    /// Gentle "look at me" pulse used by guided hints. Works on 3D objects and UI elements.
    /// Restores the original scale when removed.
    /// </summary>
    public sealed class HintPulse : MonoBehaviour
    {
        private Vector3 m_BaseScale;
        private float m_Time;
        private HighlightRing m_Ring;

        public static HintPulse Begin(Transform target, float ringRadius = 0f)
        {
            if (target == null) return null;
            HintPulse p = target.GetComponent<HintPulse>();
            if (p == null)
            {
                p = target.gameObject.AddComponent<HintPulse>();
                p.m_BaseScale = target.localScale;
            }
            if (ringRadius > 0f && p.m_Ring == null && !(target is RectTransform))
            {
                p.m_Ring = HighlightRing.Create(target, target.position, ringRadius);
                p.m_Ring.SetColor(new Color(0.55f, 0.95f, 0.75f));
                p.m_Ring.SetVisible(true);
            }
            return p;
        }

        public static void End(Transform target)
        {
            if (target == null) return;
            HintPulse p = target.GetComponent<HintPulse>();
            if (p != null) p.Stop();
        }

        public void Stop()
        {
            transform.localScale = m_BaseScale;
            if (m_Ring != null) Destroy(m_Ring.gameObject);
            Destroy(this);
        }

        private void Update()
        {
            m_Time += Time.deltaTime;
            float s = 1f + Mathf.Abs(Mathf.Sin(m_Time * 4.5f)) * 0.1f;
            transform.localScale = m_BaseScale * s;
        }
    }
}
