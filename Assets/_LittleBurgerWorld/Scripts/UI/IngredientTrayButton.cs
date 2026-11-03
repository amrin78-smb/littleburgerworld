using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.InputHandling;
using LittleBurgerWorld.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// One big picture button in the bottom ingredient tray. An endless supply:
    /// - drag (or touch-and-hold) to pull a fresh ingredient out under your finger;
    /// - tap to make the ingredient fly to where it belongs (board, or grill for raw patties).
    /// </summary>
    public sealed class IngredientTrayButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IAssistable
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private Image m_Background;
        [SerializeField] private float m_HoldToDrag = 0.22f;

        private IngredientData m_Data;
        private bool m_Down;
        private bool m_Spawned;
        private float m_DownTime;
        private Vector3 m_Rest = Vector3.one;

        public IngredientData Data => m_Data;

        public void Setup(IngredientData data)
        {
            m_Data = data;
            if (m_Icon != null)
            {
                m_Icon.sprite = data.icon;
                m_Icon.preserveAspect = true;
            }
            if (m_Background != null)
            {
                Color c = Color.Lerp(data.color, Color.white, 0.65f);
                m_Background.color = c;
            }
            name = "TrayButton_" + data.id;
            var key = GetComponent<SceneKey>();
            if (key == null) key = gameObject.AddComponent<SceneKey>();
            key.SetKey("Tray." + data.id);
            m_Rest = transform.localScale;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (m_Data == null) return;
            m_Down = true;
            m_Spawned = false;
            m_DownTime = Time.unscaledTime;
            Tween.Kill(transform);
            Tween.Scale(transform, m_Rest * 0.9f, 0.08f, Ease.OutQuad);
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(SfxId.Pop, 0.7f);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (m_Down && !m_Spawned) SpawnAndDrag(e.position);
        }

        public void OnDrag(PointerEventData e) { }
        public void OnEndDrag(PointerEventData e) { }

        public void OnPointerUp(PointerEventData e)
        {
            Tween.Kill(transform);
            Tween.Scale(transform, m_Rest, 0.35f, Ease.OutElastic);
            if (m_Down && !m_Spawned) Tap();
            m_Down = false;
            m_Spawned = false;
        }

        private void Update()
        {
            if (!m_Down || m_Spawned) return;
            if (Time.unscaledTime - m_DownTime >= m_HoldToDrag)
            {
                Vector2 pos = InputManager.Instance != null ? InputManager.Instance.PointerPosition : (Vector2)transform.position;
                SpawnAndDrag(pos);
            }
        }

        private FoodItem Spawn(Vector2 screenPos)
        {
            InteractionManager im = InteractionManager.Instance;
            if (im == null || m_Data == null || m_Data.prefab == null) return null;
            FoodItem probe = m_Data.prefab.GetComponent<FoodItem>();
            float h = probe != null ? probe.DragHeight : 2f;
            Vector3 world = im.ScreenToDragPlane(screenPos + new Vector2(0f, Screen.height * 0.06f), h);
            FoodItem item = FoodManager.SpawnIngredient(m_Data, world, true);
            if (item == null) return null;
            Vector3 rest = item.RestScale;
            item.transform.localScale = rest * 0.3f;
            Tween.Scale(item.transform, rest * 1.12f, 0.2f, Ease.OutBack);
            return item;
        }

        private void SpawnAndDrag(Vector2 screenPos)
        {
            FoodItem item = Spawn(screenPos);
            m_Spawned = true;
            if (item == null) return;
            InteractionManager.Instance.BeginExternalDrag(item, screenPos);
        }

        private void Tap()
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, transform.position);
            FoodItem item = Spawn(screen);
            if (item == null) return;
            if (FoodItem.AutoSendHandler == null || !FoodItem.AutoSendHandler(item))
            {
                // Nowhere to go right now: the ingredient pops back into the tray.
                item.Vanish(0.15f);
                Wiggle();
            }
        }

        private void Wiggle()
        {
            Tween.Value(transform, 0.4f, k =>
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 4f) * 10f * (1f - k));
            }, Ease.Linear, () => { if (this != null) transform.localRotation = Quaternion.identity; });
        }

        /// <summary>Guided assist: send one of these ingredients to the target (board or grill).</summary>
        public bool Assist(string itemId, string targetKey)
        {
            if (m_Data == null) return false;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, transform.position);
            FoodItem item = Spawn(screen);
            if (item == null) return false;
            IDropTarget target = !string.IsNullOrEmpty(targetKey) ? SceneObjectRegistry.Find<IDropTarget>(targetKey) : null;
            bool sent = target != null ? item.SendTo(target) : FoodItem.AutoSendHandler != null && FoodItem.AutoSendHandler(item);
            if (!sent) item.Vanish();
            else FxManager.Play(FxType.Sparkle, item.transform.position, 6);
            return sent;
        }
    }
}
