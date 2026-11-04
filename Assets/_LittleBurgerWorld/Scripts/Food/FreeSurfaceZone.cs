using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// A stretch of counter where anything can be put down freely (Free Play experimentation).
    /// Lowest drop priority: stations always win when in range.
    /// </summary>
    public sealed class FreeSurfaceZone : DropZone, IFoodContainer
    {
        [SerializeField] private float m_SurfaceHeight = 0.95f;
        [Tooltip("World XZ footprints of stations standing on this counter (x, z = min corner). Food is never put down inside them.")]
        [SerializeField] private List<Rect> m_KeepOut = new List<Rect>();
        private const float k_KeepOutMargin = 0.3f;
        private bool m_Returning;

        private readonly List<FoodItem> m_Items = new List<FoodItem>();

        public IReadOnlyList<FoodItem> Items => m_Items;

        private void Reset()
        {
            m_DropPriority = 0;
            m_BoxSize = new Vector2(3f, 1.2f);
        }

        public override bool CanAccept(IDraggable item)
        {
            return item is FoodItem f && !f.IsConsumed && f.Type != FoodType.Tray;
        }

        public override void SetDropHighlight(bool on)
        {
            // Counters are everywhere – no ring, the drag shadow is enough.
        }

        public override void Accept(IDraggable item, Vector3 worldPoint)
        {
            if (!(item is FoodItem f)) return;
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            Vector3 ls = transform.lossyScale;
            float hx = m_BoxSize.x * 0.5f / Mathf.Max(0.001f, ls.x) * 0.9f;
            float hz = m_BoxSize.y * 0.5f / Mathf.Max(0.001f, ls.z) * 0.9f;
            local.x = Mathf.Clamp(local.x, -hx, hx);
            local.z = Mathf.Clamp(local.z, -hz, hz);
            Vector3 p = transform.TransformPoint(local);
            p.y = m_SurfaceHeight;
            if (!AvoidStations(ref p, hx, hz) && !m_Returning)
            {
                // No free spot here (e.g. right on top of the grill): back where it came from.
                m_Returning = true;
                try { f.ReturnHome(); }
                finally { m_Returning = false; }
                return;
            }

            // Resting on another loose item? Sit on top of it instead of intersecting.
            foreach (FoodItem other in m_Items)
            {
                if (other == null || other == f) continue;
                Vector3 o = other.transform.position;
                if (new Vector2(o.x - p.x, o.z - p.z).magnitude < 0.55f) p.y = Mathf.Max(p.y, o.y + other.Height);
            }

            m_Items.Add(f);
            f.AttachTo(this);
            Tween.Kill(f.transform);
            Tween.Jump(f.transform, p, 0.3f, 0.25f, () =>
            {
                if (f == null) return;
                f.transform.localScale = f.RestScale;
                Tween.Squash(f.transform, f.RestScale, 0.22f);
                f.PlaySfx(SfxId.Plop, 0.8f);
            });
            Tween.RotateLocal(f.transform, Quaternion.Euler(0f, f.transform.eulerAngles.y, 0f), 0.2f);
        }

        /// <summary>Moves a point that landed on a station's body to the nearest free spot beside it.</summary>
        private bool AvoidStations(ref Vector3 p, float hx, float hz)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                bool moved = false;
                foreach (Rect r in m_KeepOut)
                {
                    Rect k = new Rect(r.xMin - k_KeepOutMargin, r.yMin - k_KeepOutMargin, r.width + 2f * k_KeepOutMargin, r.height + 2f * k_KeepOutMargin);
                    if (!k.Contains(new Vector2(p.x, p.z))) continue;
                    Vector3 best = p;
                    float bestDist = float.MaxValue;
                    foreach (Vector3 c in new[]
                             {
                                 new Vector3(k.xMin, p.y, p.z), new Vector3(k.xMax, p.y, p.z),
                                 new Vector3(p.x, p.y, k.yMin), new Vector3(p.x, p.y, k.yMax)
                             })
                    {
                        if (!OnSurface(c, hx, hz)) continue;
                        float d = (c - p).sqrMagnitude;
                        if (d < bestDist) { bestDist = d; best = c; }
                    }
                    if (bestDist == float.MaxValue) return false;
                    p = best;
                    moved = true;
                }
                if (!moved) return true;
            }
            return false;
        }

        private bool OnSurface(Vector3 world, float hx, float hz)
        {
            Vector3 l = transform.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= hx + 0.001f && Mathf.Abs(l.z) <= hz + 0.001f;
        }

        public void Release(FoodItem item) => m_Items.Remove(item);
        public bool HandleTap(FoodItem item) => false;
        public bool AllowPickup(FoodItem item) => true;
    }
}
