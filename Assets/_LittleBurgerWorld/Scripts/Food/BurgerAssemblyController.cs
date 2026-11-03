using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// The burger board. Ingredients dropped here stack neatly (transform-based, no physics), each
    /// one hopping into place with a squash. A top bun on a stack completes the burger: it is matched
    /// against the recipes, celebrated, and becomes one draggable burger.
    /// Guided Play can restrict which ingredient is accepted next and lock the stack.
    /// </summary>
    public sealed class BurgerAssemblyController : DropZone, IFoodContainer, IAssistable, IHintTarget
    {
        [SerializeField] private Transform m_StackBase;
        [SerializeField] private int m_MaxItems = 12;

        private readonly BurgerStackModel m_Model = new BurgerStackModel();
        private readonly List<FoodItem> m_Items = new List<FoodItem>();
        private FoodItem m_Finished;
        private bool m_Completing;
        private string m_ExpectedIngredientId;

        public BurgerStackModel Model => m_Model;
        public Vector3 HintPosition => m_Finished != null ? m_Finished.transform.position + Vector3.up * 0.4f : SnapPoint;

        /// <summary>Assist: a finished burger flies on to the tray / customer.</summary>
        public bool Assist(string itemId, string targetKey)
        {
            if (m_Finished == null || FoodItem.AutoSendHandler == null) return false;
            if (!string.IsNullOrEmpty(targetKey))
            {
                var target = SceneObjectRegistry.Find<IDropTarget>(targetKey);
                if (target != null && m_Finished.SendTo(target)) return true;
            }
            return FoodItem.AutoSendHandler(m_Finished);
        }
        public IReadOnlyList<FoodItem> Items => m_Items;
        public FoodItem FinishedBurger => m_Finished;
        public bool IsCompleting => m_Completing;
        /// <summary>Guided Play: when set, only this ingredient id is accepted.</summary>
        public string ExpectedIngredientId { get => m_ExpectedIngredientId; set => m_ExpectedIngredientId = value; }
        /// <summary>Guided Play: stacked ingredients cannot be pulled off (keeps the activity coherent).</summary>
        public bool LockStack { get; set; }

        private Vector3 BasePosition => m_StackBase != null ? m_StackBase.position : transform.position;

        public override Vector3 SnapPoint => BasePosition + Vector3.up * (m_Model.TotalHeight + 0.05f);

        private void Awake() => m_Model.MaxItems = m_MaxItems;

        // ------------------------------------------------------------------ DropZone
        public override bool CanAccept(IDraggable item)
        {
            if (!(item is FoodItem f) || f.IsConsumed || m_Completing) return false;
            if (f.Type == FoodType.Burger) return m_Finished == null && m_Model.IsEmpty;
            if (f.Type != FoodType.Ingredient || m_Finished != null) return false;
            if (!string.IsNullOrEmpty(m_ExpectedIngredientId) && f.ItemId != m_ExpectedIngredientId) return false;
            return m_Model.CanAdd(f.Ingredient);
        }

        public override void Accept(IDraggable item, Vector3 worldPoint)
        {
            if (!(item is FoodItem f)) return;
            if (f.Type == FoodType.Burger)
            {
                PlaceFinished(f);
                return;
            }
            AddIngredient(f);
        }

        /// <summary>Adds an ingredient to the top of the stack (animated). Returns false if it does not fit.</summary>
        public bool AddIngredient(FoodItem f)
        {
            if (f == null || !m_Model.Add(f.Ingredient)) return false;
            int index = m_Items.Count;
            m_Items.Add(f);
            f.AttachTo(this);
            f.transform.SetParent(transform, true);

            Vector3 target = BasePosition + Vector3.up * m_Model.HeightBelow(index);
            float yaw = f.Ingredient != null && f.Ingredient.id == "cheese" ? 45f + Random.Range(-12f, 12f) : Random.Range(0f, 360f);
            Tween.Kill(f.transform);
            float dist = Vector3.Distance(f.transform.position, target);
            Tween.RotateLocal(f.transform, Quaternion.Euler(0f, yaw, 0f), 0.3f);
            Tween.Jump(f.transform, target, 0.25f + dist * 0.12f, 0.22f + Mathf.Min(0.3f, dist * 0.05f), () =>
            {
                if (f == null) return;
                f.transform.localScale = f.RestScale;
                Tween.Squash(f.transform, f.RestScale, 0.3f, 0.35f);
                f.PlaySfx(f.Ingredient != null ? f.Ingredient.placeSound : SfxId.Snap);
                FxManager.Play(FxType.Sparkle, f.transform.position + Vector3.up * 0.1f, 6);
            });

            RaiseDropped(GameplayEventType.IngredientStacked, f.ItemId);

            if (m_Model.IsComplete) BeginCompletion();
            return true;
        }

        private void PlaceFinished(FoodItem burger)
        {
            m_Finished = burger;
            burger.AttachTo(this);
            Tween.Kill(burger.transform);
            Tween.Jump(burger.transform, BasePosition, 0.5f, 0.3f, () =>
            {
                if (burger != null) Tween.Squash(burger.transform, burger.RestScale, 0.2f);
            });
        }

        // ------------------------------------------------------------------ completion
        private void BeginCompletion()
        {
            m_Completing = true;
            foreach (FoodItem f in m_Items) f.Locked = true;
            Tween.Delay(this, 0.45f, CompleteBurger);
        }

        private void CompleteBurger()
        {
            m_Completing = false;
            var parts = new List<FoodItem>(m_Items);
            parts.RemoveAll(p => p == null);
            if (parts.Count == 0) { m_Items.Clear(); m_Model.Clear(); return; }

            RecipeData recipe = GameManager.Instance != null && GameManager.Instance.Recipes != null
                ? GameManager.Instance.Recipes.Match(m_Model.Items)
                : null;
            float height = m_Model.TotalHeight;
            int layers = m_Model.Count;

            m_Items.Clear();
            m_Model.Clear();
            foreach (FoodItem p in parts) p.Locked = false;

            FoodItem burger = FoodManager.CreateBurger(parts, recipe, BasePosition, height, transform);
            m_Finished = burger;
            burger.AttachTo(this);
            Celebrate(burger, recipe);

            GameEvents.Raise(new GameplayEvent(GameplayEventType.BurgerCompleted, burger.ItemId,
                SceneObjectRegistry.KeyOf(this), burger, recipe != null ? recipe.id : "custom", layers));
        }

        private void Celebrate(FoodItem burger, RecipeData recipe)
        {
            Transform t = burger.transform;
            Vector3 basePos = t.position;
            Quaternion baseRot = t.localRotation;
            burger.Locked = true;
            Tween.Value(t, 0.9f, k =>
            {
                float hop = Mathf.Sin(k * Mathf.PI) * 0.6f;
                t.position = basePos + Vector3.up * hop;
                t.localRotation = baseRot * Quaternion.Euler(0f, k * 360f, 0f);
            }, Ease.InOutQuad, () =>
            {
                if (burger == null) return;
                t.position = basePos;
                t.localRotation = baseRot;
                burger.Locked = false;
                Tween.Squash(t, burger.RestScale, 0.25f, 0.4f);
            });
            FxManager.Play(FxType.Sparkle, basePos + Vector3.up * 0.6f, 24);
            FxManager.Play(FxType.Stars, basePos + Vector3.up * 0.9f, 8);
            burger.PlaySfx(recipe != null ? recipe.completionSound : SfxId.Jingle);
        }

        // ------------------------------------------------------------------ IFoodContainer
        public void Release(FoodItem item)
        {
            if (item == m_Finished)
            {
                m_Finished = null;
                return;
            }
            int idx = m_Items.IndexOf(item);
            if (idx < 0) return;
            m_Items.RemoveAt(idx);
            m_Model.RemoveAt(idx);
            Restack();
        }

        public bool HandleTap(FoodItem item)
        {
            if (item == m_Finished) return false; // let it fly to the tray/customer
            if (m_Items.Contains(item))
            {
                Tween.Squash(item.transform, item.RestScale, 0.2f);
                item.PlaySfx(SfxId.Pop, 0.6f);
                return true;
            }
            return false;
        }

        public bool AllowPickup(FoodItem item)
        {
            if (m_Completing) return false;
            if (item == m_Finished) return true;
            return !LockStack;
        }

        private void Restack()
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                FoodItem f = m_Items[i];
                if (f == null) continue;
                Vector3 target = BasePosition + Vector3.up * m_Model.HeightBelow(i);
                Tween.Move(f.transform, target, 0.25f, Ease.OutBack);
            }
        }

        /// <summary>Gently clears the board (Free Play reset / activity start).</summary>
        public void Clear(float stagger = 0.05f)
        {
            Tween.Kill(this);
            m_Completing = false;
            var items = new List<FoodItem>(m_Items);
            m_Items.Clear();
            m_Model.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null) items[i].Vanish(i * stagger);
            }
            if (m_Finished != null)
            {
                m_Finished.Vanish();
                m_Finished = null;
            }
        }
    }
}
