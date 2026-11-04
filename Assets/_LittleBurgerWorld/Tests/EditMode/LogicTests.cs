using System.Collections.Generic;
using System.IO;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Cameras;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.InputHandling;
using LittleBurgerWorld.Save;
using NUnit.Framework;
using UnityEngine;

namespace LittleBurgerWorld.Tests
{
    internal static class TestData
    {
        public static IngredientData Ing(string id, float h, bool top = false, IngredientData cooked = null)
        {
            var d = ScriptableObject.CreateInstance<IngredientData>();
            d.id = id;
            d.stackHeight = h;
            d.completesBurger = top;
            d.cookedVariant = cooked;
            return d;
        }
    }

    public class GestureTrackerTests
    {
        [Test]
        public void QuickPressAndRelease_IsTap()
        {
            var g = new GestureTracker();
            g.Down(new Vector2(100, 100), 0f, true);
            Assert.AreEqual(GestureEventType.Tap, g.Up(new Vector2(102, 101), 0.1f).Type);
        }

        [Test]
        public void MovingBeyondThreshold_StartsDrag_ThenEndsDrag()
        {
            var g = new GestureTracker { DragThreshold = 18f };
            g.Down(Vector2.zero, 0f, true);
            Assert.AreEqual(GestureEventType.None, g.Move(new Vector2(5, 0), 0.05f).Type);
            Assert.AreEqual(GestureEventType.BeginDrag, g.Move(new Vector2(30, 0), 0.1f).Type);
            Assert.AreEqual(GestureEventType.Drag, g.Move(new Vector2(60, 0), 0.15f).Type);
            Assert.AreEqual(GestureEventType.EndDrag, g.Up(new Vector2(60, 0), 0.2f).Type);
        }

        [Test]
        public void HoldingStill_OnDraggable_StartsDrag()
        {
            var g = new GestureTracker { HoldToDragTime = 0.3f };
            g.Down(Vector2.zero, 0f, true);
            Assert.AreEqual(GestureEventType.None, g.Tick(0.1f).Type);
            Assert.AreEqual(GestureEventType.BeginDrag, g.Tick(0.35f).Type);
        }

        [Test]
        public void NonDraggable_NeverDrags_AndReleaseIsTap()
        {
            var g = new GestureTracker();
            g.Down(Vector2.zero, 0f, false);
            Assert.AreEqual(GestureEventType.None, g.Move(new Vector2(200, 0), 0.1f).Type);
            Assert.AreEqual(GestureEventType.None, g.Tick(5f).Type);
            Assert.AreEqual(GestureEventType.Tap, g.Up(new Vector2(200, 0), 5f).Type);
        }

        [Test]
        public void Cancel_DuringDrag_ReportsCancel_AndResets()
        {
            var g = new GestureTracker();
            g.Down(Vector2.zero, 0f, true);
            g.Move(new Vector2(100, 0), 0.1f);
            Assert.AreEqual(GestureEventType.Cancel, g.Cancel().Type);
            Assert.AreEqual(GestureTracker.State.Idle, g.Current);
            Assert.AreEqual(GestureEventType.None, g.Cancel().Type, "Second cancel is a no-op");
        }
    }

    public class BurgerStackModelTests
    {
        [Test]
        public void Heights_AreCumulative()
        {
            var m = new BurgerStackModel();
            m.Add(TestData.Ing("bun_bottom", 0.26f));
            m.Add(TestData.Ing("patty", 0.2f));
            m.Add(TestData.Ing("cheese", 0.05f));
            Assert.AreEqual(0f, m.HeightBelow(0), 1e-5f);
            Assert.AreEqual(0.26f, m.HeightBelow(1), 1e-5f);
            Assert.AreEqual(0.46f, m.HeightBelow(2), 1e-5f);
            Assert.AreEqual(0.51f, m.TotalHeight, 1e-5f);
        }

        [Test]
        public void TopBun_CompletesOnlyWithSomethingBelow()
        {
            var m = new BurgerStackModel();
            m.Add(TestData.Ing("bun_top", 0.48f, true));
            Assert.IsFalse(m.IsComplete, "A lone top bun is not a burger");
            m.Clear();
            m.Add(TestData.Ing("lettuce", 0.07f));
            m.Add(TestData.Ing("bun_top", 0.48f, true));
            Assert.IsTrue(m.IsComplete, "Free Play: any order completes");
        }

        [Test]
        public void RawIngredients_AreNotStackable()
        {
            var cooked = TestData.Ing("patty", 0.2f);
            var raw = TestData.Ing("patty_raw", 0.2f, false, cooked);
            Assert.IsFalse(new BurgerStackModel().CanAdd(raw));
            Assert.IsTrue(new BurgerStackModel().CanAdd(cooked));
        }

