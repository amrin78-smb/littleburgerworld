using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleBurgerWorld.Audio
{
    [Serializable]
    public sealed class SfxEntry
    {
        public SfxId id;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 0.8f;
        [Range(0f, 0.5f)] public float pitchVariance = 0.06f;
    }

    [Serializable]
    public sealed class VoiceEntry
    {
        [Tooltip("Key referenced by ActivityStep.voiceKey (e.g. add_cheese).")]
        public string key;
        [Tooltip("Language code of this clip (en, es...). One entry per key and language.")]
        public string language = "en";
        [Tooltip("Spoken line. Empty = silent (the on-screen text and pictures still guide).")]
        public AudioClip clip;
        [TextArea] public string transcript;
    }

    /// <summary>Maps sound ids and voice keys to audio clips.</summary>
    [CreateAssetMenu(menuName = "Little Burger World/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        public AudioClip menuMusic;
        public AudioClip restaurantMusic;
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        public List<SfxEntry> sfx = new List<SfxEntry>();
        public List<VoiceEntry> voice = new List<VoiceEntry>();

        private Dictionary<SfxId, SfxEntry> m_Lookup;

        public SfxEntry Get(SfxId id)
        {
            if (m_Lookup == null || m_Lookup.Count != sfx.Count)
            {
                m_Lookup = new Dictionary<SfxId, SfxEntry>();
                foreach (SfxEntry e in sfx)
                {
                    if (e != null) m_Lookup[e.id] = e;
                }
            }
            return m_Lookup.TryGetValue(id, out SfxEntry entry) ? entry : null;
        }

        /// <summary>Voice line for a key in the given language (no cross-language fallback, so the
        /// voice always matches the on-screen text). Null language = first entry for the key.</summary>
        public VoiceEntry GetVoice(string key, string language = null)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (string.IsNullOrEmpty(language)) return voice.Find(v => v != null && v.key == key);
            return voice.Find(v => v != null && v.key == key &&
                (v.language == language || (string.IsNullOrEmpty(v.language) && language == "en")));
        }

        private void OnValidate() => m_Lookup = null;
    }
}
