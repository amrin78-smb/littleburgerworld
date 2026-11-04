using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using UnityEngine;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>
    /// Milkshake machine: pick a flavour, put a cup under the nozzle, tap (or hold) the big lever,
    /// add whipped cream and a straw. Steps are forgiving – a missing cup is placed automatically and
    /// an unchosen flavour defaults to the first one.
    /// </summary>
    public sealed class DrinkStation : MonoBehaviour, IFoodContainer, IAssistable, IHintTarget
    {
        public enum CupState { None, Empty, Filling, Filled, Creamed, Complete }

        [SerializeField] private List<DrinkData> m_Flavours = new List<DrinkData>();
        [SerializeField] private List<TapButton3D> m_FlavourButtons = new List<TapButton3D>();
        [SerializeField] private TapButton3D m_CupStack;
        [SerializeField] private TapButton3D m_Lever;
        [SerializeField] private TapButton3D m_CreamButton;
        [SerializeField] private TapButton3D m_StrawJar;
        [SerializeField] private Transform m_CupSpot;
        [SerializeField] private Transform m_CupSpawn;
        [SerializeField] private Renderer m_NozzleRenderer;
        [SerializeField] private GameObject m_CupPrefab;
        [SerializeField] private float m_FillSeconds = 1.6f;

        private int m_Selected;
        private FoodItem m_Cup;
        private MilkshakeVisual m_CupVisual;
        private CupState m_State = CupState.None;
        private float m_FillTimer;
        private int m_PourLoop;
        private MaterialPropertyBlock m_Block;
        private readonly List<System.Action> m_FlavourHandlers = new List<System.Action>();

        public CupState State => m_State;
        public Vector3 HintPosition => m_Cup != null ? m_Cup.transform.position + Vector3.up * 0.6f : m_CupSpot.position + Vector3.up * 0.5f;
        public DrinkData SelectedFlavour => m_Flavours.Count > 0 ? m_Flavours[Mathf.Clamp(m_Selected, 0, m_Flavours.Count - 1)] : null;
        public FoodItem Cup => m_Cup;
        public float FillSeconds { get => m_FillSeconds; set => m_FillSeconds = value; }

        private void OnEnable()
        {
            m_FlavourHandlers.Clear();
            for (int i = 0; i < m_FlavourButtons.Count; i++)
            {
                int idx = i;
                System.Action h = () => SelectFlavour(idx);
                m_FlavourHandlers.Add(h);
                if (m_FlavourButtons[i] != null) m_FlavourButtons[i].Tapped += h;
            }
            if (m_CupStack != null) m_CupStack.Tapped += OnCupStack;
            if (m_Lever != null) m_Lever.Tapped += OnLever;
            if (m_CreamButton != null) m_CreamButton.Tapped += OnCream;
            if (m_StrawJar != null) m_StrawJar.Tapped += OnStraw;
            TintNozzle();
        }

        private void OnDisable()
        {
            for (int i = 0; i < m_FlavourButtons.Count && i < m_FlavourHandlers.Count; i++)
            {
                if (m_FlavourButtons[i] != null) m_FlavourButtons[i].Tapped -= m_FlavourHandlers[i];
            }
            m_FlavourHandlers.Clear();
            if (m_CupStack != null) m_CupStack.Tapped -= OnCupStack;
            if (m_Lever != null) m_Lever.Tapped -= OnLever;
            if (m_CreamButton != null) m_CreamButton.Tapped -= OnCream;
            if (m_StrawJar != null) m_StrawJar.Tapped -= OnStraw;
            StopPour();
        }

        // ------------------------------------------------------------------ steps
        public void SelectFlavour(int index)
        {
            if (index < 0 || index >= m_Flavours.Count) return;
            m_Selected = index;
            TintNozzle();
            DrinkData d = SelectedFlavour;
            FxManager.Play(FxType.Sparkle, m_FlavourButtons[index].transform.position + Vector3.up * 0.3f, 6, d.liquidColor);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.DrinkFlavourChosen, d.id, SceneObjectRegistry.KeyOf(this), this));
            if (m_State == CupState.Empty) StartFill(); // forgiving shortcut: cup waiting → fill straight away
        }

        private void TintNozzle()
        {
            DrinkData d = SelectedFlavour;
            if (m_NozzleRenderer == null || d == null) return;
            m_Block ??= new MaterialPropertyBlock();
            m_NozzleRenderer.GetPropertyBlock(m_Block);
            m_Block.SetColor("_BaseColor", d.liquidColor);
            m_NozzleRenderer.SetPropertyBlock(m_Block);
        }

        private void OnCupStack()
        {
            if (m_Cup != null) { m_CupStack.Wiggle(); return; }
            PlaceCup(null);
        }

        /// <summary>Puts a fresh cup under the nozzle.</summary>
        public void PlaceCup(System.Action onPlaced)
        {
            if (m_Cup != null || m_CupPrefab == null) { onPlaced?.Invoke(); return; }
            Vector3 from = m_CupSpawn != null ? m_CupSpawn.position : m_CupSpot.position + Vector3.up;
            GameObject go = Instantiate(m_CupPrefab, from, Quaternion.identity, m_CupSpot);
            m_Cup = go.GetComponent<FoodItem>();
            if (m_Cup == null) m_Cup = go.AddComponent<FoodItem>();
            m_Cup.Configure(FoodType.Milkshake, 0.9f);
            m_Cup.AttachTo(this);
            m_Cup.Locked = true;
            m_CupVisual = go.GetComponent<MilkshakeVisual>();
            if (m_CupVisual != null) m_CupVisual.ResetVisual();
            m_State = CupState.Empty;
            FoodItem cup = m_Cup;
            Tween.Jump(go.transform, m_CupSpot.position, 0.6f, 0.4f, () =>
            {
                if (cup == null) return;
                Tween.Squash(cup.transform, cup.RestScale, 0.2f);
                cup.PlaySfx(SfxId.Plop);
                onPlaced?.Invoke();
            });
            GameEvents.Raise(new GameplayEvent(GameplayEventType.CupPlaced, "cup", SceneObjectRegistry.KeyOf(this), this));
        }

        private void OnLever()
        {
            switch (m_State)
            {
                case CupState.None: PlaceCup(StartFill); break;
                case CupState.Empty: StartFill(); break;
                default: m_Lever.Wiggle(); break;
            }
        }

        public void StartFill()
        {
            if (m_State != CupState.Empty || m_Cup == null) return;
            m_State = CupState.Filling;
            m_FillTimer = 0f;
            DrinkData d = SelectedFlavour;
            if (m_CupVisual != null && d != null) m_CupVisual.SetColors(d.liquidColor, d.creamColor);
            m_Cup.SetDrink(d);
            GameManager gm = GameManager.Instance;
            if (gm != null && m_PourLoop == 0) m_PourLoop = gm.Audio.PlayLoop(SfxId.Pour, 0.6f);
        }

        private void Update()
        {
            if (m_State != CupState.Filling) return;
            if (m_Cup == null) { m_State = CupState.None; StopPour(); return; }
            m_FillTimer += Time.deltaTime;
            float k = Mathf.Clamp01(m_FillTimer / m_FillSeconds);
            if (m_CupVisual != null) m_CupVisual.SetFill(k);
            if (k >= 1f)
            {
                m_State = CupState.Filled;
                StopPour();
                Tween.Squash(m_Cup.transform, m_Cup.RestScale, 0.15f);
                FxManager.Play(FxType.Sparkle, m_Cup.transform.position + Vector3.up * 0.9f, 8);
                m_Cup.PlaySfx(SfxId.Ding, 0.6f);
                GameEvents.Raise(new GameplayEvent(GameplayEventType.DrinkFilled, SelectedFlavour != null ? SelectedFlavour.id : "milkshake",
                    SceneObjectRegistry.KeyOf(this), this));
            }
        }

        private void StopPour()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && m_PourLoop != 0) gm.Audio.StopLoop(m_PourLoop);
            m_PourLoop = 0;
        }

        private void OnCream()
        {
            if (m_State != CupState.Filled || m_CupVisual == null) { m_CreamButton.Wiggle(); return; }
            AddCream();
        }

        public void AddCream()
        {
            if (m_State != CupState.Filled) return;
            m_State = CupState.Creamed;
            GameObject cream = m_CupVisual != null ? m_CupVisual.Cream : null;
            if (cream != null)
            {
                cream.SetActive(true);
                Vector3 rest = cream.transform.localScale;
                cream.transform.localScale = Vector3.zero;
                Tween.Scale(cream.transform, rest, 0.4f, Ease.OutElastic);
            }
            m_Cup.PlaySfx(SfxId.Cream);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.CreamAdded, "cream", SceneObjectRegistry.KeyOf(this), this));
        }

        private void OnStraw()
        {
            if (m_State != CupState.Filled && m_State != CupState.Creamed) { m_StrawJar.Wiggle(); return; }
            AddStraw();
        }

        public void AddStraw()
        {
            if (m_State != CupState.Filled && m_State != CupState.Creamed) return;
            m_State = CupState.Complete;
            GameObject straw = m_CupVisual != null ? m_CupVisual.Straw : null;
            if (straw != null)
            {
                straw.SetActive(true);
                Vector3 rest = straw.transform.localPosition;
                straw.transform.localPosition = rest + Vector3.up * 0.8f;
                Tween.MoveLocal(straw.transform, rest, 0.35f, Ease.OutBounce);
            }
            m_Cup.PlaySfx(SfxId.Straw);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.StrawAdded, "straw", SceneObjectRegistry.KeyOf(this), this));
            CompleteShake();
        }

        private void CompleteShake()
        {
            FoodItem cup = m_Cup;
            cup.Locked = false;
            Tween.Delay(this, 0.35f, () =>
            {
                if (cup == null) return;
                Tween.Squash(cup.transform, cup.RestScale, 0.25f, 0.45f);
                FxManager.Play(FxType.Confetti, cup.transform.position + Vector3.up * 1f, 22);
                FxManager.Play(FxType.Sparkle, cup.transform.position + Vector3.up * 1f, 14);
                cup.PlaySfx(SfxId.Celebration, 0.8f);
            });
            GameEvents.Raise(new GameplayEvent(GameplayEventType.MilkshakeCompleted, "milkshake",
                SceneObjectRegistry.KeyOf(this), this, SelectedFlavour != null ? SelectedFlavour.id : null));
        }

        // ------------------------------------------------------------------ container
        public void Release(FoodItem item)
        {
            if (item != m_Cup) return;
            m_Cup = null;
            m_CupVisual = null;
            m_State = CupState.None;
            StopPour();
        }

        public bool HandleTap(FoodItem item)
        {
            if (item != m_Cup) return false;
            switch (m_State)
            {
                case CupState.Empty: StartFill(); return true;
                case CupState.Filled: AddCream(); return true;
                case CupState.Creamed: AddStraw(); return true;
                case CupState.Complete: return false;
                default: return true;
            }
        }

        public bool AllowPickup(FoodItem item) => item != m_Cup || m_State == CupState.Complete;

        /// <summary>Advances the milkshake by one step (guided assist and tests).</summary>
        public bool Assist(string itemId, string targetKey)
        {
            switch (m_State)
            {
                case CupState.None: PlaceCup(null); return true;
                case CupState.Empty: StartFill(); return true;
                case CupState.Filled: AddCream(); return true;
                case CupState.Creamed: AddStraw(); return true;
                case CupState.Complete:
                    return m_Cup != null && m_Cup.SendToKeyOrAuto(targetKey);
            }
            return false;
        }

        public void Clear()
        {
            StopPour();
            if (m_Cup != null)
            {
                FoodItem cup = m_Cup;
                Release(cup);
                cup.AttachTo(null);
                cup.Vanish();
            }
        }
    }
}
