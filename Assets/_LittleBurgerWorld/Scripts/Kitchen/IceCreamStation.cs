using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using UnityEngine;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>
    /// Ice cream cart: tap the cone stack (or just a flavour – a cone appears by itself), tap tubs to add
    /// up to three scoops of any flavours, shake on sprinkles and finish with a cherry. Every step is
    /// forgiving and the ice cream can be served as soon as it has one scoop.
    /// </summary>
    public sealed class IceCreamStation : MonoBehaviour, IFoodContainer, IAssistable, IHintTarget
    {
        [SerializeField] private List<string> m_FlavourIds = new List<string>();
        [SerializeField] private List<Color> m_FlavourColors = new List<Color>();
        [SerializeField] private List<TapButton3D> m_Tubs = new List<TapButton3D>();
        [SerializeField] private TapButton3D m_Cones;
        [SerializeField] private TapButton3D m_SprinkleShaker;
        [SerializeField] private TapButton3D m_CherryJar;
        [SerializeField] private Transform m_Spot;
        [SerializeField] private Transform m_Spawn;
        [SerializeField] private GameObject m_ConePrefab;

        private FoodItem m_Cone;
        private IceCreamVisual m_Visual;
        private bool m_Completed;
        private int m_NextAssistFlavour;
        private readonly List<System.Action> m_TubHandlers = new List<System.Action>();

        public FoodItem Cone => m_Cone;
        public int ScoopCount => m_Visual != null ? m_Visual.ScoopCount : 0;
        public bool HasSprinkles => m_Visual != null && m_Visual.HasSprinkles;
        public bool IsComplete => m_Completed;
        public Vector3 HintPosition => m_Cone != null ? m_Cone.transform.position + Vector3.up * 0.8f : m_Spot.position + Vector3.up * 0.5f;

        private void OnEnable()
        {
            m_TubHandlers.Clear();
            for (int i = 0; i < m_Tubs.Count; i++)
            {
                int idx = i;
                System.Action h = () => OnTub(idx);
                m_TubHandlers.Add(h);
                if (m_Tubs[i] != null) m_Tubs[i].Tapped += h;
            }
            if (m_Cones != null) m_Cones.Tapped += OnCones;
            if (m_SprinkleShaker != null) m_SprinkleShaker.Tapped += OnSprinkles;
            if (m_CherryJar != null) m_CherryJar.Tapped += OnCherry;
        }

        private void OnDisable()
        {
            for (int i = 0; i < m_Tubs.Count && i < m_TubHandlers.Count; i++)
                if (m_Tubs[i] != null) m_Tubs[i].Tapped -= m_TubHandlers[i];
            m_TubHandlers.Clear();
            if (m_Cones != null) m_Cones.Tapped -= OnCones;
            if (m_SprinkleShaker != null) m_SprinkleShaker.Tapped -= OnSprinkles;
            if (m_CherryJar != null) m_CherryJar.Tapped -= OnCherry;
        }

        // ------------------------------------------------------------------ steps
        private void OnCones()
        {
            if (m_Cone != null) { m_Cones.Wiggle(); return; }
            PlaceCone(null);
        }

        private void OnTub(int index)
        {
            if (m_Cone == null) { PlaceCone(() => AddScoop(index)); return; }
            if (ScoopCount >= (m_Visual != null ? m_Visual.MaxScoops : 3) || m_Completed) { m_Tubs[index].Wiggle(); return; }
            AddScoop(index);
        }

        private void OnSprinkles()
        {
            if (ScoopCount == 0 || HasSprinkles || m_Completed) { m_SprinkleShaker.Wiggle(); return; }
            AddSprinkles();
        }

        private void OnCherry()
        {
            if (ScoopCount == 0 || m_Completed) { m_CherryJar.Wiggle(); return; }
            AddCherry();
        }

        /// <summary>Puts an empty cone in the holder.</summary>
        public void PlaceCone(System.Action onPlaced)
        {
            if (m_Cone != null || m_ConePrefab == null) { onPlaced?.Invoke(); return; }
            Vector3 from = m_Spawn != null ? m_Spawn.position : m_Spot.position + Vector3.up;
            GameObject go = Instantiate(m_ConePrefab, from, m_Spot.rotation, m_Spot);
            m_Cone = go.GetComponent<FoodItem>();
            if (m_Cone == null) m_Cone = go.AddComponent<FoodItem>();
            m_Cone.Configure(FoodType.IceCream, 1.1f);
            m_Cone.AttachTo(this);
            m_Cone.Locked = true;
            m_Visual = go.GetComponent<IceCreamVisual>();
            if (m_Visual != null) m_Visual.ResetVisual();
            m_Completed = false;
            FoodItem cone = m_Cone;
            Tween.Jump(go.transform, m_Spot.position, 0.5f, 0.4f, () =>
            {
                if (cone == null) return;
                Tween.Squash(cone.transform, cone.RestScale, 0.2f);
                cone.PlaySfx(SfxId.Plop);
                onPlaced?.Invoke();
            });
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ConePlaced, "cone", SceneObjectRegistry.KeyOf(this), this));
        }

        public void AddScoop(int flavourIndex)
        {
            if (m_Cone == null || m_Visual == null || m_Completed) return;
            if (m_FlavourIds.Count == 0) return;
            flavourIndex = Mathf.Clamp(flavourIndex, 0, m_FlavourIds.Count - 1);
            Color color = flavourIndex < m_FlavourColors.Count ? m_FlavourColors[flavourIndex] : Color.white;
            Transform scoop = m_Visual.AddScoop(color);
            if (scoop == null) return;
            Vector3 rest = scoop.localScale;
            scoop.localScale = rest * 0.2f;
            Tween.Scale(scoop, rest, 0.45f, Ease.OutElastic);
            FxManager.Play(FxType.Sparkle, scoop.position, 6, color);
            m_Cone.PlaySfx(SfxId.Plop);
            m_Cone.Locked = false; // servable from the first scoop
            GameEvents.Raise(new GameplayEvent(GameplayEventType.IceCreamScoop, m_FlavourIds[flavourIndex], SceneObjectRegistry.KeyOf(this), this,
                null, ScoopCount));
        }

        public void AddSprinkles()
        {
            if (m_Visual == null || ScoopCount == 0 || HasSprinkles) return;
            m_Visual.ShowSprinkles(true);
            Transform s = m_Visual.Sprinkles;
            if (s != null)
            {
                Vector3 rest = s.localScale;
                s.localScale = rest * 0.3f;
                Tween.Scale(s, rest, 0.4f, Ease.OutBack);
            }
            FxManager.Play(FxType.Confetti, m_Cone.transform.position + Vector3.up * 1.2f, 10);
            m_Cone.PlaySfx(SfxId.Sparkle);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.SprinklesAdded, "sprinkles", SceneObjectRegistry.KeyOf(this), this));
        }

        public void AddCherry()
        {
            if (m_Visual == null || ScoopCount == 0 || m_Completed) return;
            m_Visual.ShowCherry(true);
            Transform c = m_Visual.Cherry;
            if (c != null)
            {
                Vector3 rest = c.localPosition;
                c.localPosition = rest + Vector3.up * 0.7f;
                Tween.MoveLocal(c, rest, 0.4f, Ease.OutBounce);
            }
            m_Cone.PlaySfx(SfxId.Pop);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.CherryAdded, "cherry", SceneObjectRegistry.KeyOf(this), this));
            Complete();
        }

        private void Complete()
        {
            m_Completed = true;
            FoodItem cone = m_Cone;
            Tween.Delay(this, 0.4f, () =>
            {
                if (cone == null) return;
                Tween.Squash(cone.transform, cone.RestScale, 0.25f, 0.45f);
                FxManager.Play(FxType.Confetti, cone.transform.position + Vector3.up * 1.3f, 22);
                FxManager.Play(FxType.Sparkle, cone.transform.position + Vector3.up * 1.3f, 14);
                cone.PlaySfx(SfxId.Celebration, 0.8f);
            });
            GameEvents.Raise(new GameplayEvent(GameplayEventType.IceCreamCompleted, "ice_cream", SceneObjectRegistry.KeyOf(this), this,
                null, ScoopCount));
        }

        // ------------------------------------------------------------------ container
        public void Release(FoodItem item)
        {
            if (item != m_Cone) return;
            m_Cone = null;
            m_Visual = null;
            m_Completed = false;
        }

        /// <summary>Tapping the cone itself moves it along: scoop → sprinkles → cherry, then it can be served.</summary>
        public bool HandleTap(FoodItem item)
        {
            if (item != m_Cone) return false;
            if (m_Completed) return false;
            if (ScoopCount == 0) { AddScoop(m_NextAssistFlavour++ % Mathf.Max(1, m_FlavourIds.Count)); return true; }
            if (!HasSprinkles) { AddSprinkles(); return true; }
            AddCherry();
            return true;
        }

        public bool AllowPickup(FoodItem item) => item != m_Cone || ScoopCount > 0;

        /// <summary>One step forward (guided assist and tests): cone, two scoops, sprinkles, cherry, serve.</summary>
        public bool Assist(string itemId, string targetKey)
        {
            if (m_Cone == null) { PlaceCone(null); return true; }
            if (!m_Completed)
            {
                if (ScoopCount < 2) { AddScoop(m_NextAssistFlavour++ % Mathf.Max(1, m_FlavourIds.Count)); return true; }
                if (!HasSprinkles) { AddSprinkles(); return true; }
                AddCherry();
                return true;
            }
            return m_Cone.SendToKeyOrAuto(targetKey);
        }

        public void Clear()
        {
            if (m_Cone == null) return;
            FoodItem cone = m_Cone;
            Release(cone);
            cone.AttachTo(null);
            cone.Vanish();
        }
    }
}
