using LittleBurgerWorld.Core;
using LittleBurgerWorld.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// Parent area (behind <see cref="ParentGate"/>): volumes, voice prompts, language, reset progress
    /// and privacy information. No links, no purchases, no accounts.
    /// </summary>
    public sealed class ParentSettingsUI : MonoBehaviour
    {
        [SerializeField] private Slider m_Music;
        [SerializeField] private Slider m_Sfx;
        [SerializeField] private Slider m_Voice;
        [SerializeField] private Toggle m_VoicePrompts;
        [SerializeField] private SquishyButton m_Language;
        [SerializeField] private Text m_LanguageLabel;
        [SerializeField] private HoldButton m_ResetProgress;
        [SerializeField] private Text m_ResetDone;
        [SerializeField] private SquishyButton m_PrivacyButton;
        [SerializeField] private GameObject m_PrivacyPanel;
        [SerializeField] private SquishyButton m_PrivacyClose;
        [SerializeField] private SquishyButton m_Close;

        private bool m_Binding;

        private void Awake()
        {
            if (m_Music != null) m_Music.onValueChanged.AddListener(_ => Apply());
            if (m_Sfx != null) m_Sfx.onValueChanged.AddListener(_ => Apply());
            if (m_Voice != null) m_Voice.onValueChanged.AddListener(_ => Apply());
            if (m_VoicePrompts != null) m_VoicePrompts.onValueChanged.AddListener(_ => Apply());
            if (m_Language != null) m_Language.OnClick.AddListener(CycleLanguage);
            if (m_ResetProgress != null) { m_ResetProgress.HoldSeconds = 2f; m_ResetProgress.OnHoldComplete.AddListener(ResetProgress); }
            if (m_PrivacyButton != null) m_PrivacyButton.OnClick.AddListener(() => m_PrivacyPanel.SetActive(true));
            if (m_PrivacyClose != null) m_PrivacyClose.OnClick.AddListener(() => m_PrivacyPanel.SetActive(false));
            if (m_Close != null) m_Close.OnClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (m_PrivacyPanel != null) m_PrivacyPanel.SetActive(false);
            if (m_ResetDone != null) m_ResetDone.gameObject.SetActive(false);
            Bind();
        }

        public void Hide()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Save.Save();
            gameObject.SetActive(false);
        }

        private void Bind()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            SettingsData s = gm.Save.Data.settings;
            m_Binding = true;
            if (m_Music != null) m_Music.value = s.musicVolume;
            if (m_Sfx != null) m_Sfx.value = s.sfxVolume;
            if (m_Voice != null) m_Voice.value = s.voiceVolume;
            if (m_VoicePrompts != null) m_VoicePrompts.isOn = s.voicePrompts;
            m_Binding = false;
            RefreshLanguageLabel();
        }

        private void Apply()
        {
            if (m_Binding) return;
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            SettingsData s = gm.Save.Data.settings;
            if (m_Music != null) s.musicVolume = m_Music.value;
            if (m_Sfx != null) s.sfxVolume = m_Sfx.value;
            if (m_Voice != null) s.voiceVolume = m_Voice.value;
            if (m_VoicePrompts != null) s.voicePrompts = m_VoicePrompts.isOn;
            gm.Save.MarkDirty();
            gm.ApplySettings();
        }

        private void CycleLanguage()
        {
            GameManager gm = GameManager.Instance;
            LocalizationTable table = Localization.Table;
            if (gm == null || table == null || table.languages.Count == 0) return;
            SettingsData s = gm.Save.Data.settings;
            int i = table.languages.IndexOf(s.language);
            s.language = table.languages[(i + 1 + table.languages.Count) % table.languages.Count];
            gm.Save.MarkDirty();
            gm.ApplySettings();
            RefreshLanguageLabel();
        }

        private void RefreshLanguageLabel()
        {
            LocalizationTable table = Localization.Table;
            if (m_LanguageLabel == null || table == null) return;
            int i = Mathf.Max(0, table.languages.IndexOf(Localization.Language));
            m_LanguageLabel.text = i < table.languageNames.Count ? table.languageNames[i] : Localization.Language;
        }

        private void ResetProgress()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.Save.ResetProgress();
            gm.ApplySettings();
            if (m_ResetDone != null)
            {
                m_ResetDone.gameObject.SetActive(true);
                m_ResetDone.text = Localization.Get("progress_reset_done");
            }
        }
    }
}
