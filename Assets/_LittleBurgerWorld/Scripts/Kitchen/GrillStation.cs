using System;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>
    /// Friendly grill: drop a raw patty, watch it cook, tap to flip, then take the cooked patty.
    /// No burning, no fire, no failure – a patty left on the grill simply stays warm and ready.
    /// </summary>
    public sealed class GrillStation : DropZone, IFoodContainer, IAssistable, IHintTarget
    {
        public enum SpotState { Empty, CookingFirstSide, WaitingForFlip, CookingSecondSide, Cooked }

        [Serializable]
        private sealed class Spot
        {
            public Transform anchor;
            [NonSerialized] public FoodItem item;
            [NonSerialized] public SpotState state;
            [NonSerialized] public float timer;
            [NonSerialized] public float steamTimer;
        }

        [SerializeField] private Spot[] m_Spots = Array.Empty<Spot>();
        [SerializeField] private float m_SideSeconds = 2.2f;
        [SerializeField] private Renderer m_GlowRenderer;

        private int m_SizzleLoop;

        public int SpotCount => m_Spots.Length;
        public float SideSeconds { get => m_SideSeconds; set => m_SideSeconds = value; }

        public Vector3 HintPosition
        {
            get
            {
                foreach (Spot s in m_Spots) if (s.item != null) return s.item.transform.position + Vector3.up * 0.15f;
                return SnapPoint;
            }
        }

        public SpotState GetState(int index) => index >= 0 && index < m_Spots.Length ? m_Spots[index].state : SpotState.Empty;
        public FoodItem GetItem(int index) => index >= 0 && index < m_Spots.Length ? m_Spots[index].item : null;

        public override Vector3 SnapPoint
        {
            get
            {
                int i = FreeSpot(transform.position);
                return i >= 0 ? m_Spots[i].anchor.position : base.SnapPoint;
            }
        }

        // ------------------------------------------------------------------ DropZone
        public override bool CanAccept(IDraggable item)
        {
            if (!(item is FoodItem f) || f.Type != FoodType.Ingredient || f.IsConsumed) return false;
            if (f.GetComponent<CookableVisual>() == null) return false;
            return FreeSpot(transform.position) >= 0;
        }

        private int FreeSpot(Vector3 near)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < m_Spots.Length; i++)
            {
                if (m_Spots[i].item != null) continue;
                float d = (m_Spots[i].anchor.position - near).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        public override void Accept(IDraggable item, Vector3 worldPoint)
        {
            if (!(item is FoodItem f)) return;
            int i = FreeSpot(worldPoint);
            if (i < 0) { f.ReturnHome(); return; }
            Spot s = m_Spots[i];
            s.item = f;
            s.timer = 0f;
            s.state = f.IsRawIngredient ? SpotState.CookingFirstSide : SpotState.Cooked;
            f.AttachTo(this);
            f.Locked = s.state != SpotState.Cooked;
            f.transform.SetParent(s.anchor, true);
            Tween.Kill(f.transform);
            Tween.RotateLocal(f.transform, Quaternion.identity, 0.25f);
            Tween.Jump(f.transform, s.anchor.position, 0.4f, 0.3f, () =>
            {
                if (f == null) return;
                f.transform.localScale = f.RestScale;
                Tween.Squash(f.transform, f.RestScale, 0.25f);
                f.PlaySfx(SfxId.Plop);
                FxManager.Play(FxType.Steam, s.anchor.position + Vector3.up * 0.2f, 4);
            });
            RaiseDropped(GameplayEventType.PattyOnGrill, f.ItemId);
            UpdateSizzle();
        }

        // ------------------------------------------------------------------ cooking
        private void Update()
        {
            bool anyCooking = false;
            for (int i = 0; i < m_Spots.Length; i++)
            {
                Spot s = m_Spots[i];
                if (s.item == null) { if (s.state != SpotState.Empty) Free(s); continue; }
                switch (s.state)
                {
                    case SpotState.CookingFirstSide:
                    case SpotState.CookingSecondSide:
                        anyCooking = true;
                        s.timer += Time.deltaTime;
                        float side = Mathf.Clamp01(s.timer / m_SideSeconds);
                        float amount = s.state == SpotState.CookingFirstSide ? side * 0.5f : 0.5f + side * 0.5f;
                        SetCook(s.item, amount);
                        s.steamTimer -= Time.deltaTime;
                        if (s.steamTimer <= 0f)
                        {
                            s.steamTimer = 0.35f;
                            FxManager.Play(FxType.Steam, s.anchor.position + Vector3.up * 0.25f, 1);
                        }
                        if (side >= 1f)
                        {
                            if (s.state == SpotState.CookingFirstSide) EnterWaitForFlip(s);
                            else FinishCooking(s);
                        }
                        break;
                    case SpotState.WaitingForFlip:
                        anyCooking = true;
                        s.timer += Time.deltaTime;
                        // Gentle "flip me!" bounce.
                        float bob = Mathf.Abs(Mathf.Sin(s.timer * 5f)) * 0.08f;
                        s.item.transform.localPosition = new Vector3(0f, bob, 0f);
                        break;
                }
            }
            if (m_GlowRenderer != null)
            {
                m_GlowRenderer.enabled = true;
            }
            if (!anyCooking && m_SizzleLoop != 0) UpdateSizzle();
        }

        private static void SetCook(FoodItem item, float amount)
        {
            CookableVisual cv = item.GetComponent<CookableVisual>();
            if (cv != null) cv.SetCookAmount(amount);
        }

        private void EnterWaitForFlip(Spot s)
        {
            s.state = SpotState.WaitingForFlip;
            s.timer = 0f;
            FxManager.Play(FxType.Sparkle, s.anchor.position + Vector3.up * 0.3f, 6);
            s.item.PlaySfx(SfxId.Ding, 0.5f);
        }

        private void Flip(Spot s)
        {
            if (s.state != SpotState.WaitingForFlip) return;
            s.state = SpotState.CookingSecondSide;
            s.timer = 0f;
            Transform t = s.item.transform;
            t.localPosition = Vector3.zero;
            Tween.Kill(t);
            Tween.Value(t, 0.5f, k =>
            {
                t.localPosition = new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.7f, 0f);
                t.localRotation = Quaternion.Euler(k * 180f, 0f, 0f);
            }, Ease.InOutQuad, () =>
            {
                if (t == null) return;
                t.localRotation = Quaternion.identity;
                t.localPosition = Vector3.zero;
                Tween.Squash(t, s.item.RestScale, 0.3f);
                FxManager.Play(FxType.Steam, s.anchor.position + Vector3.up * 0.2f, 4);
            });
            s.item.PlaySfx(SfxId.Flip);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.PattyFlipped, s.item.ItemId,
                SceneObjectRegistry.KeyOf(this), this));
        }

        private void FinishCooking(Spot s)
        {
            s.state = SpotState.Cooked;
            FoodItem f = s.item;
            if (f.Ingredient != null && f.Ingredient.cookedVariant != null) f.SetIngredient(f.Ingredient.cookedVariant);
            SetCook(f, 1f);
            f.Locked = false;
            f.transform.localPosition = Vector3.zero;
            Tween.Squash(f.transform, f.RestScale, 0.3f, 0.4f);
            FxManager.Play(FxType.Sparkle, s.anchor.position + Vector3.up * 0.35f, 14);
            f.PlaySfx(SfxId.Ding);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.PattyCooked, f.ItemId, SceneObjectRegistry.KeyOf(this), this));
            UpdateSizzle();
        }

        private void UpdateSizzle()
        {
            bool cooking = false;
            foreach (Spot s in m_Spots)
            {
                if (s.item != null && s.state != SpotState.Cooked && s.state != SpotState.Empty) cooking = true;
            }
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (cooking && m_SizzleLoop == 0) m_SizzleLoop = gm.Audio.PlayLoop(SfxId.Sizzle, 0.6f);
            else if (!cooking && m_SizzleLoop != 0) { gm.Audio.StopLoop(m_SizzleLoop); m_SizzleLoop = 0; }
        }

        private void Free(Spot s)
        {
            s.item = null;
            s.state = SpotState.Empty;
            s.timer = 0f;
        }

        // ------------------------------------------------------------------ container
        public void Release(FoodItem item)
        {
            foreach (Spot s in m_Spots)
            {
                if (s.item != item) continue;
                Free(s);
            }
            UpdateSizzle();
        }

        public bool HandleTap(FoodItem item)
        {
            foreach (Spot s in m_Spots)
            {
                if (s.item != item) continue;
                switch (s.state)
                {
                    case SpotState.WaitingForFlip: Flip(s); return true;
                    case SpotState.CookingFirstSide:
                    case SpotState.CookingSecondSide:
                        Tween.Squash(item.transform, item.RestScale, 0.12f);
                        FxManager.Play(FxType.Steam, s.anchor.position + Vector3.up * 0.2f, 2);
                        return true;
                    default: return false; // cooked: fly to the board
                }
            }
            return false;
        }

        public bool AllowPickup(FoodItem item)
        {
            foreach (Spot s in m_Spots)
            {
                if (s.item == item) return s.state == SpotState.Cooked;
            }
            return true;
        }

        /// <summary>First cooked patty on the grill, if any.</summary>
        public FoodItem FindCooked()
        {
            foreach (Spot s in m_Spots)
            {
                if (s.item != null && s.state == SpotState.Cooked) return s.item;
            }
            return null;
        }

        public bool HasWaitingForFlip()
        {
            foreach (Spot s in m_Spots) if (s.item != null && s.state == SpotState.WaitingForFlip) return true;
            return false;
        }

        /// <summary>Flips the first patty waiting to be flipped (used by tests and guided assist).</summary>
        public bool FlipAny()
        {
            foreach (Spot s in m_Spots)
            {
                if (s.item != null && s.state == SpotState.WaitingForFlip) { Flip(s); return true; }
            }
            return false;
        }

        public bool Assist(string itemId, string targetKey)
        {
            if (FlipAny()) return true;
            FoodItem cooked = FindCooked();
            if (cooked != null) return cooked.SendToKeyOrAuto(targetKey == "Grill" ? null : targetKey);
            return false;
        }

        public void Clear()
        {
            foreach (Spot s in m_Spots)
            {
                if (s.item != null) s.item.Vanish();
                Free(s);
            }
            UpdateSizzle();
        }

        private void OnDestroy()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && m_SizzleLoop != 0) gm.Audio.StopLoop(m_SizzleLoop);
        }
    }
}
