using System.IO;
using System.Linq;
using LittleBurgerWorld.Cameras;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>
    /// Renders the Restaurant (3D + HUD) and Main Menu to PNG at phone and tablet aspect ratios,
    /// with a sample burger, fries, milkshake and Benny placed for composition review.
    /// Batch: -executeMethod LittleBurgerWorld.EditorTools.ScreenshotTool.CaptureBatch
    /// </summary>
    public static class ScreenshotTool
    {
        public const string OutFolder = "Logs/Screenshots";

        private static readonly (string name, int w, int h)[] s_Sizes =
        {
            ("16x9", 1920, 1080),
            ("19.5x9", 2340, 1080),
            ("4x3", 2048, 1536)
        };

        [MenuItem(LittleBurgerWorldSetup.MenuRoot + "Capture Layout Screenshots", priority = 40)]
        public static void CaptureBatch()
        {
            Directory.CreateDirectory(OutFolder);
            EditorSceneManager.OpenScene(SceneBuilder.RestaurantPath, OpenSceneMode.Single);
            DynamicGI.UpdateEnvironment();
            PopulateSample();
            foreach (var s in s_Sizes) Capture($"restaurant_{s.name}", s.w, s.h);
            EditorSceneManager.OpenScene(SceneBuilder.MenuPath, OpenSceneMode.Single);
            DynamicGI.UpdateEnvironment();
            foreach (var s in s_Sizes) Capture($"menu_{s.name}", s.w, s.h);
            Debug.Log("[LBW Shots] Screenshots written to " + Path.GetFullPath(OutFolder));
        }

        private static void PopulateSample()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(DataBuilder.ConfigPath);
            var board = Object.FindFirstObjectByType<BurgerAssemblyController>();
            if (cfg != null && board != null)
            {
                Transform b = board.transform.Find("StackBase");
                float y = 0f;
                foreach (string id in new[] { "bun_bottom", "patty", "cheese", "lettuce", "tomato", "bun_top" })
                {
                    IngredientData d = cfg.FindIngredient(id);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(d.prefab);
                    go.transform.position = b.position + Vector3.up * y;
                    go.transform.rotation = Quaternion.Euler(0f, id == "cheese" ? 45f : 0f, 0f);
                    var cook = go.GetComponent<CookableVisual>();
                    if (cook != null) cook.SetCookAmount(1f);
                    y += d.stackHeight;
                }
                var grill = Object.FindFirstObjectByType<Kitchen.GrillStation>();
                if (grill != null)
                {
                    var raw = (GameObject)PrefabUtility.InstantiatePrefab(cfg.FindIngredient("patty_raw").prefab);
                    raw.transform.position = grill.transform.Find("SpotA").position;
                    raw.GetComponent<CookableVisual>()?.SetCookAmount(0.3f);
                }
                CharacterData benny = cfg.FindCharacter("benny");
                var cust = (GameObject)PrefabUtility.InstantiatePrefab(benny.prefab);
                cust.transform.position = GameObject.Find("CustomerPaths/ServePoint").transform.position;
                Vector3 face = GameObject.Find("CustomerPaths/FaceTowards").transform.position - cust.transform.position;
                face.y = 0f;
                cust.transform.rotation = Quaternion.LookRotation(face);
                Transform bubble = cust.transform.Find("RequestBubble");
                if (bubble != null)
                {
                    bubble.gameObject.SetActive(true);
                    var icon = bubble.Find("Icon0").GetComponent<SpriteRenderer>();
                    icon.sprite = cfg.FindIcon("cheeseburger");
                    bubble.rotation = Camera.main.transform.rotation;
                }
                var trayGo = Object.FindFirstObjectByType<ServingTray>();
                if (trayGo != null)
                {
                    var fries = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.FoodFolder}/Food_FriesBox.prefab"));
                    fries.transform.position = trayGo.transform.Find("Slots/Fries").position;
                    var shake = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.FoodFolder}/Food_MilkshakeCup.prefab"));
                    shake.transform.position = trayGo.transform.Find("Slots/Drink").position;
                }
            }
            // Fill the ingredient tray with buttons so the HUD looks like it does in play.
            var trayUi = Object.FindFirstObjectByType<UI.IngredientTrayUI>();
            if (trayUi != null && cfg != null)
            {
                var content = trayUi.transform.Find("Content");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.UiFolder}/UI_TrayButton.prefab");
                foreach (IngredientData d in cfg.ingredients)
                {
                    if (!d.showInTray || !d.unlockedByDefault) continue;
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(prefab, content);
                    b.transform.Find("Background/Icon").GetComponent<UnityEngine.UI.Image>().sprite = d.icon;
                    b.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(d.color, Color.white, 0.65f);
                }
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
            }
        }

        public static void Capture(string name, int w, int h)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            bool noAa = System.Environment.GetCommandLineArgs().Contains("-lbw-noaa");
            bool noUi = System.Environment.GetCommandLineArgs().Contains("-lbw-noui");
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = noAa ? 1 : 4 };
            cam.targetTexture = rt;
            cam.aspect = w / (float)h;
            var iso = cam.GetComponent<IsometricCameraController>();
            if (iso != null) iso.ApplyForAspect(w / (float)h);

            // Render UI canvases through the camera for the capture.
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!noUi && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay && canvases[i].isRootCanvas)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = cam;
                    canvases[i].planeDistance = cam.nearClipPlane + 0.5f;
                }
            }
            // Hide overlays that start hidden at runtime.
            foreach (string path in new[] { "HUD/ActivitySelect", "HUD/Celebration", "HUD/StickerToast", "HUD/HintLayer", "HUD/ReplayButton", "HUD/ActivitiesButton", "HUD/PromptBubble" })
            {
                GameObject g = GameObject.Find(path);
                if (g != null) g.SetActive(false);
            }
            Canvas.ForceUpdateCanvases();
            // Warm-up renders: the first frames after opening a scene in batch mode can render
            // SRP-batched materials before their constant buffers are uploaded.
            for (int i = 0; i < 4; i++) cam.Render();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes($"{OutFolder}/{name}.png", tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
            Object.DestroyImmediate(tex);
            rt.Release();
        }
    }
}
