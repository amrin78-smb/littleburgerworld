using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using UnityEngine;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// Welcoming main menu: two huge picture buttons (LET'S PLAY! / FREE PLAY), a sticker book,
    /// and a small parent button behind a grown-up gate. Benny waves from the 3D backdrop.
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private SquishyButton m_GuidedButton;
        [SerializeField] private SquishyButton m_FreePlayButton;
        [SerializeField] private SquishyButton m_StickersButton;
        [SerializeField] private SquishyButton m_ParentButton;
        [SerializeField] private ParentGate m_Gate;
        [SerializeField] private ParentSettingsUI m_Settings;
        [SerializeField] private StickerBookUI m_StickerBook;
        [SerializeField] private RectTransform m_Logo;
        [SerializeField] private ProceduralCharacterRig m_Mascot;

        private float m_Time;
        private float m_NextMascotMove = 2f;

        public SquishyButton GuidedButton => m_GuidedButton;
        public SquishyButton FreePlayButton => m_FreePlayButton;

        private void Awake()
        {
            if (m_GuidedButton != null) m_GuidedButton.OnClick.AddListener(() => Launch(GameMode.Guided));
            if (m_FreePlayButton != null) m_FreePlayButton.OnClick.AddListener(() => Launch(GameMode.FreePlay));
            if (m_StickersButton != null) m_StickersButton.OnClick.AddListener(() => m_StickerBook.Show());
            if (m_ParentButton != null) m_ParentButton.OnClick.AddListener(() => m_Gate.Show(() => m_Settings.Show()));
            if (m_Gate != null) m_Gate.gameObject.SetActive(false);
            if (m_Settings != null) m_Settings.gameObject.SetActive(false);
            if (m_StickerBook != null) m_StickerBook.gameObject.SetActive(false);
        }

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.PlayMenuMusic();
            if (m_Logo != null)
            {
                m_Logo.localScale = Vector3.zero;
                Tween.Scale(m_Logo, Vector3.one, 0.7f, Ease.OutElastic, null, 0.2f);
            }
        }

        private void Launch(GameMode mode)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.StartGame(mode);
        }

        private void Update()
        {
            m_Time += Time.deltaTime;
            if (m_Logo != null) m_Logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(m_Time * 1.6f) * 2.5f);
            if (m_Mascot != null && m_Time > m_NextMascotMove)
            {
                CharacterPose p = Random.value > 0.5f ? CharacterPose.Wave : CharacterPose.Happy;
                m_Mascot.SetPose(p);
                Tween.Delay(this, 1.6f, () => { if (m_Mascot != null) m_Mascot.SetPose(CharacterPose.Idle); });
                m_NextMascotMove = m_Time + Random.Range(4f, 7f);
            }
        }
    }
}