        [Test]
        public void MaxItems_KeepsTowerStable_AndRemovalRestacks()
        {
            var m = new BurgerStackModel { MaxItems = 3 };
            for (int i = 0; i < 3; i++) Assert.IsTrue(m.Add(TestData.Ing("x" + i, 0.1f)));
            Assert.IsFalse(m.Add(TestData.Ing("x4", 0.1f)));
            Assert.IsTrue(m.RemoveAt(0));
            Assert.AreEqual(0.2f, m.TotalHeight, 1e-5f);
            Assert.IsFalse(m.RemoveAt(10));
        }
    }

    public class RecipeManagerTests
    {
        private IngredientData m_Bottom, m_Patty, m_Cheese, m_Top;
        private RecipeManager m_Recipes;
        private RecipeData m_Cheeseburger;

        [SetUp]
        public void SetUp()
        {
            m_Bottom = TestData.Ing("bun_bottom", 0.26f);
            m_Patty = TestData.Ing("patty", 0.2f);
            m_Cheese = TestData.Ing("cheese", 0.05f);
            m_Top = TestData.Ing("bun_top", 0.48f, true);
            var basic = ScriptableObject.CreateInstance<RecipeData>();
            basic.id = "burger";
            basic.ingredients = new List<IngredientData> { m_Bottom, m_Patty, m_Top };
            m_Cheeseburger = ScriptableObject.CreateInstance<RecipeData>();
            m_Cheeseburger.id = "cheeseburger";
            m_Cheeseburger.ingredients = new List<IngredientData> { m_Bottom, m_Patty, m_Cheese, m_Top };
            m_Recipes = new RecipeManager(new[] { basic, m_Cheeseburger });
        }

        [Test]
        public void ExactStack_MatchesRecipe()
        {
            Assert.AreEqual("burger", m_Recipes.Match(new[] { m_Bottom, m_Patty, m_Top }).id);
            Assert.AreEqual("cheeseburger", m_Recipes.Match(new[] { m_Bottom, m_Patty, m_Cheese, m_Top }).id);
        }

        [Test]
        public void OtherStacks_AreCustomBurgers()
        {
            Assert.IsNull(m_Recipes.Match(new[] { m_Bottom, m_Cheese, m_Patty, m_Top }));
            Assert.IsNull(m_Recipes.Match(new IngredientData[0]));
        }

        [Test]
        public void NextIngredient_FollowsRecipe()
        {
            Assert.AreEqual(m_Bottom, RecipeManager.NextIngredient(m_Cheeseburger, new IngredientData[0]));
            Assert.AreEqual(m_Cheese, RecipeManager.NextIngredient(m_Cheeseburger, new[] { m_Bottom, m_Patty }));
            Assert.IsNull(RecipeManager.NextIngredient(m_Cheeseburger, new[] { m_Bottom, m_Patty, m_Cheese, m_Top }));
            Assert.IsNull(RecipeManager.NextIngredient(m_Cheeseburger, new[] { m_Cheese }), "Off-recipe: no suggestion");
        }
    }

    public class SaveManagerTests
    {
        private string m_Dir;
        private string Path => System.IO.Path.Combine(m_Dir, "save.json");

        [SetUp]
        public void SetUp()
        {
            m_Dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lbw_save_tests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_Dir);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(m_Dir, true);

        [Test]
        public void MissingFile_CreatesDefaults()
        {
            var s = new SaveManager(Path);
            s.Load();
            Assert.AreEqual(SaveLoadResult.CreatedNew, s.LastLoadResult);
            Assert.AreEqual(SaveData.CurrentVersion, s.Data.version);
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            var s = new SaveManager(Path);
            s.Load();
            s.Data.stickers.Add("first_burger");
            s.Data.settings.musicVolume = 0.25f;
            s.Data.activities.Add(new ActivityProgress { id = "make_fries", stars = 2, timesCompleted = 2 });
            Assert.IsTrue(s.Save());

            var s2 = new SaveManager(Path);
            s2.Load();
            Assert.AreEqual(SaveLoadResult.Loaded, s2.LastLoadResult);
            CollectionAssert.Contains(s2.Data.stickers, "first_burger");
            Assert.AreEqual(0.25f, s2.Data.settings.musicVolume, 1e-5f);
            Assert.IsTrue(s2.Data.IsActivityCompleted("make_fries"));
        }

