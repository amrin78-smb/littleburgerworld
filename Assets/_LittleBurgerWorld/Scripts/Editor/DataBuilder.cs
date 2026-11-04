using System.Collections.Generic;
using System.IO;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Rewards;
using UnityEditor;
using UnityEngine;
using E = LittleBurgerWorld.EditorTools.EditorAssetUtil;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>Creates/updates every ScriptableObject. Existing assets are updated in place (GUIDs kept).</summary>
    public static class DataBuilder
    {
        public const string SO = E.Root + "/ScriptableObjects";
        public const string ConfigPath = E.Root + "/Resources/LBW_GameConfig.asset";

        public static readonly Dictionary<string, IngredientData> Ingredients = new Dictionary<string, IngredientData>();
        public static readonly List<DrinkData> Drinks = new List<DrinkData>();
        public static readonly List<RecipeData> Recipes = new List<RecipeData>();

        // ------------------------------------------------------------------ ingredients / recipes / drinks
        public static void BuildFoodData()
        {
            Ingredients.Clear();
            string pf = PrefabBuilder.FoodFolder;
            IngredientData cooked = Ingredient("patty", "Cooked Patty", $"{pf}/Ingredient_Patty.prefab", "icon_patty", "#7E4A2C", 0.2f, 1, show: false);
            Ingredient("bun_bottom", "Bottom Bun", $"{pf}/Ingredient_BunBottom.prefab", "icon_bun_bottom", "#F0B55C", 0.26f, 0, isBase: true);
            IngredientData raw = Ingredient("patty_raw", "Burger Patty", $"{pf}/Ingredient_Patty.prefab", "icon_patty_raw", "#EE9A93", 0.2f, 1);
            raw.cookedVariant = cooked;
            raw.placeSound = SfxId.Plop;
            Ingredient("cheese", "Cheese", $"{pf}/Ingredient_Cheese.prefab", "icon_cheese", "#F7B731", 0.055f, 2);
            Ingredient("egg", "Fried Egg", $"{pf}/Ingredient_Egg.prefab", "icon_egg", "#FFB627", 0.07f, 3);
            Ingredient("lettuce", "Lettuce", $"{pf}/Ingredient_Lettuce.prefab", "icon_lettuce", "#8CCB5E", 0.07f, 4);
            Ingredient("tomato", "Tomato", $"{pf}/Ingredient_Tomato.prefab", "icon_tomato", "#E4503F", 0.08f, 5);
            Ingredient("onion", "Onion", $"{pf}/Ingredient_Onion.prefab", "icon_onion", "#C99AD6", 0.05f, 6);
            IngredientData pickle = Ingredient("pickle", "Pickles", $"{pf}/Ingredient_Pickle.prefab", "icon_pickle", "#6FAF4A", 0.05f, 7);
            pickle.unlockedByDefault = false;
            IngredientData ketchup = Ingredient("ketchup", "Ketchup", $"{pf}/Ingredient_Ketchup.prefab", "icon_ketchup", "#D93A2E", 0.035f, 8);
            IngredientData mustard = Ingredient("mustard", "Mustard", $"{pf}/Ingredient_Mustard.prefab", "icon_mustard", "#F2B71E", 0.035f, 9);
            ketchup.placeSound = mustard.placeSound = SfxId.Squeeze;
            IngredientData top = Ingredient("bun_top", "Top Bun", $"{pf}/Ingredient_BunTop.prefab", "icon_bun_top", "#E8993D", 0.48f, 10);
            top.completesBurger = true;
            foreach (IngredientData i in Ingredients.Values) EditorUtility.SetDirty(i);

            Recipes.Clear();
            Recipe("burger", "Basic Burger", "icon_burger", "bun_bottom", "patty", "bun_top");
            Recipe("cheeseburger", "Cheeseburger", "icon_cheeseburger", "bun_bottom", "patty", "cheese", "bun_top");
            // Extensibility examples (no code needed): more recipes are just more assets.
            Recipe("veggie_burger", "Veggie Burger", "icon_burger", "bun_bottom", "lettuce", "tomato", "lettuce", "bun_top");
            Recipe("double_burger", "Double Burger", "icon_cheeseburger", "bun_bottom", "patty", "cheese", "patty", "cheese", "bun_top");
            Recipe("special_burger", "Special Burger", "icon_cheeseburger", "bun_bottom", "patty", "cheese", "lettuce", "tomato", "bun_top");
            Recipe("egg_burger", "Sunny Egg Burger", "icon_egg_burger", "bun_bottom", "patty", "egg", "ketchup", "bun_top");

            Drinks.Clear();
            Drink("strawberry", "Strawberry", "icon_milkshake_strawberry", "#F48FB1", "#FFF6F8");
            Drink("chocolate", "Chocolate", "icon_milkshake_chocolate", "#8B5A3C", "#FFF3E6");
            Drink("vanilla", "Vanilla", "icon_milkshake_vanilla", "#FFF0C9", "#FFFFFF");
        }

        private static IngredientData Ingredient(string id, string name, string prefab, string icon, string color, float height, int order,
            bool isBase = false, bool show = true)
        {
            var d = E.LoadOrCreateSO<IngredientData>($"{SO}/Ingredients/Ingredient_{id}.asset");
            d.id = id;
            d.displayName = name;
            d.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
            d.icon = IconLibrary.Get(icon);
            d.color = E.Hex(color);
            d.stackHeight = height;
            d.trayOrder = order;
            d.isBase = isBase;
            d.showInTray = show;
            d.stackable = true;
            d.completesBurger = false;
            d.cookedVariant = null;
            d.unlockedByDefault = true;
            d.pickSound = SfxId.Pop;
            d.placeSound = SfxId.Snap;
            Ingredients[id] = d;
            return d;
        }

        private static void Recipe(string id, string name, string icon, params string[] ingredients)
        {
            var r = E.LoadOrCreateSO<RecipeData>($"{SO}/Recipes/Recipe_{id}.asset");
            r.id = id;
            r.displayName = name;
            r.icon = IconLibrary.Get(icon);
            r.ingredients = new List<IngredientData>();
            foreach (string i in ingredients) r.ingredients.Add(Ingredients[i]);
            r.completionSound = SfxId.Jingle;
            r.completionAnimation = "spin";
            EditorUtility.SetDirty(r);
            Recipes.Add(r);
        }

        private static void Drink(string id, string name, string icon, string liquid, string cream)
        {
            var d = E.LoadOrCreateSO<DrinkData>($"{SO}/Drinks/Drink_{id}.asset");
            d.id = id;
            d.displayName = name;
            d.icon = IconLibrary.Get(icon);
            d.liquidColor = E.Hex(liquid);
            d.creamColor = E.Hex(cream);
            d.unlockedByDefault = true;
            EditorUtility.SetDirty(d);
            Drinks.Add(d);
        }

        // ------------------------------------------------------------------ characters
        public static List<CharacterData> BuildCharacters()
        {
            var list = new List<CharacterData>();
            var benny = E.LoadOrCreateSO<CharacterData>($"{SO}/Characters/Character_benny.asset");
            benny.id = "benny";
            benny.displayName = "Benny the Bear";
            benny.portrait = IconLibrary.Get("portrait_benny");
            benny.unlockedByDefault = true;
            benny.voicePitch = 0.9f;
            benny.walkSpeed = 1.7f;
            benny.prefab = PrefabBuilder.BuildCharacter(new PrefabBuilder.CharacterLook
            {
                Name = "Benny", Fur = ArtLibrary.BearFur, FurLight = ArtLibrary.BearLight, Accent = ArtLibrary.TomatoCoral,
                Ears = PrefabBuilder.EarStyle.Round, Snout = true
            }, benny, $"{PrefabBuilder.CharFolder}/Character_Benny.prefab");
            EditorUtility.SetDirty(benny);
            list.Add(benny);

            var penny = E.LoadOrCreateSO<CharacterData>($"{SO}/Characters/Character_penny.asset");
            penny.id = "penny";
            penny.displayName = "Penny the Bunny";
            penny.portrait = IconLibrary.Get("portrait_penny");
            penny.unlockedByDefault = false;
            penny.voicePitch = 1.2f;
            penny.walkSpeed = 1.9f;
            penny.prefab = PrefabBuilder.BuildCharacter(new PrefabBuilder.CharacterLook
            {
                Name = "Penny", Fur = ArtLibrary.BunnyFur, FurLight = ArtLibrary.White, Accent = ArtLibrary.Lavender,
                Ears = PrefabBuilder.EarStyle.Long, Snout = false
            }, penny, $"{PrefabBuilder.CharFolder}/Character_Penny.prefab");
            EditorUtility.SetDirty(penny);
            list.Add(penny);

            list.Add(Character("milo", "Milo the Cat", "portrait_milo", true, 1.15f, 1.8f, new PrefabBuilder.CharacterLook
            {
                Name = "Milo", Fur = ArtLibrary.CatFur, FurLight = ArtLibrary.CatLight, Accent = ArtLibrary.Sky,
                Ears = PrefabBuilder.EarStyle.Pointy, Stripes = ArtLibrary.CatStripe, Whiskers = true, Tail = PrefabBuilder.TailStyle.Long
            }));
            list.Add(Character("dotty", "Dotty the Dog", "portrait_dotty", true, 1.05f, 1.9f, new PrefabBuilder.CharacterLook
            {
                Name = "Dotty", Fur = ArtLibrary.DogFur, FurLight = ArtLibrary.DogLight, Accent = ArtLibrary.TomatoCoral,
                Ears = PrefabBuilder.EarStyle.Floppy, EarFur = ArtLibrary.DogPatch, EyeSpot = ArtLibrary.DogPatch, Snout = true,
                Tail = PrefabBuilder.TailStyle.Short
            }));
            list.Add(Character("ellie", "Ellie the Elephant", "portrait_ellie", false, 0.8f, 1.5f, new PrefabBuilder.CharacterLook
            {
                Name = "Ellie", Fur = ArtLibrary.ElephantSkin, FurLight = ArtLibrary.ElephantLight, Accent = ArtLibrary.Lavender,
                Ears = PrefabBuilder.EarStyle.Big, Trunk = true, Tail = PrefabBuilder.TailStyle.Thin, Scale = 1.1f
            }));
            list.Add(Character("bao", "Bao the Panda", "portrait_bao", false, 0.95f, 1.6f, new PrefabBuilder.CharacterLook
            {
                Name = "Bao", Fur = ArtLibrary.PandaWhite, FurLight = ArtLibrary.PandaWhite, Accent = ArtLibrary.Mint,
                Ears = PrefabBuilder.EarStyle.Round, EarFur = ArtLibrary.PandaBlack, LimbFur = ArtLibrary.PandaBlack,
                EyePatches = ArtLibrary.PandaBlack, Snout = true, Tail = PrefabBuilder.TailStyle.Puff
            }));
            return list;
        }

        private static CharacterData Character(string id, string name, string portrait, bool unlocked, float pitch, float speed,
            PrefabBuilder.CharacterLook look)
        {
            var c = E.LoadOrCreateSO<CharacterData>($"{SO}/Characters/Character_{id}.asset");
            c.id = id;
            c.displayName = name;
            c.portrait = IconLibrary.Get(portrait);
            c.unlockedByDefault = unlocked;
            c.voicePitch = pitch;
            c.walkSpeed = speed;
            c.prefab = PrefabBuilder.BuildCharacter(look, c, $"{PrefabBuilder.CharFolder}/Character_{look.Name}.prefab");
            EditorUtility.SetDirty(c);
            return c;
        }

        // ------------------------------------------------------------------ activities
        private static ActivityStep S(ActivityStepType type, string target = null, string source = null, string item = null,
            string prompt = null, GameplayEventType evt = GameplayEventType.None, float duration = 1f, float hintDelay = 2.5f,
            int assist = 4, string voice = null, string animation = null)
        {
            return new ActivityStep
            {
                animation = animation,
                type = type,
                targetKey = target,
                sourceKey = source,
                itemId = item,
                eventType = evt,
                promptIcon = string.IsNullOrEmpty(prompt) ? null : IconLibrary.Get(prompt),
                duration = duration,
                hintDelay = hintDelay,
                autoAssistAfterHints = assist,
                voiceKey = voice,
                requestItems = new List<string>()
            };
        }

        private static ActivityStep Call(string character, params string[] request)
        {
            ActivityStep s = S(ActivityStepType.CallCustomer, duration: 0.4f);
            s.characterId = character;
            s.requestItems = new List<string>(request);
            return s;
        }

        private static IEnumerable<ActivityStep> BurgerSteps(bool cheese)
        {
            yield return S(ActivityStepType.WaitForDrop, "Board", "Tray.bun_bottom", "bun_bottom", "icon_bun_bottom", voice: "add_bun");
            yield return S(ActivityStepType.WaitForDrop, "Grill", "Tray.patty_raw", "patty_raw", "icon_patty_raw", voice: "patty_on_grill");
            yield return S(ActivityStepType.WaitForEvent, "Grill", null, null, "icon_patty", GameplayEventType.PattyFlipped, hintDelay: 3.2f, voice: "flip_patty");
            yield return S(ActivityStepType.WaitForEvent, null, null, null, "icon_patty", GameplayEventType.PattyCooked, hintDelay: 30f, assist: 0, voice: "patty_cooking");
            yield return S(ActivityStepType.WaitForDrop, "Board", "Grill", "patty", "icon_patty", voice: "patty_on_bun");
            if (cheese) yield return S(ActivityStepType.WaitForDrop, "Board", "Tray.cheese", "cheese", "icon_cheese", voice: "add_cheese");
            yield return S(ActivityStepType.WaitForDrop, "Board", "Tray.bun_top", "bun_top", "icon_bun_top", voice: "add_top_bun");
            yield return S(ActivityStepType.WaitForEvent, null, null, null, null, GameplayEventType.BurgerCompleted, hintDelay: 30f, assist: 0);
            yield return S(ActivityStepType.Wait, duration: 1f);
        }

        private static IEnumerable<ActivityStep> FriesSteps()
        {
            yield return S(ActivityStepType.WaitForTap, "Fryer.Potatoes", null, null, "icon_potato", voice: "tap_potato");
            yield return S(ActivityStepType.WaitForEvent, null, null, null, "icon_fries", GameplayEventType.FriesReady, hintDelay: 30f, assist: 0, voice: "fries_cooking");
            yield return S(ActivityStepType.WaitForDrop, "FriesBox", "Fryer.Batch", "fries_batch", "icon_fries", voice: "fries_in_box");
            yield return S(ActivityStepType.WaitForEvent, null, null, null, null, GameplayEventType.FriesBoxFilled, hintDelay: 30f, assist: 0);
        }

        private static IEnumerable<ActivityStep> ShakeSteps(bool full)
        {
            if (full)
            {
                yield return S(ActivityStepType.WaitForEvent, "Drinks.Flavour.strawberry", null, null, "icon_milkshake_strawberry",
                    GameplayEventType.DrinkFlavourChosen, voice: "pick_flavour");
                yield return S(ActivityStepType.WaitForEvent, "Drinks.Cups", null, null, "icon_milkshake_vanilla", GameplayEventType.CupPlaced, voice: "pick_cup");
            }
            yield return S(ActivityStepType.WaitForEvent, "Drinks.Lever", null, null, "icon_milkshake_strawberry", GameplayEventType.DrinkFilled, voice: "press_lever");
            if (full) yield return S(ActivityStepType.WaitForEvent, "Drinks.Cream", null, null, "icon_milkshake_vanilla", GameplayEventType.CreamAdded, voice: "add_cream");
            yield return S(ActivityStepType.WaitForEvent, "Drinks.Straws", null, null, "icon_milkshake_strawberry", GameplayEventType.StrawAdded, voice: "add_straw");
        }

        public static List<ActivityData> BuildActivities()
        {
            var list = new List<ActivityData>();

            var a1 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("benny", "burger") };
            a1.AddRange(BurgerSteps(false));
            a1.Add(S(ActivityStepType.WaitForDrop, "Customer", "Board", "burger", "icon_burger", voice: "serve_benny"));
            a1.Add(S(ActivityStepType.Wait, duration: 3.2f));
            list.Add(Activity("my_first_burger", "My First Burger", "icon_burger", "#FFE3A3", a1, unlocks: null));

            var a2 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("benny", "cheeseburger") };
            a2.AddRange(BurgerSteps(true));
            a2.Add(S(ActivityStepType.WaitForDrop, "Customer", "Board", "burger", "icon_cheeseburger", voice: "serve_benny"));
            a2.Add(S(ActivityStepType.Wait, duration: 3.2f));
            list.Add(Activity("add_some_cheese", "Add Some Cheese", "icon_cheeseburger", "#FFD6A0", a2, "pickle"));

            var a3 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("dotty", "fries") };
            a3.AddRange(FriesSteps());
            a3.Add(S(ActivityStepType.WaitForDrop, "Customer", "FriesBox", "fries", "icon_fries", voice: "serve_dotty_fries"));
            a3.Add(S(ActivityStepType.Wait, duration: 3.2f));
            list.Add(Activity("make_fries", "Make French Fries", "icon_fries", "#FFC9BE", a3, "ellie"));

            var a4 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("milo", "milkshake") };
            a4.AddRange(ShakeSteps(true));
            a4.Add(S(ActivityStepType.WaitForDrop, "Customer", "Drinks.Cup", "milkshake", "icon_milkshake_strawberry", voice: "serve_milo_shake"));
            a4.Add(S(ActivityStepType.Wait, duration: 3.2f));
            list.Add(Activity("make_milkshake", "Make a Milkshake", "icon_milkshake_strawberry", "#FAD3DD", a4, "penny"));

            var a5 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("benny", "burger", "fries", "milkshake") };
            a5.AddRange(BurgerSteps(false));
            a5.Add(S(ActivityStepType.WaitForDrop, "Tray", "Board", "burger", "icon_burger", voice: "burger_on_tray"));
            a5.AddRange(FriesSteps());
            a5.Add(S(ActivityStepType.WaitForDrop, "Tray", "FriesBox", "fries", "icon_fries", voice: "fries_on_tray"));
            a5.AddRange(ShakeSteps(false));
            a5.Add(S(ActivityStepType.WaitForDrop, "Tray", "Drinks.Cup", "milkshake", "icon_milkshake_strawberry", voice: "shake_on_tray"));
            // Normally already done by the three drops above; if a dish was taken off again the tray asks for it back.
            a5.Add(S(ActivityStepType.WaitForEvent, "Tray", null, null, "icon_meal", GameplayEventType.MealReady, hintDelay: 6f, assist: 2, voice: "meal_ready"));
            a5.Add(S(ActivityStepType.WaitForDrop, "Customer", "Tray", "meal", "icon_meal", voice: "serve_meal"));
            a5.Add(S(ActivityStepType.Wait, duration: 3.4f));
            list.Add(Activity("complete_meal", "Prepare a Complete Meal", "icon_meal", "#C9EBDC", a5, "bao"));

            var a6 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("benny", "cheeseburger") };
            a6.AddRange(BurgerSteps(true));
            a6.Add(S(ActivityStepType.WaitForDrop, "Customer", "Board", "burger", "icon_cheeseburger", voice: "serve_benny"));
            a6.Add(S(ActivityStepType.Wait, duration: 3.4f));
            list.Add(Activity("serve_benny", "Serve Benny", "portrait_benny", "#CFE2F7", a6, null));

            var a7 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("milo", "fries") };
            a7.AddRange(FriesSteps());
            a7.Add(S(ActivityStepType.WaitForDrop, "Customer", "FriesBox", "fries", "icon_fries", voice: "serve_milo"));
            a7.Add(S(ActivityStepType.Wait, duration: 3.2f));
            list.Add(Activity("milos_snack", "Milo's Snack", "portrait_milo", "#FFE0C2", a7, null));

            var a8 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("dotty", "milkshake") };
            a8.AddRange(ShakeSteps(true));
            a8.Add(S(ActivityStepType.WaitForDrop, "Customer", "Drinks.Cup", "milkshake", "icon_milkshake_strawberry", voice: "serve_dotty"));
            a8.Add(S(ActivityStepType.Wait, duration: 3.2f));
            var a9 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen), Call("benny", "ice_cream") };
            a9.Add(S(ActivityStepType.WaitForEvent, "IceCream.Cones", null, null, "icon_cone", GameplayEventType.ConePlaced, voice: "get_cone"));
            a9.Add(S(ActivityStepType.WaitForEvent, "IceCream.Tub.strawberry", null, null, "icon_ice_cream", GameplayEventType.IceCreamScoop, voice: "add_scoop"));
            a9.Add(S(ActivityStepType.WaitForEvent, "IceCream.Tub.vanilla", null, null, "icon_ice_cream", GameplayEventType.IceCreamScoop, voice: "one_more_scoop"));
            a9.Add(S(ActivityStepType.WaitForEvent, "IceCream.Sprinkles", null, null, "icon_sprinkles", GameplayEventType.SprinklesAdded, voice: "add_sprinkles"));
            a9.Add(S(ActivityStepType.WaitForEvent, "IceCream.Cherry", null, null, "icon_cherry", GameplayEventType.CherryAdded, voice: "add_cherry"));
            a9.Add(S(ActivityStepType.WaitForDrop, "Customer", "IceCream.Holder", "ice_cream", "icon_ice_cream", voice: "serve_ice_cream"));
            a9.Add(S(ActivityStepType.Wait, duration: 3.2f));
            list.Add(Activity("ice_cream_time", "Ice Cream Time", "icon_ice_cream", "#FFE1EC", a9, null));

            var a10 = new List<ActivityStep> { S(ActivityStepType.PrepareKitchen) };
            a10.Add(S(ActivityStepType.WaitForTap, "Sink", null, null, "icon_sink", voice: "wash_dishes"));
            ActivityStep scrub = S(ActivityStepType.WaitForEvent, "Sink.Plate", null, null, "icon_sponge", GameplayEventType.AllDishesClean,
                hintDelay: 4f, assist: 2, voice: "scrub_plate");
            a10.Add(scrub);
            a10.Add(S(ActivityStepType.Wait, duration: 1.5f));
            list.Add(Activity("wash_dishes", "Wash the Dishes", "icon_sink", "#D6EEFF", a10, null));

            list.Add(Activity("dottys_milkshake", "Dotty's Milkshake", "portrait_dotty", "#DCEBFF", a8, null));

            return list;
        }

        private static ActivityData Activity(string id, string name, string thumb, string color, List<ActivityStep> steps, string unlocks)
        {
            var a = E.LoadOrCreateSO<ActivityData>($"{SO}/Activities/Activity_{id}.asset");
            a.id = id;
            a.displayName = name;
            a.thumbnail = IconLibrary.Get(thumb);
            a.cardColor = E.Hex(color);
            a.steps = steps;
            a.completionSound = SfxId.Celebration;
            a.completionAnimation = "balloons";
            a.starReward = 1;
            a.unlocks = string.IsNullOrEmpty(unlocks) ? new List<string>() : new List<string> { unlocks };
            EditorUtility.SetDirty(a);
            return a;
        }

        // ------------------------------------------------------------------ stickers
        public static List<StickerData> BuildStickers()
        {
            var list = new List<StickerData>
            {
                Sticker("first_burger", "First Burger", "sticker_first_burger", StickerCondition.FirstBurger, 1),
                Sticker("cheese_champ", "Cheese Champ", "sticker_cheese_champ", StickerCondition.FirstCheeseburger, 1),
                Sticker("fries_fan", "Fries Fan", "sticker_fries_fan", StickerCondition.FirstFries, 1),
                Sticker("shake_maker", "Shake Maker", "sticker_shake_maker", StickerCondition.FirstMilkshake, 1),
                Sticker("meal_master", "Meal Master", "sticker_meal_master", StickerCondition.FirstMeal, 1),
                Sticker("bear_buddy", "Bear Buddy", "sticker_bear_buddy", StickerCondition.ServeCustomers, 1),
                Sticker("tall_tower", "Tall Tower", "sticker_tall_tower", StickerCondition.TallBurger, 8),
                Sticker("balloon_pop", "Balloon Popper", "sticker_balloon_pop", StickerCondition.PopBalloons, 5),
                Sticker("sweet_treat", "Sweet Treat", "sticker_sweet_treat", StickerCondition.FirstIceCream, 1),
                Sticker("squeaky_clean", "Squeaky Clean", "sticker_squeaky_clean", StickerCondition.WashDishes, 5),
                Sticker("toy_collector", "Toy Collector", "sticker_toy_collector", StickerCondition.CollectToys, 10),
                Sticker("animal_friends", "Animal Friends", "sticker_animal_friends", StickerCondition.ServeDifferentAnimals, 4),
                Sticker("super_chef", "Super Chef", "sticker_super_chef", StickerCondition.CompleteAllActivities, 1)
            };
            return list;
        }

        private static StickerData Sticker(string id, string name, string sprite, StickerCondition c, int threshold)
        {
            var s = E.LoadOrCreateSO<StickerData>($"{SO}/Stickers/Sticker_{id}.asset");
            s.id = id;
            s.displayName = name;
            s.sprite = IconLibrary.Get(sprite);
            s.condition = c;
            s.threshold = threshold;
            EditorUtility.SetDirty(s);
            return s;
        }

        // ------------------------------------------------------------------ audio & localization
        public static AudioLibrary BuildAudioLibrary()
        {
            var lib = E.LoadOrCreateSO<AudioLibrary>($"{SO}/Settings/AudioLibrary.asset");
            lib.menuMusic = AudioSynth.Get("music_menu_loop");
            lib.restaurantMusic = AudioSynth.Get("music_restaurant_loop");
            lib.musicVolume = 0.42f;
            lib.sfx = new List<SfxEntry>();
            void Add(SfxId id, string clip, float vol, float pitchVar = 0.06f) =>
                lib.sfx.Add(new SfxEntry { id = id, clips = new[] { AudioSynth.Get(clip) }, volume = vol, pitchVariance = pitchVar });
            Add(SfxId.Pop, "sfx_pop", 0.6f, 0.1f);
            Add(SfxId.Plop, "sfx_plop", 0.7f, 0.08f);
            Add(SfxId.Snap, "sfx_snap", 0.6f, 0.08f);
            Add(SfxId.Whoosh, "sfx_whoosh", 0.4f);
            Add(SfxId.Sizzle, "sfx_sizzle_loop", 0.35f, 0f);
            Add(SfxId.Flip, "sfx_flip", 0.7f);
            Add(SfxId.Ding, "sfx_ding", 0.55f, 0.02f);
            Add(SfxId.Bubble, "sfx_bubble_loop", 0.35f, 0f);
            Add(SfxId.FriesReady, "sfx_fries_ready", 0.6f, 0.02f);
            Add(SfxId.Pour, "sfx_pour_loop", 0.4f, 0f);
            Add(SfxId.Cream, "sfx_cream", 0.55f);
            Add(SfxId.Straw, "sfx_straw", 0.5f);
            Add(SfxId.Jingle, "sfx_jingle", 0.7f, 0f);
            Add(SfxId.Celebration, "sfx_celebration", 0.7f, 0f);
            Add(SfxId.ThankYou, "sfx_thankyou", 0.7f, 0.03f);
            Add(SfxId.Munch, "sfx_munch", 0.6f, 0.1f);
            Add(SfxId.Happy, "sfx_happy", 0.55f, 0.06f);
            Add(SfxId.ButtonTap, "sfx_button", 0.55f, 0.05f);
            Add(SfxId.Sparkle, "sfx_sparkle", 0.45f, 0.03f);
            Add(SfxId.BalloonPop, "sfx_balloon_pop", 0.6f, 0.1f);
            Add(SfxId.Star, "sfx_star", 0.55f, 0f);
            Add(SfxId.DoorChime, "sfx_door_chime", 0.45f, 0f);
            Add(SfxId.Boing, "sfx_boing", 0.45f, 0.05f);
            Add(SfxId.Footstep, "sfx_footstep", 0.3f, 0.1f);
            Add(SfxId.Squeeze, "sfx_squeeze", 0.55f, 0.1f);
            Add(SfxId.Scrub, "sfx_scrub", 0.4f, 0.15f);
            Add(SfxId.Capsule, "sfx_capsule", 0.6f, 0.03f);
            Add(SfxId.Chime, "sfx_chime", 0.5f, 0.02f);
            Add(SfxId.Rain, "sfx_rain_loop", 0.3f, 0f);

            // Voice lines: Tools/voice/voice_lines.json is the single source for the spoken clip
            // (Audio/Voice/<lang>/<key>.wav, generated by Tools/voice/generate_voice.sh) and the on-screen text.
            lib.voice = new List<VoiceEntry>();
            foreach (VoiceLine line in VoiceLines())
            {
                foreach (string lang in new[] { "en", "th" })
                {
                    string text = line.Text(lang);
                    if (string.IsNullOrEmpty(text)) continue;
                    string path = $"{VoiceFolder}/{lang}/{line.key}.wav";
                    AudioClip clip = null;
                    if (File.Exists(path))
                    {
                        AudioSynth.ConfigureClip(path, false);
                        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                    lib.voice.Add(new VoiceEntry { key = line.key, language = lang, clip = clip, transcript = text });
                }
            }
            EditorUtility.SetDirty(lib);
            return lib;
        }

        public const string VoiceFolder = EditorAssetUtil.Root + "/Audio/Voice";
        public const string VoiceLinesPath = "Tools/voice/voice_lines.json";

        [System.Serializable]
        public sealed class VoiceLine
        {
            public string key, en, th;
            public string Text(string lang) => lang == "th" ? th : en;
        }

        [System.Serializable]
        private sealed class VoiceLinesFile
        {
            public List<VoiceLine> lines = new List<VoiceLine>();
        }

        public static List<VoiceLine> VoiceLines()
        {
            if (!File.Exists(VoiceLinesPath)) throw new FileNotFoundException("Missing " + VoiceLinesPath);
            var file = JsonUtility.FromJson<VoiceLinesFile>(File.ReadAllText(VoiceLinesPath));
            return file != null && file.lines != null ? file.lines : new List<VoiceLine>();
        }

        public static LocalizationTable BuildLocalization()
        {
            var t = E.LoadOrCreateSO<LocalizationTable>($"{SO}/Settings/LocalizationTable.asset");
            t.languages = new List<string> { "en", "th" };
            t.languageNames = new List<string> { "English", "ไทย" };
            t.entries = new List<LocalizedEntry>();
            void L(string key, string en, string th) => t.entries.Add(new LocalizedEntry { key = key, values = new List<string> { en, th } });
            L("lets_play", "LET'S PLAY!", "มาเล่นกัน!");
            L("free_play", "FREE PLAY", "เล่นอิสระ");
            L("parent_gate_question", "Grown-ups only! Tap the answer:", "สำหรับผู้ใหญ่เท่านั้น! แตะคำตอบ:");
            L("settings_title", "Parent Settings", "การตั้งค่าสำหรับผู้ปกครอง");
            L("music", "Music", "เพลง");
            L("sounds", "Sounds", "เสียงประกอบ");
            L("voice", "Voice", "เสียงพูด");
            L("voice_prompts", "Spoken hints", "คำแนะนำด้วยเสียง");
            L("language", "Language", "ภาษา");
            L("reset_progress", "Hold to reset progress", "กดค้างเพื่อเริ่มใหม่ทั้งหมด");
            L("progress_reset_done", "Progress has been reset.", "เริ่มใหม่ทั้งหมดแล้ว");
            L("privacy", "Privacy", "ความเป็นส่วนตัว");
            L("privacy_text",
                "Little Burger World works completely offline.\nIt does not collect, store or share any personal information.\nThere are no ads, no purchases, no accounts, no analytics and no third-party tracking.\nProgress (stars, stickers and settings) is saved only on this device and can be erased with \"Reset progress\".",
                "Little Burger World ทำงานแบบออฟไลน์ทั้งหมด\nไม่เก็บ ไม่บันทึก และไม่แชร์ข้อมูลส่วนตัวใด ๆ\nไม่มีโฆษณา ไม่มีการซื้อ ไม่มีบัญชีผู้ใช้ ไม่มีการวิเคราะห์ และไม่มีการติดตามจากบุคคลที่สาม\nความคืบหน้า (ดาว สติกเกอร์ และการตั้งค่า) บันทึกไว้ในเครื่องนี้เท่านั้น และลบได้ด้วยปุ่ม \"เริ่มใหม่ทั้งหมด\"");
            L("stickers", "Stickers", "สติกเกอร์");
            L("hold_home", "Hold to go home", "กดค้างเพื่อกลับหน้าหลัก");
            L("hold_tidy", "Hold to tidy up", "กดค้างเพื่อเก็บของ");

            // On-screen instructions for guided steps ("say." + voice key), same text as the voice.
            foreach (VoiceLine line in VoiceLines()) L("say." + line.key, line.en, line.th);
            EditorUtility.SetDirty(t);
            return t;
        }

        // ------------------------------------------------------------------ config
        public static GameConfig BuildConfig(List<CharacterData> characters, List<ActivityData> activities, List<StickerData> stickers,
            AudioLibrary audio, LocalizationTable loc)
        {
            var cfg = E.LoadOrCreateSO<GameConfig>(ConfigPath);
            cfg.ingredients = new List<IngredientData>(Ingredients.Values);
            cfg.ingredients.Sort((a, b) => a.trayOrder != b.trayOrder ? a.trayOrder.CompareTo(b.trayOrder) : string.CompareOrdinal(a.id, b.id));
            cfg.recipes = new List<RecipeData>(Recipes);
            cfg.drinks = new List<DrinkData>(Drinks);
            cfg.characters = characters;
            cfg.activities = activities;
            cfg.stickers = stickers;
            cfg.audioLibrary = audio;
            cfg.localization = loc;
            cfg.friesIcon = IconLibrary.Get("icon_fries");
            cfg.iceCreamIcon = IconLibrary.Get("icon_ice_cream");
            cfg.toys = new List<Sprite>();
            foreach (string id in IconLibrary.ToyIds) cfg.toys.Add(IconLibrary.Get("toy_" + id));
            cfg.trayIcon = IconLibrary.Get("icon_meal");
            cfg.burgerIcon = IconLibrary.Get("icon_burger");
            cfg.starIcon = IconLibrary.Get("icon_star");
            cfg.heartIcon = IconLibrary.Get("icon_heart");
            cfg.sparkleMaterial = ArtLibrary.Sprite("FX_Sparkle", IconLibrary.Get("fx_sparkle").texture);
            cfg.softParticleMaterial = ArtLibrary.Sprite("FX_Soft", IconLibrary.Get("fx_soft").texture);
            cfg.confettiMaterial = ArtLibrary.Sprite("FX_Confetti", IconLibrary.Get("fx_confetti").texture);
            cfg.heartMaterial = ArtLibrary.Sprite("FX_Heart", IconLibrary.Get("fx_heart").texture);
            cfg.highlightRingMaterial = ArtLibrary.Sprite("FX_Ring", null);
            cfg.balloonMaterial = ArtLibrary.Balloon;
            cfg.targetFrameRate = 60;
            EditorUtility.SetDirty(cfg);
            return cfg;
        }
    }
}
