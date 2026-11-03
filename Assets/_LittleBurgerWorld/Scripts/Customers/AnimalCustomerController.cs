using System;
using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Effects;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Customers
{
    /// <summary>
    /// An animal customer: walks in, shows a picture request, waits happily for as long as needed,
    /// accepts ANY food with delight, eats it (chomp chomp), celebrates and waves goodbye.
    /// Reusable for every animal – appearance comes from the prefab, settings from <see cref="CharacterData"/>.
    /// </summary>
    public sealed class AnimalCustomerController : DropZone, IInteractable, IFoodContainer, IAssistable
    {
        [SerializeField] private CharacterData m_Data;
        [SerializeField] private ProceduralCharacterRig m_Rig;
        [SerializeField] private RequestBubble m_Bubble;

        private readonly CustomerStateMachine m_Fsm = new CustomerStateMachine();
        private readonly List<Vector3> m_Path = new List<Vector3>();
        private readonly List<FoodItem> m_Holding = new List<FoodItem>();
        private readonly List<string> m_Request = new List<string>();
        private List<Vector3> m_ExitPath = new List<Vector3>();
        private int m_PathIndex;
        private Vector3 m_WaitFacing = new Vector3(-1f, 0f, -1f);
        private Action m_OnPathDone;
        private float m_ShuffleTimer;

        public event Action<AnimalCustomerController> Left;
        public event Action<AnimalCustomerController> Arrived;

        public CharacterData Data => m_Data;
        public CustomerState State => m_Fsm.State;
        public CustomerStateMachine StateMachine => m_Fsm;
        public IReadOnlyList<string> Request => m_Request;
        public ProceduralCharacterRig Rig => m_Rig;
        public RequestBubble Bubble => m_Bubble;
        /// <summary>Speeds up eating/celebrating (tests).</summary>
        public float TimeScale { get; set; } = 1f;

        public override Vector3 SnapPoint => m_Rig != null && m_Rig.holdPoint != null ? m_Rig.holdPoint.position : transform.position + Vector3.up;

        protected override Vector3 RingPosition => transform.position + Vector3.up * 0.03f;

        public void Setup(CharacterData data, IList<Vector3> enterPath, IList<Vector3> exitPath, Vector3 waitFacing)
        {
            if (data != null) m_Data = data;
            m_Path.Clear();
            if (enterPath != null) m_Path.AddRange(enterPath);
            m_ExitPath = exitPath != null ? new List<Vector3>(exitPath) : new List<Vector3>();
            m_WaitFacing = waitFacing.sqrMagnitude > 0.001f ? waitFacing : m_WaitFacing;
            m_DropPriority = 20;
            m_AcceptRadius = 1.6f;
            m_RingRadius = 1.1f;
        }

        // ------------------------------------------------------------------ flow
        public void Enter()
        {
            if (!m_Fsm.TryTransition(CustomerState.Entering)) return;
            Pose(CharacterPose.Walk);
            PlaySfx(SfxId.DoorChime, 0.7f);
            FollowPath(m_Path, () =>
            {
                if (!m_Fsm.TryTransition(CustomerState.Waiting)) return;
                Pose(CharacterPose.Idle);
                FaceSmoothly(m_WaitFacing);
                ShowRequest();
                PlaySfx(SfxId.Happy, 0.6f);
                Arrived?.Invoke(this);
                GameEvents.Raise(new GameplayEvent(GameplayEventType.CustomerArrived, null, SceneObjectRegistry.KeyOf(this), this,
                    m_Data != null ? m_Data.id : null));
            });
        }

        /// <summary>Places the customer at the counter immediately (scene setup / tests).</summary>
        public void ArriveInstantly()
        {
            if (m_Fsm.State == CustomerState.Idle) m_Fsm.TryTransition(CustomerState.Entering);
            if (m_Path.Count > 0) transform.position = m_Path[m_Path.Count - 1];
            m_OnPathDone = null;
            m_Fsm.TryTransition(CustomerState.Waiting);
            Pose(CharacterPose.Idle);
            transform.rotation = Quaternion.LookRotation(m_WaitFacing.normalized, Vector3.up);
            ShowRequest();
            Arrived?.Invoke(this);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.CustomerArrived, null, SceneObjectRegistry.KeyOf(this), this,
                m_Data != null ? m_Data.id : null));
        }

        public void SetRequest(IEnumerable<string> items)
        {
            m_Request.Clear();
            if (items != null) m_Request.AddRange(items);
            if (m_Fsm.State == CustomerState.Waiting) ShowRequest();
        }

        private void ShowRequest()
        {
            if (m_Bubble == null) return;
            GameConfig cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            var icons = new List<Sprite>();
            if (cfg != null)
            {
                foreach (string id in m_Request)
                {
                    Sprite s = cfg.FindIcon(id);
                    if (s != null) icons.Add(s);
                }
                if (icons.Count == 0 && cfg.heartIcon != null) icons.Add(cfg.heartIcon);
            }
            m_Bubble.Show(icons);
        }

        public void Leave()
        {
            if (m_Fsm.State == CustomerState.Leaving || m_Fsm.State == CustomerState.Idle) return;
            if (!m_Fsm.TryTransition(CustomerState.Leaving))
            {
                // Mid-meal: finish gracefully by forcing the remaining steps.
                m_Fsm.ForceReset();
                m_Fsm.TryTransition(CustomerState.Entering);
                m_Fsm.TryTransition(CustomerState.Leaving);
            }
            if (m_Bubble != null) m_Bubble.Hide();
            foreach (FoodItem f in m_Holding.ToArray()) if (f != null) f.Consume();
            m_Holding.Clear();
            Pose(CharacterPose.Wave);
            Tween.Delay(this, 0.9f / TimeScale, () =>
            {
                Pose(CharacterPose.Walk);
                FollowPath(m_ExitPath, () =>
                {
                    m_Fsm.TryTransition(CustomerState.Idle);
                    GameEvents.Raise(new GameplayEvent(GameplayEventType.CustomerLeft, null, SceneObjectRegistry.KeyOf(this), this,
                        m_Data != null ? m_Data.id : null));
                    Left?.Invoke(this);
                    Destroy(gameObject);
                });
            });
        }

        private void FollowPath(List<Vector3> path, Action done)
        {
            var copy = new List<Vector3>(path); // path may be m_Path itself
            m_Path.Clear();
            m_Path.AddRange(copy);
            m_PathIndex = 0;
            m_OnPathDone = done;
            if (m_Path.Count == 0)
            {
                Action d = m_OnPathDone;
                m_OnPathDone = null;
                d?.Invoke();
            }
        }

        private void Update()
        {
            if (m_OnPathDone == null) return;
            if (m_PathIndex >= m_Path.Count)
            {
                Action d = m_OnPathDone;
                m_OnPathDone = null;
                d?.Invoke();
                return;
            }
            Vector3 target = m_Path[m_PathIndex];
            target.y = transform.position.y;
            float speed = (m_Data != null ? m_Data.walkSpeed : 1.6f) * TimeScale;
            Vector3 to = target - transform.position;
            if (to.sqrMagnitude > 0.0004f)
            {
                Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-10f * Time.deltaTime));
            }
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if ((transform.position - target).sqrMagnitude < 0.0004f) m_PathIndex++;
        }

        private void FaceSmoothly(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            Tween.RotateLocal(transform, Quaternion.LookRotation(dir.normalized, Vector3.up), 0.4f);
        }

        // ------------------------------------------------------------------ receiving food
        public override bool CanAccept(IDraggable item)
        {
            if (!m_Fsm.CanReceiveFood || !(item is FoodItem f) || f.IsConsumed) return false;
            if (f.IsRawIngredient) return false;
            if (f.Type == FoodType.Tray) return f.GetComponent<ServingTray>() is ServingTray t && t.ItemCount > 0;
            if (f.Type == FoodType.Fries && f.Locked) return false; // empty box
            return true;
        }

        public override void Accept(IDraggable item, Vector3 worldPoint)
        {
            if (!(item is FoodItem f)) return;
            var foods = new List<FoodItem>();
            ServingTray tray = f.Type == FoodType.Tray ? f.GetComponent<ServingTray>() : null;
            if (tray != null)
            {
                foods.AddRange(tray.TakeAll());
                f.Locked = true;
                Tween.Delay(this, 0.5f, () => { if (tray != null) tray.ReturnToCounter(); });
            }
            else
            {
                foods.Add(f);
            }
            Receive(foods, tray != null ? "meal" : f.CategoryId);
        }

        /// <summary>Takes the food with joy. Every food is the right food.</summary>
        public void Receive(List<FoodItem> foods, string servedId)
        {
            if (!m_Fsm.TryTransition(CustomerState.ReceivingFood)) return;
            if (m_Bubble != null) m_Bubble.Hide();
            m_Holding.Clear();
            Transform hold = m_Rig != null && m_Rig.holdPoint != null ? m_Rig.holdPoint : transform;
            for (int i = 0; i < foods.Count; i++)
            {
                FoodItem food = foods[i];
                if (food == null) continue;
                food.AttachTo(this);
                food.Locked = true;
                food.transform.SetParent(hold, true);
                m_Holding.Add(food);
                Vector3 local = new Vector3((i - (foods.Count - 1) * 0.5f) * 0.55f, 0f, 0f);
                Tween.Kill(food.transform);
                Vector3 targetWorld = hold.TransformPoint(local);
                Tween.Jump(food.transform, targetWorld, 0.5f, 0.35f);
            }

            Pose(CharacterPose.Happy);
            PlaySfx(SfxId.ThankYou);
            PlaySfx(SfxId.Happy, 0.8f);
            FxManager.Play(FxType.Hearts, transform.position + Vector3.up * 2.2f, 8);

            GameEvents.Raise(new GameplayEvent(GameplayEventType.CustomerServed, servedId, SceneObjectRegistry.KeyOf(this), this,
                m_Data != null ? m_Data.id : null, foods.Count));

            Tween.Delay(this, 1.1f / TimeScale, StartEating);
        }

        private void StartEating()
        {
            if (!m_Fsm.TryTransition(CustomerState.Eating)) return;
            Pose(CharacterPose.Eat);
            int bites = 3;
            float biteTime = 0.45f / TimeScale;
            for (int b = 1; b <= bites; b++)
            {
                int bite = b;
                Tween.Delay(this, biteTime * b, () =>
                {
                    PlaySfx(SfxId.Munch);
                    Vector3 mouthPos = m_Rig != null && m_Rig.mouth != null ? m_Rig.mouth.position : transform.position + Vector3.up * 1.6f;
                    FxManager.Play(FxType.Crumbs, mouthPos, 6);
                    float k = 1f - bite / (float)bites;
                    foreach (FoodItem f in m_Holding)
                    {
                        if (f == null) continue;
                        Tween.Scale(f.transform, f.RestScale * Mathf.Max(0.001f, k), 0.15f, Ease.OutQuad);
                    }
                });
            }
            Tween.Delay(this, biteTime * (bites + 1), () =>
            {
                foreach (FoodItem f in m_Holding.ToArray()) if (f != null) f.Consume();
                m_Holding.Clear();
                Celebrate();
            });
        }

        private void Celebrate()
        {
            if (!m_Fsm.TryTransition(CustomerState.Celebrating)) return;
            Pose(CharacterPose.Celebrate);
            PlaySfx(SfxId.Celebration, 0.8f);
            PlaySfx(SfxId.Happy);
            Vector3 top = transform.position + Vector3.up * 2.4f;
            FxManager.Play(FxType.Confetti, top, 40);
            FxManager.Play(FxType.Hearts, top, 10);
            FxManager.Play(FxType.Stars, top, 10);
            GameEvents.Raise(new GameplayEvent(GameplayEventType.CustomerFinishedEating, null, SceneObjectRegistry.KeyOf(this), this,
                m_Data != null ? m_Data.id : null));
            Tween.Delay(this, 1.8f / TimeScale, Leave);
        }

        // ------------------------------------------------------------------ IInteractable (tap = giggle)
        public int InteractionPriority => 4;
        public bool IsInteractable => isActiveAndEnabled;
        public bool CanDrag => false;
        public void OnSelect() { }
        public void OnDeselect() { }
        public IDraggable BeginDrag() => null;

        public void OnTap()
        {
            if (m_Fsm.State != CustomerState.Waiting) return;
            Pose(CharacterPose.Happy);
            PlaySfx(SfxId.Happy, 0.7f);
            FxManager.Play(FxType.Hearts, transform.position + Vector3.up * 2.2f, 3);
            Tween.Kill(this);
            Tween.Delay(this, 1.1f, () => { if (m_Fsm.State == CustomerState.Waiting) Pose(CharacterPose.Idle); });
        }

        // ------------------------------------------------------------------ container (held food)
        public void Release(FoodItem item) => m_Holding.Remove(item);
        public bool HandleTap(FoodItem item) { OnTap(); return true; }
        public bool AllowPickup(FoodItem item) => false;

        /// <summary>Assist: serve whatever finished food is around (tray first).</summary>
        public bool Assist(string itemId, string targetKey)
        {
            if (!m_Fsm.CanReceiveFood) return false;
            FoodItem best = null;
            foreach (FoodItem f in FoodItem.All)
            {
                if (f == null || f.IsConsumed || !CanAccept(f) || !f.CanDrag) continue;
                if (f.Type == FoodType.Ingredient || f.Type == FoodType.FriesBatch) continue;
                if (!string.IsNullOrEmpty(itemId) && f.CategoryId != itemId && f.ItemId != itemId && f.Type != FoodType.Tray) continue;
                if (best == null || f.Type == FoodType.Tray) best = f;
            }
            return best != null && best.SendTo(this);
        }

        private void Pose(CharacterPose p)
        {
            if (m_Rig != null) m_Rig.SetPose(p);
        }

        private void PlaySfx(SfxId id, float volume = 1f)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            gm.Audio.Play(id, volume, m_Data != null ? m_Data.voicePitch : 1f);
        }
    }
}
