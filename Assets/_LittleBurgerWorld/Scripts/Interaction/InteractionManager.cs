using System;
using System.Collections.Generic;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.InputHandling;
using UnityEngine;

namespace LittleBurgerWorld.Interaction
{
    /// <summary>
    /// Turns pointer input into toddler-friendly interactions: tap, touch-and-hold, drag, drop and snap.
    /// - Picks the top-most interactable with the highest priority, with a generous touch radius.
    /// - Presses that start on UI never reach the 3D world.
    /// - Dragged objects glide on a raised plane slightly above the finger so they stay visible.
    /// - Releasing over nothing, or a cancelled touch, sends the object safely back.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [SerializeField] private Camera m_Camera;
        [SerializeField] private InputManager m_Input;
        [SerializeField] private LayerMask m_InteractableMask = 1 << 6;
        [Tooltip("Extra touch radius in world units when the finger is near, but not on, an object.")]
        [SerializeField] private float m_TouchRadius = 0.45f;
        [Tooltip("Dragged objects float this fraction of screen height above the finger so it never hides them.")]
        [SerializeField] private float m_FingerOffset = 0.06f;

        private readonly GestureTracker m_Gesture = new GestureTracker();
        private readonly RaycastHit[] m_Hits = new RaycastHit[24];
        private readonly List<(IInteractable item, float dist)> m_Candidates = new List<(IInteractable, float)>();

        private IInteractable m_Pressed;
        private IDraggable m_Dragging;
        private IDropTarget m_Hover;
        private bool m_IgnoreCurrentPress;
        private bool m_ExternalDrag;
        private float m_DragStartTime;
        private Vector3 m_DragTarget;
        private Vector3 m_DropPoint;

        /// <summary>Height of the counters: drops are detected where the finger's ray meets this plane,
        /// i.e. exactly where the lifted item appears to be on screen.</summary>
        public const float SurfaceHeight = 0.95f;
        private DragShadow m_Shadow;

        public event Action<IDraggable> DragStarted;
        public event Action<IDraggable, IDropTarget> DragEnded;
        public event Action<IInteractable> Tapped;

        public bool IsDragging => m_Dragging != null;
        public IDraggable CurrentDrag => m_Dragging;
        public IDropTarget CurrentHover => m_Hover;
        public Vector3 CurrentDropPoint => m_DropPoint;
        public Camera Camera => m_Camera;
        public bool Locked { get; set; }

