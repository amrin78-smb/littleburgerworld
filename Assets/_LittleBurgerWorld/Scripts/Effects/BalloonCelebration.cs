using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Effects
{
    /// <summary>
    /// Colourful balloons that float up through the restaurant after a success.
    /// Children can tap them to pop them (confetti!) – a tiny bonus game with no pressure.
    /// </summary>
    public sealed class BalloonCelebration : MonoBehaviour, IInteractable
    {
        private static readonly Color[] s_Colors =
        {
            new Color(0.96f, 0.65f, 0.14f), new Color(0.91f, 0.42f, 0.32f), new Color(0.47f, 0.75f, 0.63f),
            new Color(0.51f, 0.71f, 0.91f), new Color(0.95f, 0.66f, 0.72f), new Color(0.75f, 0.6f, 0.95f)
        };
        private static Mesh s_Balloon;
        private static Mesh s_String;

        private float m_Speed;
        private float m_Phase;
        private float m_Life;
        private bool m_Popped;
        private Color m_Color;

        /// <summary>Launches balloons from below the visible kitchen.</summary>
        public static void Launch(int count)
        {
            Camera cam = Camera.main;
            GameConfig cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cam == null || cfg == null || cfg.balloonMaterial == null) return;
            EnsureMeshes();
            Vector3 right = cam.transform.right;
            right.y = 0f;
            right.Normalize();
            Vector3 center = cam.transform.position + cam.transform.forward * 40f;
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (plane.Raycast(ray, out float d)) center = ray.GetPoint(d);
            float width = cam.orthographic ? cam.orthographicSize * cam.aspect * 1.6f : 8f;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = center + right * Random.Range(-width * 0.5f, width * 0.5f) - (Vector3.Cross(right, Vector3.up)) * Random.Range(-2f, 2f);
                p.y = Random.Range(0.2f, 1.6f);
                Spawn(p, s_Colors[Random.Range(0, s_Colors.Length)], cfg.balloonMaterial);
            }
        }

        private static void EnsureMeshes()
        {
            if (s_Balloon == null)
            {
                var profile = new System.Collections.Generic.List<Vector2>();
                for (int i = 0; i <= 16; i++)
                {
                    float a = -Mathf.PI * 0.5f + i / 16f * Mathf.PI;
                    float r = Mathf.Cos(a) * 0.45f * (a < 0f ? (0.75f + 0.25f * Mathf.Cos(a)) : 1f);
                    profile.Add(new Vector2(Mathf.Max(0f, r), Mathf.Sin(a) * 0.55f + 0.55f));
                }
                profile[0] = new Vector2(0f, 0f);
                profile.Insert(0, new Vector2(0.07f, -0.06f));
                profile.Insert(0, new Vector2(0f, -0.07f));
                s_Balloon = MeshFactory.Lathe(profile, 20, "Balloon");
            }
            if (s_String == null) s_String = MeshFactory.Capsule(0.012f, 1.1f, 6, 2, "BalloonString");
        }

        private static void Spawn(Vector3 position, Color color, Material mat)
        {
            var go = new GameObject("Balloon");
            go.layer = 6;
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = s_Balloon;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            mr.SetPropertyBlock(block);
            var str = new GameObject("String");
            str.transform.SetParent(go.transform, false);
            str.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            str.AddComponent<MeshFilter>().sharedMesh = s_String;
            var smr = str.AddComponent<MeshRenderer>();
            smr.sharedMaterial = mat;
            var sb = new MaterialPropertyBlock();
            sb.SetColor("_BaseColor", new Color(1f, 1f, 1f));
            smr.SetPropertyBlock(sb);
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.6f;
            col.center = new Vector3(0f, 0.55f, 0f);
            go.transform.localScale = Vector3.one * Random.Range(0.9f, 1.25f);
            var b = go.AddComponent<BalloonCelebration>();
            b.m_Speed = Random.Range(1.6f, 2.6f);
            b.m_Phase = Random.Range(0f, 10f);
            b.m_Color = color;
        }

        private void Update()
        {
            m_Life += Time.deltaTime;
            Vector3 p = transform.position;
            p.y += m_Speed * Time.deltaTime;
            p += new Vector3(Mathf.Sin(m_Life * 1.3f + m_Phase), 0f, Mathf.Cos(m_Life * 1.1f + m_Phase)) * (0.25f * Time.deltaTime);
            transform.position = p;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(m_Life * 2f + m_Phase) * 8f);
            if (m_Life > 9f) Destroy(gameObject);
        }

        public int InteractionPriority => 30;
        public bool IsInteractable => !m_Popped;
        public bool CanDrag => false;
        public void OnSelect() { }
        public void OnDeselect() { }
        public IDraggable BeginDrag() => null;

        public void OnTap()
        {
            if (m_Popped) return;
            m_Popped = true;
            FxManager.Play(FxType.Confetti, transform.position + Vector3.up * 0.5f, 25, null);
            FxManager.Play(FxType.Sparkle, transform.position + Vector3.up * 0.5f, 10, m_Color);
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(SfxId.BalloonPop);
            GameEvents.Raise(GameplayEventType.BalloonPopped);
            Destroy(gameObject);
        }
    }
}
