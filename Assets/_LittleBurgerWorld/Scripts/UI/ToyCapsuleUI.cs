using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// Surprise capsule machine in the restaurant HUD. Serving a customer or finishing an activity drops
    /// a capsule into the machine (always free – there is no currency and nothing to buy). Tapping the
    /// machine opens the capsule with a little show and the toy goes onto the Toy Shelf.
    /// </summary>
    public sealed class ToyCapsuleUI : MonoBehaviour, IAssistable
    {
        [SerializeField] private SquishyButton m_Machine;
        [SerializeField] private RectTransform m_WaitingCapsule;
        [SerializeField] private Text m_CountText;
        [Header("Reveal")]
        [SerializeField] private GameObject m_Reveal;
        [SerializeField] private CanvasGroup m_RevealGroup;
        [SerializeField] private RectTransform m_CapsuleTop;
        [SerializeField] private RectTransform m_CapsuleBottom;
        [SerializeField] private Image m_CapsuleTopImage;
        [SerializeField] private Image m_CapsuleBottomImage;
        [SerializeField] private Image m_Toy;
        [SerializeField] private RectTransform m_Burst;
        [SerializeField] private SquishyButton m_TapCatcher;
        [SerializeField] private Color[] m_CapsuleColors =
        {
            new Color(1f, 0.55f, 0.7f), new Color(0.45f, 0.75f, 0.98f), new Color(1f, 0.82f, 0.3f), new Color(0.55f, 0.85f, 0.6f), new Color(0.75f, 0.62f, 0.95f)
        };

        private enum Phase { Idle, Dropping, Waiting, Opening, Showing }
        private Phase m_Phase = Phase.Idle;
        private string m_CurrentToy;
        private float m_IdleTimer;
        private Vector2 m_TopRest, m_BottomRest;

        public bool IsRevealing => m_Reveal != null && m_Reveal.activeSelf;
        public string LastToy { get; private set; }

        private void Awake()
        {
            if (m_Reveal != null) m_Reveal.SetActive(false);
            if (m_Machine != null) m_Machine.OnClick.AddListener(OpenNext);
            if (m_TapCatcher != null) m_TapCatcher.OnClick.AddListener(OnRevealTap);
            if (m_CapsuleTop != null) m_TopRest = m_CapsuleTop.anchoredPosition;
            if (m_CapsuleBottom != null) m_BottomRest = m_CapsuleBottom.anchoredPosition;
        }

        private void OnEnable()
        {
            GameEvents.ToyCapsuleEarned += OnEarned;
            Refresh();
        }

        private void OnDisable() => GameEvents.ToyCapsuleEarned -= OnEarned;

        private int Pending
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return gm != null ? gm.Rewards.PendingCapsules : 0;
            }
        }

        private void OnEarned()
        {
            Refresh();
            if (m_WaitingCapsule == null) return;
            // Capsule rattles down into the machine's tray.
            Vector2 rest = m_WaitingCapsule.anchoredPosition;
            m_WaitingCapsule.anchoredPosition = rest + new Vector2(0f, 90f);
            Tween.Kill(m_WaitingCapsule);
            Tween.Value(m_WaitingCapsule, 0.6f, k => m_WaitingCapsule.anchoredPosition = Vector2.LerpUnclamped(rest + new Vector2(0f, 90f), rest, Easing.Evaluate(Ease.OutBounce, k)),
                Ease.Linear, null, 0.2f, true);
            Play(SfxId.Capsule);
        }

        private void Refresh()
        {
            int n = Pending;
            if (m_WaitingCapsule != null) m_WaitingCapsule.gameObject.SetActive(n > 0);
            if (m_CountText != null) m_CountText.text = n > 1 ? n.ToString() : string.Empty;
        }

        private void Update()
        {
            // Gentle wiggle while a capsule is waiting, to invite a tap.
            if (m_WaitingCapsule == null || !m_WaitingCapsule.gameObject.activeSelf || IsRevealing) return;
            m_IdleTimer += Time.unscaledDeltaTime;
            float wiggle = Mathf.Sin(m_IdleTimer * 9f) * 9f * Mathf.Clamp01(Mathf.Sin(m_IdleTimer * 1.3f));
            m_WaitingCapsule.localEulerAngles = new Vector3(0f, 0f, wiggle);
        }

        // ------------------------------------------------------------------ reveal
        public void OpenNext()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || IsRevealing) return;
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ObjectTapped, null, SceneObjectRegistry.KeyOf(m_Machine), m_Machine));
            if (Pending <= 0)
            {
                if (m_Machine != null) Tween.Squash(m_Machine.transform, Vector3.one, 0.2f, 0.3f);
                return;
            }
            m_CurrentToy = gm.Rewards.OpenCapsule();
            Refresh();
            if (string.IsNullOrEmpty(m_CurrentToy)) return;
            Sprite toy = gm.Config.FindToy(m_CurrentToy);
            m_Toy.sprite = toy;
            m_Toy.preserveAspect = true;
            m_Toy.gameObject.SetActive(false);
            Color c = m_CapsuleColors[Random.Range(0, m_CapsuleColors.Length)];
            if (m_CapsuleTopImage != null) m_CapsuleTopImage.color = c;
            m_Reveal.SetActive(true);
            if (m_RevealGroup != null)
            {
                m_RevealGroup.alpha = 0f;
                Tween.Value(m_RevealGroup, 0.2f, k => m_RevealGroup.alpha = k, Ease.OutQuad, null, 0f, true);
            }
            m_CapsuleTop.anchoredPosition = m_TopRest + new Vector2(0f, 700f);
            m_CapsuleBottom.anchoredPosition = m_BottomRest + new Vector2(0f, 700f);
            m_CapsuleTop.localEulerAngles = m_CapsuleBottom.localEulerAngles = Vector3.zero;
            m_CapsuleTop.gameObject.SetActive(true);
            m_CapsuleBottom.gameObject.SetActive(true);
            if (m_Burst != null) m_Burst.gameObject.SetActive(false);
            m_Phase = Phase.Dropping;
            Tween.Value(m_CapsuleTop, 0.7f, k =>
            {
                float e = Easing.Evaluate(Ease.OutBounce, k);
                m_CapsuleTop.anchoredPosition = Vector2.LerpUnclamped(m_TopRest + new Vector2(0f, 700f), m_TopRest, e);
                m_CapsuleBottom.anchoredPosition = Vector2.LerpUnclamped(m_BottomRest + new Vector2(0f, 700f), m_BottomRest, e);
            }, Ease.Linear, () =>
            {
                m_Phase = Phase.Waiting;
                Play(SfxId.Capsule);
                // Opens by itself after a moment so even the youngest never get stuck.
                Tween.Delay(m_CapsuleBottom, 1.6f, () => { if (m_Phase == Phase.Waiting) Crack(); }, true);
            }, 0f, true);
        }

        private void OnRevealTap()
        {
            switch (m_Phase)
            {
                case Phase.Waiting: Crack(); break;
                case Phase.Showing: Finish(); break;
            }
        }

        private void Crack()
        {
            if (m_Phase != Phase.Waiting) return;
            m_Phase = Phase.Opening;
            Play(SfxId.Pop);
            Tween.Value(m_CapsuleTop, 0.45f, k =>
            {
                m_CapsuleTop.anchoredPosition = m_TopRest + new Vector2(-120f * k, 260f * k);
                m_CapsuleTop.localEulerAngles = new Vector3(0f, 0f, 35f * k);
                m_CapsuleBottom.anchoredPosition = m_BottomRest + new Vector2(110f * k, -150f * k);
                m_CapsuleBottom.localEulerAngles = new Vector3(0f, 0f, -25f * k);
            }, Ease.OutQuad, null, 0f, true);
            m_Toy.gameObject.SetActive(true);
            m_Toy.rectTransform.localScale = Vector3.zero;
            Tween.Scale(m_Toy.rectTransform, Vector3.one, 0.6f, Ease.OutElastic);
            if (m_Burst != null)
            {
                m_Burst.gameObject.SetActive(true);
                m_Burst.localScale = Vector3.one * 0.3f;
                Tween.Value(m_Burst, 3f, k =>
                {
                    m_Burst.localEulerAngles = new Vector3(0f, 0f, k * 120f);
                    m_Burst.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.15f, Mathf.Min(1f, k * 4f));
                }, Ease.Linear, null, 0f, true);
            }
            Play(SfxId.Chime);
            LastToy = m_CurrentToy;
            Tween.Delay(m_Toy, 0.5f, () => m_Phase = Phase.Showing, true);
            Tween.Delay(m_Toy, 3.2f, () => { if (m_Phase == Phase.Showing) Finish(); }, true);
        }

        private void Finish()
        {
            if (m_Phase != Phase.Showing) return;
            m_Phase = Phase.Idle;
            Vector3 target = m_Machine != null ? m_Machine.transform.position : m_Toy.rectTransform.position;
            Tween.Kill(m_Toy);
            Tween.Move(m_Toy.rectTransform, target, 0.45f, Ease.InBack);
            Tween.Scale(m_Toy.rectTransform, Vector3.one * 0.2f, 0.45f, Ease.InBack, () =>
            {
                if (m_RevealGroup != null)
                    Tween.Value(m_RevealGroup, 0.2f, k => m_RevealGroup.alpha = 1f - k, Ease.OutQuad, () =>
                    {
                        m_Reveal.SetActive(false);
                        m_Toy.rectTransform.anchoredPosition = Vector2.zero;
                    }, 0f, true);
                else m_Reveal.SetActive(false);
                if (m_Machine != null) Tween.Squash(m_Machine.transform, Vector3.one, 0.25f, 0.4f);
                Play(SfxId.Star);
            });
        }

        /// <summary>Opens a waiting capsule and finishes the show (tests / guided assist).</summary>
        public bool Assist(string itemId, string targetKey)
        {
            if (!IsRevealing) { OpenNext(); return true; }
            if (m_Phase == Phase.Waiting) { Crack(); return true; }
            if (m_Phase == Phase.Showing) { Finish(); return true; }
            return false;
        }

        private static void Play(SfxId id)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(id);
        }
    }
}
