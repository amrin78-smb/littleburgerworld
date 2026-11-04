using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// Washing-up mini-game. Customers leave a dirty plate when they finish eating (the sink button shows
    /// how many). Opening the sink shows one plate at a time: rubbing (or tapping) it with the sponge
    /// melts the splotches away with bubbles; clean plates hop onto the drying rack. There is always
    /// something to wash – an empty sink gets a few "play" plates – and nothing can go wrong.
    /// </summary>
    public sealed class WashingUpUI : MonoBehaviour, IAssistable
    {
        [Header("HUD")]
        [SerializeField] private SquishyButton m_OpenButton;
        [SerializeField] private RectTransform m_Badge;
        [SerializeField] private Text m_BadgeText;
        [Header("Overlay")]
        [SerializeField] private GameObject m_Overlay;
        [SerializeField] private CanvasGroup m_Group;
        [SerializeField] private RectTransform m_Plate;
        [SerializeField] private RectTransform m_PlateHome;
        [SerializeField] private RectTransform m_PlateSpawn;
        [SerializeField] private RectTransform m_Sponge;
        [SerializeField] private RectTransform m_SpongeHome;
        [SerializeField] private RectTransform m_BubbleLayer;
        [SerializeField] private Image[] m_RackPlates = new Image[0];
        [SerializeField] private SquishyButton m_Close;
        [SerializeField] private Sprite m_SplotchSprite;
        [SerializeField] private Sprite m_BubbleSprite;
        [SerializeField] private Color[] m_SplotchColors = { new Color(0.85f, 0.23f, 0.18f), new Color(0.96f, 0.76f, 0.19f), new Color(0.55f, 0.35f, 0.23f) };
        [Header("Tuning")]
        [SerializeField] private int m_MaxDirty = 5;
        [SerializeField] private int m_PlayPlates = 3;
        [SerializeField] private float m_ScrubRadius = 110f;
        [SerializeField] private float m_ScrubPerMove = 0.09f;
        [SerializeField] private float m_ScrubPerTap = 0.4f;

        private readonly List<Image> m_Splotches = new List<Image>();
        private readonly List<Image> m_BubblePool = new List<Image>();
        private int m_Dirty;
        private int m_ToWash;
        private int m_Washed;
        private bool m_PlateBusy;
        private float m_LastScrubSound;

        public int DirtyCount => m_Dirty;
        public bool IsOpen => m_Overlay != null && m_Overlay.activeSelf;
        public int SplotchesLeft { get { int n = 0; foreach (Image s in m_Splotches) if (s != null && s.color.a > k_Clean) n++; return n; } }
        public int WashedThisVisit => m_Washed;

        private const float k_Clean = 0.08f;

        private void Awake()
        {
            if (m_Overlay != null) m_Overlay.SetActive(false);
            if (m_OpenButton != null) m_OpenButton.OnClick.AddListener(Open);
            if (m_Close != null) m_Close.OnClick.AddListener(Close);
            var scrub = m_Plate != null ? m_Plate.GetComponent<ScrubSurface>() : null;
            if (scrub == null && m_Plate != null) scrub = m_Plate.gameObject.AddComponent<ScrubSurface>();
            if (scrub != null) scrub.Owner = this;
            RefreshBadge();
        }

        private void OnEnable() => GameEvents.Gameplay += OnGameplay;
        private void OnDisable() => GameEvents.Gameplay -= OnGameplay;

        private void OnGameplay(GameplayEvent e)
        {
            if (e.Type != GameplayEventType.CustomerFinishedEating) return;
            AddDirty(1);
        }

        public void AddDirty(int n)
        {
            m_Dirty = Mathf.Clamp(m_Dirty + n, 0, m_MaxDirty);
            RefreshBadge();
            if (m_OpenButton != null && n > 0) Tween.Squash(m_OpenButton.transform, Vector3.one, 0.25f, 0.4f);
        }

        private void RefreshBadge()
        {
            if (m_Badge != null) m_Badge.gameObject.SetActive(m_Dirty > 0);
            if (m_BadgeText != null) m_BadgeText.text = m_Dirty.ToString();
        }

        // ------------------------------------------------------------------ open / close
        public void Open()
        {
            if (IsOpen || m_Overlay == null) return;
            Tween.Kill(this); // a pending auto-close from the last visit must not shut this one
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ObjectTapped, null, SceneObjectRegistry.KeyOf(m_OpenButton), m_OpenButton));
            m_ToWash = m_Dirty > 0 ? m_Dirty : m_PlayPlates;
            m_Washed = 0;
            foreach (Image r in m_RackPlates) if (r != null) r.gameObject.SetActive(false);
            m_Overlay.SetActive(true);
            if (m_Group != null)
            {
                m_Group.alpha = 0f;
                Tween.Value(m_Group, 0.25f, k => m_Group.alpha = k, Ease.OutQuad, null, 0f, true);
            }
            ParkSponge();
            NextPlate();
        }

        public void Close()
        {
            if (!IsOpen) return;
            Tween.Kill(m_Plate);
            if (m_Group != null)
                Tween.Value(m_Group, 0.2f, k => m_Group.alpha = 1f - k, Ease.OutQuad, () => m_Overlay.SetActive(false), 0f, true);
            else m_Overlay.SetActive(false);
        }

        private void NextPlate()
        {
            if (m_Washed >= m_ToWash)
            {
                AllClean();
                return;
            }
            BuildSplotches(m_Washed);
            m_Plate.gameObject.SetActive(true);
            m_Plate.position = m_PlateSpawn != null ? m_PlateSpawn.position : m_PlateHome.position;
            m_Plate.localScale = Vector3.one * 0.6f;
            m_PlateBusy = true;
            Tween.Move(m_Plate, m_PlateHome.position, 0.45f, Ease.OutBack);
            Tween.Scale(m_Plate, Vector3.one, 0.45f, Ease.OutBack, () => m_PlateBusy = false);
            Play(SfxId.Whoosh);
        }

        private void BuildSplotches(int seed)
        {
            foreach (Image s in m_Splotches) if (s != null) Destroy(s.gameObject);
            m_Splotches.Clear();
            var rnd = new System.Random(seed * 31 + 7);
            int count = 4 + rnd.Next(0, 3);
            float r = m_Plate.rect.width * 0.3f;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Splotch" + i, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(m_Plate, false);
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float d = Mathf.Sqrt((float)rnd.NextDouble()) * r;
                rt.anchoredPosition = new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
                float size = 70f + (float)rnd.NextDouble() * 70f;
                rt.sizeDelta = new Vector2(size, size * (0.75f + (float)rnd.NextDouble() * 0.4f));
                rt.localEulerAngles = new Vector3(0f, 0f, rnd.Next(0, 360));
                var img = go.GetComponent<Image>();
                img.sprite = m_SplotchSprite;
                img.raycastTarget = false;
                Color c = m_SplotchColors[rnd.Next(0, m_SplotchColors.Length)];
                c.a = 0.95f;
                img.color = c;
                m_Splotches.Add(img);
            }
        }

        // ------------------------------------------------------------------ scrubbing
        internal void Scrub(Vector2 screenPoint, bool tap)
        {
            if (!IsOpen || m_PlateBusy) return;
            MoveSponge(screenPoint);
            float amount = tap ? m_ScrubPerTap : m_ScrubPerMove;
            float radius = m_ScrubRadius * (tap ? 1.4f : 1f);
            Canvas canvas = m_Plate.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            bool any = false;
            foreach (Image s in m_Splotches)
            {
                if (s == null || s.color.a <= k_Clean) continue;
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, s.rectTransform.position);
                if ((sp - screenPoint).magnitude / scale > radius + s.rectTransform.sizeDelta.x * 0.4f) continue;
                Color c = s.color;
                c.a = Mathf.Max(0f, c.a - amount);
                s.color = c;
                any = true;
            }
            if (any || tap) SpawnBubbles(screenPoint, tap ? 4 : 1);
            if (Time.unscaledTime - m_LastScrubSound > 0.14f)
            {
                m_LastScrubSound = Time.unscaledTime;
                Play(SfxId.Scrub);
            }
            if (SplotchesLeft == 0) PlateClean();
        }

        /// <summary>Washes the current plate in one go (guided assist and tests).</summary>
        public void CleanCurrentPlate()
        {
            if (!IsOpen) Open();
            if (m_PlateBusy) return;
            foreach (Image s in m_Splotches) if (s != null) { Color c = s.color; c.a = 0f; s.color = c; }
            SpawnBubbles(RectTransformUtility.WorldToScreenPoint(null, m_Plate.position), 6);
            PlateClean();
        }

        private void PlateClean()
        {
            if (m_PlateBusy) return;
            m_PlateBusy = true;
            Play(SfxId.Sparkle);
            Tween.Squash(m_Plate, Vector3.one, 0.2f, 0.35f);
            int slot = Mathf.Min(m_Washed, m_RackPlates.Length - 1);
            m_Washed++;
            if (m_Dirty > 0) m_Dirty--;
            RefreshBadge();
            GameEvents.Raise(new GameplayEvent(GameplayEventType.DishWashed, "plate", SceneObjectRegistry.KeyOf(this), this, null, m_Washed));
            Tween.Delay(m_Plate, 0.35f, () =>
            {
                Image rack = slot >= 0 && slot < m_RackPlates.Length ? m_RackPlates[slot] : null;
                Vector3 to = rack != null ? rack.rectTransform.position : m_Plate.position + Vector3.right * 400f;
                Tween.Move(m_Plate, to, 0.4f, Ease.InOutQuad);
                Tween.Scale(m_Plate, Vector3.one * 0.35f, 0.4f, Ease.InOutQuad, () =>
                {
                    if (rack != null)
                    {
                        rack.gameObject.SetActive(true);
                        Tween.Squash(rack.rectTransform, Vector3.one, 0.25f, 0.35f);
                    }
                    Play(SfxId.Plop);
                    m_Plate.gameObject.SetActive(false);
                    m_PlateBusy = false;
                    NextPlate();
                });
            }, true);
        }

        private void AllClean()
        {
            Play(SfxId.Celebration);
            ParkSponge();
            GameEvents.Raise(new GameplayEvent(GameplayEventType.AllDishesClean, "dishes", SceneObjectRegistry.KeyOf(this), this, null, m_Washed));
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.PlayVoice("dishes_clean");
            Tween.Delay(this, 1.6f, Close, true);
        }

        // ------------------------------------------------------------------ sponge & bubbles
        internal void MoveSponge(Vector2 screenPoint)
        {
            if (m_Sponge == null) return;
            Tween.Kill(m_Sponge);
            m_Sponge.position = screenPoint;
            m_Sponge.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(Time.unscaledTime * 18f) * 8f);
        }

        internal void ParkSponge()
        {
            if (m_Sponge == null || m_SpongeHome == null) return;
            Tween.Kill(m_Sponge);
            Tween.Move(m_Sponge, m_SpongeHome.position, 0.3f, Ease.OutBack);
            m_Sponge.localEulerAngles = Vector3.zero;
        }

        private void SpawnBubbles(Vector2 screenPoint, int count)
        {
            if (m_BubbleLayer == null || m_BubbleSprite == null) return;
            for (int i = 0; i < count; i++)
            {
                Image b = GetBubble();
                RectTransform rt = b.rectTransform;
                rt.position = screenPoint + Random.insideUnitCircle * 50f;
                float size = Random.Range(28f, 70f);
                rt.sizeDelta = new Vector2(size, size);
                rt.localScale = Vector3.one * 0.3f;
                b.color = new Color(1f, 1f, 1f, 0.9f);
                Vector3 start = rt.position;
                Vector3 drift = new Vector3(Random.Range(-40f, 40f), Random.Range(70f, 160f), 0f);
                Tween.Kill(rt);
                Tween.Value(rt, Random.Range(0.6f, 1.1f), k =>
                {
                    rt.position = start + drift * k;
                    rt.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, Mathf.Min(1f, k * 3f));
                    b.color = new Color(1f, 1f, 1f, 0.9f * (1f - k));
                }, Ease.OutQuad, () => b.gameObject.SetActive(false), 0f, true);
            }
        }

        private Image GetBubble()
        {
            foreach (Image b in m_BubblePool) if (b != null && !b.gameObject.activeSelf) { b.gameObject.SetActive(true); return b; }
            var go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(m_BubbleLayer, false);
            var img = go.GetComponent<Image>();
            img.sprite = m_BubbleSprite;
            img.raycastTarget = false;
            m_BubblePool.Add(img);
            return img;
        }

        private static void Play(SfxId id)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(id);
        }

        // ------------------------------------------------------------------ guided assist
        public bool Assist(string itemId, string targetKey)
        {
            if (!IsOpen) { Open(); return true; }
            CleanCurrentPlate();
            return true;
        }
    }
}
