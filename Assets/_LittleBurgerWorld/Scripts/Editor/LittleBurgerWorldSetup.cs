using System;
using System.Collections.Generic;
using System.Linq;
using LittleBurgerWorld.Food;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using E = LittleBurgerWorld.EditorTools.EditorAssetUtil;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>
    /// One-click (or batch-mode) project generator: art, audio, materials, prefabs, data, scenes and
    /// player settings. Safe to re-run: assets are updated in place and keep their GUIDs; generated
    /// scenes are rebuilt (you will be asked first in the Editor).
    ///
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod LittleBurgerWorld.EditorTools.LittleBurgerWorldSetup.RunBatch
    /// </summary>
    public static class LittleBurgerWorldSetup
    {
        public const string MenuRoot = "Little Burger World/";
        public const string BundleId = "com.nocvault.littleburgerworld";

        [MenuItem(MenuRoot + "Setup Project (generate everything)", priority = 0)]
        public static void RunInteractive()
        {
            bool scenesExist = System.IO.File.Exists(SceneBuilder.RestaurantPath);
            if (scenesExist && !EditorUtility.DisplayDialog("Little Burger World",
                    "This regenerates art, audio, prefabs, data and the three game scenes (Boot, MainMenu, Restaurant).\n\n" +
                    "Manual edits to those generated scenes will be replaced. Continue?", "Regenerate", "Cancel"))
            {
                return;
            }
            Run(true);
        }

        /// <summary>Batch-mode entry point. Exits with code 1 if validation fails.</summary>
        public static void RunBatch()
        {
            try
            {
                Run(true);
                List<string> problems = LittleBurgerWorldValidator.Validate(out _);
                if (problems.Count > 0)
                {
                    foreach (string p in problems) Debug.LogError("[LBW Validate] " + p);
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Run(bool includeScenes)
        {
            using var progress = new Progress();
            AssetDatabase.StartAssetEditing();
            try
            {
                progress.Step("Folders & project settings", 0.02f);
                EnsureFolders();
                ConfigureTagsAndLayers();
                ConfigureRenderPipeline();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            ConfigurePlayerSettings();

            progress.Step("Painting icons & textures", 0.1f);
            IconLibrary.GenerateAll();
            progress.Step("Synthesising audio", 0.25f);
            AudioSynth.GenerateAll();
            ConfigureFonts();
            ConfigureSplash();

            progress.Step("Materials, meshes & food prefabs", 0.4f);
            ArtLibrary.ClearCache();
            PrefabBuilder.BuildFood();
            progress.Step("Ingredient, recipe & drink data", 0.5f);
            DataBuilder.BuildFoodData();
            progress.Step("Kitchen & environment prefabs", 0.55f);
            PrefabBuilder.BuildKitchen(DataBuilder.Drinks);
            PrefabBuilder.BuildEnvironment();
            progress.Step("Characters", 0.65f);
            var characters = DataBuilder.BuildCharacters();
            PrefabBuilder.BuildMascot(characters[0].prefab, $"{PrefabBuilder.CharFolder}/Mascot_Benny.prefab");
            PrefabBuilder.BuildUiPrefabs();
            progress.Step("Activities, stickers, audio & config", 0.72f);
            var activities = DataBuilder.BuildActivities();
            var stickers = DataBuilder.BuildStickers();
            var audio = DataBuilder.BuildAudioLibrary();
            var loc = DataBuilder.BuildLocalization();
            DataBuilder.BuildConfig(characters, activities, stickers, audio, loc);
            AssetDatabase.SaveAssets();

            if (includeScenes)
            {
                progress.Step("Scenes", 0.82f);
                SceneBuilder.BuildBoot();
                SceneBuilder.BuildMainMenu();
                SceneBuilder.BuildRestaurant();
                ConfigureBuildScenes();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LBW Setup] Little Burger World project generated successfully.");
        }

        // ------------------------------------------------------------------ project configuration
        private static void EnsureFolders()
        {
            string r = E.Root;
            string[] folders =
            {
                "Art/Characters", "Art/Environment", "Art/Food", "Art/Kitchen", "Art/Props", "Art/UI/Icons", "Art/UI/Fonts", "Art/Textures", "Art/Meshes",
                "Animations", "Audio/Music", "Audio/SFX", "Audio/Voice", "Materials",
                "Prefabs/Characters", "Prefabs/Environment", "Prefabs/Food", "Prefabs/Kitchen", "Prefabs/UI", "Prefabs/FX",
                "Scenes", "ScriptableObjects/Ingredients", "ScriptableObjects/Recipes", "ScriptableObjects/Characters",
                "ScriptableObjects/Activities", "ScriptableObjects/Drinks", "ScriptableObjects/Stickers", "ScriptableObjects/Settings",
                "Resources", "Tests/EditMode", "Tests/PlayMode", "Documentation"
            };
            foreach (string f in folders) E.EnsureFolder($"{r}/{f}");
        }

        public static void ConfigureTagsAndLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(FoodItem.InteractableLayer);
            if (string.IsNullOrEmpty(layer.stringValue) || layer.stringValue == "Interactable")
            {
                layer.stringValue = "Interactable";
            }
            else
            {
                Debug.LogWarning($"[LBW Setup] Layer {FoodItem.InteractableLayer} is already used by '{layer.stringValue}'. Not renamed.");
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Nocvault";
            PlayerSettings.productName = "Little Burger World";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);

            // Landscape only (both directions).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.useAnimatedAutorotation = true;

            // Android: IL2CPP + ARM64, modern API levels.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // 32-bit (ARMv7) + 64-bit: budget phones such as the Redmi A3 run a 32-bit-only Android.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            ConfigureAndroidGraphics();
            if ((int)PlayerSettings.Android.minSdkVersion < 25) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.runInBackground = true; // desktop test players run unfocused; mobile pauses regardless
            PlayerSettings.muteOtherAudioSources = false;
            PlayerSettings.SplashScreen.backgroundColor = E.Hex("#FFEFD8");
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUserBuildSettings.buildAppBundle = false;

            // Desktop test builds: windowed landscape.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
        }

        public const string VulkanFilterPath = "Assets/_LittleBurgerWorld/ScriptableObjects/Settings/VulkanDeviceFilterLists.asset";
        public const string PowerVrPattern = ".*PowerVR.*";

        /// <summary>
        /// Vulkan + OpenGLES3, with PowerVR GPUs denied Vulkan. On PowerVR GE8xxx drivers (e.g. Redmi A3) the saved
        /// Vulkan pipeline cache crashes the game on every launch after the first; those devices fall back to GLES3.
        /// </summary>
        public static void ConfigureAndroidGraphics()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            E.EnsureFolder(System.IO.Path.GetDirectoryName(VulkanFilterPath));
            var lists = AssetDatabase.LoadAssetAtPath<VulkanDeviceFilterLists>(VulkanFilterPath);
            if (lists == null)
            {
                lists = new VulkanDeviceFilterLists { name = "VulkanDeviceFilterLists" };
                AssetDatabase.CreateAsset(lists, VulkanFilterPath);
            }
            var so = new SerializedObject(lists);
            SerializedProperty deny = null;
            var paths = new System.Text.StringBuilder();
            SerializedProperty it = so.GetIterator();
            while (it.Next(true))
            {
                paths.Append(it.propertyPath).Append(' ');
                if (deny == null && it.isArray && it.propertyType == SerializedPropertyType.Generic &&
                    it.name.IndexOf("deny", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    deny = it.Copy();
                }
            }
            if (deny == null)
            {
                Debug.LogError("[LBW Setup] Could not find the deny list on VulkanDeviceFilterLists. Fields: " + paths);
                return;
            }
            bool present = false;
            for (int i = 0; i < deny.arraySize && !present; i++)
            {
                SerializedProperty d = FindDeviceName(deny.GetArrayElementAtIndex(i));
                present = d != null && d.stringValue == PowerVrPattern;
            }
            if (!present)
            {
                deny.arraySize++;
                SerializedProperty element = deny.GetArrayElementAtIndex(deny.arraySize - 1);
                SerializedProperty dn = FindDeviceName(element);
                if (dn == null)
                {
                    Debug.LogError("[LBW Setup] No device-name field in Vulkan deny entry. Fields: " + paths);
                    return;
                }
                dn.stringValue = PowerVrPattern;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lists);
            AssetDatabase.SaveAssets();
            PlayerSettings.Android.androidVulkanDeviceFilterListAsset = lists;
            Debug.Log("[LBW Setup] Vulkan deny list: " + PowerVrPattern + " (PowerVR → OpenGL ES 3).");
        }

        private static SerializedProperty FindDeviceName(SerializedProperty element)
        {
            SerializedProperty end = element.GetEndProperty();
            SerializedProperty p = element.Copy();
            if (!p.Next(true)) return null;
            while (!SerializedProperty.EqualContents(p, end))
            {
                if (p.propertyType == SerializedPropertyType.String && p.name.IndexOf("deviceName", StringComparison.OrdinalIgnoreCase) >= 0)
                    return p.Copy();
                if (!p.Next(true)) break;
            }
            return null;
        }

        public const string ThaiFontPath = EditorAssetUtil.Root + "/Art/UI/Fonts/Mitr-Medium.ttf";

        /// <summary>Fredoka has Latin glyphs only; Mitr (OFL, rounded) supplies Thai through Unity's font fallback.</summary>
        public static void ConfigureFonts()
        {
            var thai = AssetImporter.GetAtPath(ThaiFontPath) as TrueTypeFontImporter;
            var main = AssetImporter.GetAtPath(PrefabBuilder.FontPath) as TrueTypeFontImporter;
            if (thai == null || main == null) { Debug.LogError("[LBW Setup] Fonts missing"); return; }
            thai.includeFontData = true;
            thai.fontTextureCase = FontTextureCase.Dynamic;
            thai.SaveAndReimport();
            Font thaiFont = AssetDatabase.LoadAssetAtPath<Font>(ThaiFontPath);
            // Set the serialized fields directly (the C# property setters do not persist reliably in batch mode).
            var so = new SerializedObject(main);
            SerializedProperty P(string n) => so.FindProperty(n) ?? so.FindProperty("m_" + char.ToUpperInvariant(n[0]) + n.Substring(1));
            SerializedProperty refs = P("fallbackFontReferences") ?? P("fontReferences");
            SerializedProperty names = P("fontNames");
            if (refs == null || names == null)
            {
                var all = new List<string>();
                SerializedProperty it = so.GetIterator();
                while (it.NextVisible(true)) all.Add(it.propertyPath);
                Debug.LogError("[LBW Setup] Font importer properties: " + string.Join(", ", all));
                return;
            }
            SerializedProperty include = P("includeFontData");
            if (include != null) include.boolValue = true;
            refs.arraySize = 1;
            refs.GetArrayElementAtIndex(0).objectReferenceValue = thaiFont;
            names.arraySize = 2;
            names.GetArrayElementAtIndex(0).stringValue = "Fredoka";
            names.GetArrayElementAtIndex(1).stringValue = "Mitr";
            so.ApplyModifiedPropertiesWithoutUndo();
            main.SaveAndReimport();
            Font fredoka = AssetDatabase.LoadAssetAtPath<Font>(PrefabBuilder.FontPath);
            Debug.Log($"[LBW Setup] Font fallback: {string.Join(",", fredoka.fontNames)} refs={refs.arraySize} thai={(thaiFont != null)}");
        }

        /// <summary>Unity's start-up splash: our burger logo with the Unity logo below, on the game's cream colour.</summary>
        public static void ConfigureSplash()
        {
            Sprite logo = IconLibrary.Get("logo_burger");
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.backgroundColor = E.Hex("#FFEFD8");
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.logos = logo != null ? new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) } : null;
        }

        public static void ApplyAppIcon()
        {
            Texture2D icon = IconLibrary.GetTexture("AppIcon");
            if (icon == null) return;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        public static void ConfigureRenderPipeline()
        {
            // Mobile URP asset: soft shadows, MSAA for clean toy edges, no HDR (bandwidth), modest shadow distance.
            var mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            var pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            foreach (RenderPipelineAsset a in new[] { mobile, pc })
            {
                if (a == null) continue;
                var so = new SerializedObject(a);
                void SetI(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
                void SetF(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
                void SetB(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
                SetB("m_MainLightShadowsSupported", true);
                SetB("m_SoftShadowsSupported", true);
                SetF("m_ShadowDistance", 45f);
                SetI("m_ShadowCascadeCount", 1);
                SetI("m_MainLightShadowmapResolution", a == mobile ? 2048 : 4096);
                SetI("m_MSAA", 4);
                if (a == mobile)
                {
                    SetB("m_SupportsHDR", false);
                    SetF("m_RenderScale", 0.9f);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
            }
        }

        public static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneBuilder.BootPath, true),
                new EditorBuildSettingsScene(SceneBuilder.MenuPath, true),
                new EditorBuildSettingsScene(SceneBuilder.RestaurantPath, true)
            };
            ApplyAppIcon();
        }

        [MenuItem(MenuRoot + "Regenerate Art && Audio Only", priority = 20)]
        public static void RegenerateArtAndAudio()
        {
            IconLibrary.GenerateAll();
            AudioSynth.GenerateAll();
            AssetDatabase.SaveAssets();
        }

        private sealed class Progress : IDisposable
        {
            public void Step(string label, float t)
            {
                Debug.Log("[LBW Setup] " + label);
                if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Little Burger World Setup", label, t);
            }

            public void Dispose()
            {
                if (!Application.isBatchMode) EditorUtility.ClearProgressBar();
            }
        }
    }
}
