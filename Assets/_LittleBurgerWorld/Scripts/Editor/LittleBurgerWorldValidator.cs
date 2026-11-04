using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>
    /// Project health check: scenes, prefabs, ScriptableObjects, broken references, layers,
    /// activity scene keys and Android build configuration.
    /// </summary>
    public static class LittleBurgerWorldValidator
    {
        private static readonly string[] s_RequiredPrefabs =
        {
            "Food/Ingredient_BunBottom", "Food/Ingredient_Patty", "Food/Ingredient_Cheese", "Food/Ingredient_Lettuce",
            "Food/Ingredient_Tomato", "Food/Ingredient_BunTop", "Food/Ingredient_Pickle", "Food/Food_Potato", "Food/Food_FriesBatch",
            "Food/Food_FriesBox", "Food/Food_MilkshakeCup", "Kitchen/BurgerBoard", "Kitchen/Station_Grill", "Kitchen/Station_Fryer",
            "Kitchen/Station_FriesBox", "Kitchen/Station_Drinks", "Kitchen/ServingTray", "Kitchen/Counter_2m",
            "Characters/Character_Benny", "Characters/Character_Penny", "Characters/Mascot_Benny",
            "UI/UI_TrayButton", "UI/UI_ActivityCard", "UI/UI_StickerSlot",
            "Environment/Prop_Table", "Environment/Prop_Stool", "Environment/Prop_Plant", "Environment/Prop_Sign"
        };

        [MenuItem(LittleBurgerWorldSetup.MenuRoot + "Validate Project", priority = 10)]
        public static void ValidateMenu()
        {
            List<string> problems = Validate(out string report);
            Debug.Log(report);
            EditorUtility.DisplayDialog("Little Burger World Validation",
                problems.Count == 0 ? "All checks passed." : $"{problems.Count} problem(s) found. See the Console.", "OK");
        }

        /// <summary>Batch entry: exits 1 when problems are found.</summary>
        public static void ValidateBatch()
        {
            List<string> problems = Validate(out string report);
            Debug.Log(report);
            File.WriteAllText("Logs/validation_report.txt", report);
            if (problems.Count > 0) EditorApplication.Exit(1);
        }

        public static List<string> Validate(out string report)
        {
            var problems = new List<string>();
            var warnings = new List<string>();
            var sb = new StringBuilder("[LBW Validate] Little Burger World validation report\n");

            // Scenes ---------------------------------------------------------------
            string[] scenes = { SceneBuilder.BootPath, SceneBuilder.MenuPath, SceneBuilder.RestaurantPath };
            foreach (string s in scenes)
            {
                if (!File.Exists(s)) problems.Add("Missing scene " + s);
                else if (!EditorBuildSettings.scenes.Any(b => b.path == s && b.enabled)) problems.Add("Scene not enabled in Build Settings: " + s);
            }
            if (EditorBuildSettings.scenes.Length > 0 && EditorBuildSettings.scenes[0].path != SceneBuilder.BootPath)
                problems.Add("Boot scene must be first in Build Settings.");

            // Prefabs --------------------------------------------------------------
            foreach (string p in s_RequiredPrefabs)
            {
                string path = $"{EditorAssetUtil.Root}/Prefabs/{p}.prefab";
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) { problems.Add("Missing prefab " + path); continue; }
                CheckObjectTree(go, path, problems, warnings);
            }

            // Config & ScriptableObjects --------------------------------------------
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(DataBuilder.ConfigPath);
            if (cfg == null) problems.Add("Missing Resources/LBW_GameConfig.asset");
            else ValidateConfig(cfg, problems, warnings);

            // Layers -----------------------------------------------------------------
            if (LayerMask.LayerToName(FoodItem.InteractableLayer) != "Interactable")
                problems.Add($"Layer {FoodItem.InteractableLayer} must be named 'Interactable'.");

            // Android build config ----------------------------------------------------
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP) problems.Add("Android scripting backend is not IL2CPP.");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0) problems.Add("Android ARM64 is not enabled.");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) == 0) problems.Add("Android ARMv7 (32-bit devices) is not enabled.");
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (!apis.Contains(UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)) problems.Add("OpenGLES3 must be kept as a Graphics API fallback.");
            var filter = PlayerSettings.Android.androidVulkanDeviceFilterListAsset;
            if (filter == null) problems.Add("No Vulkan Device Filter Lists asset assigned (PowerVR crash protection).");
            else
            {
                string yaml = File.ReadAllText(AssetDatabase.GetAssetPath(filter));
                if (!yaml.Contains(LittleBurgerWorldSetup.PowerVrPattern)) problems.Add("Vulkan deny list does not contain " + LittleBurgerWorldSetup.PowerVrPattern);
            }
            if (PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown) problems.Add("Portrait orientation is allowed.");
            if (!PlayerSettings.allowedAutorotateToLandscapeLeft && !PlayerSettings.allowedAutorotateToLandscapeRight) problems.Add("No landscape orientation allowed.");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != LittleBurgerWorldSetup.BundleId) warnings.Add("Unexpected Android application id.");

            // Scenes contents ------------------------------------------------------------
            if (problems.Count == 0 || File.Exists(SceneBuilder.RestaurantPath)) ValidateScenes(scenes, cfg, problems, warnings);

            sb.AppendLine(problems.Count == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({problems.Count} problem(s))");
            foreach (string p in problems) sb.AppendLine("  ERROR   " + p);
            foreach (string w in warnings) sb.AppendLine("  WARNING " + w);
            report = sb.ToString();
            return problems;
        }

        private static void ValidateConfig(GameConfig cfg, List<string> problems, List<string> warnings)
        {
            if (cfg.audioLibrary == null) problems.Add("GameConfig.audioLibrary missing");
            if (cfg.localization == null) problems.Add("GameConfig.localization missing");
            foreach (var (m, n) in new[] { (cfg.sparkleMaterial, "sparkleMaterial"), (cfg.softParticleMaterial, "softParticleMaterial"),
                         (cfg.confettiMaterial, "confettiMaterial"), (cfg.heartMaterial, "heartMaterial"), (cfg.highlightRingMaterial, "highlightRingMaterial"),
                         (cfg.balloonMaterial, "balloonMaterial") })
            {
                if (m == null) problems.Add("GameConfig." + n + " missing");
            }

            string[] required = { "bun_bottom", "patty_raw", "patty", "cheese", "lettuce", "tomato", "bun_top" };
            foreach (string id in required) if (cfg.FindIngredient(id) == null) problems.Add("Missing ingredient " + id);
            var ids = new HashSet<string>();
            foreach (IngredientData i in cfg.ingredients)
            {
                if (i == null) { problems.Add("Null ingredient in config"); continue; }
                if (!ids.Add(i.id)) problems.Add("Duplicate ingredient id " + i.id);
                if (i.prefab == null) problems.Add($"Ingredient {i.id} has no prefab");
                else if (i.prefab.GetComponent<FoodItem>() == null) problems.Add($"Ingredient {i.id} prefab has no FoodItem");
                if (i.icon == null) problems.Add($"Ingredient {i.id} has no icon");
                if (i.stackHeight <= 0f) problems.Add($"Ingredient {i.id} has no stack height");
            }
            if (!cfg.ingredients.Any(i => i != null && i.completesBurger)) problems.Add("No ingredient completes a burger (top bun).");

            if (cfg.FindRecipe("burger") == null) problems.Add("Missing recipe 'burger' (Basic Burger)");
            if (cfg.FindRecipe("cheeseburger") == null) problems.Add("Missing recipe 'cheeseburger'");
            foreach (RecipeData r in cfg.recipes)
            {
                if (r == null || r.ingredients.Count < 2 || r.ingredients.Any(x => x == null)) problems.Add("Invalid recipe " + (r != null ? r.id : "null"));
                else if (!r.ingredients[r.ingredients.Count - 1].completesBurger) warnings.Add($"Recipe {r.id} does not end with a top bun");
            }
            if (cfg.drinks.Count < 3) problems.Add("Expected 3 milkshake flavours");
            if (cfg.FindCharacter("benny") == null) problems.Add("Missing Benny the Bear character");
            foreach (CharacterData c in cfg.characters)
            {
                if (c == null || c.prefab == null || c.prefab.GetComponent<AnimalCustomerController>() == null)
                    problems.Add("Invalid character " + (c != null ? c.id : "null"));
            }
            if (cfg.activities.Count < 6) problems.Add("Expected 6 guided activities");
            foreach (ActivityData a in cfg.activities)
            {
                if (a == null || a.steps.Count == 0) problems.Add("Empty activity " + (a != null ? a.id : "null"));
                else if (a.thumbnail == null) problems.Add($"Activity {a.id} has no thumbnail");
            }
            foreach (var st in cfg.stickers) if (st == null || st.sprite == null) problems.Add("Sticker without sprite");

            if (cfg.audioLibrary != null)
            {
                if (cfg.audioLibrary.menuMusic == null || cfg.audioLibrary.restaurantMusic == null) problems.Add("Music clips missing");
                foreach (Audio.SfxId id in System.Enum.GetValues(typeof(Audio.SfxId)))
                {
                    if (id == Audio.SfxId.None) continue;
                    var e = cfg.audioLibrary.Get(id);
                    if (e == null || e.clips.Length == 0 || e.clips.Any(c => c == null)) problems.Add("No clip for sound " + id);
                }
                // Thai text needs the Mitr fallback on the UI font.
                var fontImp = AssetImporter.GetAtPath(PrefabBuilder.FontPath) as TrueTypeFontImporter;
                if (fontImp == null || fontImp.fontReferences == null || !fontImp.fontReferences.Any(f => f != null && f.name.Contains("Mitr")))
                    problems.Add("UI font has no Thai fallback (Mitr) – Thai text would be invisible");

                // Every spoken step needs a clip in every language (Tools/voice/generate_voice.sh).
                var languages = cfg.localization != null ? cfg.localization.languages : new List<string> { "en" };
                var voiceKeys = new HashSet<string>(cfg.activities.Where(a => a != null).SelectMany(a => a.steps)
                    .Where(st => !string.IsNullOrEmpty(st.voiceKey)).Select(st => st.voiceKey)) { "well_done" };
                foreach (string key in voiceKeys)
                    foreach (string lang in languages)
                    {
                        var v = cfg.audioLibrary.GetVoice(key, lang);
                        if (v == null || v.clip == null) problems.Add($"No voice clip for '{key}' ({lang}) – run Tools/voice/generate_voice.sh");
                    }
            }
        }

        private static void ValidateScenes(string[] scenes, GameConfig cfg, List<string> problems, List<string> warnings)
        {
            string active = SceneManager.GetActiveScene().path;
            var keys = new HashSet<string> { "Customer" };
            if (cfg != null) foreach (IngredientData i in cfg.ingredients) if (i != null && i.showInTray) keys.Add("Tray." + i.id);

            foreach (string path in scenes)
            {
                if (!File.Exists(path)) continue;
                Scene s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in s.GetRootGameObjects())
                {
                    CheckObjectTree(root, path, problems, warnings);
                    foreach (SceneKey k in root.GetComponentsInChildren<SceneKey>(true)) keys.Add(k.Key);
                }
                if (path == SceneBuilder.RestaurantPath)
                {
                    if (Object.FindFirstObjectByType<Camera>() == null) problems.Add("Restaurant has no camera");
                    if (Object.FindFirstObjectByType<Cameras.IsometricCameraController>() == null) problems.Add("Restaurant camera lacks IsometricCameraController");
                    if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null) problems.Add("Restaurant has no EventSystem");
                    foreach (System.Type t in new[] { typeof(InputHandling.InputManager), typeof(Interaction.InteractionManager), typeof(FoodManager),
                                 typeof(Kitchen.KitchenManager), typeof(CustomerManager), typeof(ActivityManager), typeof(UI.UIManager), typeof(RestaurantController) })
                    {
                        if (Object.FindFirstObjectByType(t) == null) problems.Add("Restaurant is missing system " + t.Name);
                    }
                }
            }

            if (cfg != null)
            {
                foreach (ActivityData a in cfg.activities)
                {
                    if (a == null) continue;
                    for (int i = 0; i < a.steps.Count; i++)
                    {
                        ActivityStep st = a.steps[i];
                        foreach (string key in new[] { st.targetKey, st.sourceKey })
                        {
                            if (!string.IsNullOrEmpty(key) && !keys.Contains(key))
                                problems.Add($"Activity {a.id} step {i} references unknown scene key '{key}'");
                        }
                        if (st.type == ActivityStepType.WaitForEvent && st.eventType == GameplayEventType.None)
                            problems.Add($"Activity {a.id} step {i} waits for no event");
                    }
                }
            }
            if (!string.IsNullOrEmpty(active) && File.Exists(active)) EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }

        /// <summary>Missing scripts and broken (dangling) object references in a hierarchy.</summary>
        private static void CheckObjectTree(GameObject root, string context, List<string> problems, List<string> warnings)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missing > 0) problems.Add($"{context}: '{t.name}' has {missing} missing script(s)");
                foreach (Component c in t.GetComponents<Component>())
                {
                    if (c == null) continue;
                    if (!(c is MonoBehaviour mb) || mb.GetType().Namespace == null || !mb.GetType().Namespace.StartsWith("LittleBurgerWorld")) continue;
                    var so = new SerializedObject(c);
                    SerializedProperty p = so.GetIterator();
                    while (p.NextVisible(true))
                    {
                        if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0)
                            problems.Add($"{context}: {c.GetType().Name}.{p.propertyPath} on '{t.name}' is a broken reference");
                    }
                }
            }
        }
    }
}
