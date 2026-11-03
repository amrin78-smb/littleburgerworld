using UnityEngine;

namespace LittleBurgerWorld.Customers
{
    public enum CharacterPose
    {
        Idle,
        Walk,
        Happy,
        Eat,
        Celebrate,
        Wave
    }

    /// <summary>
    /// Procedural transform animation for the toy-like animal characters (no Animator assets needed).
    /// Limbs are pivots placed at shoulders/hips; the character faces local +Z.
    /// Poses: idle breathing + blinking, waddle walk, happy jump, chomp-chomp eating,
    /// spinning celebration and a friendly wave.
    /// </summary>
    public sealed class ProceduralCharacterRig : MonoBehaviour
    {
        public Transform model;
        public Transform body;
        public Transform head;
        public Transform earLeft;
        public Transform earRight;
        public Transform armLeft;
        public Transform armRight;
        public Transform legLeft;
        public Transform legRight;
        public Transform eyeLeft;
        public Transform eyeRight;
        public Transform mouth;
        public Transform holdPoint;

        private Pose m_Model, m_Body, m_Head, m_EarL, m_EarR, m_ArmL, m_ArmR, m_LegL, m_LegR;
        private Vector3 m_EyeScaleL, m_EyeScaleR, m_MouthScale;
        private bool m_Captured;
        private CharacterPose m_Pose = CharacterPose.Idle;
        private float m_PoseTime;
        private float m_Time;
        private float m_NextBlink = 2f;
        private float m_BlinkT = -1f;
        private float m_NextIdleWave = 6f;

        private struct Pose
        {
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 scale;
        }

        public CharacterPose CurrentPose => m_Pose;
        /// <summary>When true, waiting characters occasionally wave on their own.</summary>
        public bool IdleWaves { get; set; } = true;

        private void Awake() => Capture();

        private void Capture()
        {
            if (m_Captured) return;
            m_Model = Grab(model); m_Body = Grab(body); m_Head = Grab(head);
            m_EarL = Grab(earLeft); m_EarR = Grab(earRight);
            m_ArmL = Grab(armLeft); m_ArmR = Grab(armRight);
            m_LegL = Grab(legLeft); m_LegR = Grab(legRight);
            m_EyeScaleL = eyeLeft != null ? eyeLeft.localScale : Vector3.one;
            m_EyeScaleR = eyeRight != null ? eyeRight.localScale : Vector3.one;
            m_MouthScale = mouth != null ? mouth.localScale : Vector3.one;
            m_Captured = true;
        }

        private static Pose Grab(Transform t) =>
            t == null ? default : new Pose { pos = t.localPosition, rot = t.localRotation, scale = t.localScale };

        public void SetPose(CharacterPose pose)
        {
            if (pose == m_Pose) return;
            m_Pose = pose;
            m_PoseTime = 0f;
        }

