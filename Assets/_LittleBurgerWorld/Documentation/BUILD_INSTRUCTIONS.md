# Build & Extension Instructions

## Open and run
1. Unity Hub → **Add project from disk** → this repository root (Unity **6000.3.27f2**, Android Build Support module).
2. Open `Assets/_LittleBurgerWorld/Scenes/Boot.unity` and press **Play** (any scene can be played directly; services bootstrap automatically).
3. Mouse = finger in the Editor (click, drag, hold).

If `Assets/_LittleBurgerWorld/Resources/LBW_GameConfig.asset` is missing (fresh clone without generated assets), run **Little Burger World ▸ Setup Project**.

## Editor menu
| Menu item | Purpose |
|---|---|
| Setup Project (generate everything) | Regenerates art, audio, prefabs, data, scenes, settings (asks before rebuilding scenes). |
| Validate Project | Health check (scenes, prefabs, data, references, layer, activity keys, Android config). |
| Build Android APK | IL2CPP / ARM64 APK → `Builds/Android/LittleBurgerWorld.apk`. |
| Build Linux Test Player | Desktop player used by the automated play-through. |
| Capture Layout Screenshots | Restaurant + menu at 16:9, 19.5:9 and 4:3 → `Logs/Screenshots`. |

## Command line (headless)
```bash
U=~/Unity/Hub/Editor/6000.3.27f2/Editor/Unity
# Generate + validate (exit code 1 on validation failure)
Tools/unity_batch.sh LittleBurgerWorld.EditorTools.LittleBurgerWorldSetup.RunBatch setup
# Tests
$U -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
xvfb-run -a $U -batchmode -force-glcore -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-results.xml
# Android APK
$U -batchmode -nographics -quit -buildTarget Android -projectPath . \
   -executeMethod LittleBurgerWorld.EditorTools.BuildScript.BuildAndroidBatch -logFile Logs/android_build.log
# Automated play-through with screenshots (Linux player)
Tools/unity_batch.sh LittleBurgerWorld.EditorTools.BuildScript.BuildLinuxBatch linux_build
xvfb-run -a -s "-screen 0 1600x900x24" Builds/Linux/LittleBurgerWorld.x86_64 -force-glcore -screen-width 1600 -screen-height 900 \
   -lbw-autopilot -lbw-shots "$PWD/Logs/AutoPilot" -logFile Logs/autopilot.log
```

## Android settings (applied by setup)
Landscape left/right only · IL2CPP · **ARMv7 + ARM64** (32-bit budget phones such as the Redmi A3 are supported) · min SDK 25, target = highest installed · package `com.nocvault.littleburgerworld` · Linear colour · Mobile URP quality level · APK (not AAB) · debug keystore.
### PowerVR / budget-phone protection
Graphics APIs are **Vulkan + OpenGLES3**, and `ScriptableObjects/Settings/VulkanDeviceFilterLists.asset` (assigned to *Player Settings ▸ Android ▸ Vulkan Device Filter Lists*) **denies Vulkan for `.*PowerVR.*`**. On PowerVR GE8xxx drivers (e.g. Redmi A3 / GE8320) the Vulkan pipeline cache Unity saves on exit crashes the game on every launch after the first; those devices now use OpenGL ES 3 while all others keep Vulkan. The validator fails if ARMv7, the GLES3 fallback or the deny entry is missing.

**Device test procedure (important):** install → play → fully close the app (swipe it away) → reopen → play; repeat once more. A fresh-install test alone does not catch the restart crash.

**For store release:** create a keystore (Player Settings ▸ Publishing Settings), switch `EditorUserBuildSettings.buildAppBundle` to true for an AAB, bump `bundleVersionCode`, and complete the Google Play "Designed for Families" questionnaire (the app collects no data).

## Install on a device
```bash
adb install -r Builds/Android/LittleBurgerWorld.apk
```
(`adb` is in `<Unity>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/`.)

## How to add content (no code needed)

### Ingredient
1. Create a prefab: root with `FoodItem` (Type Ingredient) + `BoxCollider` on layer **Interactable**, pivot at the bottom centre, visuals as children. (Copy `Prefabs/Food/Ingredient_Cheese` as a template.)
2. *Create ▸ Little Burger World ▸ Ingredient*: unique `id`, prefab, icon (256 px sprite), colour, `stackHeight` (thickness), `trayOrder`. Set `unlockedByDefault` false to make it a reward.
3. Add it to `Resources/LBW_GameConfig ▸ ingredients`. It appears in the tray automatically.
   Cookable? Give the prefab a `CookableVisual` and set `cookedVariant` on the raw ingredient.

### Recipe
*Create ▸ Little Burger World ▸ Recipe* – id, name, icon, ingredients bottom → top (end with the top bun) → add to config `recipes`. Matching is automatic. Requests can use the recipe id (e.g. `double_burger`).

### Character
Build a prefab with `AnimalCustomerController`, `ProceduralCharacterRig` (assign limb pivots), a `RequestBubble`, `SceneKey = Customer`, collider on layer Interactable – or extend `PrefabBuilder.BuildCharacter` (round/long ears, colours). Create *Character* data (id, prefab, portrait, voice pitch, unlocked?) → add to config `characters`. Free Play picks unlocked characters at random.

### Guided activity
*Create ▸ Little Burger World ▸ Activity* → add steps:
* `PrepareKitchen`, `CallCustomer` (characterId + requestItems),
* `WaitForDrop` (targetKey e.g. `Board`/`Grill`/`Tray`/`Customer`/`FriesBox`, sourceKey for the hand, itemId, promptIcon),
* `WaitForTap` (targetKey), `WaitForEvent` (eventType + optional itemId), `Wait`, `PlayAnimation`…
Add it to config `activities`; a card appears automatically. **Validate Project** reports unknown scene keys. Available keys: `Board, Grill, Fryer, Fryer.Potatoes, Fryer.Batch, FriesBox, Drinks, Drinks.Cup, Drinks.Cups, Drinks.Lever, Drinks.Cream, Drinks.Straws, Drinks.Flavour.<id>, Tray, Customer, Tray.<ingredientId>`.

### Voice prompts and instruction text
`Tools/voice/voice_lines.json` is the single source for each step's on-screen instruction (`say.<key>`) and its spoken clip.
1. Edit or add a line (key + `en` + `es`; reference it from an activity step's `voiceKey`).
2. `Tools/voice/generate_voice.sh [key...]` – generates `Audio/Voice/<lang>/<key>.wav` offline with Kokoro TTS (first run installs it into `~/.cache/lbw-tts`, ~350 MB). Voices and speed are set in the JSON (`af_heart`, `ef_dora`).
3. **Setup Project** – rebuilds the AudioLibrary and localization table. The validator fails if a spoken step has no clip in some language.
To use a recorded voice actor instead, replace the WAVs with files of the same name.

### Language
Add a column to `ScriptableObjects/Settings/LocalizationTable` (language code + display name + one value per entry).