        private void Awake()
        {
            Instance = this;
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Input == null) m_Input = FindFirstObjectByType<InputManager>();
            m_Gesture.DragThreshold = Mathf.Max(12f, Screen.dpi > 0 ? Screen.dpi * 0.08f : 18f);
        }

        private void OnEnable()
        {
            if (m_Input == null) m_Input = FindFirstObjectByType<InputManager>();
            if (m_Input != null) m_Input.Pointer += OnPointer;
        }

        private void OnDisable()
        {
            if (m_Input != null) m_Input.Pointer -= OnPointer;
            CancelAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetCamera(Camera cam) => m_Camera = cam;

        // ------------------------------------------------------------------ pointer pipeline
        private void OnPointer(PointerSample s)
        {
            if (Locked && s.Phase != PointerPhase.Up && s.Phase != PointerPhase.Cancel) return;
            switch (s.Phase)
            {
                case PointerPhase.Down: HandleDown(s); break;
                case PointerPhase.Move: if (!m_IgnoreCurrentPress) Handle(m_Gesture.Move(s.Position, s.Time)); break;
                case PointerPhase.Up: if (!m_IgnoreCurrentPress) Handle(m_Gesture.Up(s.Position, s.Time)); m_IgnoreCurrentPress = false; break;
                case PointerPhase.Cancel: Handle(m_Gesture.Cancel()); m_IgnoreCurrentPress = false; break;
            }
        }

        private void HandleDown(PointerSample s)
        {
            if (m_Dragging != null) return; // one thing at a time
            m_IgnoreCurrentPress = s.StartedOverUI;
            if (m_IgnoreCurrentPress) return;

            m_Pressed = Pick(s.Position);
            m_Gesture.Down(s.Position, s.Time, m_Pressed != null && m_Pressed.CanDrag);
            m_Pressed?.OnSelect();
        }

        private static bool Alive(object o) => o != null && !(o is UnityEngine.Object u && u == null);

        private void Handle(GestureEvent e)
        {
            if (!Alive(m_Pressed)) m_Pressed = null;
            switch (e.Type)
            {
                case GestureEventType.BeginDrag:
                    if (m_Pressed == null) return;
                    m_Pressed.OnDeselect();
                    IDraggable d = m_Pressed.BeginDrag();
                    if (d == null) return;
                    StartDragInternal(d, e.Position, false);
                    break;
                case GestureEventType.Drag:
                    UpdateDrag(e.Position);
                    break;
                case GestureEventType.EndDrag:
                    FinishDrag(e.Position, e.StartPosition);
                    break;
                case GestureEventType.Tap:
                    DoTap(m_Pressed);
                    m_Pressed = null;
                    break;
                case GestureEventType.Cancel:
                    if (m_Dragging != null) CancelDrag();
                    m_Pressed?.OnDeselect();
                    m_Pressed = null;
                    break;
            }
        }

        private void DoTap(IInteractable target)
        {
            if (!Alive(target)) return;
            target.OnDeselect();
            if (!target.IsInteractable) return;
            target.OnTap();
            Tapped?.Invoke(target);
            string key = target is Component c ? SceneObjectRegistry.KeyOf(c) : null;
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ObjectTapped, null, key, target as Component));
        }

        // ------------------------------------------------------------------ picking
        /// <summary>Finds the best interactable under a screen position (public for tests and hints).</summary>
        public IInteractable Pick(Vector2 screenPos)
        {
            if (m_Camera == null) return null;
            Physics.SyncTransforms();
            Ray ray = m_Camera.ScreenPointToRay(screenPos);
            m_Candidates.Clear();
            int n = Physics.RaycastNonAlloc(ray, m_Hits, 500f, m_InteractableMask, QueryTriggerInteraction.Collide);
            Collect(n);
            if (m_Candidates.Count == 0)
            {
                n = Physics.SphereCastNonAlloc(ray, m_TouchRadius, m_Hits, 500f, m_InteractableMask,
                    QueryTriggerInteraction.Collide);
                Collect(n);
            }
            IInteractable best = null;
            int bestPriority = int.MinValue;
            float bestDist = float.MaxValue;
            foreach (var (item, dist) in m_Candidates)
            {
                if (item.InteractionPriority > bestPriority ||
                    (item.InteractionPriority == bestPriority && dist < bestDist))
                {
                    best = item;
                    bestPriority = item.InteractionPriority;
                    bestDist = dist;
                }
            }
            return best;
        }

        private void Collect(int n)
        {
            for (int i = 0; i < n; i++)
            {
                IInteractable it = m_Hits[i].collider.GetComponentInParent<IInteractable>();
                if (it == null || !it.IsInteractable) continue;
                bool dup = false;
                for (int k = 0; k < m_Candidates.Count; k++)
                {
                    if (ReferenceEquals(m_Candidates[k].item, it)) { dup = true; break; }
                }
                if (!dup) m_Candidates.Add((it, m_Hits[i].distance));
            }
        }

        // ------------------------------------------------------------------ dragging
        /// <summary>
        /// Starts dragging an object that was created from the UI (ingredient tray). The finger is
        /// already down on the UI; from now on the 3D world owns the press.
        /// </summary>
        public void BeginExternalDrag(IDraggable item, Vector2 screenPos)
        {
            if (item == null) return;
            if (m_Dragging != null) CancelDrag();
            m_IgnoreCurrentPress = false;
            m_Input?.ClaimPressForWorld();
            m_Gesture.ForceDrag(screenPos, Time.unscaledTime);
            m_Pressed = null;
            StartDragInternal(item, screenPos, true);
        }

        private void StartDragInternal(IDraggable d, Vector2 screenPos, bool external)
        {
            m_Dragging = d;
            m_ExternalDrag = external;
            m_DragStartTime = Time.unscaledTime;
            d.OnDragStarted();
            if (m_Shadow == null) m_Shadow = DragShadow.Create();
            m_Shadow.Follow(d.Transform);
            UpdateDrag(screenPos);
            DragStarted?.Invoke(d);
            var food = d as Component;
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ItemPicked, ItemIdOf(d), null, food));
        }

        private void UpdateDrag(Vector2 screenPos)
        {
            if (m_Dragging == null) return;
            if (m_Dragging.Transform == null) { ClearDrag(); return; }
            m_DragTarget = ScreenToDragPlane(screenPos + FingerOffset(), m_Dragging.DragHeight);
            m_DropPoint = ScreenToDragPlane(screenPos + FingerOffset(), SurfaceHeight);
            m_Dragging.OnDragMoved(m_DragTarget);
            if (m_Shadow != null) m_Shadow.SetPoint(m_DropPoint);
            SetHover(DropTargetRegistry.FindBest(m_Dragging, m_DropPoint));
        }

        private Vector2 FingerOffset() => new Vector2(0f, Screen.height * m_FingerOffset);

        private void FinishDrag(Vector2 screenPos, Vector2 startPos)
        {
            IDraggable d = m_Dragging;
            if (d == null) return;
            if (d.Transform == null) { ClearDrag(); return; }

            // A slow "tap" that turned into a hold-drag without moving is still a tap.
            bool tapLike = !m_ExternalDrag && m_Pressed != null &&
                           (screenPos - startPos).magnitude < m_Gesture.DragThreshold &&
                           Time.unscaledTime - m_DragStartTime < 1.2f;
            if (tapLike)
            {
                IInteractable pressed = m_Pressed;
                ClearDrag();
                d.OnDragCancelled();
                DoTap(pressed);
                m_Pressed = null;
                return;
            }

            Vector3 point = ScreenToDragPlane(screenPos + FingerOffset(), SurfaceHeight);
            IDropTarget target = DropTargetRegistry.FindBest(d, point);
            ClearDrag();
            if (target != null)
            {
                target.Accept(d, point);
                d.OnDropped(target);
            }
            else
            {
                d.OnDragCancelled();
            }
            m_Pressed = null;
            DragEnded?.Invoke(d, target);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ItemDropped, ItemIdOf(d),
                target is Component tc ? SceneObjectRegistry.KeyOf(tc) : null, d as Component));
        }

        /// <summary>Drops the current drag at a world point (used by tests and auto-assist).</summary>
        public void DropAt(Vector3 worldPoint)
        {
            IDraggable d = m_Dragging;
            if (d == null) return;
            IDropTarget target = DropTargetRegistry.FindBest(d, worldPoint);
            ClearDrag();
            if (target != null) { target.Accept(d, worldPoint); d.OnDropped(target); }
            else d.OnDragCancelled();
            DragEnded?.Invoke(d, target);
        }

        public void CancelDrag()
        {
            IDraggable d = m_Dragging;
            ClearDrag();
            if (d != null && d.Transform != null) d.OnDragCancelled();
            if (d != null) DragEnded?.Invoke(d, null);
        }

        private void ClearDrag()
        {
            SetHover(null);
            m_Dragging = null;
            m_ExternalDrag = false;
            if (m_Shadow != null) m_Shadow.Follow(null);
            m_Gesture.Cancel();
        }

        private void CancelAll()
        {
            if (m_Dragging != null) CancelDrag();
            m_Pressed = null;
        }

        private void SetHover(IDropTarget t)
        {
            if (ReferenceEquals(t, m_Hover)) return;
            if (m_Hover != null && !(m_Hover is UnityEngine.Object o && o == null)) m_Hover.SetDropHighlight(false);
            m_Hover = t;
            m_Hover?.SetDropHighlight(true);
        }

        private void Update()
        {
            if (m_Dragging == null && m_Input != null && m_Input.IsPressed && !m_IgnoreCurrentPress && m_Pressed != null)
            {
                Handle(m_Gesture.Tick(Time.unscaledTime));
            }
        }

        public Vector3 ScreenToDragPlane(Vector2 screenPos, float height)
        {
            if (m_Camera == null) return Vector3.zero;
            Ray ray = m_Camera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : ray.origin;
        }

        public Vector2 WorldToScreen(Vector3 world) =>
            m_Camera != null ? (Vector2)m_Camera.WorldToScreenPoint(world) : Vector2.zero;

        public static string ItemIdOf(IDraggable d) => d is Food.FoodItem f ? f.ItemId : null;
    }

    /// <summary>A soft blob shadow under the dragged item so toddlers see where it will land.</summary>
    public sealed class DragShadow : MonoBehaviour
    {
        private Transform m_Target;
        private Vector3 m_Point;
        private MeshRenderer m_Renderer;
        private readonly RaycastHit[] m_Hits = new RaycastHit[8];

        public static DragShadow Create()
        {
            var go = new GameObject("DragShadow");
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.FlatQuad(1f, 1f, "ShadowQuad");
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            GameConfig cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cfg != null && cfg.softParticleMaterial != null)
            {
                mr.sharedMaterial = cfg.softParticleMaterial;
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", new Color(0.25f, 0.15f, 0.1f, 0.35f));
                mr.SetPropertyBlock(block);
            }
            var s = go.AddComponent<DragShadow>();
            s.m_Renderer = mr;
            go.SetActive(false);
            return s;
        }

        public void SetPoint(Vector3 p) => m_Point = p;

        public void Follow(Transform target)
        {
            m_Target = target;
            gameObject.SetActive(target != null);
        }

        private void LateUpdate()
        {
            if (m_Target == null) { gameObject.SetActive(false); return; }
            Vector3 origin = new Vector3(m_Point.x, m_Target.position.y + 0.05f, m_Point.z);
            float y = 0.01f;
            int n = Physics.RaycastNonAlloc(origin, Vector3.down, m_Hits, 20f, ~(1 << 6), QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (m_Hits[i].distance < best) { best = m_Hits[i].distance; y = m_Hits[i].point.y + 0.015f; }
            }
            transform.position = new Vector3(m_Point.x, y, m_Point.z);
            float h = Mathf.Clamp(m_Target.position.y - y, 0f, 3f);
            float size = Mathf.Lerp(1.1f, 0.7f, h / 3f);
            transform.localScale = new Vector3(size, 1f, size);
        }
    }
}
