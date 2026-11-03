using System;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>A big squishy 3D button / tappable prop (potato basket, flavour buttons, cup stack...).</summary>
    public sealed class TapButton3D : MonoBehaviour, IInteractable, IAssistable
    {
        [SerializeField] private int m_Priority = 6;
        [SerializeField] private Transform m_Visual;
        [SerializeField] private SfxId m_Sound = SfxId.ButtonTap;
        [SerializeField] private UnityEvent m_OnTap = new UnityEvent();

        private Vector3 m_RestScale;
        private bool m_Captured;

        public event Action Tapped;
        public bool Interactable { get; set; } = true;
        public Transform Visual => m_Visual != null ? m_Visual : transform;

        private void Awake() => Capture();

        private void Capture()
        {
            if (m_Captured) return;
            m_RestScale = Visual.localScale;
            m_Captured = true;
        }

        public int InteractionPriority => m_Priority;
        public bool IsInteractable => Interactable && isActiveAndEnabled;
        public bool CanDrag => false;

        public void OnSelect()
        {
            Capture();
            Tween.Kill(Visual);
            Tween.Scale(Visual, new Vector3(m_RestScale.x * 1.08f, m_RestScale.y * 0.85f, m_RestScale.z * 1.08f), 0.08f, Ease.OutQuad);
        }

        public void OnDeselect()
        {
            Tween.Kill(Visual);
            Tween.Scale(Visual, m_RestScale, 0.25f, Ease.OutBack);
        }

        public void OnTap()
        {
            Capture();
            Tween.Kill(Visual);
            Visual.localScale = m_RestScale;
            Tween.Squash(Visual, m_RestScale, 0.22f, 0.35f);
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(m_Sound);
            Tapped?.Invoke();
            m_OnTap.Invoke();
        }

        public IDraggable BeginDrag() => null;

        public bool Assist(string itemId, string targetKey)
        {
            if (!IsInteractable) return false;
            OnTap();
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ObjectTapped, null, SceneObjectRegistry.KeyOf(this), this));
            return true;
        }

        /// <summary>Plays the "not yet" wiggle (never negative, just a playful shake).</summary>
        public void Wiggle()
        {
            Capture();
            Transform v = Visual;
            Quaternion baseRot = v.localRotation;
            Tween.Kill(v);
            v.localScale = m_RestScale;
            Tween.Value(v, 0.4f, k =>
            {
                v.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 4f) * 10f * (1f - k));
            }, Ease.Linear, () => { if (v != null) v.localRotation = baseRot; });
        }
    }
}
