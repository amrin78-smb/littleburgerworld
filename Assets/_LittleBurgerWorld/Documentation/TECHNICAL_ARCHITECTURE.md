# Technical Architecture

Unity 6000.3 · URP 17.3 · Input System 1.20 · uGUI · Test Framework 1.6. All code is in `Assets/_LittleBurgerWorld/Scripts`, namespace `LittleBurgerWorld.*`, assembly `LittleBurgerWorld.Runtime` (+ `LittleBurgerWorld.Editor`, `…Tests.EditMode`, `…Tests.PlayMode`). No third-party packages.

## Project structure

```
Assets/_LittleBurgerWorld/
  Art/        Fonts (Fredoka, OFL), Icons (generated PNG sprites), Textures, Meshes (generated .asset meshes)
  Audio/      SFX + Music (generated WAV), Voice/<lang> (Kokoro TTS clips)
  Materials/  URP Lit pastel materials + Sprites/Default FX materials
  Prefabs/    Food, Kitchen, Environment, Characters, UI
  Resources/  LBW_GameConfig.asset (root config, loaded at boot)
  Scenes/     Boot, MainMenu, Restaurant
  ScriptableObjects/ Ingredients, Recipes, Drinks, Characters, Activities, Stickers, Settings
  Scripts/    Core, Camera, Input, Interaction, Food, Kitchen, Customers, Activities, UI, Audio, SaveSystem, Effects, Rewards, Editor
  Tests/      EditMode, PlayMode
  Documentation/
Tools/unity_batch.sh   run an Editor method in batch mode inside Xvfb
```

## Systems (one responsibility each)

| # | System | Where | Responsibility |
|---|---|---|---|
| 1 | `GameManager` | persistent | Composition root; creates services; auto-created before the first scene (`RuntimeInitializeOnLoadMethod`) so any scene can be played directly. Frame-rate governor (60 → 30 on slow devices). |
| 2 | `GameModeManager` | persistent | Guided / Free Play selection across scenes. |
| 3 | `InputManager` | Restaurant | Input System touch + mouse → one primary pointer; extra fingers ignored; cancels on focus loss; detects presses that start on UI. |
| 4 | `InteractionManager` | Restaurant | Gesture tracker (tap / hold / drag), priority + generous sphere-cast picking, drag plane, drop detection at counter height under the finger, drop highlight, blob drag-shadow, safe cancel. |
| 5 | `FoodManager` | Restaurant | Spawns ingredients from data, wraps stacks into burgers, gentle tidy. |
| 6 | `RecipeManager` | persistent | Data-driven recipe matching (exact order) + next-ingredient helper. |
| 7 | `KitchenManager` | Restaurant | Station references, tap-to-send routing, work-surface reset, guided restrictions. |
| 8 | `CustomerManager` | Restaurant | Spawns customers, door animation, Free Play auto-spawn with random picture requests. |
| 9 | `ActivityManager` (+ `ActivityStepController`) | Restaurant | Runs data-driven guided activities. |
| 10 | `UIManager` | Restaurant HUD | Tray, prompt, hint hand, activity picker, celebration, sticker toast. |
| 11 | `AudioManager` | persistent | Music, pooled SFX with throttling, loops, voice; volume settings. |
| 12 | `SaveManager` | persistent | Versioned JSON, atomic writes, backup, corruption recovery, migration. |

Plus `RewardManager` (stats, stickers, unlocks), `SceneFlow` (fade transitions), `FxManager` (one emitter per effect type), `RestaurantController` (scene composition root), `Tween` (tiny tween engine for all toy motion), `MeshFactory` (procedural rounded meshes).

### Communication
Systems talk through `GameEvents` (static, exception-isolated) – e.g. `IngredientStacked`, `PattyCooked`, `CustomerServed`. The activity engine, rewards and audio only listen; they never reference stations directly. Scene objects get stable string ids through `SceneKey` (e.g. `Board`, `Grill`, `Tray.cheese`, `Drinks.Lever`, `Customer`) so activity data can point at them.

