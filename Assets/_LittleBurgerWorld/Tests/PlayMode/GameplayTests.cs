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
        public IEnumerator NewAnimalActivities_CompleteAndServeTheRightFriend(
            [Values("milos_snack:milo", "dottys_milkshake:dotty", "ice_cream_time:benny", "make_fries:dotty", "make_milkshake:milo",
                "complete_meal:benny", "my_first_burger:benny")] string pair)
        {
            string activity = pair.Split(':')[0], animal = pair.Split(':')[1];
            yield return LoadRestaurant(GameMode.Guided);
            KitchenManager k = KitchenManager.Instance;
            k.Grill.SideSeconds = 0.3f;
            ActivityManager am = ActivityManager.Instance;
            int finished = 0;
            am.ActivityFinished += (a, st) => finished++;
            am.StartActivity(activity);
            float t = 0f;
            while (finished == 0 && t < 150f)
            {
                ActivityStep s = am.Controller.CurrentStep;
                if (am.Controller.IsWaiting && s != null) am.Assist(s);
                yield return new WaitForSeconds(0.4f);
                t += 0.4f * Time.timeScale;
            }
            ActivityStep stuck = am.Controller.CurrentStep;
            Assert.AreEqual(1, finished, activity + " completed (stuck at " + (stuck != null ? stuck.type + " " + stuck.targetKey + " " + stuck.itemId : "-") + ")");
            Assert.IsTrue(m_Events.Exists(e => e.Type == GameplayEventType.CustomerServed && e.ExtraId == animal), animal + " was served");
            CollectionAssert.Contains(GM.Save.Data.stats.animalsServed, animal);
        }

        [UnityTest]
        public IEnumerator IceCream_TappingATubBehindTheCone_AddsAScoop()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            CustomerManager.Instance.AutoSpawn = false;
            IceCreamStation station = KitchenManager.Instance.IceCream;
            var im = InteractionManager.Instance;
            station.PlaceCone(null);
            yield return new WaitForSeconds(0.6f);
            station.AddScoop(0);
            yield return null;
            Assert.AreEqual(1, station.ScoopCount);
            int expected = 1;
            foreach (string flavour in new[] { "strawberry", "chocolate", "vanilla" })
            {
                Transform tub = SceneObjectRegistry.Find("IceCream.Tub." + flavour);
                Assert.IsNotNull(tub, flavour);
                // Aim at the tub's ice cream, which the cone stands right in front of.
                foreach (Vector3 offset in new[] { Vector3.up * 0.25f, Vector3.up * 0.1f })
                {
                    IInteractable picked = im.Pick(im.WorldToScreen(tub.position + offset));
                    Assert.AreSame(tub.GetComponent<TapButton3D>(), picked, flavour + " tub should win the tap over the cone");
                }
                if (expected < 3)
                {
                    ((TapButton3D)im.Pick(im.WorldToScreen(tub.position + Vector3.up * 0.25f))).OnTap();
                    expected++;
                    yield return null;
                    Assert.AreEqual(expected, station.ScoopCount, flavour + " scoop added");
                }
            }
            // The cone itself is still easy to grab from its scoops.
            IInteractable cone = im.Pick(im.WorldToScreen(station.Cone.transform.position + Vector3.up * 1.2f));
            Assert.AreSame(station.Cone, cone, "Cone can still be picked up by its scoops");
        }

        [UnityTest]
        public IEnumerator IngredientTray_AllButtonsFitOnScreen()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            GM.Rewards.Unlock("pickle");
            yield return null;
            yield return null;
            IngredientTrayUI tray = Object.FindFirstObjectByType<UIManager>().Tray;
            Assert.GreaterOrEqual(tray.Buttons.Count, 11, "All ingredients incl. new foods are in the tray");
            var corners = new Vector3[4];
            foreach (IngredientTrayButton b in tray.Buttons)
            {
                ((RectTransform)b.transform).GetWorldCorners(corners);
                Assert.GreaterOrEqual(corners[0].x, -1f, b.Data.id + " off the left edge");
                Assert.LessOrEqual(corners[2].x, Screen.width + 1f, b.Data.id + " off the right edge");
            }
        }

        [UnityTest]
        public IEnumerator WashingUp_RubbingCleansPlates_AndAllCleanIsCelebrated()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            Time.timeScale = 1f;
            WashingUpUI wash = Object.FindFirstObjectByType<WashingUpUI>();
            Assert.IsNotNull(wash, "Sink in the HUD");
            wash.Open();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsTrue(wash.IsOpen);
            int before = wash.SplotchesLeft;
            Assert.Greater(before, 0, "Plate starts dirty");
            // Rub in circles over the plate with real pointer events.
            ScrubSurface surface = Object.FindFirstObjectByType<ScrubSurface>();
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, surface.transform.position);
            float radius = ((RectTransform)surface.transform).rect.width * surface.GetComponentInParent<Canvas>().scaleFactor * 0.3f;
            var e = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            for (int i = 0; i < 160 && wash.WashedThisVisit == 0; i++)
            {
                float a = i * 0.45f, r = radius * ((i % 20) / 20f);
                e.position = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (i % 10 == 0) surface.OnPointerDown(e); else surface.OnDrag(e);
                if (i % 10 == 0) yield return null;
            }
            Assert.GreaterOrEqual(wash.WashedThisVisit, 1, "Rubbing washes the plate");
            while (wash.IsOpen && wash.WashedThisVisit < 3)
            {
                wash.CleanCurrentPlate();
                yield return new WaitForSecondsRealtime(0.9f);
            }
            Assert.IsTrue(Has(GameplayEventType.AllDishesClean), "All clean celebrated");
            Assert.GreaterOrEqual(GM.Save.Data.stats.dishesWashed, 3);
        }

        [UnityTest]
        public IEnumerator SurpriseCapsule_RevealsAToy_ForTheToyShelf()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            Time.timeScale = 1f;
            ToyCapsuleUI capsules = Object.FindFirstObjectByType<ToyCapsuleUI>();
            Assert.IsNotNull(capsules);
            GM.Rewards.EarnCapsule();
            Assert.AreEqual(1, GM.Rewards.PendingCapsules);
            capsules.OpenNext();
            Assert.IsTrue(capsules.IsRevealing);
            yield return new WaitForSecondsRealtime(4.5f); // drops, opens by itself, toy flies to the shelf
            yield return new WaitForSecondsRealtime(3.0f);
            Assert.IsFalse(capsules.IsRevealing, "Reveal finishes on its own");
            Assert.AreEqual(0, GM.Rewards.PendingCapsules);
            Assert.AreEqual(1, GM.Save.Data.toys.Count, "Toy collected");
            Assert.IsTrue(Has(GameplayEventType.ToyCollected));
        }

        [UnityTest]
        public IEnumerator Themes_CycleWithTheWindow_AndBirthdayHasPartyHats()
        {
            yield return LoadRestaurant(GameMode.FreePlay);
            ThemeManager themes = ThemeManager.Instance;
            Assert.IsNotNull(themes);
            yield return Until(() => CustomerManager.Instance.Current != null, 10f);
            var seen = new HashSet<RestaurantTheme>();
            for (int i = 0; i < 4; i++)
            {
                themes.NextTheme();
                yield return null;
                seen.Add(ThemeManager.Current);
                Transform hat = CustomerManager.Instance.Current.transform.Find("Model/Head/PartyHat");
                Assert.IsNotNull(hat);
                Assert.AreEqual(ThemeManager.Current == RestaurantTheme.Birthday, hat.gameObject.activeSelf, "Party hat only on birthday days");
                Assert.IsTrue(GameObject.Find("Environment/Themes/Theme_" + ThemeManager.Current) != null, "Theme decorations shown");
            }
            Assert.AreEqual(4, seen.Count, "All four moods reachable");
        }

        [UnityTest]
        public IEnumerator WashTheDishesActivity_Completes()
        {
            yield return LoadRestaurant(GameMode.Guided);
            ActivityManager am = ActivityManager.Instance;
            int finished = 0;
            am.ActivityFinished += (a, st) => finished++;
            am.StartActivity("wash_dishes");
            float t = 0f;
            while (finished == 0 && t < 40f)
            {
                ActivityStep s = am.Controller.CurrentStep;
                if (am.Controller.IsWaiting && s != null) am.Assist(s);
                yield return new WaitForSecondsRealtime(0.5f);
                t += 0.5f;
            }
            Assert.AreEqual(1, finished, "Wash the Dishes completed");
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
