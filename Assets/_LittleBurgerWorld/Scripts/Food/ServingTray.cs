using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// The meal tray. Burgers, fries and milkshakes snap into friendly slots; the whole tray
    /// (a <see cref="FoodItem"/> of type Tray on the same object) can then be served to a customer.
    /// </summary>
    [RequireComponent(typeof(FoodItem))]
    public sealed class ServingTray : DropZone, IFoodContainer
    {
        [Tooltip("0 burger, 1 fries, 2 drink, then extra slots.")]
        [SerializeField] private Transform[] m_Slots;

        private FoodItem[] m_Contents;
        private FoodItem m_TrayItem;
        private bool m_MealAnnounced;

        public FoodItem TrayItem => m_TrayItem;

        public int ItemCount
        {
            get
            {
                EnsureArrays();
                int n = 0;
                foreach (FoodItem f in m_Contents) if (f != null) n++;
                return n;
            }
        }

        public bool HasMeal => Has(FoodType.Burger) && Has(FoodType.Fries) && Has(FoodType.Milkshake);

        private void Awake()
        {
            m_TrayItem = GetComponent<FoodItem>();
            m_TrayItem.Configure(FoodType.Tray, 0.1f);
            EnsureArrays();
        }

        private void EnsureArrays()
        {
            if (m_Contents == null || m_Contents.Length != (m_Slots?.Length ?? 0))
            {
                m_Contents = new FoodItem[m_Slots?.Length ?? 0];
            }
        }

        public bool Has(FoodType type)
        {
            EnsureArrays();
            foreach (FoodItem f in m_Contents) if (f != null && f.Type == type) return true;
            return false;
        }

        public List<FoodItem> GetContents()
        {
            EnsureArrays();
            var list = new List<FoodItem>();
            foreach (FoodItem f in m_Contents) if (f != null) list.Add(f);
            return list;
        }

        // ------------------------------------------------------------------ DropZone
        public override bool CanAccept(IDraggable item)
        {
            if (!(item is FoodItem f) || f == m_TrayItem || f.IsConsumed) return false;
            if (m_TrayItem != null && m_TrayItem.IsDragging) return false;
            if (f.Type != FoodType.Burger && f.Type != FoodType.Fries && f.Type != FoodType.Milkshake) return false;
            return FindSlot(f.Type) >= 0;
        }

        private int FindSlot(FoodType type)
        {
            EnsureArrays();
            int preferred = type == FoodType.Burger ? 0 : type == FoodType.Fries ? 1 : type == FoodType.Milkshake ? 2 : 3;
            if (preferred < m_Contents.Length && m_Contents[preferred] == null) return preferred;
            for (int i = 3; i < m_Contents.Length; i++) if (m_Contents[i] == null) return i;
            for (int i = 0; i < m_Contents.Length; i++) if (m_Contents[i] == null) return i;
            return -1;
        }

        public override void Accept(IDraggable item, Vector3 worldPoint)
        {
            if (!(item is FoodItem f)) return;
            int slot = FindSlot(f.Type);
            if (slot < 0) { f.ReturnHome(); return; }
            m_Contents[slot] = f;
            f.AttachTo(this);
            m_TrayItem.AddContent(f);
            Vector3 target = m_Slots[slot].position;
            Tween.Kill(f.transform);
            Tween.RotateLocal(f.transform, Quaternion.Euler(0f, 0f, 0f), 0.25f);
            Tween.Jump(f.transform, target, 0.4f, 0.3f, () =>
            {
                if (f == null) return;
                f.transform.localScale = f.RestScale;
                Tween.Squash(f.transform, f.RestScale, 0.25f);
                f.PlaySfx(SfxId.Snap);
                FxManager.Play(FxType.Sparkle, target + Vector3.up * 0.3f, 6);
            });
            RaiseDropped(GameplayEventType.TrayItemAdded, f.CategoryId);

            if (HasMeal && !m_MealAnnounced)
            {
                m_MealAnnounced = true;
                Tween.Delay(this, 0.4f, () =>
                {
                    FxManager.Play(FxType.Stars, transform.position + Vector3.up * 0.8f, 14);
                    m_TrayItem.PlaySfx(SfxId.Celebration, 0.8f);
                    Tween.Squash(transform, m_TrayItem.RestScale, 0.15f, 0.4f);
                });
                RaiseDropped(GameplayEventType.MealReady, "meal");
            }
        }

        // ------------------------------------------------------------------ container
        public void Release(FoodItem item)
        {
            EnsureArrays();
            for (int i = 0; i < m_Contents.Length; i++)
            {
                if (m_Contents[i] != item) continue;
                m_Contents[i] = null;
                if (m_TrayItem != null) m_TrayItem.RemoveContent(item);
            }
            if (!HasMeal) m_MealAnnounced = false;
        }

        public bool HandleTap(FoodItem item) => false;
        public bool AllowPickup(FoodItem item) => true;

        /// <summary>Removes and returns everything on the tray (a customer takes it).</summary>
        public List<FoodItem> TakeAll()
        {
            List<FoodItem> items = GetContents();
            foreach (FoodItem f in items) Release(f);
            foreach (FoodItem f in items) f.AttachTo(null);
            m_MealAnnounced = false;
            return items;
        }

        public void Clear()
        {
            foreach (FoodItem f in TakeAll()) f.Vanish();
        }

        /// <summary>Slides the empty tray back to its spot on the counter.</summary>
        public void ReturnToCounter()
        {
            m_TrayItem.Locked = false;
            Tween.Kill(transform);
            Tween.Jump(transform, m_TrayItem.HomePosition, 0.5f, 0.5f, () =>
            {
                transform.rotation = Quaternion.identity;
                Tween.Squash(transform, m_TrayItem.RestScale, 0.15f);
            });
        }
    }
}