### Interaction contracts
* `IInteractable` – priority, `OnSelect/OnDeselect/OnTap`, `BeginDrag()` (may return a different object, e.g. a spawned item).
* `IDraggable` – drag plane height, `OnDragMoved`, `OnDropped`, `OnDragCancelled`.
* `IDropTarget` (`DropZone` base) – catch circle/rectangle, priority, `Accept`, highlight ring. Registered in `DropTargetRegistry` (no physics queries during drags).
* `IFoodContainer` – board, grill, tray, counter, customer: `Release`, `HandleTap`, `AllowPickup`.
* `IAssistable` – "do it for me" used by guided auto-assist. `IHintTarget` – where the hint hand should point.

### Food
`FoodItem` is the single food component (ingredient, burger, fries batch, fries, milkshake, tray). `CookableVisual` blends raw → cooked via `MaterialPropertyBlock`. `BurgerStackModel` is the pure stacking logic (unit tested); `BurgerAssemblyController` is its scene view.

### Guided activity engine
`ActivityData` = list of `ActivityStep` (PrepareKitchen, CallCustomer, ShowObject, HighlightObject, DemonstrateDrag, WaitForSelection, WaitForDrop, WaitForTap, WaitForEvent, PlayAnimation, PlayAudio, Wait, CompleteActivity). The controller is tick-driven (works in unit tests with a fake `IActivityContext`). Anti-stuck: event history (early actions count), repeating hints, auto-assist after N hints, finite non-wait steps, replay.

### Characters
Procedural rig (`ProceduralCharacterRig`): limb pivots animated in `LateUpdate` – idle breathing/blinking/waving, waddle walk, happy jump, chomp eating, spin celebration, wave. No Animator controllers are required (none missing).

## Content pipeline (all original, generated, re-runnable)
`Little Burger World ▸ Setup Project` (or batch `LittleBurgerWorldSetup.RunBatch`):
1. Folders, `Interactable` layer (6), player settings, URP settings.
2. `IconLibrary` paints every 2D asset with an anti-aliased SDF rasterizer (`Canvas2D`) → PNG sprites (icons, stickers, portraits, particles, floor/wall textures, app icon).
3. `AudioSynth` synthesises 24 SFX and 2 music loops → WAV.
4. `ArtLibrary` + `PrefabBuilder` build materials, procedural meshes and all prefabs.
5. `DataBuilder` creates ScriptableObjects and `LBW_GameConfig`.
6. `SceneBuilder` builds Boot, MainMenu and Restaurant; build settings updated.
Assets are updated in place (GUIDs preserved). Generated scenes are rebuilt (confirmation dialog in the Editor).

`Little Burger World ▸ Validate Project` checks scenes, build settings, prefabs, ScriptableObjects, broken references/missing scripts, the layer, activity scene keys and Android settings.

## Rendering & performance
* Fixed orthographic iso camera (pitch 35°, yaw 45°). `IsometricCameraController` frames a screen-aligned focus box inside the safe area minus the HUD bands (top 10 %, bottom 20 %) for any aspect ratio (tested 16:9, 18:9, 19.5:9, 4:3).
* **Lighting**: one directional light + trilight ambient, no realtime shadows in the Restaurant; soft blob shadows under counters, stations, furniture and characters instead (cheap and identical on every GPU). The main menu (perspective camera) uses soft realtime shadows. No post-processing.
* Mobile URP asset: MSAA 4×, render scale 0.9, HDR off, SRP Batcher on, GPU instancing on materials.
* Particles: one looping system per effect type, emitted on demand (no instantiation during play). Tweens are pooled in one runner.
* Simple box/sphere/capsule colliders only on interactive objects, no rigidbodies, no physics simulation.

## UI text
Legacy uGUI `Text` with the Fredoka SemiBold font (instanced from the OFL variable font) instead of TextMeshPro: the toddler UI is picture-first and only a handful of labels exist; this avoids importing TMP essential resources in batch mode. Localization is data-driven (`LocalizationTable`, `LocalizedText`).

## Privacy
Fully offline. No analytics, ads, IAP, accounts or third-party SDKs. Only `littleburgerworld_save.json` (+ `.bak`) in `Application.persistentDataPath` containing stars, stickers, unlocks, stats and settings.