        [Test]
        public void CorruptedFile_RecoversFromBackup()
        {
            var s = new SaveManager(Path);
            s.Load();
            s.Data.stickers.Add("a");
            s.Save();
            s.Data.stickers.Add("b");
            s.Save(); // previous good file becomes the backup
            File.WriteAllText(Path, "{ this is not json");
            var s2 = new SaveManager(Path);
            s2.Load();
            Assert.AreEqual(SaveLoadResult.RecoveredFromBackup, s2.LastLoadResult);
            CollectionAssert.Contains(s2.Data.stickers, "a");
            Assert.IsTrue(File.Exists(Path + ".corrupt"));
        }

        [Test]
        public void CorruptedFile_WithoutBackup_ResetsSafely()
        {
            File.WriteAllText(Path, "garbage");
            var s = new SaveManager(Path);
            s.Load();
            Assert.AreEqual(SaveLoadResult.ResetAfterCorruption, s.LastLoadResult);
            Assert.IsNotNull(s.Data.settings);
        }

        [Test]
        public void OldVersion_IsMigrated()
        {
            File.WriteAllText(Path, "{\"version\":1,\"stickers\":[\"x\"]}");
            var s = new SaveManager(Path);
            s.Load();
            Assert.AreEqual(SaveLoadResult.Migrated, s.LastLoadResult);
            Assert.AreEqual(SaveData.CurrentVersion, s.Data.version);
            Assert.IsNotNull(s.Data.stats);
            CollectionAssert.Contains(s.Data.stickers, "x");
        }

        [Test]
        public void ResetProgress_KeepsParentSettings()
        {
            var s = new SaveManager(Path);
            s.Load();
            s.Data.settings.sfxVolume = 0.3f;
            s.Data.stickers.Add("x");
            s.ResetProgress();
            Assert.IsEmpty(s.Data.stickers);
            Assert.AreEqual(0.3f, s.Data.settings.sfxVolume, 1e-5f);
        }
    }

    public class CustomerStateMachineTests
    {
        [Test]
        public void HappyPath_Transitions()
        {
            var f = new CustomerStateMachine();
            foreach (CustomerState s in new[] { CustomerState.Entering, CustomerState.Waiting, CustomerState.ReceivingFood,
                         CustomerState.Eating, CustomerState.Celebrating, CustomerState.Leaving, CustomerState.Idle })
            {
                Assert.IsTrue(f.TryTransition(s), "Could not enter " + s);
            }
        }

        [Test]
        public void InvalidTransitions_AreRejected()
        {
            var f = new CustomerStateMachine();
            Assert.IsFalse(f.TryTransition(CustomerState.Eating));
            f.TryTransition(CustomerState.Entering);
            Assert.IsFalse(f.CanReceiveFood, "Food only while waiting");
            f.TryTransition(CustomerState.Waiting);
            Assert.IsTrue(f.CanReceiveFood);
            Assert.IsFalse(f.TryTransition(CustomerState.Celebrating));
        }

        [Test]
        public void ThereIsNoNegativeState()
        {
            foreach (string n in System.Enum.GetNames(typeof(CustomerState)))
            {
                StringAssert.DoesNotContain("angry", n.ToLowerInvariant());
                StringAssert.DoesNotContain("impatient", n.ToLowerInvariant());
            }
        }
    }

    public class CameraFramingTests
    {
        [TestCase(16f / 9f)]
        [TestCase(18f / 9f)]
        [TestCase(19.5f / 9f)]
        [TestCase(4f / 3f)]
        public void FocusBox_FitsInsideUsableArea(float aspect)
        {
            var size = new Vector3(11.6f, 1.8f, 7.4f);
            Quaternion rot = Quaternion.Euler(35f, 45f, 0f);
            IsometricCameraController.Framing f = IsometricCameraController.ComputeFraming(Vector3.zero, size,
                Quaternion.Euler(0f, 45f, 0f), rot, aspect, 0.1f, 0.2f, 0.02f, new Rect(0, 0, 1, 1));
            float usableHalfH = f.OrthographicSize * (1f - 0.1f - 0.2f);
            float halfW = f.OrthographicSize * aspect;
            Assert.LessOrEqual(f.HalfExtents.y, usableHalfH + 1e-3f);
            Assert.LessOrEqual(f.HalfExtents.x, halfW + 1e-3f);
            // Box centre shifted up out of the tray band (camera moves down → negative offset).
            Assert.Less(f.Offset.y, 0f);
        }

