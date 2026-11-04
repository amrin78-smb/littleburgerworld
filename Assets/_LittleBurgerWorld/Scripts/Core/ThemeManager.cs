using System;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Kitchen;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleBurgerWorld.Core
{
    public enum RestaurantTheme { Sunny = 0, Birthday = 1, Rainy = 2, Night = 3 }

    /// <summary>
    /// Gives the restaurant a different mood each visit: sunny day, birthday party, rainy day or cosy
    /// night. The theme rotates every time the restaurant opens and children can tap the window to
    /// change it. Themes only change decorations, lighting and a few customer touches (party hats) –
    /// gameplay is identical, so nothing is ever confusing.
    /// </summary>
    public sealed class ThemeManager : MonoBehaviour
    {
        [Serializable]
        public sealed class ThemeLook
        {
            public RestaurantTheme theme;
            public GameObject decorations;
            public Color sunColor = new Color(1f, 0.95f, 0.88f);
            public float sunIntensity = 0.95f;
            public Color ambientSky = new Color(0.86f, 0.89f, 0.96f);
            public Color ambientEquator = new Color(0.86f, 0.82f, 0.78f);
            public Color ambientGround = new Color(0.62f, 0.56f, 0.52f);
            public Color background = new Color(0.78f, 0.94f, 0.78f);
            public Color ground = new Color(0.78f, 0.94f, 0.78f);
            public SfxId loop = SfxId.None;
        }

        [SerializeField] private ThemeLook[] m_Looks = new ThemeLook[0];
        [SerializeField] private Light m_Sun;
        [SerializeField] private Camera m_Camera;
        [SerializeField] private TapButton3D m_Window;
        [SerializeField] private Renderer m_Ground;

        private MaterialPropertyBlock m_Block;

        private int m_Loop;

        public static RestaurantTheme Current { get; private set; } = RestaurantTheme.Sunny;
        public static ThemeManager Instance { get; private set; }
        public RestaurantTheme Theme => Current;

        private void Awake()
        {
            Instance = this;
            RestaurantTheme next = RestaurantTheme.Sunny;
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Save != null)
            {
                // A new mood every visit.
                int last = gm.Save.Data.settings.lastTheme;
                next = (RestaurantTheme)((last + 1 + 4) % 4);
                gm.Save.Data.settings.lastTheme = (int)next;
                gm.Save.MarkDirty();
            }
            Apply(next, false);
        }

        private void OnEnable() { if (m_Window != null) m_Window.Tapped += NextTheme; }

        private void OnDisable()
        {
            if (m_Window != null) m_Window.Tapped -= NextTheme;
            StopLoop();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Window tap: switch to the next mood with a little sparkle.</summary>
        public void NextTheme()
        {
            Apply((RestaurantTheme)(((int)Current + 1) % 4), true);
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Save != null)
            {
                gm.Save.Data.settings.lastTheme = (int)Current;
                gm.Save.MarkDirty();
            }
        }

        public void Apply(RestaurantTheme theme, bool announce)
        {
            Current = theme;
            ThemeLook look = null;
            foreach (ThemeLook l in m_Looks)
            {
                if (l == null) continue;
                bool on = l.theme == theme;
                if (l.decorations != null) l.decorations.SetActive(on);
                if (on) look = l;
            }
            if (look != null)
            {
                if (m_Sun != null)
                {
                    m_Sun.color = look.sunColor;
                    m_Sun.intensity = look.sunIntensity;
                }
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = look.ambientSky;
                RenderSettings.ambientEquatorColor = look.ambientEquator;
                RenderSettings.ambientGroundColor = look.ambientGround;
                DynamicGI.UpdateEnvironment();
                if (m_Camera != null) m_Camera.backgroundColor = look.background;
                if (m_Ground != null)
                {
                    m_Block ??= new MaterialPropertyBlock();
                    m_Ground.GetPropertyBlock(m_Block);
                    m_Block.SetColor("_BaseColor", look.ground);
                    m_Ground.SetPropertyBlock(m_Block);
                }
                StopLoop();
                GameManager gm = GameManager.Instance;
                if (look.loop != SfxId.None && gm != null) m_Loop = gm.Audio.PlayLoop(look.loop, 0.5f);
            }
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ThemeChanged, theme.ToString().ToLowerInvariant()));
        }

        private void StopLoop()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && m_Loop != 0) gm.Audio.StopLoop(m_Loop);
            m_Loop = 0;
        }
    }
}
