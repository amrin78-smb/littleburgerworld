using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using UnityEngine;

namespace LittleBurgerWorld.Interaction
{
    /// <summary>
    /// Base class for drop targets. Handles registration, a generous circular (or rectangular)
    /// catch area in the XZ plane, and the glowing "drop here" ring.
    /// </summary>
    public abstract class DropZone : MonoBehaviour, IDropTarget
    {
        [Header("Drop area")]
        [SerializeField] protected Transform m_SnapPoint;
        [SerializeField] protected float m_AcceptRadius = 1.2f;
        [Tooltip("When > 0 the catch area is a rectangle of this size (XZ, local) instead of a circle.")]
        [SerializeField] protected Vector2 m_BoxSize = Vector2.zero;
        [SerializeField] protected int m_DropPriority = 10;
        [SerializeField] protected float m_RingRadius = 0.9f;

        private HighlightRing m_Ring;
        private bool m_Highlighted;

        public virtual int DropPriority => m_DropPriority;
        public virtual Vector3 SnapPoint => m_SnapPoint != null ? m_SnapPoint.position : transform.position;
        public float AcceptRadius => m_AcceptRadius;
        /// <summary>Where the "drop here" ring is drawn (defaults to the snap point).</summary>
        protected virtual Vector3 RingPosition => SnapPoint;

        protected virtual void OnEnable() => DropTargetRegistry.Register(this);

        protected virtual void OnDisable()
        {
            DropTargetRegistry.Unregister(this);
            SetDropHighlight(false);
        }

        public bool TryScore(IDraggable item, Vector3 worldPoint, out float distance)
        {
            distance = float.MaxValue;
            if (!isActiveAndEnabled || item == null) return false;
            if (!InCatchArea(worldPoint, out distance)) return false;
            return CanAccept(item);
        }

        protected bool InCatchArea(Vector3 worldPoint, out float distance)
        {
            Vector3 center = SnapPoint;
            if (m_BoxSize.x > 0f && m_BoxSize.y > 0f)
            {
                Vector3 local = transform.InverseTransformPoint(worldPoint);
                Vector3 s = transform.lossyScale;
                float hx = m_BoxSize.x * 0.5f / Mathf.Max(0.0001f, s.x);
                float hz = m_BoxSize.y * 0.5f / Mathf.Max(0.0001f, s.z);
                distance = new Vector2(worldPoint.x - center.x, worldPoint.z - center.z).magnitude;
                return Mathf.Abs(local.x) <= hx && Mathf.Abs(local.z) <= hz;
            }
            distance = new Vector2(worldPoint.x - center.x, worldPoint.z - center.z).magnitude;
            return distance <= m_AcceptRadius;
        }

        /// <summary>Food-specific acceptance rule.</summary>
        public abstract bool CanAccept(IDraggable item);

        public abstract void Accept(IDraggable item, Vector3 worldPoint);

        public virtual void SetDropHighlight(bool on)
        {
            if (on == m_Highlighted) return;
            m_Highlighted = on;
            if (on && m_Ring == null) m_Ring = HighlightRing.Create(transform, RingPosition, m_RingRadius);
            if (m_Ring != null) m_Ring.SetVisible(on);
        }

        protected void RaiseDropped(GameplayEventType type, string itemId, string extra = null)
        {
            GameEvents.Raise(new GameplayEvent(type, itemId, SceneObjectRegistry.KeyOf(this), this, extra));
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.6f, 0.7f);
            if (m_BoxSize.x > 0f)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(m_BoxSize.x / transform.lossyScale.x, 0.05f,
                    m_BoxSize.y / transform.lossyScale.z));
            }
            else
            {
                Gizmos.DrawWireSphere(SnapPoint, m_AcceptRadius);
            }
        }
    }
}
