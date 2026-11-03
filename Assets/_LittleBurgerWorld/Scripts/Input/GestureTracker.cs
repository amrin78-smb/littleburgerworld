using UnityEngine;

namespace LittleBurgerWorld.InputHandling
{
    public enum GestureEventType
    {
        None,
        /// <summary>Quick press and release without moving.</summary>
        Tap,
        BeginDrag,
        Drag,
        EndDrag,
        /// <summary>The press or drag was cancelled (finger left the screen edge, app lost focus...).</summary>
        Cancel
    }

    public struct GestureEvent
    {
        public GestureEventType Type;
        public Vector2 Position;
        public Vector2 StartPosition;

        public GestureEvent(GestureEventType type, Vector2 position, Vector2 start)
        {
            Type = type;
            Position = position;
            StartPosition = start;
        }
    }

    /// <summary>
    /// Pure gesture state machine (no Unity input dependency, fully unit tested).
    /// Toddler-tuned: generous movement threshold, holding still on a draggable starts a drag,
    /// and very long presses still count as a tap so slow fingers are never "wrong".
    /// </summary>
    public sealed class GestureTracker
    {
        public enum State { Idle, Pressed, Dragging }

        /// <summary>Pixels the finger must travel before a press becomes a drag.</summary>
        public float DragThreshold = 18f;
        /// <summary>Holding still this long on a draggable object starts a drag (touch-and-hold).</summary>
        public float HoldToDragTime = 0.35f;

        public State Current { get; private set; } = State.Idle;
        public Vector2 StartPosition { get; private set; }
        public Vector2 LastPosition { get; private set; }
        public float PressTime { get; private set; }
        public bool CanDrag { get; set; }

        public GestureEvent Down(Vector2 position, float time, bool canDrag)
        {
            Current = State.Pressed;
            StartPosition = position;
            LastPosition = position;
            PressTime = time;
            CanDrag = canDrag;
            return default;
        }

        public GestureEvent Move(Vector2 position, float time)
        {
            LastPosition = position;
            switch (Current)
            {
                case State.Pressed:
                    if (CanDrag && (position - StartPosition).sqrMagnitude >= DragThreshold * DragThreshold)
                    {
                        Current = State.Dragging;
                        return new GestureEvent(GestureEventType.BeginDrag, position, StartPosition);
                    }
                    return Tick(time);
                case State.Dragging:
                    return new GestureEvent(GestureEventType.Drag, position, StartPosition);
            }
            return default;
        }

        /// <summary>Call every frame while pressed; converts a still hold into a drag.</summary>
        public GestureEvent Tick(float time)
        {
            if (Current == State.Pressed && CanDrag && time - PressTime >= HoldToDragTime)
            {
                Current = State.Dragging;
                return new GestureEvent(GestureEventType.BeginDrag, LastPosition, StartPosition);
            }
            return default;
        }

        public GestureEvent Up(Vector2 position, float time)
        {
            LastPosition = position;
            State was = Current;
            Current = State.Idle;
            switch (was)
            {
                case State.Pressed: return new GestureEvent(GestureEventType.Tap, position, StartPosition);
                case State.Dragging: return new GestureEvent(GestureEventType.EndDrag, position, StartPosition);
            }
            return default;
        }

        public GestureEvent Cancel()
        {
            State was = Current;
            Current = State.Idle;
            return was == State.Idle
                ? default
                : new GestureEvent(GestureEventType.Cancel, LastPosition, StartPosition);
        }

        /// <summary>Forces the tracker into dragging (used when a drag starts from the UI ingredient tray).</summary>
        public void ForceDrag(Vector2 position, float time)
        {
            Current = State.Dragging;
            StartPosition = position;
            LastPosition = position;
            PressTime = time;
            CanDrag = true;
        }
    }
}