        [Test]
        public void SafeAreaInsets_EnlargeView()
        {
            Quaternion rot = Quaternion.Euler(35f, 45f, 0f);
            var size = new Vector3(11.6f, 1.8f, 7.4f);
            var full = IsometricCameraController.ComputeFraming(Vector3.zero, size, Quaternion.Euler(0, 45, 0), rot, 2.2f, 0.1f, 0.2f, 0f, new Rect(0, 0, 1, 1));
            var notch = IsometricCameraController.ComputeFraming(Vector3.zero, size, Quaternion.Euler(0, 45, 0), rot, 2.2f, 0.1f, 0.2f, 0f, new Rect(0.25f, 0, 0.5f, 1));
            Assert.Greater(notch.OrthographicSize, full.OrthographicSize - 1e-4f);
        }
    }

    public class ActivityEngineTests
    {
        private sealed class FakeContext : IActivityContext
        {
            public int Hints, Assists, Resets, Successes;
            public string Filter;
            public bool Busy;
            public System.Action<ActivityStep> OnAssist;
            public void ResetKitchen() => Resets++;
            public void CallCustomer(string c, IList<string> r) { }
            public void SetBoardFilter(string id) => Filter = id;
            public void SetPrompt(Sprite icon, string textKey) { }
            public void ShowHint(ActivityStep s) => Hints++;
            public void HideHint() { }
            public bool Assist(ActivityStep s) { Assists++; OnAssist?.Invoke(s); return true; }
            public void PlayAnimation(string k, string a) { }
            public void PlaySound(ActivityStep s) { }
            public void ShowObject(string k) { }
            public void Highlight(string k, bool on) { }
            public void StepSucceeded(ActivityStep s) => Successes++;
            public bool IsChildBusy => Busy;
        }

        private static ActivityData Make(params ActivityStep[] steps)
        {
            var a = ScriptableObject.CreateInstance<ActivityData>();
            a.id = "test";
            a.steps = new List<ActivityStep>(steps);
            return a;
        }

        private static void Run(ActivityStepController c, float seconds)
        {
            for (float t = 0; t < seconds; t += 0.1f) c.Tick(0.1f);
        }

        [TearDown]
        public void TearDown() => GameEvents.ClearAll();

        [Test]
        public void DropStep_SetsBoardFilter_AndCompletesOnMatchingEvent()
        {
            var ctx = new FakeContext();
            var c = new ActivityStepController(ctx);
            bool done = false;
            c.Completed += _ => done = true;
            c.Start(Make(new ActivityStep { type = ActivityStepType.PrepareKitchen, duration = 0.5f },
                new ActivityStep { type = ActivityStepType.WaitForDrop, targetKey = "Board", itemId = "bun_bottom", hintDelay = 2f }));
            Run(c, 1f);
            Assert.AreEqual(1, ctx.Resets);
            Assert.AreEqual("bun_bottom", ctx.Filter);
            GameEvents.Raise(GameplayEventType.IngredientStacked, "cheese", "Board");
            Assert.IsTrue(c.IsWaiting, "Wrong item does not complete the step");
            GameEvents.Raise(GameplayEventType.IngredientStacked, "bun_bottom", "Board");
            Run(c, 1f);
            Assert.IsTrue(done);
            Assert.AreEqual(1, ctx.Successes);
        }

        [Test]
        public void ActingAhead_CompletesLaterStepImmediately()
        {
            var ctx = new FakeContext();
            var c = new ActivityStepController(ctx);
            bool done = false;
            c.Completed += _ => done = true;
            c.Start(Make(new ActivityStep { type = ActivityStepType.WaitForEvent, eventType = GameplayEventType.PattyOnGrill },
                new ActivityStep { type = ActivityStepType.WaitForEvent, eventType = GameplayEventType.PattyFlipped }));
            GameEvents.Raise(GameplayEventType.PattyOnGrill);
            GameEvents.Raise(GameplayEventType.PattyFlipped); // child flipped before the hint appeared
            Run(c, 2f);
            Assert.IsTrue(done, "Activity must never get stuck on something already done");
        }

        [Test]
        public void Inactivity_ShowsHints_ThenAutoAssists()
        {
            var ctx = new FakeContext();
            var c = new ActivityStepController(ctx);
            ctx.OnAssist = s => GameEvents.Raise(GameplayEventType.ObjectTapped, null, s.targetKey);
            bool done = false;
            c.Completed += _ => done = true;
            c.Start(Make(new ActivityStep { type = ActivityStepType.WaitForTap, targetKey = "Fryer.Potatoes", hintDelay = 1f, autoAssistAfterHints = 2 }));
            Run(c, 1.5f);
            Assert.AreEqual(1, ctx.Hints);
            Run(c, 30f);
            Assert.GreaterOrEqual(ctx.Assists, 1);
            Assert.IsTrue(done);
        }

