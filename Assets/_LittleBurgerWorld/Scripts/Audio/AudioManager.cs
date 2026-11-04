using System.Collections.Generic;
using UnityEngine;
using LittleBurgerWorld.Core;

namespace LittleBurgerWorld.Audio
{
    /// <summary>
    /// Central audio service: background music, pooled one-shot effects, looping effects
    /// (sizzle, bubbles, pouring) and optional voice prompts. Volumes come from parent settings.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int k_SfxVoices = 10;
        private const float k_MinRepeatInterval = 0.045f;

        [SerializeField] private AudioLibrary m_Library;

        private AudioSource m_Music;
        private AudioSource m_Voice;
        private readonly List<AudioSource> m_SfxPool = new List<AudioSource>();
        private readonly Dictionary<int, AudioSource> m_Loops = new Dictionary<int, AudioSource>();
        private readonly Dictionary<int, float> m_LoopBaseVolume = new Dictionary<int, float>();
        private readonly Dictionary<SfxId, float> m_LastPlayed = new Dictionary<SfxId, float>();
        private int m_NextLoopHandle = 1;
        private int m_NextVoice;
        private float m_Duck = 1f;
        private const float k_VoiceDuck = 0.4f;
        private float m_MusicVolume = 0.8f;
        private float m_SfxVolume = 0.9f;
        private float m_VoiceVolume = 1f;
        private float m_MusicFade = 1f;

        public AudioLibrary Library => m_Library;
        public float MusicVolume => m_MusicVolume;
        public float SfxVolume => m_SfxVolume;
        public float VoiceVolume => m_VoiceVolume;

        public void Initialize(AudioLibrary library)
        {
            m_Library = library;
            EnsureSources();
        }

        private void Awake() => EnsureSources();

        private void EnsureSources()
        {
            if (m_Music != null) return;
            m_Music = CreateSource("Music", true);
            m_Voice = CreateSource("Voice", false);
            for (int i = 0; i < k_SfxVoices; i++) m_SfxPool.Add(CreateSource("Sfx" + i, false));
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f; // 2D: toddlers need clear, consistent audio
            return src;
        }

        public void SetVolumes(float music, float sfx, float voice)
        {
            m_MusicVolume = Mathf.Clamp01(music);
            m_SfxVolume = Mathf.Clamp01(sfx);
            m_VoiceVolume = Mathf.Clamp01(voice);
            ApplyMusicVolume();
            foreach (var kv in m_Loops)
            {
                if (kv.Value != null && m_LoopBaseVolume.TryGetValue(kv.Key, out float b)) kv.Value.volume = b * m_SfxVolume;
            }
            if (m_Voice != null) m_Voice.volume = m_VoiceVolume;
        }

        private void ApplyMusicVolume()
        {
            if (m_Music == null) return;
            float baseVol = m_Library != null ? m_Library.musicVolume : 0.45f;
            m_Music.volume = baseVol * m_MusicVolume * m_MusicFade * m_Duck;
        }

        public void PlayMusic(AudioClip clip)
        {
            EnsureSources();
            if (clip == null) { m_Music.Stop(); return; }
            if (m_Music.clip == clip && m_Music.isPlaying) return;
            m_Music.clip = clip;
            m_MusicFade = 0f;
            ApplyMusicVolume();
            m_Music.Play();
        }

        public void PlayMenuMusic() => PlayMusic(m_Library != null ? m_Library.menuMusic : null);
        public void PlayRestaurantMusic() => PlayMusic(m_Library != null ? m_Library.restaurantMusic : null);

        /// <summary>Plays a one-shot effect. Rapid repeats of the same id are throttled.</summary>
        public void Play(SfxId id, float volumeScale = 1f, float pitch = 1f)
        {
            if (id == SfxId.None || m_Library == null) return;
            SfxEntry e = m_Library.Get(id);
            if (e == null || e.clips == null || e.clips.Length == 0) return;

            float now = Time.unscaledTime;
            if (m_LastPlayed.TryGetValue(id, out float last) && now - last < k_MinRepeatInterval) return;
            m_LastPlayed[id] = now;

            AudioClip clip = e.clips[Random.Range(0, e.clips.Length)];
            if (clip == null) return;
            AudioSource src = NextSfxSource();
            src.pitch = pitch * (1f + Random.Range(-e.pitchVariance, e.pitchVariance));
            src.PlayOneShot(clip, e.volume * volumeScale * m_SfxVolume);
        }

        private AudioSource NextSfxSource()
        {
            EnsureSources();
            for (int i = 0; i < m_SfxPool.Count; i++)
            {
                AudioSource s = m_SfxPool[(m_NextVoice + i) % m_SfxPool.Count];
                if (!s.isPlaying)
                {
                    m_NextVoice = (m_NextVoice + i + 1) % m_SfxPool.Count;
                    return s;
                }
            }
            m_NextVoice = (m_NextVoice + 1) % m_SfxPool.Count;
            return m_SfxPool[m_NextVoice];
        }

        /// <summary>Starts a looping effect and returns a handle for <see cref="StopLoop"/>.</summary>
        public int PlayLoop(SfxId id, float volumeScale = 1f)
        {
            if (m_Library == null) return 0;
            SfxEntry e = m_Library.Get(id);
            if (e == null || e.clips == null || e.clips.Length == 0 || e.clips[0] == null) return 0;
            AudioSource src = CreateSource("Loop_" + id, true);
            src.clip = e.clips[0];
            src.volume = e.volume * volumeScale * m_SfxVolume;
            src.Play();
            int handle = m_NextLoopHandle++;
            m_Loops[handle] = src;
            m_LoopBaseVolume[handle] = e.volume * volumeScale;
            return handle;
        }

        public void StopLoop(int handle)
        {
            if (handle == 0) return;
            if (m_Loops.TryGetValue(handle, out AudioSource src))
            {
                m_Loops.Remove(handle);
                m_LoopBaseVolume.Remove(handle);
                if (src != null) Destroy(src.gameObject);
            }
        }

        public void StopAllLoops()
        {
            foreach (var kv in m_Loops)
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            }
            m_Loops.Clear();
            m_LoopBaseVolume.Clear();
        }

        /// <summary>Plays a spoken prompt if one is configured; silently does nothing otherwise.</summary>
        public bool PlayVoice(string key)
        {
            if (m_Library == null) return false;
            VoiceEntry v = m_Library.GetVoice(key, Localization.Language);
            if (v == null || v.clip == null) return false;
            m_Voice.Stop();
            m_Voice.clip = v.clip;
            m_Voice.volume = m_VoiceVolume;
            m_Voice.Play();
            LastVoiceKey = key;
            return true;
        }

        /// <summary>Key of the last voice line started (diagnostics/tests).</summary>
        public string LastVoiceKey { get; private set; }

        /// <summary>True while a voice line is playing.</summary>
        public bool VoicePlaying => m_Voice != null && m_Voice.isPlaying;

        private void Update()
        {
            // Music dips while the voice speaks so the words are easy to hear.
            float duck = VoicePlaying && m_VoiceVolume > 0.001f ? k_VoiceDuck : 1f; // no dip for muted voice
            if (!Mathf.Approximately(duck, m_Duck))
            {
                m_Duck = Mathf.MoveTowards(m_Duck, duck, Time.unscaledDeltaTime * (VoicePlaying ? 4f : 1.2f));
                ApplyMusicVolume();
            }
            if (m_MusicFade < 1f)
            {
                m_MusicFade = Mathf.MoveTowards(m_MusicFade, 1f, Time.unscaledDeltaTime * 0.6f);
                ApplyMusicVolume();
            }
        }
    }
}
