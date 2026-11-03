using System;
using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Interaction;
using LittleBurgerWorld.Kitchen;
using LittleBurgerWorld.UI;
using UnityEngine;

namespace LittleBurgerWorld.Activities
{
    /// <summary>
    /// Scene-side runner for guided activities: connects the data-driven
    /// <see cref="ActivityStepController"/> to the kitchen, customers and HUD.
    /// </summary>
    public sealed class ActivityManager : MonoBehaviour, IActivityContext
    {
        private const string k_ClosedFilter = "__closed__";

        public static ActivityManager Instance { get; private set; }

        [SerializeField] private KitchenManager m_Kitchen;
        [SerializeField] private CustomerManager m_Customers;
        [SerializeField] private UIManager m_UI;

        private ActivityStepController m_Controller;
        private readonly List<Transform> m_Highlighted = new List<Transform>();

        public ActivityStepController Controller => m_Controller;
        public ActivityData Current => m_Controller?.Activity;
        public bool IsRunning => m_Controller != null && m_Controller.IsRunning;

        public event Action<ActivityData, int> ActivityFinished;

        private void Awake()
        {
            Instance = this;
            m_Controller = new ActivityStepController(this);
            m_Controller.Completed += OnCompleted;
            if (m_Kitchen == null) m_Kitchen = FindFirstObjectByType<KitchenManager>();
            if (m_Customers == null) m_Customers = FindFirstObjectByType<CustomerManager>();
            if (m_UI == null) m_UI = FindFirstObjectByType<UIManager>();
        }

        private void OnDestroy()
        {
            m_Controller?.Stop();
            if (Instance == this) Instance = null;
        }

        private void Update() => m_Controller.Tick(Time.deltaTime);

        public void StartActivity(ActivityData activity)
        {
            if (activity == null) return;
            if (m_Kitchen != null) m_Kitchen.SetGuidedLock(true);
            GameEvents.RaiseActivityStarted(activity.id);
            m_Controller.Start(activity);
        }

        public void StartActivity(string id)
        {
            GameManager gm = GameManager.Instance;
            StartActivity(gm != null ? gm.Config.FindActivity(id) : null);
        }

        public void Replay()
        {
            if (m_Controller.Activity != null) StartActivity(m_Controller.Activity);
        }

        public void StopActivity()
        {
            m_Controller.Stop();
            ClearHighlights();
            if (m_Kitchen != null) m_Kitchen.SetGuidedLock(false);
        }

        private void OnCompleted(ActivityData activity)
        {
            ClearHighlights();
            if (m_Kitchen != null) m_Kitchen.SetGuidedLock(false);
            GameManager gm = GameManager.Instance;
            int stars = 1;
            if (gm != null)
            {
                stars = gm.Rewards.RecordActivityCompleted(activity);
                gm.Save.Save();
                gm.Audio.Play(activity.completionSound);
            }
            GameEvents.RaiseActivityCompleted(activity.id);
            ActivityFinished?.Invoke(activity, stars);
        }

        // ------------------------------------------------------------------ IActivityContext
        public bool IsChildBusy => InteractionManager.Instance != null && InteractionManager.Instance.IsDragging;

        public void ResetKitchen()
        {
            if (m_Kitchen != null) m_Kitchen.ResetWorkSurfaces();
        }

        public void CallCustomer(string characterId, IList<string> request)
        {
            if (m_Customers != null) m_Customers.CallCustomer(characterId, request);
        }

        public void SetBoardFilter(string ingredientId)
        {
            if (m_Kitchen == null) return;
            bool running = m_Controller != null && m_Controller.IsRunning;
            m_Kitchen.SetExpectedIngredient(string.IsNullOrEmpty(ingredientId) ? (running ? k_ClosedFilter : null) : ingredientId);
        }

        public void SetPrompt(Sprite icon, string textKey)
        {
            if (m_UI == null) return;
            string text = null;
            if (icon != null && !string.IsNullOrEmpty(textKey))
            {
                text = Localization.Get(textKey);
                if (text == textKey) text = null; // missing entry: picture only
            }
            m_UI.SetPrompt(icon, text);
        }

