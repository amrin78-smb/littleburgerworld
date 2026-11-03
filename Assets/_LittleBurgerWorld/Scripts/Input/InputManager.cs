using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LittleBurgerWorld.InputHandling
{
    public enum PointerPhase { Down, Move, Up, Cancel }

    public struct PointerSample
    {
        public PointerPhase Phase;
        public Vector2 Position;
        public float Time;
        /// <summary>True when the press started on a UI element (3D interaction must ignore it).</summary>
        public bool StartedOverUI;
    }

    /// <summary>
    /// Unified pointer input built on the Input System. Exposes ONE primary pointer: the first finger
    /// on a touchscreen, or the left mouse button in the Editor. Extra fingers are ignored, so
    /// a toddler resting a palm on the screen never causes double selections.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public event Action<PointerSample> Pointer;

        private bool m_Pressed;
        private bool m_StartedOverUI;
        private int m_TouchId = -1;
        private Vector2 m_LastPosition;
        private readonly List<RaycastResult> m_UiHits = new List<RaycastResult>();
        private PointerEventData m_UiEventData;

        public bool IsPressed => m_Pressed;
        public Vector2 PointerPosition => m_LastPosition;
        /// <summary>When false, all pointer events are swallowed (used during scene transitions/celebrations).</summary>
        public bool InputEnabled { get; set; } = true;
        /// <summary>When false, real devices are not read (automated tests inject samples instead).</summary>
        public bool PollDevices { get; set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelPress();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelPress();
        }

        /// <summary>Cancels the current press safely (dragged items return home).</summary>
        public void CancelPress()
        {
            if (!m_Pressed) return;
            m_Pressed = false;
            m_TouchId = -1;
            Emit(PointerPhase.Cancel, m_LastPosition);
        }

        private void Update()
        {
            if (!InputEnabled)
            {
                CancelPress();
                return;
            }
            if (!PollDevices) return;

            if (TryReadTouch(out bool touchPressed, out Vector2 touchPos, out bool touchCancelled))
            {
                Process(touchPressed, touchPos, touchCancelled);
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Process(mouse.leftButton.isPressed, mouse.position.ReadValue(), false);
            }
            else if (m_Pressed)
            {
                CancelPress();
            }
        }

        private bool TryReadTouch(out bool pressed, out Vector2 position, out bool cancelled)
        {
            pressed = false;
            position = m_LastPosition;
            cancelled = false;
            Touchscreen ts = Touchscreen.current;
            if (ts == null) return false;

            // Keep following the finger that started the press, even if another finger lands.
            if (m_Pressed && m_TouchId >= 0)
            {
                foreach (TouchControl t in ts.touches)
                {
                    if (t.touchId.ReadValue() != m_TouchId) continue;
                    UnityEngine.InputSystem.TouchPhase ph = t.phase.ReadValue();
                    position = t.position.ReadValue();
                    cancelled = ph == UnityEngine.InputSystem.TouchPhase.Canceled;
                    pressed = ph == UnityEngine.InputSystem.TouchPhase.Began ||
                              ph == UnityEngine.InputSystem.TouchPhase.Moved ||
                              ph == UnityEngine.InputSystem.TouchPhase.Stationary;
                    return true;
                }
                // Our finger vanished without an end phase: treat as released.
                pressed = false;
                return true;
            }

            TouchControl primary = ts.primaryTouch;
            if (primary.press.isPressed)
            {
                pressed = true;
                position = primary.position.ReadValue();
                m_TouchId = primary.touchId.ReadValue();
                return true;
            }
            // No touch active: let the mouse drive (Editor / devices with both).
            return m_Pressed && m_TouchId >= 0;
        }

        private void Process(bool pressed, Vector2 position, bool cancelled)
        {
            if (cancelled)
            {
                m_LastPosition = position;
                CancelPress();
                return;
            }

            if (pressed && !m_Pressed)
            {
                m_Pressed = true;
                m_LastPosition = position;
                m_StartedOverUI = IsOverUI(position);
                Emit(PointerPhase.Down, position);
            }
            else if (pressed)
            {
                if ((position - m_LastPosition).sqrMagnitude > 0.01f)
                {
                    m_LastPosition = position;
                    Emit(PointerPhase.Move, position);
                }
                else
                {
                    Emit(PointerPhase.Move, position); // keeps hold timers ticking
                }
            }
            else if (m_Pressed)
            {
                m_Pressed = false;
                m_TouchId = -1;
                m_LastPosition = position;
                Emit(PointerPhase.Up, position);
            }
        }

        private void Emit(PointerPhase phase, Vector2 position)
        {
            Pointer?.Invoke(new PointerSample
            {
                Phase = phase,
                Position = position,
                Time = Time.unscaledTime,
                StartedOverUI = m_StartedOverUI
            });
        }

        /// <summary>True if a UI raycast target is under the screen position.</summary>
        public bool IsOverUI(Vector2 screenPosition)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            m_UiEventData ??= new PointerEventData(es);
            m_UiEventData.Reset();
            m_UiEventData.position = screenPosition;
            m_UiHits.Clear();
            es.RaycastAll(m_UiEventData, m_UiHits);
            return m_UiHits.Count > 0;
        }

        /// <summary>Marks the current press as owned by the 3D world even if it began on UI (tray drags).</summary>
        public void ClaimPressForWorld() => m_StartedOverUI = false;

        /// <summary>Test hook: inject a pointer sample as if it came from a device.</summary>
        public void Inject(PointerPhase phase, Vector2 position, bool overUI = false)
        {
            if (phase == PointerPhase.Down)
            {
                m_Pressed = true;
                m_StartedOverUI = overUI;
            }
            else if (phase == PointerPhase.Up || phase == PointerPhase.Cancel)
            {
                m_Pressed = false;
            }
            m_LastPosition = position;
            Emit(phase, position);
        }
    }
}
