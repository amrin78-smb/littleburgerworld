using System.Collections.Generic;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Cameras;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.InputHandling;
using LittleBurgerWorld.Interaction;
using LittleBurgerWorld.Kitchen;
using LittleBurgerWorld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using A = LittleBurgerWorld.EditorTools.ArtLibrary;
using E = LittleBurgerWorld.EditorTools.EditorAssetUtil;
using P = LittleBurgerWorld.EditorTools.PrefabBuilder;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>Generates the three game scenes. Scenes are fully generated, so re-running rebuilds them.</summary>
    public static class SceneBuilder
    {
        public const string SceneFolder = E.Root + "/Scenes";
        public const string BootPath = SceneFolder + "/Boot.unity";
        public const string MenuPath = SceneFolder + "/MainMenu.unity";
        public const string RestaurantPath = SceneFolder + "/Restaurant.unity";

        public static readonly Color SkyColor = E.Hex("#CFE8F7");

        // ------------------------------------------------------------------ shared
        private static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static GameObject Instantiate(string prefabPath, Transform parent, Vector3 pos, Vector3? euler = null, Vector3? scale = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[LBW Setup] Missing prefab " + prefabPath);
                return new GameObject("MISSING_" + prefabPath);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler ?? Vector3.zero;
            go.transform.localScale = scale ?? Vector3.one;
            return go;
        }

        private static void Lighting(Vector3 sunEuler, float intensity = 1.15f, bool shadows = true, float ambient = 1f)
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.95f, 0.88f);
            l.intensity = intensity;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.55f;
            l.shadowBias = 0.03f;
            l.shadowNormalBias = 0.3f;
            sun.transform.eulerAngles = sunEuler;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.86f, 0.89f, 0.96f) * ambient;
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.82f, 0.78f) * ambient;
            RenderSettings.ambientGroundColor = new Color(0.62f, 0.56f, 0.52f) * ambient;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        private static Camera MakeCamera(string name, bool ortho)
        {
            var go = new GameObject(name);
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.orthographic = ortho;
            go.AddComponent<AudioListener>();
            return cam;
        }

        // ------------------------------------------------------------------ BOOT
        public static void BuildBoot()
        {
            NewScene();
            Camera cam = MakeCamera("Main Camera", true);
            cam.backgroundColor = E.Hex("#FFEFD8");
            Canvas canvas = UIFactory.Canvas("BootCanvas", 0);
            RectTransform logo = UIFactory.Rect("Logo", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(900f, 600f));
            UIFactory.Image("Burger", logo, IconLibrary.Get("logo_burger"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(360f, 360f), false);
            UIFactory.Text("Title", logo, "Little Burger World", 110, E.Hex("#F5A623"), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(1400f, 160f));
            var boot = new GameObject("BootLoader").AddComponent<BootLoader>();
            E.Set(boot, "m_Logo", logo);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), BootPath);
        }

        // ------------------------------------------------------------------ MAIN MENU
        public static void BuildMainMenu()
        {
            NewScene();
            Lighting(new Vector3(40f, 150f, 0f), 1.1f);
            Camera cam = MakeCamera("Main Camera", false);
            cam.fieldOfView = 32f;
            cam.transform.position = new Vector3(0f, 2.6f, -11f);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.6f, 0f) - cam.transform.position);

            Transform world = new GameObject("Backdrop").transform;
            P.Part(world, "Ground", A.Disc(7.5f, 0.3f, 0.12f, 48), A.MintLight, new Vector3(0f, -0.3f, 0f));
            P.Part(world, "Stage", A.Disc(1.6f, 0.2f, 0.08f, 40), A.CheeseYellow, new Vector3(-2.9f, -0.05f, 0.5f));
            P.Part(world, "Stage2", A.Disc(1.6f, 0.2f, 0.08f, 40), A.SoftPink, new Vector3(2.9f, -0.05f, 0.5f));
            GameObject mascot = Instantiate($"{P.CharFolder}/Mascot_Benny.prefab", world, new Vector3(-2.9f, 0.15f, 0.5f), new Vector3(0f, 160f, 0f), Vector3.one * 1.15f);

            // Giant showcase burger made from the real ingredient prefabs
            Transform burger = new GameObject("GiantBurger").transform;
            burger.SetParent(world, false);
            burger.localPosition = new Vector3(2.9f, 0.15f, 0.5f);
            burger.localScale = Vector3.one * 1.7f;
            burger.localEulerAngles = new Vector3(0f, 20f, 0f);
            float y = 0f;
            foreach (var (prefab, h, yaw) in new[] { ("Ingredient_BunBottom", 0.26f, 0f), ("Ingredient_Patty", 0.2f, 30f), ("Ingredient_Cheese", 0.055f, 45f),
                         ("Ingredient_Lettuce", 0.07f, 10f), ("Ingredient_Tomato", 0.08f, 70f), ("Ingredient_BunTop", 0.48f, 0f) })
            {
                GameObject layer = Instantiate($"{P.FoodFolder}/{prefab}.prefab", burger, new Vector3(0f, y, 0f), new Vector3(0f, yaw, 0f));
                StripGameplay(layer);
                if (prefab == "Ingredient_Patty") E.Set(layer.GetComponent<CookableVisual>(), "m_InitialAmount", 1f);
                y += h;
            }
            var bob = burger.gameObject.AddComponent<MenuBobber>();
            E.Set(bob, "m_Amplitude", 0.12f);
            Instantiate($"{P.EnvFolder}/Prop_Plant.prefab", world, new Vector3(-5.3f, 0f, 2.2f));
            Instantiate($"{P.EnvFolder}/Prop_Plant.prefab", world, new Vector3(5.4f, 0f, 2.4f), new Vector3(0f, 90f, 0f));
            Instantiate($"{P.EnvFolder}/Prop_Table.prefab", world, new Vector3(0f, 0f, 2.6f));

            UIFactory.EventSystem();
            Canvas canvas = UIFactory.Canvas("MenuCanvas", 10);
            Transform root = canvas.transform;

            RectTransform logo = UIFactory.Rect("Logo", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1300f, 220f));
            UIFactory.Image("Burger", logo, IconLibrary.Get("logo_burger"), Color.white, new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(220f, 220f), false);
            UIFactory.Text("Title", logo, "Little Burger World", 118, E.Hex("#F5A623"), new Vector2(0.5f, 0.5f), new Vector2(90f, 0f), new Vector2(1100f, 200f));

            SquishyButton guided = UIFactory.BigButton("LetsPlayButton", root, IconLibrary.Get("icon_play"), "LET'S PLAY!", "lets_play",
                E.Hex("#E96A52"), new Vector2(0.5f, 0f), new Vector2(-345f, 190f), new Vector2(640f, 230f));
            guided.IdleBounce = true;
            E.Set(guided, "m_IdleBounce", true);
            SquishyButton free = UIFactory.BigButton("FreePlayButton", root, IconLibrary.Get("icon_star"), "FREE PLAY", "free_play",
                E.Hex("#79BFA0"), new Vector2(0.5f, 0f), new Vector2(345f, 190f), new Vector2(640f, 230f));
            SquishyButton stickers = UIFactory.RoundButton("StickersButton", root, IconLibrary.Get("icon_stickers"), E.Hex("#B9A6E8"),
                new Vector2(0f, 0f), new Vector2(130f, 150f), 180f);
            SquishyButton parent = UIFactory.RoundButton("ParentButton", root, IconLibrary.Get("icon_gear"), new Color(1f, 1f, 1f, 0.85f),
                new Vector2(1f, 1f), new Vector2(-80f, -80f), 100f, 0.7f);

            ParentGate gate = BuildParentGate(root);
            ParentSettingsUI settings = BuildParentSettings(root);
            StickerBookUI book = BuildStickerBook(root);

            var menu = canvas.gameObject.AddComponent<MainMenuUI>();
            E.Set(menu, "m_GuidedButton", guided);
            E.Set(menu, "m_FreePlayButton", free);
            E.Set(menu, "m_StickersButton", stickers);
            E.Set(menu, "m_ParentButton", parent);
            E.Set(menu, "m_Gate", gate);
            E.Set(menu, "m_Settings", settings);
            E.Set(menu, "m_StickerBook", book);
            E.Set(menu, "m_Logo", logo);
            E.Set(menu, "m_Mascot", mascot.GetComponent<ProceduralCharacterRig>());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MenuPath);
        }

        private static void StripGameplay(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var f in go.GetComponentsInChildren<FoodItem>()) Object.DestroyImmediate(f);
        }

        private static Image Overlay(Transform parent, string name, Vector2 panelSize, Color panelColor, out RectTransform panel)
        {
            RectTransform root = UIFactory.Stretch(name, parent);
            Image dim = UIFactory.Dim(root, 0.5f);
            panel = UIFactory.Panel("Panel", root, panelColor, new Vector2(0.5f, 0.5f), Vector2.zero, panelSize).rectTransform;
            return dim;
        }

        private static ParentGate BuildParentGate(Transform root)
        {
            Overlay(root, "ParentGate", new Vector2(980f, 620f), E.Hex("#FFF6EA"), out RectTransform panel);
            Text q = UIFactory.Text("Question", panel, "Grown-ups only!", 54, E.Hex("#6B4630"), new Vector2(0.5f, 1f), new Vector2(0f, -150f),
                new Vector2(880f, 220f), TextAnchor.MiddleCenter, false);
            var answers = new Object[3];
            var labels = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                SquishyButton b = UIFactory.RoundButton("Answer" + i, panel, null, i == 0 ? E.Hex("#83B6E8") : i == 1 ? E.Hex("#79BFA0") : E.Hex("#F5A623"),
                    new Vector2(0.5f, 0f), new Vector2(-260f + i * 260f, 170f), 190f);
                labels[i] = UIFactory.Text("Label", b.transform, "0", 84, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f));
                answers[i] = b;
            }
            SquishyButton close = UIFactory.RoundButton("Close", panel, IconLibrary.Get("icon_close"), new Color(1, 1, 1, 0), new Vector2(1f, 1f),
                new Vector2(-30f, -30f), 110f, 1f);
            var gate = panel.parent.gameObject.AddComponent<ParentGate>();
            E.Set(gate, "m_Question", q);
            E.SetArray(gate, "m_Answers", answers);
            E.SetArray(gate, "m_AnswerLabels", labels);
            E.Set(gate, "m_Close", close);
            panel.parent.gameObject.SetActive(false);
            return gate;
        }

        private static Slider MakeSlider(string name, Transform parent, Vector2 pos, Color fill)
        {
            RectTransform rt = UIFactory.Rect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(560f, 70f));
            Image bg = UIFactory.Image("Background", rt, IconLibrary.Get("ui_panel"), E.Hex("#EADCC8"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 34f), true, true);
            RectTransform fillArea = UIFactory.Rect("Fill Area", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fillArea.offsetMin = new Vector2(10f, 18f);
            fillArea.offsetMax = new Vector2(-10f, -18f);
            Image f = UIFactory.Image("Fill", fillArea, IconLibrary.Get("ui_panel"), fill, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false, true);
            UIFactory.FillImage(f);
            RectTransform handleArea = UIFactory.Rect("Handle Slide Area", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(30f, 0f);
            handleArea.offsetMax = new Vector2(-30f, 0f);
            Image h = UIFactory.Image("Handle", handleArea, IconLibrary.Get("ui_circle"), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
            h.rectTransform.anchorMin = new Vector2(0f, 0f);
            h.rectTransform.anchorMax = new Vector2(0f, 1f);
            h.rectTransform.sizeDelta = new Vector2(70f, 0f);
            var outline = h.gameObject.AddComponent<Outline>();
            outline.effectColor = E.Hex("#6B4630");
            outline.effectDistance = new Vector2(3f, -3f);
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = f.rectTransform;
            s.handleRect = h.rectTransform;
            s.targetGraphic = h;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = 0.8f;
            return s;
        }

        private static ParentSettingsUI BuildParentSettings(Transform root)
        {
            Overlay(root, "ParentSettings", new Vector2(1400f, 920f), E.Hex("#FFF6EA"), out RectTransform panel);
            Color ink = E.Hex("#6B4630");
            UIFactory.Text("Title", panel, "Parent Settings", 64, ink, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(1000f, 100f), TextAnchor.MiddleCenter, false, "settings_title");

            void Row(string icon, string label, string key, float y)
            {
                UIFactory.Image(label + "Icon", panel, IconLibrary.Get(icon), Color.white, new Vector2(0.5f, 1f), new Vector2(-560f, y), new Vector2(90f, 90f), false);
                UIFactory.Text(label + "Label", panel, label, 44, ink, new Vector2(0.5f, 1f), new Vector2(-330f, y), new Vector2(340f, 80f), TextAnchor.MiddleLeft, false, key);
            }
            Row("icon_music", "Music", "music", -200f);
            Slider music = MakeSlider("MusicSlider", panel, new Vector2(230f, -200f), E.Hex("#B9A6E8"));
            Row("icon_speaker", "Sounds", "sounds", -310f);
            Slider sfx = MakeSlider("SfxSlider", panel, new Vector2(230f, -310f), E.Hex("#83B6E8"));
            Row("icon_voice", "Voice", "voice", -420f);
            Slider voice = MakeSlider("VoiceSlider", panel, new Vector2(150f, -420f), E.Hex("#79BFA0"));

            // Voice prompts toggle
            RectTransform tRt = UIFactory.Rect("VoicePromptsToggle", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(540f, -420f), new Vector2(90f, 90f));
            Image tBg = UIFactory.Image("Background", tRt, IconLibrary.Get("ui_circle"), E.Hex("#EADCC8"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            Image check = UIFactory.Image("Checkmark", tBg.transform, IconLibrary.Get("icon_check"), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f), false);
            var toggle = tRt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = tBg;
            toggle.graphic = check;
            toggle.isOn = true;

            Row("icon_language", "Language", "language", -530f);
            SquishyButton lang = UIFactory.BigButton("LanguageButton", panel, IconLibrary.Get("icon_language"), "English", null, E.Hex("#83B6E8"),
                new Vector2(0.5f, 1f), new Vector2(230f, -530f), new Vector2(460f, 100f));
            Text langLabel = lang.transform.Find("Body/Label").GetComponent<Text>();

            HoldButton reset = UIFactory.HoldButton("ResetProgress", panel, IconLibrary.Get("icon_replay"), E.Hex("#E96A52"), new Vector2(0.5f, 1f),
                new Vector2(-560f, -660f), 100f);
            UIFactory.Text("ResetLabel", panel, "Hold to reset progress", 40, ink, new Vector2(0.5f, 1f), new Vector2(-180f, -660f),
                new Vector2(620f, 80f), TextAnchor.MiddleLeft, false, "reset_progress");
            Text resetDone = UIFactory.Text("ResetDone", panel, "", 36, E.Hex("#4F9E7B"), new Vector2(0.5f, 1f), new Vector2(-180f, -720f),
                new Vector2(620f, 60f), TextAnchor.MiddleLeft, false);

            SquishyButton privacyBtn = UIFactory.BigButton("PrivacyButton", panel, IconLibrary.Get("icon_shield"), "Privacy", "privacy", E.Hex("#79BFA0"),
                new Vector2(0.5f, 1f), new Vector2(400f, -680f), new Vector2(380f, 110f));

            SquishyButton close = UIFactory.RoundButton("Close", panel, IconLibrary.Get("icon_close"), new Color(1, 1, 1, 0), new Vector2(1f, 1f),
                new Vector2(-30f, -30f), 110f, 1f);

            Overlay(panel.parent, "PrivacyPanel", new Vector2(1200f, 760f), Color.white, out RectTransform pPanel);
            UIFactory.Image("Shield", pPanel, IconLibrary.Get("icon_shield"), Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(120f, 120f), false);
            UIFactory.Text("PrivacyText", pPanel, "", 38, ink, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1060f, 480f),
                TextAnchor.MiddleCenter, false, "privacy_text");
            SquishyButton pClose = UIFactory.RoundButton("Close", pPanel, IconLibrary.Get("icon_close"), new Color(1, 1, 1, 0), new Vector2(1f, 1f),
                new Vector2(-30f, -30f), 110f, 1f);
            pPanel.parent.gameObject.SetActive(false);

            var ui = panel.parent.gameObject.AddComponent<ParentSettingsUI>();
            E.Set(ui, "m_Music", music);
            E.Set(ui, "m_Sfx", sfx);
            E.Set(ui, "m_Voice", voice);
            E.Set(ui, "m_VoicePrompts", toggle);
            E.Set(ui, "m_Language", lang);
            E.Set(ui, "m_LanguageLabel", langLabel);
            E.Set(ui, "m_ResetProgress", reset);
            E.Set(ui, "m_ResetDone", resetDone);
            E.Set(ui, "m_PrivacyButton", privacyBtn);
            E.Set(ui, "m_PrivacyPanel", pPanel.parent.gameObject);
            E.Set(ui, "m_PrivacyClose", pClose);
            E.Set(ui, "m_Close", close);
            panel.parent.gameObject.SetActive(false);
            return ui;
        }

        private static StickerBookUI BuildStickerBook(Transform root)
        {
            Overlay(root, "StickerBook", new Vector2(1400f, 860f), E.Hex("#F3EEFF"), out RectTransform panel);
            UIFactory.Image("Header", panel, IconLibrary.Get("icon_stickers"), Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(130f, 130f), false);
            RectTransform grid = UIFactory.Rect("Grid", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1200f, 620f));
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(200f, 200f);
            g.spacing = new Vector2(36f, 36f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 5;
            g.childAlignment = TextAnchor.MiddleCenter;
            SquishyButton close = UIFactory.RoundButton("Close", panel, IconLibrary.Get("icon_close"), new Color(1, 1, 1, 0), new Vector2(1f, 1f),
                new Vector2(-30f, -30f), 110f, 1f);
            var book = panel.parent.gameObject.AddComponent<StickerBookUI>();
            E.Set(book, "m_Grid", grid);
            E.Set(book, "m_SlotPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{P.UiFolder}/UI_StickerSlot.prefab").GetComponent<Image>());
            E.Set(book, "m_Close", close);
            panel.parent.gameObject.SetActive(false);
            return book;
        }

        // ------------------------------------------------------------------ RESTAURANT
        public static void BuildRestaurant()
        {
            NewScene();
            // Orthographic restaurant: soft blob shadows instead of realtime shadow maps (cheaper on
            // mobile, identical on every GPU). See TECHNICAL_ARCHITECTURE.md › Lighting.
            Lighting(new Vector3(52f, 25f, 0f), 0.95f, false, 0.82f);
            Camera cam = MakeCamera("Main Camera", true);
            var iso = cam.gameObject.AddComponent<IsometricCameraController>();
            iso.Configure(new Vector3(0.55f, 0.9f, 0.85f), new Vector3(11.6f, 1.8f, 7.4f), 45f, 0.1f, 0.2f);

            Transform env = new GameObject("Environment").transform;
            BuildRoom(env);

            Transform kitchen = new GameObject("Kitchen").transform;
            GameObject backCounter = P.CounterObject("Counter_Back", 7.6f, A.Mint);
            backCounter.transform.SetParent(kitchen, false);
            backCounter.transform.localPosition = new Vector3(-0.1f, 0f, 3.35f);
            GameObject island = P.CounterObject("Counter_Island", 4.8f, A.CheeseYellow);
            island.transform.SetParent(kitchen, false);
            island.transform.localPosition = new Vector3(-1.35f, 0f, 0.2f);
            GameObject serve = P.CounterObject("Counter_Serve", 3.6f, A.SoftPink);
            serve.transform.SetParent(kitchen, false);
            serve.transform.localPosition = new Vector3(2.45f, 0f, -1.3f);
            serve.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

            float top = P.CounterHeight;
            GameObject fryer = Instantiate($"{P.KitchenFolder}/Station_Fryer.prefab", kitchen, new Vector3(-2.45f, top, 3.3f));
            GameObject friesBox = Instantiate($"{P.KitchenFolder}/Station_FriesBox.prefab", kitchen, new Vector3(-0.55f, top, 3.25f));
            GameObject drinks = Instantiate($"{P.KitchenFolder}/Station_Drinks.prefab", kitchen, new Vector3(1.95f, top, 3.3f));
            GameObject grill = Instantiate($"{P.KitchenFolder}/Station_Grill.prefab", kitchen, new Vector3(-2.55f, top, 0.2f));
            GameObject board = Instantiate($"{P.KitchenFolder}/BurgerBoard.prefab", kitchen, new Vector3(0.05f, top, 0.2f), new Vector3(0f, 90f, 0f));
            GameObject tray = Instantiate($"{P.KitchenFolder}/ServingTray.prefab", kitchen, new Vector3(2.45f, top, -1.85f), new Vector3(0f, 90f, 0f));

            // Free counter space for experimenting
            AddFreeSurface(kitchen, "FreeSurface_Island", new Vector3(-1.35f, 0f, 0.2f), new Vector2(4.6f, 1.1f), 0f);
            AddFreeSurface(kitchen, "FreeSurface_Back", new Vector3(-0.1f, 0f, 3.3f), new Vector2(7.4f, 1.1f), 0f);
            AddFreeSurface(kitchen, "FreeSurface_Serve", new Vector3(2.45f, 0f, -1.3f), new Vector2(3.4f, 1.1f), 90f);

            // Systems
            Transform systems = new GameObject("Systems").transform;
            var input = systems.gameObject.AddComponent<InputManager>();
            var interaction = systems.gameObject.AddComponent<InteractionManager>();
            E.Set(interaction, "m_Camera", cam);
            E.Set(interaction, "m_Input", input);
            var so = new SerializedObject(interaction);
            so.FindProperty("m_InteractableMask").intValue = 1 << FoodItem.InteractableLayer;
            so.ApplyModifiedPropertiesWithoutUndo();
            Transform foodRoot = new GameObject("Food").transform;
            var food = systems.gameObject.AddComponent<FoodManager>();
            E.Set(food, "m_FoodRoot", foodRoot);
            var km = systems.gameObject.AddComponent<KitchenManager>();
            E.Set(km, "m_Board", board.GetComponent<BurgerAssemblyController>());
            E.Set(km, "m_Grill", grill.GetComponent<GrillStation>());
            E.Set(km, "m_Fryer", fryer.GetComponent<FryStation>());
            E.Set(km, "m_FriesBox", friesBox.GetComponent<FriesBoxHolder>());
            E.Set(km, "m_Drinks", drinks.GetComponent<DrinkStation>());
            E.Set(km, "m_Tray", tray.GetComponent<ServingTray>());

            // Customers: enter through the door on the right wall, wait behind the serving counter.
            Transform paths = new GameObject("CustomerPaths").transform;
            Transform spawn = P.Node(paths, "Spawn", new Vector3(6.2f, 0f, -2.5f)).transform;
            Transform door = P.Node(paths, "Door", new Vector3(4.1f, 0f, -2.5f)).transform;
            Transform inside = P.Node(paths, "Inside", new Vector3(3.45f, 0f, -2.45f)).transform;
            Transform servePoint = P.Node(paths, "ServePoint", new Vector3(3.42f, 0f, -0.15f)).transform;
            Transform exitOut = P.Node(paths, "ExitOutside", new Vector3(6.4f, 0f, -2.5f)).transform;
            Transform face = P.Node(paths, "FaceTowards", new Vector3(1.6f, 0f, -1.6f)).transform;
            var cm = systems.gameObject.AddComponent<CustomerManager>();
            E.Set(cm, "m_SpawnPoint", spawn);
            E.SetArray(cm, "m_EnterPath", new Object[] { door, inside, servePoint });
            E.SetArray(cm, "m_ExitPath", new Object[] { inside, door, exitOut });
            E.Set(cm, "m_FaceTowards", face);
            E.Set(cm, "m_DoorHinge", GameObject.Find("Environment/WallRight/DoorHinge").transform);

            UIManager ui = BuildHud();
            var am = systems.gameObject.AddComponent<ActivityManager>();
            E.Set(am, "m_Kitchen", km);
            E.Set(am, "m_Customers", cm);
            E.Set(am, "m_UI", ui);
            var rc = systems.gameObject.AddComponent<RestaurantController>();
            E.Set(rc, "m_Kitchen", km);
            E.Set(rc, "m_Customers", cm);
            E.Set(rc, "m_Activities", am);
            E.Set(rc, "m_UI", ui);
            UIFactory.EventSystem();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), RestaurantPath);
        }

        private static void AddFreeSurface(Transform parent, string name, Vector3 pos, Vector2 size, float yaw)
        {
            GameObject go = P.Node(parent, name, pos, new Vector3(0f, yaw, 0f));
            var fs = go.AddComponent<FreeSurfaceZone>();
            E.Set(fs, "m_BoxSize", size);
            E.Set(fs, "m_DropPriority", 0);
            E.Set(fs, "m_SurfaceHeight", P.CounterHeight);
        }

        private static void BuildRoom(Transform env)
        {
            // Outside ground + garden path to the door
            P.Part(env, "Ground", A.RoundedBox(40f, 0.2f, 40f, 0.05f, 1), A.Ground, new Vector3(0f, -0.3f, 0f), castShadows: false);
            P.Part(env, "Path", A.RoundedBox(3.6f, 0.06f, 1.6f, 0.03f, 2), A.Path, new Vector3(6.1f, -0.17f, -2.5f), castShadows: false);
            GameObject floor = P.Part(env, "Floor", A.RoundedBox(8.4f, 0.2f, 8.4f, 0.06f, 2), A.Floor, new Vector3(0f, -0.1f, 0f));
            floor.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            floor.AddComponent<BoxCollider>();

            // Back wall (z = 4)
            Transform back = new GameObject("WallBack").transform;
            back.SetParent(env, false);
            P.Part(back, "Wall", A.RoundedBox(8.6f, 2.7f, 0.3f, 0.08f, 2), A.Wall, new Vector3(0f, 1.35f, 4.15f));
            P.Part(back, "Wainscot", A.RoundedBox(8.3f, 0.95f, 0.06f, 0.03f, 2), A.SoftPink, new Vector3(0f, 0.48f, 3.98f), castShadows: false);
            P.Part(back, "Trim", A.RoundedBox(8.8f, 0.2f, 0.42f, 0.08f, 2), A.CheeseYellow, new Vector3(0f, 2.72f, 4.15f));

            // Right wall (x = 4) with a door opening at z ∈ [-3.25, -1.75]
            Transform right = new GameObject("WallRight").transform;
            right.SetParent(env, false);
            P.Part(right, "WallA", A.RoundedBox(0.3f, 2.7f, 1.05f, 0.08f, 2), A.Wall, new Vector3(4.15f, 1.35f, -3.78f));
            P.Part(right, "WallB", A.RoundedBox(0.3f, 2.7f, 6.05f, 0.08f, 2), A.Wall, new Vector3(4.15f, 1.35f, 1.27f));
            P.Part(right, "Lintel", A.RoundedBox(0.3f, 0.5f, 1.6f, 0.08f, 2), A.Wall, new Vector3(4.15f, 2.45f, -2.5f));
            P.Part(right, "WainscotA", A.RoundedBox(0.06f, 0.95f, 0.95f, 0.03f, 2), A.SoftPink, new Vector3(3.98f, 0.48f, -3.78f), castShadows: false);
            P.Part(right, "WainscotB", A.RoundedBox(0.06f, 0.95f, 5.9f, 0.03f, 2), A.SoftPink, new Vector3(3.98f, 0.48f, 1.27f), castShadows: false);
            P.Part(right, "Trim", A.RoundedBox(0.42f, 0.2f, 8.8f, 0.08f, 2), A.CheeseYellow, new Vector3(4.15f, 2.72f, 0f));
            P.Part(right, "DoorFrame", A.Mesh("DoorFrame", () => MeshFactory.Combine("DoorFrame",
                    (MeshFactory.RoundedBox(new Vector3(0.36f, 2.15f, 0.14f), 0.06f, 2), Matrix4x4.Translate(new Vector3(0f, 1.07f, -0.78f))),
                    (MeshFactory.RoundedBox(new Vector3(0.36f, 2.15f, 0.14f), 0.06f, 2), Matrix4x4.Translate(new Vector3(0f, 1.07f, 0.78f))),
                    (MeshFactory.RoundedBox(new Vector3(0.36f, 0.16f, 1.7f), 0.06f, 2), Matrix4x4.Translate(new Vector3(0f, 2.18f, 0f))))),
                A.TomatoCoral, new Vector3(4.15f, 0f, -2.5f));
            Transform hinge = P.Node(right, "DoorHinge", new Vector3(4.12f, 0f, -1.78f)).transform;
            P.Part(hinge, "Door", A.RoundedBox(0.1f, 2.05f, 1.38f, 0.06f, 2), A.Mint, new Vector3(0f, 1.04f, -0.71f));
            P.Part(hinge, "Porthole", A.Disc(0.25f, 0.06f, 0.025f, 24), A.Glass, new Vector3(-0.05f, 1.45f, -0.71f), new Vector3(0f, 0f, 90f), castShadows: false);
            P.Part(hinge, "Knob", A.SphereMesh, A.CheeseYellow, new Vector3(-0.08f, 1.0f, -1.2f), null, Vector3.one * 0.12f, false);

            // Decorations
            Transform deco = new GameObject("Decorations").transform;
            deco.SetParent(env, false);
            Instantiate($"{P.EnvFolder}/Prop_Sign.prefab", deco, new Vector3(-1.45f, 2.02f, 3.96f));
            Instantiate($"{P.EnvFolder}/Prop_Clock.prefab", deco, new Vector3(-3.45f, 2.05f, 3.95f));
            Instantiate($"{P.EnvFolder}/Prop_Window.prefab", deco, new Vector3(3.97f, 1.62f, 2.15f), new Vector3(0f, 90f, 0f));
            Instantiate($"{P.EnvFolder}/Prop_Picture_Burger.prefab", deco, new Vector3(3.98f, 1.75f, 0.35f), new Vector3(0f, 90f, 0f));
            Instantiate($"{P.EnvFolder}/Prop_Picture_Fries.prefab", deco, new Vector3(3.98f, 1.75f, -0.75f), new Vector3(0f, 90f, 0f));
            Instantiate($"{P.EnvFolder}/Prop_Bunting.prefab", deco, new Vector3(3.92f, 2.5f, 1.4f), new Vector3(0f, 90f, 0f), new Vector3(1.15f, 1f, 1f));
            Instantiate($"{P.EnvFolder}/Prop_Plant.prefab", deco, new Vector3(3.45f, 0f, 3.45f));
            Instantiate($"{P.EnvFolder}/Prop_Plant.prefab", deco, new Vector3(3.5f, 0f, 0.85f), new Vector3(0f, 60f, 0f), Vector3.one * 0.85f);
            Instantiate($"{P.EnvFolder}/Prop_Plant.prefab", deco, new Vector3(5.2f, -0.2f, -4.1f), new Vector3(0f, 20f, 0f));

            // Dining area
            Transform dining = new GameObject("Dining").transform;
            dining.SetParent(env, false);
            foreach (Vector3 t in new[] { new Vector3(-2.3f, 0f, -2.4f), new Vector3(0.3f, 0f, -3.0f) })
            {
                Instantiate($"{P.EnvFolder}/Prop_Table.prefab", dining, t);
                for (int i = 0; i < 3; i++)
                {
                    float a = (i / 3f) * Mathf.PI * 2f + 0.5f;
                    Instantiate($"{P.EnvFolder}/Prop_Stool.prefab", dining, t + new Vector3(Mathf.Cos(a) * 1.05f, 0f, Mathf.Sin(a) * 1.05f));
                }
            }
        }

        private static UIManager BuildHud()
        {
            Canvas canvas = UIFactory.Canvas("HUD", 10);
            Transform root = canvas.transform;

            // Ingredient tray (bottom)
            RectTransform trayRt = UIFactory.Rect("IngredientTray", root, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 215f), new Vector2(0.5f, 0f));
            Image trayBg = UIFactory.Panel("Background", trayRt, E.Hex("#FFF1DC"), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            trayBg.rectTransform.anchorMin = Vector2.zero;
            trayBg.rectTransform.anchorMax = Vector2.one;
            trayBg.rectTransform.offsetMin = new Vector2(24f, -60f);
            trayBg.rectTransform.offsetMax = new Vector2(-24f, 0f);
            RectTransform content = UIFactory.Rect("Content", trayRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(1400f, 190f));
            var hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 22f;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = false;
            hl.childControlHeight = false;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;
            var trayUi = trayRt.gameObject.AddComponent<IngredientTrayUI>();
            E.Set(trayUi, "m_Content", content);
            E.Set(trayUi, "m_ButtonPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{P.UiFolder}/UI_TrayButton.prefab").GetComponent<IngredientTrayButton>());

            // Picture prompt bubble (top centre)
            Image bubble = UIFactory.Image("PromptBubble", root, IconLibrary.Get("ui_bubble"), Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -120f),
                new Vector2(200f, 200f), false, true);
            bubble.pixelsPerUnitMultiplier = 0.35f;
            Image promptIcon = UIFactory.Image("Icon", bubble.transform, null, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(156f, 156f), false);
            // Instruction line beside the picture (read aloud by a grown-up / future voice clip).
            Text promptText = UIFactory.Text("Text", bubble.transform, "", 54, E.Hex("#5A3A26"), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(560f, 170f), TextAnchor.MiddleLeft, false);
            promptText.resizeTextForBestFit = true;
            promptText.resizeTextMinSize = 30;
            promptText.resizeTextMaxSize = 54;
            promptText.verticalOverflow = VerticalWrapMode.Truncate;
            promptText.lineSpacing = 0.9f;
            promptText.gameObject.SetActive(false);

            // Activity picker
            RectTransform pick = UIFactory.Stretch("ActivitySelect", root);
            pick.gameObject.AddComponent<CanvasGroup>();
            UIFactory.Dim(pick, 0.45f);
            Image pickPanel = UIFactory.Panel("Panel", pick, E.Hex("#FFF6EA"), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1420f, 840f));
            UIFactory.Image("Header", pickPanel.transform, IconLibrary.Get("icon_star"), Color.white, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(150f, 150f), false);
            RectTransform grid = UIFactory.Rect("Grid", pickPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1300f, 700f));
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(400f, 320f);
            g.spacing = new Vector2(36f, 36f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            g.childAlignment = TextAnchor.MiddleCenter;
            var select = pick.gameObject.AddComponent<ActivitySelectUI>();
            E.Set(select, "m_Grid", grid);
            E.Set(select, "m_CardPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{P.UiFolder}/UI_ActivityCard.prefab").GetComponent<ActivityCard>());

            // Top bar
            HoldButton home = UIFactory.HoldButton("HomeButton", root, IconLibrary.Get("icon_home"), E.Hex("#83B6E8"), new Vector2(0f, 1f), new Vector2(100f, -100f), 130f,
                "Hold to go home", "hold_home", 0.15f);
            Image badge = UIFactory.Image("ModeBadge", root, IconLibrary.Get("icon_star"), Color.white, new Vector2(0f, 1f), new Vector2(225f, -100f), new Vector2(80f, 80f), false);
            HoldButton reset = UIFactory.HoldButton("ResetKitchenButton", root, IconLibrary.Get("icon_sparkle_clean"), E.Hex("#79BFA0"), new Vector2(1f, 1f), new Vector2(-100f, -100f), 130f,
                "Hold to tidy up", "hold_tidy", 0.85f);
            SquishyButton replay = UIFactory.RoundButton("ReplayButton", root, IconLibrary.Get("icon_replay"), E.Hex("#F5A623"), new Vector2(1f, 1f), new Vector2(-100f, -100f), 120f);
            SquishyButton list = UIFactory.RoundButton("ActivitiesButton", root, IconLibrary.Get("icon_grid"), Color.white, new Vector2(1f, 1f), new Vector2(-245f, -100f), 120f);

            // Celebration
            RectTransform celebration = UIFactory.Stretch("Celebration", root);
            var cg = celebration.gameObject.AddComponent<CanvasGroup>();
            Image cPanel = UIFactory.Panel("Panel", celebration, E.Hex("#FFF6EA"), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(760f, 560f));
            Image portrait = UIFactory.Image("Portrait", cPanel.transform, IconLibrary.Get("portrait_benny"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(300f, 300f), false);
            portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            var stars = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = UIFactory.Image("Star" + i, cPanel.transform, IconLibrary.Get("icon_star"), Color.white, new Vector2(0.5f, 0f),
                    new Vector2(-190f + i * 190f, 110f + (i == 1 ? 30f : 0f)), new Vector2(170f, 170f), false);
            }
            var cel = celebration.gameObject.AddComponent<CelebrationUI>();
            E.Set(cel, "m_Group", cg);
            E.Set(cel, "m_Panel", cPanel.rectTransform);
            E.SetArray(cel, "m_Stars", stars);
            E.Set(cel, "m_Portrait", portrait);

            // Sticker toast
            RectTransform toastRoot = UIFactory.Stretch("StickerToast", root);
            Image toastPanel = UIFactory.Image("Panel", toastRoot, IconLibrary.Get("ui_circle"), new Color(1f, 1f, 1f, 0.95f), new Vector2(1f, 1f),
                new Vector2(-230f, -300f), new Vector2(280f, 280f), false);
            Image toastSticker = UIFactory.Image("Sticker", toastPanel.transform, null, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f), false);
            var toast = toastRoot.gameObject.AddComponent<StickerToast>();
            E.Set(toast, "m_Panel", toastPanel.rectTransform);
            E.Set(toast, "m_Sticker", toastSticker);

            // Hint hand (always on top, never blocks touches)
            RectTransform hintLayer = UIFactory.Stretch("HintLayer", root);
            var hcg = hintLayer.gameObject.AddComponent<CanvasGroup>();
            hcg.blocksRaycasts = false;
            hcg.interactable = false;
            hcg.alpha = 0f;
            Image ripple = UIFactory.Image("Ripple", hintLayer, IconLibrary.Get("icon_ripple"), new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f), false);
            Image hand = UIFactory.Image("Hand", hintLayer, IconLibrary.Get("icon_hand"), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f), false);
            hand.rectTransform.pivot = new Vector2(0.36f, 0.92f);
            var hh = hintLayer.gameObject.AddComponent<HintHand>();
            E.Set(hh, "m_Hand", hand.rectTransform);
            E.Set(hh, "m_Ripple", ripple.rectTransform);
            E.Set(hh, "m_Group", hcg);
            E.Set(hh, "m_CanvasRect", hintLayer);

            var ui = canvas.gameObject.AddComponent<UIManager>();
            E.Set(ui, "m_Hand", hh);
            E.Set(ui, "m_PromptBubble", bubble.rectTransform);
            E.Set(ui, "m_PromptIcon", promptIcon);
            E.Set(ui, "m_PromptText", promptText);
            E.Set(ui, "m_Tray", trayUi);
            E.Set(ui, "m_ActivitySelect", select);
            E.Set(ui, "m_Celebration", cel);
            E.Set(ui, "m_Home", home);
            E.Set(ui, "m_ResetKitchen", reset);
            E.Set(ui, "m_Replay", replay);
            E.Set(ui, "m_ActivityList", list);
            E.Set(ui, "m_ModeBadge", badge);
            E.Set(ui, "m_GuidedBadge", IconLibrary.Get("icon_hand"));
            E.Set(ui, "m_FreePlayBadge", IconLibrary.Get("icon_star"));
            return ui;
        }
    }
}