        private void LateUpdate()
        {
            Capture();
            float dt = Time.deltaTime;
            m_Time += dt;
            m_PoseTime += dt;
            Restore();

            float t = m_PoseTime;
            float armL = 8f, armR = -8f;   // z-rotation (sideways raise), left negative = outwards
            float armSwing = 0f;           // x-rotation (forward/back)
            float legSwing = 0f;
            float bob = 0f, roll = 0f, spin = 0f;
            float headNod = 0f, headTilt = Mathf.Sin(m_Time * 0.7f) * 4f;
            float breathe = 1f + Mathf.Sin(m_Time * 2.2f) * 0.02f;
            float mouthOpen = 1f;
            float eyeSquint = 1f;
            float earWiggle = Mathf.Sin(m_Time * 3f) * 4f;

            switch (m_Pose)
            {
                case CharacterPose.Idle:
                    bob = Mathf.Sin(m_Time * 2.2f) * 0.015f;
                    armL = -6f + Mathf.Sin(m_Time * 1.8f) * 4f;
                    armR = 6f - Mathf.Sin(m_Time * 1.8f) * 4f;
                    if (IdleWaves && m_Time > m_NextIdleWave)
                    {
                        float w = m_Time - m_NextIdleWave;
                        if (w < 1.4f) armR = 140f + Mathf.Sin(w * 12f) * 22f;
                        else m_NextIdleWave = m_Time + Random.Range(5f, 9f);
                    }
                    break;
                case CharacterPose.Walk:
                {
                    float c = Mathf.Sin(t * 9f);
                    bob = Mathf.Abs(c) * 0.1f;
                    roll = c * 7f;
                    legSwing = c * 32f;
                    armSwing = -c * 28f;
                    breathe = 1f;
                    break;
                }
                case CharacterPose.Happy:
                    bob = Mathf.Abs(Mathf.Sin(t * 7f)) * 0.35f * Mathf.Clamp01(1.4f - t);
                    armL = -150f + Mathf.Sin(t * 14f) * 15f;
                    armR = 150f - Mathf.Sin(t * 14f) * 15f;
                    eyeSquint = 0.35f;
                    mouthOpen = 1.4f;
                    earWiggle = Mathf.Sin(t * 20f) * 12f;
                    break;
                case CharacterPose.Eat:
                {
                    float chomp = Mathf.Abs(Mathf.Sin(t * 9f));
                    headNod = chomp * 10f;
                    mouthOpen = 1f + chomp * 1.2f;
                    armSwing = -75f;
                    armL = -20f;
                    armR = 20f;
                    bob = chomp * 0.03f;
                    eyeSquint = 0.6f;
                    break;
                }
                case CharacterPose.Celebrate:
                    bob = Mathf.Abs(Mathf.Sin(t * 7.5f)) * 0.45f;
                    spin = t < 1.2f ? Mathf.SmoothStep(0f, 360f, t / 1.2f) : 0f;
                    armL = -145f + Mathf.Sin(t * 13f) * 25f;
                    armR = 145f - Mathf.Sin(t * 13f + 1f) * 25f;
                    eyeSquint = 0.35f;
                    mouthOpen = 1.5f;
                    earWiggle = Mathf.Sin(t * 22f) * 14f;
                    break;
                case CharacterPose.Wave:
                    armR = 140f + Mathf.Sin(t * 12f) * 25f;
                    armL = -8f;
                    headTilt = 8f;
                    bob = Mathf.Sin(m_Time * 2.2f) * 0.015f;
                    break;
            }

            if (model != null)
            {
                model.localPosition = m_Model.pos + Vector3.up * bob;
                model.localRotation = m_Model.rot * Quaternion.Euler(0f, spin, roll);
            }
            if (body != null)
            {
                body.localScale = new Vector3(m_Body.scale.x * (2f - breathe), m_Body.scale.y * breathe, m_Body.scale.z * (2f - breathe));
            }
            if (head != null) head.localRotation = m_Head.rot * Quaternion.Euler(headNod, 0f, headTilt);
            if (earLeft != null) earLeft.localRotation = m_EarL.rot * Quaternion.Euler(0f, 0f, earWiggle);
            if (earRight != null) earRight.localRotation = m_EarR.rot * Quaternion.Euler(0f, 0f, -earWiggle);
            if (armLeft != null) armLeft.localRotation = m_ArmL.rot * Quaternion.Euler(armSwing, 0f, armL);
            if (armRight != null) armRight.localRotation = m_ArmR.rot * Quaternion.Euler(-armSwing * (m_Pose == CharacterPose.Eat ? -1f : 1f), 0f, armR);
            if (legLeft != null) legLeft.localRotation = m_LegL.rot * Quaternion.Euler(legSwing, 0f, 0f);
            if (legRight != null) legRight.localRotation = m_LegR.rot * Quaternion.Euler(-legSwing, 0f, 0f);
            if (mouth != null) mouth.localScale = new Vector3(m_MouthScale.x, m_MouthScale.y * mouthOpen, m_MouthScale.z);

            UpdateEyes(dt, eyeSquint);
        }

        private void UpdateEyes(float dt, float squint)
        {
            if (eyeLeft == null || eyeRight == null) return;
            float blink = 1f;
            if (m_BlinkT >= 0f)
            {
                m_BlinkT += dt;
                blink = m_BlinkT < 0.07f ? 1f - m_BlinkT / 0.07f * 0.9f : m_BlinkT < 0.14f ? 0.1f + (m_BlinkT - 0.07f) / 0.07f * 0.9f : 1f;
                if (m_BlinkT >= 0.14f) m_BlinkT = -1f;
            }
            else if (m_Time > m_NextBlink)
            {
                m_BlinkT = 0f;
                m_NextBlink = m_Time + Random.Range(2f, 4.5f);
            }
            float k = Mathf.Min(blink, squint);
            eyeLeft.localScale = new Vector3(m_EyeScaleL.x, m_EyeScaleL.y * k, m_EyeScaleL.z);
            eyeRight.localScale = new Vector3(m_EyeScaleR.x, m_EyeScaleR.y * k, m_EyeScaleR.z);
        }

        private void Restore()
        {
            Apply(model, m_Model); Apply(body, m_Body); Apply(head, m_Head);
            Apply(earLeft, m_EarL); Apply(earRight, m_EarR);
            Apply(armLeft, m_ArmL); Apply(armRight, m_ArmR);
            Apply(legLeft, m_LegL); Apply(legRight, m_LegR);
        }

        private static void Apply(Transform t, Pose p)
        {
            if (t == null) return;
            t.localPosition = p.pos;
            t.localRotation = p.rot;
            t.localScale = p.scale;
        }
    }
}
