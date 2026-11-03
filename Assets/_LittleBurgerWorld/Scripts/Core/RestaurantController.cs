using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Kitchen;
using LittleBurgerWorld.UI;
using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>
    /// Composition root of the Restaurant scene: applies the selected mode and wires HUD buttons
    /// to the kitchen, customers and activity engine.
    /// </summary>
    public sealed class RestaurantController : MonoBehaviour
    {
        [SerializeField] private KitchenManager m_Kitchen;
        [SerializeField] private CustomerManager m_Customers;
        [SerializeField] private ActivityManager m_Activities;
        [SerializeField] private UIManager m_UI;

        public GameMode Mode { get; private set; }

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            Mode = gm != null ? gm.Modes.CurrentMode : GameMode.FreePlay;
            if (gm != null) gm.Audio.PlayRestaurantMusic();

            m_UI.SetMode(Mode);
            m_UI.Home.OnHoldComplete.AddListener(GoHome);
            m_UI.ResetKitchen.OnHoldComplete.AddListener(() => m_Kitchen.ResetWorkSurfaces());
            m_UI.Replay.OnClick.AddListener(() => m_Activities.Replay());
            m_UI.ActivityList.OnClick.AddListener(BackToActivities);
            m_UI.ActivitySelect.Selected += OnActivitySelected;
            m_Activities.ActivityFinished += OnActivityFinished;

            if (Mode == GameMode.FreePlay) EnterFreePlay();
            else EnterGuided(gm != null ? gm.Modes.PendingActivityId : null);
        }

        private void OnDestroy()
        {
            if (m_Activities != null) m_Activities.ActivityFinished -= OnActivityFinished;
            if (m_UI != null && m_UI.ActivitySelect != null) m_UI.ActivitySelect.Selected -= OnActivitySelected;
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.Audio.StopAllLoops();
        }

        public void EnterFreePlay()
        {
            Mode = GameMode.FreePlay;
            m_Activities.StopActivity();
            m_Kitchen.SetGuidedLock(false);
            m_Customers.AutoSpawn = true;
            m_UI.HideActivitySelect();
        }

        public void EnterGuided(string activityId)
        {
            Mode = GameMode.Guided;
            m_Customers.AutoSpawn = false;
            if (!string.IsNullOrEmpty(activityId)) OnActivitySelected(GameManager.Instance.Config.FindActivity(activityId));
            else m_UI.ShowActivitySelect();
        }

        private void OnActivitySelected(ActivityData activity)
        {
            if (activity == null) return;
            m_UI.HideActivitySelect();
            m_UI.SetActivityControls(true);
            m_Activities.StartActivity(activity);
        }

        private void OnActivityFinished(ActivityData activity, int stars)
        {
            m_UI.SetActivityControls(false);
            GameManager gm = GameManager.Instance;
            Sprite portrait = null;
            if (gm != null && gm.Config.characters.Count > 0 && gm.Config.characters[0] != null)
                portrait = gm.Config.characters[0].portrait;
            m_UI.ShowCelebration(stars, portrait, () =>
            {
                if (Mode == GameMode.Guided) m_UI.ShowActivitySelect();
            });
        }

        private void BackToActivities()
        {
            m_Activities.StopActivity();
            m_UI.ShowActivitySelect();
        }

        public void GoHome()
        {
            m_Activities.StopActivity();
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.Audio.StopAllLoops();
                gm.Save.Save();
                gm.LoadMainMenu();
            }
        }
    }
}
