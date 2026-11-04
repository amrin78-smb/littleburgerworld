using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Rewards;
using LittleBurgerWorld.Save;
using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>
    /// Persistent composition root. Owns the cross-scene services (save, audio, mode, scene flow,
    /// recipes, rewards). Gameplay logic lives in the scene systems, not here.
    /// Created automatically before the first scene loads, so every scene can be played directly.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig m_Config;

        private float m_FlushTimer;
        private float m_FpsSampleTime;
        private int m_FpsSampleFrames;
        private bool m_FpsDecided;

        public GameConfig Config => m_Config;
        public SaveManager Save { get; private set; }
        public AudioManager Audio { get; private set; }
        public GameModeManager Modes { get; private set; }
        public SceneFlow Scenes { get; private set; }
        public RecipeManager Recipes { get; private set; }
        public RewardManager Rewards { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null) Create();
        }

        /// <summary>Creates the persistent manager (optionally with an explicit config and save path, for tests).</summary>
        public static GameManager Create(GameConfig config = null, string savePath = null)
        {
            if (Instance != null) return Instance;
            config ??= Resources.Load<GameConfig>(GameConfig.ResourcePath);
            if (config == null)
            {
                Debug.LogError("[GameManager] Missing Resources/" + GameConfig.ResourcePath +
                               ". Run 'Little Burger World ▸ Setup Project'.");
                config = ScriptableObject.CreateInstance<GameConfig>();
            }

            var go = new GameObject("[GameManager]");
            go.SetActive(false);
            GameManager gm = go.AddComponent<GameManager>();
            gm.m_Config = config;
            gm.m_SavePathOverride = savePath;
            go.SetActive(true);
            return gm;
        }

        private string m_SavePathOverride;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = m_Config != null ? m_Config.targetFrameRate : 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Save = new SaveManager(m_SavePathOverride);
            Save.Load();

            // A language that is no longer shipped (e.g. an old save) falls back to English.
            LocalizationTable table = m_Config.localization;
            if (table != null && !table.languages.Contains(Save.Data.settings.language)) Save.Data.settings.language = "en";
            Localization.Initialize(table, Save.Data.settings.language);

            Modes = new GameModeManager();
            Recipes = new RecipeManager(m_Config.recipes);
            Rewards = new RewardManager(m_Config, Save);

            Audio = new GameObject("AudioManager").AddComponent<AudioManager>();
            Audio.transform.SetParent(transform, false);
            Audio.Initialize(m_Config.audioLibrary);

            Scenes = gameObject.AddComponent<SceneFlow>();
            ApplySettings();
        }

        /// <summary>Pushes saved parent settings into the running services.</summary>
        public void ApplySettings()
        {
            SettingsData s = Save.Data.settings;
            Audio.SetVolumes(s.musicVolume, s.sfxVolume, s.voicePrompts ? s.voiceVolume : 0f);
            Localization.SetLanguage(s.language);
            GameEvents.RaiseSettingsChanged();
        }

        /// <summary>Switches to another save file (automated tests use a temporary one).</summary>
        public void UseSaveFile(string path)
        {
            Rewards?.Dispose();
            Save = new SaveManager(path);
            Save.Load();
            Rewards = new RewardManager(m_Config, Save);
            ApplySettings();
        }

        public void LoadMainMenu() => Scenes.Load(m_Config.menuScene);

        public void StartGame(GameMode mode)
        {
            Modes.SetMode(mode);
            Scenes.Load(m_Config.restaurantScene);
        }

        private void Update()
        {
            m_FlushTimer += Time.unscaledDeltaTime;
            if (m_FlushTimer > 4f)
            {
                m_FlushTimer = 0f;
                Save.Flush();
            }
            SampleFrameRate();
        }

        // Starts at 60 FPS; devices that cannot sustain it settle at a stable 30.
        private void SampleFrameRate()
        {
            if (m_FpsDecided || Application.isEditor) return;
            if (Time.realtimeSinceStartup < 6f) return;
            m_FpsSampleTime += Time.unscaledDeltaTime;
            m_FpsSampleFrames++;
            if (m_FpsSampleTime < 8f) return;
            float fps = m_FpsSampleFrames / m_FpsSampleTime;
            if (fps < 45f) Application.targetFrameRate = 30;
            m_FpsDecided = true;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save?.Flush();
        }

        private void OnApplicationQuit() => Save?.Flush();

        private void OnDestroy()
        {
            if (Instance != this) return;
            Save?.Flush();
            Rewards?.Dispose();
            Instance = null;
        }
    }
}