        [Test]
        public void HintsWait_WhileChildIsBusy()
        {
            var ctx = new FakeContext { Busy = true };
            var c = new ActivityStepController(ctx);
            c.Start(Make(new ActivityStep { type = ActivityStepType.WaitForTap, targetKey = "X", hintDelay = 0.5f }));
            Run(c, 10f);
            Assert.AreEqual(0, ctx.Hints);
        }

        [Test]
        public void Matches_Rules()
        {
            var tap = new ActivityStep { type = ActivityStepType.WaitForTap, targetKey = "Drinks.Lever" };
            Assert.IsTrue(ActivityStepController.Matches(tap, new GameplayEvent(GameplayEventType.ObjectTapped, null, "Drinks.Lever")));
            Assert.IsFalse(ActivityStepController.Matches(tap, new GameplayEvent(GameplayEventType.ObjectTapped, null, "Grill")));
            var serve = new ActivityStep { type = ActivityStepType.WaitForDrop, targetKey = "Customer", itemId = "burger" };
            Assert.IsTrue(ActivityStepController.Matches(serve, new GameplayEvent(GameplayEventType.CustomerServed, "burger", "Customer")));
            var anyFlavour = new ActivityStep { type = ActivityStepType.WaitForEvent, eventType = GameplayEventType.DrinkFlavourChosen };
            Assert.IsTrue(ActivityStepController.Matches(anyFlavour, new GameplayEvent(GameplayEventType.DrinkFlavourChosen, "vanilla")));
        }
    }

    public class GeneratedContentTests
    {
        [Test]
        public void GameConfig_HasRequiredContent()
        {
            var cfg = Resources.Load<GameConfig>(GameConfig.ResourcePath);
            Assert.IsNotNull(cfg, "Run Little Burger World ▸ Setup Project");
            foreach (string id in new[] { "bun_bottom", "patty_raw", "patty", "cheese", "lettuce", "tomato", "bun_top" })
            {
                IngredientData i = cfg.FindIngredient(id);
                Assert.IsNotNull(i, id);
                Assert.IsNotNull(i.prefab, id + " prefab");
                Assert.IsNotNull(i.prefab.GetComponent<FoodItem>(), id + " FoodItem");
                Assert.IsNotNull(i.icon, id + " icon");
            }
            Assert.IsNotNull(cfg.FindRecipe("burger"));
            Assert.IsNotNull(cfg.FindRecipe("cheeseburger"));
            Assert.AreEqual(10, cfg.activities.Count);
            Assert.AreEqual(20, cfg.toys.Count, "Surprise toys");
            Assert.IsTrue(cfg.toys.TrueForAll(t => t != null), "All toys drawn");
            foreach (string animal in new[] { "benny", "penny", "milo", "dotty", "ellie", "bao" })
            {
                CharacterData c = cfg.FindCharacter(animal);
                Assert.IsNotNull(c, animal);
                Assert.IsNotNull(c.prefab.GetComponent<AnimalCustomerController>(), animal + " controller");
                Assert.IsNotNull(c.portrait, animal + " portrait");
            }
            Assert.AreEqual(3, cfg.drinks.Count);
            Assert.IsNotNull(cfg.FindCharacter("benny").prefab.GetComponent<AnimalCustomerController>());
            Assert.IsNotNull(cfg.audioLibrary.restaurantMusic);
        }

        [Test]
        public void GuidedSteps_WithAPicture_HaveInstructionTextInEveryLanguage()
        {
            var cfg = Resources.Load<GameConfig>(GameConfig.ResourcePath);
            LocalizationTable table = cfg.localization;
            int checkedSteps = 0;
            foreach (ActivityData a in cfg.activities)
            {
                foreach (ActivityStep s in a.steps)
                {
                    if (!s.IsWaitStep || s.promptIcon == null) continue;
                    string key = s.PromptTextKey;
                    Assert.IsFalse(string.IsNullOrEmpty(key), $"{a.id}: step {s.type} {s.targetKey} has a picture but no instruction");
                    foreach (string lang in table.languages)
                        Assert.AreNotEqual(key, table.Get(key, lang), $"{a.id}: missing '{key}' ({lang})");
                    checkedSteps++;
                }
            }
            Assert.Greater(checkedSteps, 20);
        }

        [Test]
        public void Validator_ReportsNoProblems()
        {
            List<string> problems = EditorTools.LittleBurgerWorldValidator.Validate(out string report);
            Assert.IsEmpty(problems, report);
        }
    }
}
