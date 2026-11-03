using UnityEngine;

namespace LittleBurgerWorld.Cameras
{
    /// <summary>
    /// Fixed orthographic isometric camera that frames a world-space box on any aspect ratio
    /// (16:9 phones, 19.5:9 phones, 4:3 tablets). The child can never move or rotate it.
    /// Screen bands reserved for UI (top HUD, bottom ingredient tray) and the device safe area
    /// are respected so interactive objects are never hidden.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraController : MonoBehaviour
    {
        [Header("Framing")]
        [SerializeField] private Vector3 m_FocusCenter = new Vector3(0f, 0.8f, 0f);
        [SerializeField] private Vector3 m_FocusSize = new Vector3(10f, 2f, 9f);
        [Tooltip("Yaw of the focus box. 45 aligns it with the screen (x = across, z = up the screen).")]
        [SerializeField] private float m_FocusYaw = 45f;
        [SerializeField, Range(10f, 80f)] private float m_Pitch = 35f;
        [SerializeField] private float m_Yaw = 45f;
        [SerializeField] private float m_Distance = 22f;

        [Header("Screen bands (fractions of screen height)")]
        [SerializeField, Range(0f, 0.4f)] private float m_ReservedTop = 0.08f;
        [SerializeField, Range(0f, 0.4f)] private float m_ReservedBottom = 0.2f;
        [SerializeField, Range(0f, 0.3f)] private float m_Padding = 0.02f;

        private Camera m_Camera;
        private int m_LastWidth;
        private int m_LastHeight;
        private Rect m_LastSafeArea;

        public Camera Camera => m_Camera != null ? m_Camera : (m_Camera = GetComponent<Camera>());
        public Vector3 FocusCenter => m_FocusCenter;
        public Vector3 FocusSize => m_FocusSize;

        public void Configure(Vector3 center, Vector3 size, float focusYaw, float reservedTop, float reservedBottom)
        {
            m_FocusCenter = center;
            m_FocusSize = size;
            m_FocusYaw = focusYaw;
            m_ReservedTop = reservedTop;
            m_ReservedBottom = reservedBottom;
            Apply();
        }

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void LateUpdate()
        {
            Rect safe = Screen.safeArea;
            if (Screen.width != m_LastWidth || Screen.height != m_LastHeight || safe != m_LastSafeArea) Apply();
        }

        /// <summary>Re-computes rotation, position and orthographic size for the current screen.</summary>
        public void Apply()
        {
            Camera cam = Camera;
            if (cam == null) return;
            m_LastWidth = Screen.width;
            m_LastHeight = Screen.height;
            m_LastSafeArea = Screen.safeArea;
            ApplyInternal(cam.aspect > 0.01f ? cam.aspect : 16f / 9f, NormalizedSafeArea());
        }

        /// <summary>Frames for an explicit aspect ratio with no safe-area insets (screenshots, tests).</summary>
        public void ApplyForAspect(float aspect) => ApplyInternal(aspect, new Rect(0f, 0f, 1f, 1f));

        private void ApplyInternal(float aspect, Rect safeNorm)
        {
            Camera cam = Camera;
            if (cam == null) return;
            Quaternion rot = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
            Framing f = ComputeFraming(m_FocusCenter, m_FocusSize, Quaternion.Euler(0f, m_FocusYaw, 0f), rot, aspect, m_ReservedTop, m_ReservedBottom,
                m_Padding, safeNorm);

            cam.orthographic = true;
            cam.orthographicSize = f.OrthographicSize;
            transform.rotation = rot;
            transform.position = m_FocusCenter - rot * Vector3.forward * m_Distance + rot * new Vector3(f.Offset.x, f.Offset.y, 0f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = m_Distance * 2f + m_FocusSize.magnitude;
        }

        private static Rect NormalizedSafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return new Rect(0f, 0f, 1f, 1f);
            Rect s = Screen.safeArea;
            return new Rect(s.x / Screen.width, s.y / Screen.height, s.width / Screen.width, s.height / Screen.height);
        }

        /// <summary>Result of <see cref="ComputeFraming"/>.</summary>
        public struct Framing
        {
            public float OrthographicSize;
            /// <summary>Camera-space shift (x right, y up) so the box sits inside the usable screen region.</summary>
            public Vector2 Offset;
            /// <summary>Half extents of the focus box as seen by the camera.</summary>
            public Vector2 HalfExtents;
        }

        /// <summary>
        /// Pure framing maths (unit tested). Projects the focus box into camera space and finds the smallest
        /// orthographic size that fits it inside the usable screen region (excluding UI bands and unsafe edges).
        /// </summary>
        public static Framing ComputeFraming(Vector3 center, Vector3 size, Quaternion focusRotation, Quaternion rotation,
            float aspect, float reservedTop, float reservedBottom, float padding, Rect safeArea)
        {
            Quaternion inv = Quaternion.Inverse(rotation);
            Vector3 h = size * 0.5f;
            float maxX = 0f, maxY = 0f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                Vector3 c = inv * (focusRotation * corner);
                maxX = Mathf.Max(maxX, Mathf.Abs(c.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(c.y));
            }

            // Usable normalised region: the safe area minus UI bands.
            float left = Mathf.Clamp01(safeArea.xMin);
            float right = Mathf.Clamp01(safeArea.xMax);
            float bottom = Mathf.Max(Mathf.Clamp01(safeArea.yMin), reservedBottom);
            float top = Mathf.Min(Mathf.Clamp01(safeArea.yMax), 1f - reservedTop);
            float usableW = Mathf.Max(0.2f, right - left);
            float usableH = Mathf.Max(0.2f, top - bottom);

            float pad = 1f + padding * 2f;
            // Full view height = 2*size; full width = 2*size*aspect.
            float sizeForHeight = maxY * pad / usableH;
            float sizeForWidth = maxX * pad / (usableW * aspect);
            float ortho = Mathf.Max(sizeForHeight, sizeForWidth, 0.5f);

            // Shift so the box centre lands in the centre of the usable region.
            float usableCenterY = (bottom + top) * 0.5f;  // 0..1
            float usableCenterX = (left + right) * 0.5f;
            float offsetY = (0.5f - usableCenterY) * 2f * ortho;
            float offsetX = (0.5f - usableCenterX) * 2f * ortho * aspect;

            return new Framing
            {
                OrthographicSize = ortho,
                Offset = new Vector2(offsetX, offsetY),
                HalfExtents = new Vector2(maxX, maxY)
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(m_FocusCenter, Quaternion.Euler(0f, m_FocusYaw, 0f), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, m_FocusSize);
        }
    }
}
