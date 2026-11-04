using System.Collections;
using System.Collections.Generic;
using System.IO;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.InputHandling;
using LittleBurgerWorld.Interaction;
using LittleBurgerWorld.Kitchen;
using LittleBurgerWorld.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LittleBurgerWorld.Tests
{
    /// <summary>End-to-end gameplay in the real Restaurant scene (no mocks).</summary>
    public class GameplayTests
    {
        private readonly List<GameplayEvent> m_Events = new List<GameplayEvent>();
        private string m_SavePath;

        private GameManager GM => GameManager.Instance;
        private GameConfig Cfg => GM.Config;

        private IEnumerator LoadRestaurant(GameMode mode)
        {
            m_SavePath = Path.Combine(Application.temporaryCachePath, "lbw_test_" + System.Guid.NewGuid().ToString("N") + ".json");
            GameManager.Create().UseSaveFile(m_SavePath);
            GM.Modes.SetMode(mode);
            SceneManager.LoadScene("Restaurant");
            yield return null;
            yield return null;
            InputManager.Instance.PollDevices = false;
            Time.timeScale = 3f;
            m_Events.Clear();
            GameEvents.Gameplay += Record;
            yield return null;
        }

        private void Record(GameplayEvent e) => m_Events.Add(e);

        [TearDown]
        public void TearDown()
        {
            GameEvents.Gameplay -= Record;
            Time.timeScale = 1f;
            if (m_SavePath != null && File.Exists(m_SavePath)) File.Delete(m_SavePath);
        }

        private static IEnumerator Until(System.Func<bool> cond, float timeout = 10f)
        {
            float t = 0f;
            while (!cond() && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(cond(), "Timed out waiting for condition");
        }

        private FoodItem Spawn(string id) => FoodManager.SpawnIngredient(id, new Vector3(-1f, 2f, -1f), true);

        private bool Has(GameplayEventType t, string item = null) => m_Events.Exists(e => e.Type == t && (item == null || e.ItemId == item || e.ExtraId == item));

        [UnityTest]
        public IEnumerator FreePlay_BuildsCheeseburger_FromTappedIngredients()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            BurgerAssemblyController board = KitchenManager.Instance.Board;
            foreach (string id in new[] { "bun_bottom", "patty", "cheese", "bun_top" })
            {
                Assert.IsTrue(KitchenManager.Instance.AutoSend(Spawn(id)), "auto-send " + id);
                yield return new WaitForSeconds(0.2f);
            }
            Assert.IsTrue(Has(GameplayEventType.IngredientStacked, "cheese"));
            yield return Until(() => board.FinishedBurger != null);
            Assert.AreEqual("cheeseburger", board.FinishedBurger.ItemId, "Recipe matched from data");
            Assert.IsTrue(Has(GameplayEventType.BurgerCompleted, "cheeseburger"));
            Assert.AreEqual(4, board.FinishedBurger.Contents.Count);
        }

        [UnityTest]
        public IEnumerator DragAndDrop_WithPointerInput_SnapsOntoBoard()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            var im = InteractionManager.Instance;
            var input = InputManager.Instance;
            BurgerAssemblyController board = KitchenManager.Instance.Board;
            Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height * 0.1f);
            FoodItem bun = Spawn("bun_bottom");
            input.Inject(PointerPhase.Down, start, true);
            im.BeginExternalDrag(bun, start);
            Assert.IsTrue(im.IsDragging);
            Vector2 target = im.WorldToScreen(board.SnapPoint) - new Vector2(0f, Screen.height * 0.06f);
            for (int i = 1; i <= 10; i++)
            {
                input.Inject(PointerPhase.Move, Vector2.Lerp(start, target, i / 10f));
                yield return null;
            }
            Assert.AreSame(board, im.CurrentHover, "Board highlighted as drop target");
            input.Inject(PointerPhase.Up, target);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, board.Model.Count);
            Assert.AreSame(board, bun.Container);
            Assert.IsTrue(Has(GameplayEventType.ItemDropped, "bun_bottom"));
        }

        [UnityTest]
        public IEnumerator CancelledTouch_ReturnsItemSafely()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            var im = InteractionManager.Instance;
            var input = InputManager.Instance;
            FoodItem cheese = Spawn("cheese");
            Vector2 p = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            input.Inject(PointerPhase.Down, p, true);
            im.BeginExternalDrag(cheese, p);
            input.Inject(PointerPhase.Move, p + new Vector2(80f, 0f));
            input.Inject(PointerPhase.Cancel, p);
            Assert.IsFalse(im.IsDragging);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(cheese == null || cheese.IsConsumed, "Supply item dropped nowhere goes back to the tray");
        }

        [UnityTest]
        public IEnumerator Picking_SelectsTopmostIngredient_AndIgnoresUiPresses()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            BurgerAssemblyController board = KitchenManager.Instance.Board;
            KitchenManager.Instance.AutoSend(Spawn("bun_bottom"));
            KitchenManager.Instance.AutoSend(Spawn("tomato"));
            yield return new WaitForSeconds(0.5f);
            var im = InteractionManager.Instance;
            IInteractable picked = im.Pick(im.WorldToScreen(board.Items[1].transform.position + Vector3.up * 0.05f));
            Assert.AreSame(board.Items[1], picked, "Top-most (closest to camera) ingredient wins");

            int before = m_Events.Count;
            InputManager.Instance.Inject(PointerPhase.Down, im.WorldToScreen(board.Items[1].transform.position), true);
            InputManager.Instance.Inject(PointerPhase.Up, im.WorldToScreen(board.Items[1].transform.position));
            Assert.IsFalse(m_Events.Exists(e => e.Type == GameplayEventType.ObjectTapped), "A press that began on UI never reaches the world");
        }

        [UnityTest]
        public IEnumerator Grill_CooksFlipsAndServesCookedPatty()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            GrillStation grill = KitchenManager.Instance.Grill;
            grill.SideSeconds = 0.3f;
            FoodItem patty = Spawn("patty_raw");
            Assert.IsTrue(KitchenManager.Instance.AutoSend(patty), "Raw patty goes to the grill");
            Assert.IsFalse(patty.CanDrag, "Cooking patty stays put");
            yield return Until(grill.HasWaitingForFlip);
            patty.OnTap(); // tap to flip
            Assert.IsTrue(Has(GameplayEventType.PattyFlipped));
            yield return Until(() => Has(GameplayEventType.PattyCooked));
            Assert.AreEqual("patty", patty.ItemId);
            Assert.IsTrue(patty.CanDrag);
            KitchenManager.Instance.AutoSend(Spawn("bun_bottom"));
            yield return null;
            patty.OnTap(); // cooked patty flies to the bun
            yield return new WaitForSeconds(0.2f);
            Assert.AreSame(KitchenManager.Instance.Board, patty.Container);
        }

        [UnityTest]
        public IEnumerator FriesAndMilkshake_Stations_Work()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            KitchenManager k = KitchenManager.Instance;
            k.Fryer.FrySeconds = 0.3f;
            Assert.IsTrue(k.Fryer.StartFrying());
            yield return Until(() => k.Fryer.WaitingBatch != null);
            yield return Until(() => k.FriesBox.CurrentBox != null);
            Assert.IsTrue(k.AutoSend(k.Fryer.WaitingBatch));
            yield return Until(() => Has(GameplayEventType.FriesBoxFilled));

            k.Drinks.FillSeconds = 0.3f;
            k.Drinks.SelectFlavour(1);
            k.Drinks.Assist(null, null); // cup
            yield return new WaitForSeconds(0.3f);
            k.Drinks.Assist(null, null); // fill
            yield return Until(() => k.Drinks.State == DrinkStation.CupState.Filled);
            k.Drinks.Assist(null, null); // cream
            k.Drinks.Assist(null, null); // straw
            Assert.IsTrue(Has(GameplayEventType.MilkshakeCompleted, "chocolate"));
            Assert.IsTrue(k.Drinks.Cup.CanDrag);
        }

        [UnityTest]
        public IEnumerator Customer_ReceivesEatsCelebratesAndLeaves()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            CustomerManager.Instance.AutoSpawn = false;
            AnimalCustomerController benny = CustomerManager.Instance.Spawn(Cfg.FindCharacter("benny"), new[] { "fries" }, true);
            benny.TimeScale = 4f;
            Assert.AreEqual(CustomerState.Waiting, benny.State);
            Assert.IsTrue(benny.Bubble.IsVisible, "Picture request shown");
            var states = new List<CustomerState>();
            benny.StateMachine.Changed += (a, b) => states.Add(b);
            bool left = false;
            benny.Left += _ => left = true;

            FoodItem food = Spawn("cheese"); // any food makes a customer happy
            Assert.IsTrue(food.SendTo(benny));
            yield return Until(() => left, 20f);
            CollectionAssert.AreEqual(new[] { CustomerState.ReceivingFood, CustomerState.Eating, CustomerState.Celebrating,
                CustomerState.Leaving, CustomerState.Idle }, states);
            Assert.IsTrue(Has(GameplayEventType.CustomerServed));
            Assert.Greater(GM.Save.Data.stats.customersServed, 0);
        }

        [UnityTest]
        public IEnumerator Customer_WalksInThroughTheDoor_ToTheServingSpot()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            CustomerManager.Instance.AutoSpawn = false;
            AnimalCustomerController benny = CustomerManager.Instance.Spawn(Cfg.FindCharacter("benny"), new[] { "burger" });
            Vector3 start = benny.transform.position;
            Assert.AreEqual(CustomerState.Entering, benny.State);
            yield return Until(() => benny.State == CustomerState.Waiting, 20f);
            Vector3 serve = GameObject.Find("CustomerPaths/ServePoint").transform.position;
            Assert.Less(Vector3.Distance(benny.transform.position, serve), 0.1f, "Customer stands at the serving spot, inside");
            Assert.Greater(Vector3.Distance(start, serve), 2f);
        }

        [UnityTest]
        public IEnumerator Customer_WaitsHappilyForever_WithoutFood()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            CustomerManager.Instance.AutoSpawn = false;
            AnimalCustomerController benny = CustomerManager.Instance.Spawn(Cfg.FindCharacter("benny"), null, true);
            yield return new WaitForSeconds(6f);
            Assert.AreEqual(CustomerState.Waiting, benny.State);
        }

        [UnityTest]
        public IEnumerator FreePlay_AllowsUnusualStacks_AndReset()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            BurgerAssemblyController board = KitchenManager.Instance.Board;
            foreach (string id in new[] { "tomato", "lettuce", "cheese", "tomato", "lettuce" })
            {
                Assert.IsTrue(KitchenManager.Instance.AutoSend(Spawn(id)), "No recipe enforcement: " + id);
            }
            Assert.AreEqual(5, board.Model.Count);
            KitchenManager.Instance.ResetWorkSurfaces();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0, board.Model.Count);
            Assert.IsTrue(Has(GameplayEventType.WorkSurfaceReset));
        }

        [UnityTest]
        public IEnumerator Guided_RestrictsBoard_AndActivityCompletesWithAssists()
        {
            yield return LoadRestaurant(GameMode.Guided);
            KitchenManager k = KitchenManager.Instance;
            k.Grill.SideSeconds = 0.3f;
            ActivityManager am = ActivityManager.Instance;
            int finished = 0;
            am.ActivityFinished += (a, s) => finished++;
            am.StartActivity("my_first_burger");
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(k.Board.CanAccept(Spawn("cheese")), "Guided: only the pictured ingredient is accepted");
            UIManager ui = Object.FindFirstObjectByType<UIManager>();
            Assert.IsTrue(ui.PromptVisible, "Prompt bubble shown");
            Assert.AreEqual("Put the bottom bun on the board!", ui.PromptText, "Instruction text shown with the picture");
            Assert.AreEqual("add_bun", GM.Audio.LastVoiceKey, "Instruction is spoken");

            float t = 0f;
            while (finished == 0 && t < 40f)
            {
                ActivityStep s = am.Controller.CurrentStep;
                if (am.Controller.IsWaiting && s != null) am.Assist(s);
                if (k.Grill.HasWaitingForFlip()) k.Grill.FlipAny();
                yield return new WaitForSeconds(0.4f);
                t += 0.4f * Time.timeScale;
            }
            Assert.AreEqual(1, finished, "Activity completed");
            Assert.IsTrue(GM.Save.Data.IsActivityCompleted("my_first_burger"));
            Assert.IsTrue(GM.Rewards.HasSticker("first_burger"), "Sticker earned");
            Assert.AreEqual("well_done", GM.Audio.LastVoiceKey, "Completion is cheered");
        }

        [UnityTest]
        public IEnumerator MainMenu_FreePlayButton_LoadsRestaurant()
        {
            m_SavePath = Path.Combine(Application.temporaryCachePath, "lbw_test_menu.json");
            GameManager.Create().UseSaveFile(m_SavePath);
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            Assert.IsNotNull(menu);
            menu.FreePlayButton.Click();
            yield return Until(() => SceneManager.GetActiveScene().name == "Restaurant", 10f);
            Assert.AreEqual(GameMode.FreePlay, GM.Modes.CurrentMode);
            yield return Until(() => !GM.Scenes.IsLoading, 5f);
        }

        [UnityTest]
        public IEnumerator HomeButton_QuickTapShowsTip_HoldGoesToMenu()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            HoldButton home = Object.FindFirstObjectByType<UIManager>().Home;
            home.OnPointerDown(null);
            yield return null;
            home.OnPointerUp(null);
            yield return null;
            Assert.IsTrue(home.TipVisible, "Quick tap explains 'Hold to go home'");
            Assert.AreEqual("Restaurant", SceneManager.GetActiveScene().name, "Quick tap does not leave");

            home.OnPointerDown(null);
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu", 10f);
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Holding goes home");
            yield return Until(() => !GM.Scenes.IsLoading, 5f);
        }
    }
}
