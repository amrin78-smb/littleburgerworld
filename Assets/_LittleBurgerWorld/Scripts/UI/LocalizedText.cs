using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>Binds a UI Text to a localization key and refreshes on language change.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string m_Key;

        public string Key
        {
            get => m_Key;
            set { m_Key = value; Refresh(); }
        }

        private void OnEnable()
        {
            GameEvents.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => GameEvents.LanguageChanged -= Refresh;

        public void Refresh()
        {
            if (string.IsNullOrEmpty(m_Key)) return;
            var t = GetComponent<Text>();
            if (t != null && Localization.Table != null) t.text = Localization.Get(m_Key);
        }
    }
}
