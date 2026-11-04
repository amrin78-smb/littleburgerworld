using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using UnityEngine;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>
    /// French fries: tap the potato basket → a potato hops into the happy fryer → a batch of golden
    /// fries pops out onto the collection tray, ready to be dragged (or tapped) into a paper box.
    /// </summary>
    public sealed class FryStation : MonoBehaviour, IFoodContainer, IAssistable, IHintTarget
    {
        [SerializeField] private TapButton3D m_PotatoBasket;
        [SerializeField] private Transform m_PotatoSpawn;
        [SerializeField] private Transform m_FryerPoint;
        [SerializeField] private Transform m_BatchSpot;
        [SerializeField] private Transform m_FryerBasketVisual;
        [SerializeField] private GameObject m_PotatoPrefab;
        [SerializeField] private GameObject m_FriesBatchPrefab;
        [SerializeField] private float m_FrySeconds = 2f;

        private bool m_Frying;
        private float m_FryTimer;
        private float m_BubbleTimer;
        private FoodItem m_Batch;
        private int m_BubbleLoop;
        private Vector3 m_BasketRest;

        public bool IsFrying => m_Frying;
        public Vector3 HintPosition => m_Batch != null ? m_Batch.transform.position + Vector3.up * 0.2f :
            (m_BatchSpot != null ? m_BatchSpot.position : transform.position);
        public FoodItem WaitingBatch => m_Batch;
        public float FrySeconds { get => m_FrySeconds; set => m_FrySeconds = value; }

        private void Awake()
        {
            if (m_FryerBasketVisual != null) m_BasketRest = m_FryerBasketVisual.localPosition;
        }

        private void OnEnable()
        {
            if (m_PotatoBasket != null) m_PotatoBasket.Tapped += OnPotatoTapped;
        }

        private void OnDisable()
        {
            if (m_PotatoBasket != null) m_PotatoBasket.Tapped -= OnPotatoTapped;
            StopBubbles();
        }

        /// <summary>Starts frying a potato. Returns false (with a playful wiggle) if busy.</summary>
        public bool StartFrying()
        {
            if (m_Frying || m_Batch != null)
            {
                m_PotatoBasket?.Wiggle();
                if (m_Batch != null) HintPulseBriefly(m_Batch.transform);
                return false;
            }
            m_Frying = true;
            m_FryTimer = 0f;
            GameEvents.Raise(new GameplayEvent(GameplayEventType.PotatoTapped, "potato", SceneObjectRegistry.KeyOf(this), this));

            if (m_PotatoPrefab != null && m_PotatoSpawn != null)
            {
                GameObject potato = Instantiate(m_PotatoPrefab, m_PotatoSpawn.position, Quaternion.identity, transform);
                Vector3 rest = potato.transform.localScale;
                potato.transform.localScale = Vector3.zero;
                Tween.Scale(potato.transform, rest, 0.2f, Ease.OutBack);
                Tween.Jump(potato.transform, m_FryerPoint.position, 1.2f, 0.55f, () =>
                {
                    FxManager.Play(FxType.Bubbles, m_FryerPoint.position, 10);
                    Tween.Scale(potato.transform, Vector3.zero, 0.15f, Ease.InBack, () => Destroy(potato));
                    PlaySfx(SfxId.Plop);
                }, 0.1f);
            }
            GameManager gm = GameManager.Instance;
            if (gm != null && m_BubbleLoop == 0) m_BubbleLoop = gm.Audio.PlayLoop(SfxId.Bubble, 0.5f);
            return true;
        }

        private void OnPotatoTapped() => StartFrying();

        private void Update()
        {
            if (!m_Frying) return;
            m_FryTimer += Time.deltaTime;
            m_BubbleTimer -= Time.deltaTime;
            if (m_BubbleTimer <= 0f && m_FryerPoint != null)
            {
                m_BubbleTimer = 0.25f;
                FxManager.Play(FxType.Bubbles, m_FryerPoint.position, 2);
            }
            if (m_FryerBasketVisual != null)
            {
                m_FryerBasketVisual.localPosition = m_BasketRest + Vector3.up * (Mathf.Sin(m_FryTimer * 12f) * 0.03f);
            }
            if (m_FryTimer >= m_FrySeconds) FinishBatch();
        }

        private void FinishBatch()
        {
            m_Frying = false;
            StopBubbles();
            if (m_FryerBasketVisual != null) m_FryerBasketVisual.localPosition = m_BasketRest;
            if (m_FriesBatchPrefab == null || m_BatchSpot == null) return;

            GameObject go = Instantiate(m_FriesBatchPrefab, m_FryerPoint.position, Quaternion.identity);
            FoodItem batch = go.GetComponent<FoodItem>();
            if (batch == null) batch = go.AddComponent<FoodItem>();
            batch.Configure(FoodType.FriesBatch, 0.35f);
            batch.HomePosition = m_BatchSpot.position;
            batch.AttachTo(this);
            m_Batch = batch;
            Vector3 rest = batch.RestScale;
            go.transform.localScale = rest * 0.3f;
            Tween.Scale(go.transform, rest, 0.4f, Ease.OutBack);
            Tween.Jump(go.transform, m_BatchSpot.position, 1f, 0.5f, () =>
            {
                if (batch == null) return;
                Tween.Squash(batch.transform, batch.RestScale, 0.25f);
                FxManager.Play(FxType.Sparkle, m_BatchSpot.position + Vector3.up * 0.3f, 12);
            });
            PlaySfx(SfxId.FriesReady);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.FriesReady, "fries_batch", SceneObjectRegistry.KeyOf(this), this));
        }

        private void StopBubbles()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && m_BubbleLoop != 0) gm.Audio.StopLoop(m_BubbleLoop);
            m_BubbleLoop = 0;
        }

        private static void HintPulseBriefly(Transform t)
        {
            if (t == null) return;
            HintPulse p = HintPulse.Begin(t);
            Tween.Delay(t, 1.2f, () => { if (p != null) p.Stop(); });
        }

        private void PlaySfx(SfxId id)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(id);
        }

        // ------------------------------------------------------------------ container (batch spot)
        public void Release(FoodItem item)
        {
            if (item == m_Batch) m_Batch = null;
        }

        public bool HandleTap(FoodItem item) => false;
        public bool AllowPickup(FoodItem item) => true;

        public bool TakeBack(FoodItem item)
        {
            if (m_Batch != null || item == null || item.Type != FoodType.FriesBatch) return false;
            m_Batch = item;
            item.AttachTo(this);
            return true;
        }

        public bool Assist(string itemId, string targetKey)
        {
            if (m_Batch != null && FoodItem.AutoSendHandler != null) return FoodItem.AutoSendHandler(m_Batch);
            return StartFrying();
        }

        public void Clear()
        {
            m_Frying = false;
            StopBubbles();
            if (m_Batch != null) m_Batch.Vanish();
            m_Batch = null;
        }
    }
}
