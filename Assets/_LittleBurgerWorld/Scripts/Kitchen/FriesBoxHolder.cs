using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>
    /// Holds an empty paper fries box. Drop a fries batch on it and the box fills up stick by stick,
    /// celebrates, and becomes a draggable portion of fries. A new empty box appears when it is taken.
    /// </summary>
    public sealed class FriesBoxHolder : DropZone, IFoodContainer, IAssistable, IHintTarget
    {
        [SerializeField] private GameObject m_BoxPrefab;
        [SerializeField] private float m_FillSeconds = 0.7f;

        private FoodItem m_Box;
        private bool m_Filled;
        private bool m_Filling;

        public FoodItem CurrentBox => m_Box;
        public Vector3 HintPosition => m_Box != null ? m_Box.transform.position + Vector3.up * 0.5f : SnapPoint;
        public bool IsFilled => m_Filled;

        private void Start()
        {
            if (m_Box == null) SpawnBox(false);
        }

        private void SpawnBox(bool animate)
        {
            if (m_BoxPrefab == null) return;
            GameObject go = Instantiate(m_BoxPrefab, SnapPoint, Quaternion.identity, transform);
            m_Box = go.GetComponent<FoodItem>();
            if (m_Box == null) m_Box = go.AddComponent<FoodItem>();
            m_Box.Configure(FoodType.Fries, 0.6f);
            m_Box.AttachTo(this);
            m_Box.Locked = true;
            m_Filled = false;
            m_Filling = false;
            SetFillVisible(m_Box, 0);
            if (animate)
            {
                Vector3 rest = m_Box.RestScale;
                go.transform.localScale = Vector3.zero;
                Tween.Scale(go.transform, rest, 0.35f, Ease.OutBack);
            }
        }

        private static List<Transform> Sticks(FoodItem box)
        {
            var list = new List<Transform>();
            Transform fill = box.transform.Find("Fill");
            if (fill == null) return list;
            foreach (Transform c in fill) list.Add(c);
            return list;
        }

        private static void SetFillVisible(FoodItem box, int count)
        {
            List<Transform> sticks = Sticks(box);
            for (int i = 0; i < sticks.Count; i++) sticks[i].gameObject.SetActive(i < count);
        }

        public override bool CanAccept(IDraggable item)
        {
            return item is FoodItem f && f.Type == FoodType.FriesBatch && !f.IsConsumed &&
                   m_Box != null && !m_Filled && !m_Filling;
        }

        public override void Accept(IDraggable item, Vector3 worldPoint)
        {
            if (!(item is FoodItem batch)) return;
            m_Filling = true;
            batch.AttachTo(null);
            batch.Locked = true;
            batch.SetContained(true); // can't be grabbed while it pours into the box
            Tween.Kill(batch.transform);
            Vector3 top = SnapPoint + Vector3.up * 0.9f;
            Tween.Jump(batch.transform, top, 0.4f, 0.3f, () =>
            {
                if (batch != null) batch.Consume();
                FillBox();
            });
            RaiseDropped(GameplayEventType.ItemDropped, "fries_batch");
        }

        private void FillBox()
        {
            if (m_Box == null) { m_Filling = false; return; }
            FoodItem box = m_Box;
            List<Transform> sticks = Sticks(box);
            for (int i = 0; i < sticks.Count; i++)
            {
                Transform s = sticks[i];
                Vector3 rest = s.localScale;
                s.gameObject.SetActive(true);
                s.localScale = Vector3.zero;
                Tween.Scale(s, rest, 0.18f, Ease.OutBack, null, i * (m_FillSeconds / Mathf.Max(1, sticks.Count)));
            }
            Tween.Delay(this, m_FillSeconds + 0.1f, () =>
            {
                m_Filling = false;
                m_Filled = true;
                if (box == null) return;
                box.Locked = false;
                Tween.Squash(box.transform, box.RestScale, 0.25f, 0.4f);
                FxManager.Play(FxType.Sparkle, box.transform.position + Vector3.up * 0.8f, 16);
                FxManager.Play(FxType.Confetti, box.transform.position + Vector3.up * 0.6f, 18);
                box.PlaySfx(SfxId.Celebration, 0.7f);
                GameEvents.Raise(new GameplayEvent(GameplayEventType.FriesBoxFilled, "fries", SceneObjectRegistry.KeyOf(this), this));
            });
            box.PlaySfx(SfxId.FriesReady, 0.7f);
        }

        public void Release(FoodItem item)
        {
            if (item != m_Box) return;
            m_Box = null;
            m_Filled = false;
            Tween.Delay(this, 0.6f, () => { if (m_Box == null) SpawnBox(true); });
        }

        public bool HandleTap(FoodItem item)
        {
            if (item == m_Box && !m_Filled)
            {
                item.Wiggle();
                return true;
            }
            return false;
        }

        public bool AllowPickup(FoodItem item) => item != m_Box || m_Filled;

        public bool TakeBack(FoodItem item)
        {
            // Only while the spot is still empty (a fresh box appears 0.6 s after the full one is taken).
            if (m_Box != null || item == null || item.Type != FoodType.Fries) return false;
            m_Box = item;
            m_Filled = true;
            m_Filling = false;
            item.AttachTo(this);
            item.transform.SetParent(transform, true);
            return true;
        }

        public bool Assist(string itemId, string targetKey)
        {
            if (m_Box != null && m_Filled) return m_Box.SendToKeyOrAuto(targetKey);
            FryStation fry = FindFirstObjectByType<FryStation>();
            if (fry != null) return fry.Assist(itemId, targetKey);
            return false;
        }

        public void Clear()
        {
            Tween.Kill(this);
            if (m_Box != null)
            {
                FoodItem old = m_Box;
                m_Box = null;
                old.AttachTo(null);
                old.Vanish();
            }
            Tween.Delay(this, 0.5f, () => { if (m_Box == null) SpawnBox(true); });
        }
    }
}
