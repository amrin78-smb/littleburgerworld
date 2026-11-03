using System;
using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// Any piece of food the child can touch: an ingredient, a finished burger, fries, a milkshake or the tray.
    /// Handles selection feedback, smooth dragging with a playful tilt, and always finds a safe home on release.
    /// Tapping food sends it to the next sensible place (see <see cref="AutoSendHandler"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoodItem : MonoBehaviour, IInteractable, IDraggable
    {
        public const int InteractableLayer = 6;

        /// <summary>Every live food item (used by reset and tests).</summary>
        public static readonly List<FoodItem> All = new List<FoodItem>();

        /// <summary>Set by the kitchen: decides where tapped food should fly. Returns true when handled.</summary>
        public static Func<FoodItem, bool> AutoSendHandler;

        [SerializeField] private FoodType m_Type = FoodType.Ingredient;
        [SerializeField] private IngredientData m_Ingredient;
        [SerializeField] private float m_Height = 0.3f;
        [SerializeField] private float m_DragHeight = 2.1f;
        [SerializeField] private float m_FollowSharpness = 20f;

        private DrinkData m_Drink;
        private RecipeData m_Recipe;
        private string m_CustomId;
        private readonly List<FoodItem> m_Contents = new List<FoodItem>();

        private IFoodContainer m_Container;
        private IFoodContainer m_OriginContainer;
        private Vector3 m_OriginPosition;
        private Quaternion m_OriginRotation;
        private bool m_FromSupply;
        private bool m_Dragging;
        private bool m_Contained;
        private bool m_Consumed;
        private Vector3 m_DragTarget;
        private Vector3 m_LastPos;
        private Vector3 m_RestScale = Vector3.one;
        private bool m_RestScaleCaptured;
        private Collider[] m_Colliders;

        public FoodType Type => m_Type;
        public IngredientData Ingredient => m_Ingredient;
        public DrinkData Drink => m_Drink;
        public RecipeData Recipe => m_Recipe;
        public IReadOnlyList<FoodItem> Contents => m_Contents;
        public IFoodContainer Container => m_Container;
        public bool Locked { get; set; }
        public bool IsDragging => m_Dragging;
        public bool IsConsumed => m_Consumed;
        public bool FromSupply => m_FromSupply;
        public Vector3 RestScale => m_RestScale;
        public Vector3 HomePosition { get; set; }

        /// <summary>Height when stacked (ingredients) or carried (everything else).</summary>
        public float Height => m_Type == FoodType.Ingredient && m_Ingredient != null ? m_Ingredient.stackHeight : m_Height;

        /// <summary>Stable id for events, requests and activities.</summary>
        public string ItemId
        {
            get
            {
                if (!string.IsNullOrEmpty(m_CustomId)) return m_CustomId;
                switch (m_Type)
                {
                    case FoodType.Ingredient: return m_Ingredient != null ? m_Ingredient.id : "ingredient";
                    case FoodType.Burger: return m_Recipe != null ? m_Recipe.id : "burger";
                    case FoodType.FriesBatch: return "fries_batch";
                    case FoodType.Fries: return "fries";
                    case FoodType.Milkshake: return "milkshake";
                    case FoodType.Tray: return "tray";
                }
                return "food";
            }
        }

        /// <summary>Generic category used for picture requests ("burger" for any burger).</summary>
        public string CategoryId => m_Type == FoodType.Burger ? "burger" : ItemId;

        public bool IsRawIngredient => m_Type == FoodType.Ingredient && m_Ingredient != null && m_Ingredient.IsRaw;

        // ------------------------------------------------------------------ lifecycle
        private void Awake()
        {
            CaptureRestScale();
            m_Colliders = GetComponents<Collider>();
            HomePosition = transform.position;
            if (m_Ingredient != null) ApplyIngredientVisual();
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        private void CaptureRestScale()
        {
            if (m_RestScaleCaptured) return;
            m_RestScale = transform.localScale;
            m_RestScaleCaptured = true;
        }

        public void Initialize(IngredientData data)
        {
            m_Type = FoodType.Ingredient;
            m_Ingredient = data;
            ApplyIngredientVisual();
        }

        public void Configure(FoodType type, float height, string customId = null)
        {
            m_Type = type;
            m_Height = height;
            m_CustomId = customId;
        }

        public void SetDrink(DrinkData drink) => m_Drink = drink;

        public void SetRecipe(RecipeData recipe) => m_Recipe = recipe;

        /// <summary>Swaps the ingredient (raw patty → cooked patty) keeping the same object.</summary>
        public void SetIngredient(IngredientData data)
        {
            m_Ingredient = data;
            ApplyIngredientVisual();
        }

        private void ApplyIngredientVisual()
        {
            var cook = GetComponent<CookableVisual>();
            if (cook != null && m_Ingredient != null) cook.SetCookAmount(m_Ingredient.IsRaw ? 0f : 1f);
            if (m_Ingredient != null && m_Ingredient.material != null)
            {
                var r = GetComponentInChildren<MeshRenderer>();
                if (r != null) r.sharedMaterial = m_Ingredient.material;
            }
        }

        // ------------------------------------------------------------------ containment
        public void AttachTo(IFoodContainer container)
        {
            m_Container = container;
            m_FromSupply = false;
        }

        public void Detach()
        {
            m_Container?.Release(this);
            m_Container = null;
        }

        /// <summary>Marks an item as freshly taken from an infinite supply (it vanishes if dropped nowhere).</summary>
        public void MarkFromSupply() => m_FromSupply = true;

        /// <summary>Becomes part of a bigger food (burger, tray). Its own collider is switched off.</summary>
        public void SetContained(bool contained)
        {
            m_Contained = contained;
            m_Colliders ??= GetComponents<Collider>();
            foreach (Collider c in m_Colliders)
            {
                if (c != null) c.enabled = !contained;
            }
        }

        public void SetContents(IEnumerable<FoodItem> parts)
        {
            m_Contents.Clear();
            if (parts == null) return;
            foreach (FoodItem p in parts)
            {
                if (p == null) continue;
                m_Contents.Add(p);
                p.SetContained(true);
                p.AttachTo(null);
                p.transform.SetParent(transform, true);
            }
        }

        public void AddContent(FoodItem item)
        {
            if (item == null || m_Contents.Contains(item)) return;
            m_Contents.Add(item);
            item.transform.SetParent(transform, true);
        }

        public void RemoveContent(FoodItem item)
        {
            if (m_Contents.Remove(item) && item != null && item.transform.parent == transform)
            {
                item.transform.SetParent(null, true);
            }
        }

        // ------------------------------------------------------------------ IInteractable
        public int InteractionPriority
        {
            get
            {
                switch (m_Type)
                {
                    case FoodType.Burger: return 14;
                    case FoodType.Fries:
                    case FoodType.Milkshake: return 13;
                    case FoodType.FriesBatch: return 12;
                    case FoodType.Tray: return 8;
                    default: return 10;
                }
            }
        }

        public bool IsInteractable => !m_Consumed && !m_Contained && isActiveAndEnabled;

        public bool CanDrag => IsInteractable && !Locked && (m_Container == null || m_Container.AllowPickup(this));

        public void OnSelect()
        {
            if (m_Dragging) return;
            Tween.Kill(transform);
            Tween.Squash(transform, m_RestScale, 0.18f, 0.3f);
            PlaySfx(m_Ingredient != null ? m_Ingredient.pickSound : SfxId.Pop);
        }

        public void OnDeselect() { }

        public void OnTap()
        {
            if (m_Container != null && m_Container.HandleTap(this)) return;
            if (!CanDrag) { Wiggle(); return; }
            if (AutoSendHandler == null || !AutoSendHandler(this)) Wiggle();
        }

        public IDraggable BeginDrag()
        {
            if (!CanDrag) return null;
            m_OriginContainer = m_Container;
            m_OriginPosition = transform.position;
            m_OriginRotation = transform.rotation;
            Detach();
            transform.SetParent(null, true);
            Tween.Kill(transform);
            return this;
        }

        /// <summary>Removes the item from its container so a script can move it (auto-send, assists).</summary>
        public bool TryLift()
        {
            if (BeginDrag() == null) return false;
            return true;
        }

        // ------------------------------------------------------------------ IDraggable
        public Transform Transform => this != null ? transform : null;
        public float DragHeight => m_DragHeight;

        public void OnDragStarted()
        {
            m_Dragging = true;
            m_LastPos = transform.position;
            m_DragTarget = transform.position;
            Tween.Scale(transform, m_RestScale * 1.12f, 0.15f, Ease.OutBack);
        }

        public void OnDragMoved(Vector3 worldTarget) => m_DragTarget = worldTarget;

        public void OnDropped(IDropTarget target)
        {
            m_Dragging = false;
            Tween.Scale(transform, m_RestScale, 0.18f, Ease.OutQuad);
        }

        public void OnDragCancelled()
        {
            m_Dragging = false;
            transform.localScale = m_RestScale;
            ReturnHome();
        }

        private void Update()
        {
            if (!m_Dragging) return;
            float k = 1f - Mathf.Exp(-m_FollowSharpness * Time.deltaTime);
            Vector3 p = Vector3.Lerp(transform.position, m_DragTarget, k);
            Vector3 vel = (p - m_LastPos) / Mathf.Max(Time.deltaTime, 0.001f);
            m_LastPos = p;
            transform.position = p;
            // Playful tilt in the direction of travel.
            Vector3 tilt = new Vector3(Mathf.Clamp(vel.z * 3f, -18f, 18f), transform.eulerAngles.y, Mathf.Clamp(-vel.x * 3f, -18f, 18f));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(tilt.x, tilt.y, tilt.z), k);
        }

        // ------------------------------------------------------------------ movement helpers
        /// <summary>Goes back where it came from, or somewhere safe. Never lost, never punished.</summary>
        public void ReturnHome()
        {
            if (m_OriginContainer is DropZone zone && zone != null && zone.isActiveAndEnabled && zone.CanAccept(this))
            {
                zone.Accept(this, m_OriginPosition);
                return;
            }
            if (m_FromSupply)
            {
                Vanish();
                return;
            }
            Vector3 target = m_Type == FoodType.Tray ? HomePosition : m_OriginPosition;
            Tween.Jump(transform, target, 0.6f, 0.4f, () =>
            {
                transform.rotation = m_OriginRotation;
                Tween.Squash(transform, m_RestScale, 0.15f);
            });
            PlaySfx(SfxId.Whoosh, 0.6f);
        }

        /// <summary>Flies the item into a drop target (used when a child taps food).</summary>
        public bool SendTo(IDropTarget target)
        {
            if (target == null || !CanDrag) return false;
            if (target is DropZone z && !z.CanAccept(this)) return false;
            m_OriginContainer = m_Container;
            m_OriginPosition = transform.position;
            Detach();
            transform.SetParent(null, true);
            PlaySfx(SfxId.Whoosh, 0.7f);
            target.Accept(this, target.SnapPoint);
            return true;
        }

        public void Wiggle()
        {
            Tween.Kill(transform);
            Quaternion baseRot = transform.localRotation;
            Tween.Value(transform, 0.4f, k =>
            {
                float a = Mathf.Sin(k * Mathf.PI * 4f) * 12f * (1f - k);
                transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, a);
            }, Ease.Linear, () => { if (this != null) transform.localRotation = baseRot; });
            PlaySfx(SfxId.Boing, 0.6f);
        }

        /// <summary>Disappears with a soft poof (supply items dropped nowhere, kitchen reset).</summary>
        public void Vanish(float delay = 0f)
        {
            if (m_Consumed) return;
            m_Consumed = true;
            Detach();
            Tween.Kill(transform);
            Tween.Scale(transform, Vector3.zero, 0.25f, Ease.InBack, () =>
            {
                FxManager.Play(FxType.Poof, transform.position + Vector3.up * 0.1f, 6);
                Destroy(gameObject);
            }, delay);
        }

        /// <summary>Eaten by a customer.</summary>
        public void Consume()
        {
            if (m_Consumed) return;
            m_Consumed = true;
            Detach();
            Destroy(gameObject);
        }

        public void PlaySfx(SfxId id, float volume = 1f)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Audio != null) gm.Audio.Play(id, volume);
        }

        /// <summary>Configures colliders/layer so the item can be picked.</summary>
        public void EnsurePickable(Vector3 colliderSize, Vector3 colliderCenter)
        {
            gameObject.layer = InteractableLayer;
            var box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();
            box.size = colliderSize;
            box.center = colliderCenter;
            m_Colliders = GetComponents<Collider>();
        }
    }
}
