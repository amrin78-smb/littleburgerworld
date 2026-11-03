using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleBurgerWorld.Core
{
    [Serializable]
    public sealed class LocalizedEntry
    {
        public string key;
        public List<string> values = new List<string>();
    }

    /// <summary>
    /// Simple string table. Column order matches <see cref="languages"/>. The toddler-facing game is
    /// picture-first, so text only appears on a handful of buttons and the parent area.
    /// </summary>
    [CreateAssetMenu(menuName = "Little Burger World/Localization Table", fileName = "LocalizationTable")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [Tooltip("Language codes, e.g. en, es.")]
        public List<string> languages = new List<string> { "en" };
        [Tooltip("Display names shown in the parent settings, same order as languages.")]
        public List<string> languageNames = new List<string> { "English" };
        public List<LocalizedEntry> entries = new List<LocalizedEntry>();

        public string Get(string key, string language)
        {
            int col = Mathf.Max(0, languages.IndexOf(language));
            LocalizedEntry e = entries.Find(x => x != null && x.key == key);
            if (e == null || e.values.Count == 0) return key;
            if (col < e.values.Count && !string.IsNullOrEmpty(e.values[col])) return e.values[col];
            return e.values[0];
        }
    }

    /// <summary>Static access to the active language.</summary>
    public static class Localization
    {
        private static LocalizationTable s_Table;
        private static string s_Language = "en";

        public static string Language => s_Language;
        public static LocalizationTable Table => s_Table;

        public static void Initialize(LocalizationTable table, string language)
        {
            s_Table = table;
            s_Language = string.IsNullOrEmpty(language) ? "en" : language;
        }

        public static void SetLanguage(string language)
        {
            if (string.IsNullOrEmpty(language) || language == s_Language) return;
            s_Language = language;
            GameEvents.RaiseLanguageChanged();
        }

        public static string Get(string key)
        {
            if (s_Table == null) return key;
            return s_Table.Get(key, s_Language);
        }
    }
}
