using System.Collections.Generic;
using UnityEngine;

namespace LittleBurgerWorld.Interaction
{
    /// <summary>Anything the child can touch: food, station buttons, customers, balloons.</summary>
    public interface IInteractable
    {
        /// <summary>Higher wins when several objects are under the finger.</summary>
        int InteractionPriority { get; }
        bool IsInteractable { get; }
        bool CanDrag { get; }
        /// <summary>Finger touched the object (subtle highlight + sound).</summary>
        void OnSelect();
        /// <summary>Finger released or the press turned into a drag.</summary>
        void OnDeselect();
        void OnTap();
        /// <summary>
        /// Starts a drag. Returns the object that is actually dragged – usually itself,
        /// but a supply (e.g. potato basket) may return a freshly spawned item. Null = not draggable now.
        /// </summary>
        IDraggable BeginDrag();
    }

    /// <summary>An object following the finger.</summary>
    public interface IDraggable
    {
        Transform Transform { get; }
        /// <summary>Height of the horizontal plane the object glides on while dragged.</summary>
        float DragHeight { get; }
        void OnDragStarted();
        /// <summary>Called every frame with the target point on the drag plane.</summary>
        void OnDragMoved(Vector3 worldTarget);
        /// <summary>Released over nothing, or the touch was cancelled: go back somewhere safe.</summary>
        void OnDragCancelled();
        /// <summary>Called after a drop target accepted the item.</summary>
        void OnDropped(IDropTarget target);
    }

    /// <summary>A place things can be dropped on (burger board, grill, tray, customer...).</summary>
    public interface IDropTarget
    {
        /// <summary>Higher wins when several targets are in range (specific stations beat free counter space).</summary>
        int DropPriority { get; }
        /// <summary>True when the target accepts the item at that point; distance is used to pick the closest.</summary>
        bool TryScore(IDraggable item, Vector3 worldPoint, out float distance);
        void Accept(IDraggable item, Vector3 worldPoint);
        void SetDropHighlight(bool on);
        /// <summary>Where the item will land – used by hint animations and auto-send.</summary>
        Vector3 SnapPoint { get; }
    }

    /// <summary>Registry of active drop targets; avoids physics queries during drags.</summary>
    public static class DropTargetRegistry
    {
        private static readonly List<IDropTarget> s_Targets = new List<IDropTarget>();

        public static IReadOnlyList<IDropTarget> All => s_Targets;

        public static void Register(IDropTarget t)
        {
            if (t != null && !s_Targets.Contains(t)) s_Targets.Add(t);
        }

        public static void Unregister(IDropTarget t) => s_Targets.Remove(t);

        public static IDropTarget FindBest(IDraggable item, Vector3 worldPoint)
        {
            IDropTarget best = null;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;
            for (int i = s_Targets.Count - 1; i >= 0; i--)
            {
                IDropTarget t = s_Targets[i];
                if (t == null || (t is Object o && o == null)) { s_Targets.RemoveAt(i); continue; }
                if (ReferenceEquals(t, item)) continue;
                if (!t.TryScore(item, worldPoint, out float d)) continue;
                if (t.DropPriority > bestPriority || (t.DropPriority == bestPriority && d < bestDistance))
                {
                    best = t;
                    bestPriority = t.DropPriority;
                    bestDistance = d;
                }
            }
            return best;
        }
    }
}
