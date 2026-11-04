using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.InputHandling;
using LittleBurgerWorld.Interaction;
using LittleBurgerWorld.Kitchen;
using LittleBurgerWorld.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleBurgerWorld.Core
{
    /// <summary>
    /// Automated smoke play-through for desktop test players. Only runs when the player is launched with
    /// <c>-lbw-autopilot [-lbw-shots &lt;dir&gt;]</c>; it does nothing on devices. Drives the real game
    /// systems, captures screenshots and writes a pass/fail report, then quits.
    /// </summary>
    public sealed class AutoPilot : MonoBehaviour
    {
        private string m_Dir;
        private readonly StringBuilder m_Report = new StringBuilder();
        private int m_Failures;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Contains("-lbw-autopilot")) return;
            var go = new GameObject("[AutoPilot]");
            DontDestroyOnLoad(go);
            var ap = go.AddComponent<AutoPilot>();
            int i = Array.IndexOf(args, "-lbw-shots");
            ap.m_Dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "autopilot");
            Directory.CreateDirectory(ap.m_Dir);
        }

        private void Check(bool ok, string what)
        {
            m_Report.AppendLine((ok ? "PASS " : "FAIL ") + what);
            if (!ok) m_Failures++;
            Debug.Log("[AutoPilot] " + (ok ? "PASS " : "FAIL ") + what);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(m_Dir, name + ".png"));
            yield return null;
        }

        private static IEnumerator Until(Func<bool> c, float timeout)
        {
            float t = 0f;
            while (!c() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private IEnumerator Start()
        {
            GameManager gm = GameManager.Instance;
            gm.UseSaveFile(Path.Combine(m_Dir, "autopilot_save.json"));
            gm.Save.ResetProgress();
            string[] args = Environment.GetCommandLineArgs();
            int li = Array.IndexOf(args, "-lbw-lang");
            if (li >= 0 && li + 1 < args.Length)
            {
                gm.Save.Data.settings.language = args[li + 1];
                Localization.SetLanguage(args[li + 1]);
            }

            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && !gm.Scenes.IsLoading, 15f);
            yield return new WaitForSeconds(1.5f);
            Check(SceneManager.GetActiveScene().name == "MainMenu", "Boot → Main menu");
            yield return Shot("01_main_menu");

            // ---------------------------------------------------------------- Free Play
            FindFirstObjectByType<MainMenuUI>().FreePlayButton.Click();
            yield return Until(() => SceneManager.GetActiveScene().name == "Restaurant" && !gm.Scenes.IsLoading, 15f);
            yield return new WaitForSeconds(1f);
            Check(KitchenManager.Instance != null, "Free Play → Restaurant loaded");
            yield return Shot("02_free_play");

            KitchenManager k = KitchenManager.Instance;
            UIManager ui = FindFirstObjectByType<UIManager>();
            IngredientTrayButton Tray(string id) => ui.Tray.Buttons.First(b => b.Data.id == id);
            k.Grill.SideSeconds = 1.2f;

            Tray("bun_bottom").Assist(null, "Board");
            Tray("patty_raw").Assist(null, "Grill");
            yield return new WaitForSeconds(0.9f);
            yield return Shot("03_grill_cooking");
            yield return Until(k.Grill.HasWaitingForFlip, 5f);
            k.Grill.FlipAny();
            yield return new WaitForSeconds(0.25f);
            yield return Shot("04_flip");
            yield return Until(() => k.Grill.FindCooked() != null, 5f);
            Check(k.Grill.FindCooked() != null, "Patty cooked on the grill");
            k.AutoSend(k.Grill.FindCooked());

            // Real pointer drag of cheese from the tray onto the board.
            InputManager input = InputManager.Instance;
            InteractionManager im = InteractionManager.Instance;
            input.PollDevices = false;
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, Tray("cheese").transform.position);
            FoodItem cheese = FoodManager.SpawnIngredient(gm.Config.FindIngredient("cheese"),
                im.ScreenToDragPlane(start, 2.1f), true);
            input.Inject(PointerPhase.Down, start, true);
            im.BeginExternalDrag(cheese, start);
            Vector2 end = im.WorldToScreen(k.Board.SnapPoint) - new Vector2(0f, Screen.height * 0.06f);
            for (int i = 1; i <= 20; i++)
            {
                input.Inject(PointerPhase.Move, Vector2.Lerp(start, end, i / 20f));
                yield return null;
            }
            yield return Shot("05_dragging_cheese");
            input.Inject(PointerPhase.Up, end);
            input.PollDevices = true;
            yield return new WaitForSeconds(0.5f);
            Check(cheese != null && cheese.Container == (IFoodContainer)k.Board, "Drag-and-drop snaps cheese onto the burger");

            Tray("egg").Assist(null, "Board");
            Tray("lettuce").Assist(null, "Board");
            Tray("tomato").Assist(null, "Board");
            Tray("onion").Assist(null, "Board");
            yield return new WaitForSeconds(0.3f);
            Tray("ketchup").Assist(null, "Board");
            Tray("mustard").Assist(null, "Board");
            yield return new WaitForSeconds(0.4f);
            yield return Shot("05b_new_toppings");
            Check(k.Board.Items.Any(i => i.ItemId == "egg") && k.Board.Items.Any(i => i.ItemId == "ketchup"), "New toppings (egg, onion, sauces) stack on the burger");
            Tray("bun_top").Assist(null, "Board");
            yield return new WaitForSeconds(0.6f);
            yield return Shot("06_burger_complete");
            yield return Until(() => k.Board.FinishedBurger != null && !k.Board.IsCompleting, 4f);
            Check(k.Board.FinishedBurger != null, "Burger completed");

            // Fries
            k.Fryer.FrySeconds = 1f;
            k.Fryer.StartFrying();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("07_frying");
            yield return Until(() => k.Fryer.WaitingBatch != null, 4f);
            yield return new WaitForSeconds(0.4f);
            k.AutoSend(k.Fryer.WaitingBatch);
            yield return Until(() => k.FriesBox.IsFilled, 4f);
            Check(k.FriesBox.IsFilled, "Fries box filled");

            // Milkshake
            k.Drinks.FillSeconds = 0.8f;
            k.Drinks.SelectFlavour(0);
            k.Drinks.Assist(null, null);
            yield return new WaitForSeconds(0.5f);
            k.Drinks.Assist(null, null);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("08_milkshake_filling");
            yield return Until(() => k.Drinks.State == DrinkStation.CupState.Filled, 3f);
            k.Drinks.Assist(null, null);
            yield return new WaitForSeconds(0.5f);
            k.Drinks.Assist(null, null);
            yield return new WaitForSeconds(0.6f);
            Check(k.Drinks.State == DrinkStation.CupState.Complete, "Milkshake completed");

            // Tray
            k.AutoSend(k.Board.FinishedBurger);
            k.AutoSend(k.FriesBox.CurrentBox);
            k.AutoSend(k.Drinks.Cup);
            yield return new WaitForSeconds(0.9f);
            Check(k.Tray.HasMeal, "Complete meal on the tray");
            yield return Shot("09_meal_on_tray");

            // Customer
            yield return Until(() => CustomerManager.Instance.Current != null && CustomerManager.Instance.Current.State == CustomerState.Waiting, 15f);
            AnimalCustomerController c = CustomerManager.Instance.Current;
            Check(c != null, "A customer arrived and is waiting");
            yield return Shot("10_customer_waiting");
            if (c != null)
            {
                k.AutoSend(k.Tray.TrayItem);
                yield return new WaitForSeconds(0.9f);
                yield return Shot("11_customer_happy");
                yield return new WaitForSeconds(1.2f);
                yield return Shot("12_customer_eating");
                yield return Until(() => c == null || c.State == CustomerState.Celebrating, 6f);
                yield return new WaitForSeconds(0.4f);
                yield return Shot("13_customer_celebrating");
                Check(gm.Save.Data.stats.customersServed > 0, "Customer served");
            }

            // Ice cream cart: cone, two scoops, sprinkles, cherry, serve the next customer.
            IceCreamStation ice = k.IceCream;
            Check(ice != null, "Ice cream cart present");
            if (ice != null)
            {
                for (int i = 0; i < 5; i++) { ice.Assist(null, null); yield return new WaitForSeconds(0.6f); }
                yield return Shot("13b_ice_cream");
                Check(ice.IsComplete && ice.ScoopCount == 2 && ice.HasSprinkles, "Ice cream built (2 scoops, sprinkles, cherry)");
                yield return Until(() => CustomerManager.Instance.Current != null && CustomerManager.Instance.Current.State == CustomerState.Waiting, 20f);
                int before = gm.Save.Data.stats.customersServed;
                ice.Assist(null, "Customer");
                yield return new WaitForSeconds(1.0f);
                yield return Shot("13c_ice_cream_served");
                Check(gm.Save.Data.stats.customersServed > before, "Ice cream served to a customer");
            }

            // Washing up: rub the plate (real pointer events), then finish the pile.
            WashingUpUI wash = FindFirstObjectByType<WashingUpUI>();
            Check(wash != null && wash.DirtyCount > 0, "Dirty dishes after customers ate");
            if (wash != null)
            {
                wash.Open();
                yield return new WaitForSecondsRealtime(0.8f);
                ScrubSurface surface = FindFirstObjectByType<ScrubSurface>();
                Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, surface.transform.position);
                var pe = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                for (int i = 0; i < 40; i++)
                {
                    pe.position = centre + new Vector2(Mathf.Cos(i * 0.5f), Mathf.Sin(i * 0.5f)) * (20f + i * 3f);
                    surface.OnDrag(pe);
                    if (i % 8 == 0) yield return null;
                }
                yield return Shot("13d_washing_up");
                for (int i = 0; i < 12 && wash.IsOpen; i++) { wash.CleanCurrentPlate(); yield return new WaitForSecondsRealtime(0.9f); }
                Check(gm.Save.Data.stats.dishesWashed > 0, "Dishes washed");
            }

            // Surprise capsule
            ToyCapsuleUI capsules = FindFirstObjectByType<ToyCapsuleUI>();
            Check(capsules != null && gm.Rewards.PendingCapsules > 0, "Capsule earned by serving");
            yield return Shot("13e_capsule_waiting");
            if (capsules != null)
            {
                capsules.OpenNext();
                yield return new WaitForSecondsRealtime(1.0f);
                capsules.Assist(null, null); // crack open
                yield return new WaitForSecondsRealtime(0.9f);
                yield return Shot("13f_capsule_toy");
                yield return new WaitForSecondsRealtime(3.5f);
                Check(gm.Save.Data.toys.Count > 0, "Toy found in capsule");
            }

            // Restaurant moods (tap the window)
            ThemeManager themes = ThemeManager.Instance;
            Check(themes != null, "Theme manager present");
            if (themes != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    themes.NextTheme();
                    yield return new WaitForSeconds(1.2f);
                    yield return Shot("13g_theme_" + ThemeManager.Current.ToString().ToLowerInvariant());
                }
            }

            // ---------------------------------------------------------------- Guided Play
            FindFirstObjectByType<RestaurantController>().GoHome();
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && !gm.Scenes.IsLoading, 15f);
            yield return new WaitForSeconds(0.5f);
            MainMenuUI menuUi = FindFirstObjectByType<MainMenuUI>();
            if (menuUi != null && menuUi.ToyShelf != null)
            {
                menuUi.ToyShelf.Show();
                yield return new WaitForSeconds(1.0f);
                yield return Shot("13h_toy_shelf");
                Check(menuUi.ToyShelf.SlotCount == gm.Config.toys.Count, "Toy shelf shows every toy slot");
                menuUi.ToyShelf.Hide();
            }
            FindFirstObjectByType<MainMenuUI>().GuidedButton.Click();
            yield return Until(() => SceneManager.GetActiveScene().name == "Restaurant" && !gm.Scenes.IsLoading, 15f);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("14_activity_select");
            ActivitySelectUI select = FindFirstObjectByType<ActivitySelectUI>();
            Check(select != null && select.Cards.Count == gm.Config.activities.Count, "All activity cards shown");
            select.Cards[0].Button.Click();
            yield return new WaitForSeconds(4.5f);
            yield return Shot("15_guided_hint");

            ActivityManager am = ActivityManager.Instance;
            bool finished = false;
            am.ActivityFinished += (a, s) => finished = true;
            KitchenManager.Instance.Grill.SideSeconds = 1f;
            float t = 0f;
            while (!finished && t < 90f)
            {
                if (am.Controller.IsWaiting && am.Controller.CurrentStep != null) am.Assist(am.Controller.CurrentStep);
                yield return new WaitForSeconds(1.2f);
                t += 1.2f;
            }
            Check(finished, "Guided activity 'My First Burger' completed");
            yield return new WaitForSeconds(1.4f);
            yield return Shot("16_celebration");
            yield return new WaitForSeconds(3.5f);
            yield return Shot("17_activity_select_after");
            Check(gm.Rewards.HasSticker("first_burger"), "Sticker earned");

            m_Report.Insert(0, m_Failures == 0 ? "AUTOPILOT RESULT: PASS\n" : $"AUTOPILOT RESULT: FAIL ({m_Failures})\n");
            File.WriteAllText(Path.Combine(m_Dir, "autopilot_report.txt"), m_Report.ToString());
            yield return new WaitForSeconds(0.5f);
            Application.Quit(m_Failures == 0 ? 0 : 1);
        }
    }
}
