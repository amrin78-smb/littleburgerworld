using UnityEngine;

namespace LittleBurgerWorld.UI
{
    /// <summary>Gentle floating + turning for showcase props (main menu giant burger).</summary>
    public sealed class MenuBobber : MonoBehaviour
    {
        [SerializeField] private float m_Amplitude = 0.1f;
        [SerializeField] private float m_Speed = 1.6f;
        [SerializeField] private float m_TurnSpeed = 18f;

        private Vector3 m_Start;

        private void Awake() => m_Start = transform.localPosition;

        private void Update()
        {
            transform.localPosition = m_Start + Vector3.up * (Mathf.Sin(Time.time * m_Speed) * m_Amplitude);
            transform.Rotate(0f, m_TurnSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
