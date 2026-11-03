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
        [Tooltip("Spoken line. Leave empty until recorded – the game works silently without it.")]
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

        public VoiceEntry GetVoice(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return voice.Find(v => v != null && v.key == key);
        }

        private void OnValidate() => m_Lookup = null;
    }
}