        public void ShowHint(ActivityStep step)
        {
            if (m_UI == null || step == null) return;
            Highlight(step.targetKey, true);
            bool drag = !string.IsNullOrEmpty(step.sourceKey) && !string.IsNullOrEmpty(step.targetKey);
            if (drag)
            {
                Highlight(step.sourceKey, true);
                m_UI.Hand.ShowDrag(() => ScreenPositionOf(step.sourceKey), () => ScreenPositionOf(step.targetKey));
            }
            else if (!string.IsNullOrEmpty(step.targetKey))
            {
                m_UI.Hand.ShowTap(() => ScreenPositionOf(step.targetKey));
            }
        }

        public void HideHint()
        {
            if (m_UI != null) m_UI.Hand.Hide();
            ClearHighlights();
        }

        public bool Assist(ActivityStep step)
        {
            if (step == null) return false;
            // Prefer the source (e.g. tray button spawns the ingredient), then the target.
            foreach (string key in new[] { step.sourceKey, step.targetKey })
            {
                if (string.IsNullOrEmpty(key)) continue;
                Transform t = SceneObjectRegistry.Find(key);
                if (t == null) continue;
                foreach (IAssistable a in t.GetComponentsInParent<IAssistable>())
                {
                    if (a.Assist(step.itemId, step.targetKey)) return true;
                }
            }
            return false;
        }

        public void PlayAnimation(string targetKey, string animation)
        {
            Transform t = SceneObjectRegistry.Find(targetKey);
            if (t == null) return;
            var customer = t.GetComponentInParent<AnimalCustomerController>();
            if (customer != null && customer.Rig != null)
            {
                CharacterPose pose = animation == "wave" ? CharacterPose.Wave :
                    animation == "cheer" || animation == "celebrate" ? CharacterPose.Celebrate : CharacterPose.Happy;
                customer.Rig.SetPose(pose);
                Tween.Delay(customer, 1.5f, () =>
                {
                    if (customer != null && customer.State == CustomerState.Waiting) customer.Rig.SetPose(CharacterPose.Idle);
                });
                return;
            }
            Tween.Squash(t, t.localScale, 0.3f, 0.5f);
            FxManager.Play(FxType.Sparkle, t.position + Vector3.up * 0.5f, 10);
        }

        public void PlaySound(ActivityStep step)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || step == null) return;
            if (!string.IsNullOrEmpty(step.voiceKey)) gm.Audio.PlayVoice(step.voiceKey);
            if (step.sound != SfxId.None) gm.Audio.Play(step.sound);
        }

        public void ShowObject(string targetKey)
        {
            Transform t = SceneObjectRegistry.Find(targetKey);
            if (t == null) return;
            Vector3 rest = t.localScale;
            t.localScale = rest * 0.6f;
            Tween.Scale(t, rest, 0.4f, Ease.OutBack);
            FxManager.Play(FxType.Poof, t.position + Vector3.up * 0.3f, 8);
        }

        public void Highlight(string targetKey, bool on)
        {
            Transform t = SceneObjectRegistry.Find(targetKey);
            if (t == null) return;
            if (on)
            {
                HintPulse.Begin(t, t is RectTransform ? 0f : 0.9f);
                if (!m_Highlighted.Contains(t)) m_Highlighted.Add(t);
            }
            else
            {
                HintPulse.End(t);
                m_Highlighted.Remove(t);
            }
        }

        private void ClearHighlights()
        {
            foreach (Transform t in m_Highlighted) HintPulse.End(t);
            m_Highlighted.Clear();
        }

        public void StepSucceeded(ActivityStep step)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play(SfxId.Star, 0.7f);
            if (m_UI != null) m_UI.FlashStepSuccess();
        }

        /// <summary>Screen position of a keyed object (UI or world) for the hint hand.</summary>
        public Vector2 ScreenPositionOf(string key)
        {
            Transform t = SceneObjectRegistry.Find(key);
            if (t == null) return new Vector2(-9999f, -9999f);
            if (t is RectTransform rt) return RectTransformUtility.WorldToScreenPoint(null, rt.position);
            Vector3 world = t.position;
            var hint = t.GetComponentInParent<IHintTarget>();
            var drop = t.GetComponentInParent<IDropTarget>();
            if (hint != null && (hint as Component)?.transform == t) world = hint.HintPosition;
            else if (drop != null && (drop as Component)?.transform == t) world = drop.SnapPoint;
            else if (hint != null && t.GetComponent<Collider>() == null) world = hint.HintPosition;
            Camera cam = InteractionManager.Instance != null ? InteractionManager.Instance.Camera : Camera.main;
            return cam != null ? (Vector2)cam.WorldToScreenPoint(world) : Vector2.zero;
        }
    }
}
