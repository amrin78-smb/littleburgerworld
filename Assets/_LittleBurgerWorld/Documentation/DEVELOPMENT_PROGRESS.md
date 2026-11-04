# Little Burger World – Development Progress

Legend: `[x]` done and verified · `[~]` implemented, needs on-device verification · `[ ]` not done

## Environment (inspected 2026-11-03)

| Item | Value |
|---|---|
| Unity | 6000.3.27f2 (Unity 6.3), Personal license |
| Render pipeline | URP 17.3.0 (Mobile_RPAsset for Android quality level) |
| Input | Input System 1.20.1 (active input handler = Input System) |
| UI | uGUI 2.0 (legacy `Text` + Fredoka font – see TECHNICAL_ARCHITECTURE.md) |
| Tests | Unity Test Framework 1.6 |
| Android | Embedded SDK / NDK / OpenJDK from the Unity Hub module |
| Workspace | Empty git repository → new project from the Unity 6 URP 3D template |

## Checklist

### Phase 1 – Foundation
- [x] Unity 6 URP project, folder structure, unused template packages/content removed
- [x] Assembly definitions (Runtime, Editor, EditMode tests, PlayMode tests)
- [x] Boot / MainMenu / Restaurant scenes in build settings (Boot first)
- [x] GameManager (auto-bootstrap), fade scene transitions
- [x] IsometricCameraController – framing unit-tested for 16:9, 18:9, 19.5:9, 4:3; screenshots verified

### Phase 2 – Environment
- [x] Procedural rounded meshes; ~60 pastel materials
- [x] Floor, walls, door (animated), window, sign, bunting, pictures, clock, plants, tables, stools
- [x] Burger station, grill, fry station, drinks station, serving counter + tray
- [x] Lighting + blob shadows; screenshots reviewed at three aspect ratios

### Phase 3 – Interaction
- [x] InputManager (touch + mouse, primary pointer, cancellation, UI blocking)
- [x] GestureTracker (tap / hold / drag / cancel) – unit tested
- [x] IInteractable / IDraggable / IDropTarget, InteractionManager (priority + generous picking)
- [x] Drop detection under the finger at counter height, snapping, highlight ring, drag shadow – PlayMode tested with injected pointer input

### Phase 4 – Burger gameplay
- [x] IngredientData / RecipeData / DrinkData; 7 ingredients + cooked patty; 5 recipes
- [x] FoodItem, FoodManager, RecipeManager, BurgerAssemblyController (stack, remove, restack, complete)
- [x] Grill (cook, flip, cooked, no burning), fries station, milkshake station, serving tray
- [x] Tap-to-send routing for children who cannot drag yet

### Phase 5 – Customers
- [x] Benny the Bear (+ unlockable Penny the Bunny), procedural rig (idle, walk, happy, eat, celebrate, wave)
- [x] Customer state machine (unit tested), walk-in path (PlayMode tested), serving, eating, celebrating, leaving

### Phase 6 – Game modes
- [x] GameModeManager, main menu buttons (PlayMode tested)
- [x] ActivityData + ActivityStepController (hint hand, highlight, prompt, auto-assist, act-ahead history) – unit tested
- [x] Six guided activities; "My First Burger" completed end-to-end in tests and in the player
- [x] Free Play: unrestricted stacks, free counter space, auto customers, hold-to-tidy

### Phase 7 – Audio & polish
- [x] AudioManager; 24 synthesized SFX + 2 original music loops
- [x] FX: sparkles, stars, confetti, hearts, crumbs, steam, bubbles, poofs, tappable balloons
- [x] Rewards: stars, 9 stickers, unlocks; save system (versioned, backup, corruption recovery, migration)
- [x] Parent gate + settings (volumes, voice toggle, language, reset, privacy)
- [x] Voice prompts: 21 lines in English (Kokoro TTS) + Thai (VachanaTTS), generated offline; intelligibility checked by transcribing every clip back with Whisper

### Phase 8 – Android
- [x] Validator passes (setup batch exits 0)
- [x] EditMode 38/38, PlayMode 30/30 passing (executed in batch mode)
- [x] v0.7.0 bug sweep (4 area reviewers + adversarial verifiers, 23 confirmed bugs fixed):
  - Guided activities can no longer soft-lock: early actions are kept per event (lever before flavour, sprinkles before scoop, potato tapped early); serving via the tray counts; animals only take food on the serving step and are called back if missing; tapped food goes straight to the animal on that step; hints/assists fall back to the parent key (closed sink); Complete Meal's "meal ready" step can put a removed dish back
  - Ice cream toppings in any order (cherry no longer blocks scoops/sprinkles; sprinkles/cherry make a cone if needed); cream still fits after the straw
  - Touch: a burger touched mid-spin no longer locks; touched hops finish instead of freezing (fries batch stuck mid-air); flavour buttons/lever outrank the cup; empty fries box no longer steals taps from the batch; a dragged-and-released cone/cup/fries box/batch returns into its station (no duplicates)
  - Food dropped on the grill/fryer/milkshake machine lands beside it instead of inside; the top bun always fits on a 12-layer tower
  - UI: scene-load fade blocks taps (no mode switch while loading); interrupted hold buttons reset when hidden; no music dip when the voice is muted; Toy Shelf wobble works; stale sink auto-close cancelled; capsule rest position fixed; washing-up panel above the capsule machine
- [x] v0.6.1 device-test fixes: every cooking activity calls an animal and ends with serving them; ice cream cart buttons (tubs above all) win taps over the cone standing in front of them, bigger tub touch areas
- [x] Automated play-through in a Linux player build: 13/13 checks passing, 17 screenshots
- [x] ARMv7 + ARM64, IL2CPP, landscape, Vulkan + GLES3 with PowerVR → GLES3 deny list
- [x] APK built to `Builds/Android/LittleBurgerWorld.apk`
- [~] On-device testing (touch, performance, Redmi A3 close-and-reopen test) – needs a physical device

## Progress reports

**Phases 1–2.** Project created from the URP template; content pipeline generates art/audio/meshes/prefabs/data/scenes. Layout iterated with offscreen screenshots. Batch-mode first frames rendered SRP-batched materials black (warm-up renders added to the screenshot tool; not a game issue). Realtime shadows with the orthographic camera were unreliable, so the Restaurant uses soft blob shadows (cheaper on mobile anyway).

**Phases 3–6.** All systems implemented. Tests caught: save migration not reported or persisted (fixed), customer food list modified during iteration (fixed), and drop detection using the lifted item's position instead of the point under the finger (fixed – a real usability bug). Player screenshots caught customers stopping at the door because the walk path was cleared before being copied (fixed + regression test).

**Phase 7–8.** Polished lighting; sticker toasts, celebrations and balloons verified in the player. Per the owner's Redmi A3 report, added 32-bit support and the PowerVR Vulkan deny list; the validator enforces both.

## Known limitations
* Voice is AI-generated (Kokoro). Natural, but a recorded voice actor would be warmer for a commercial release.
* Visuals were verified with a software renderer (llvmpipe) and a desktop player; touch feel and frame rate still need checking on real phones and tablets, especially the Redmi A3 on OpenGL ES 3.
* Models are procedural (rounded primitives); see ASSET_INVENTORY.md for recommended professional replacements.
* Debug keystore and APK only; store release needs a release keystore and an AAB.
* Only English and Thai are present (more languages are data-only additions). Thai uses uGUI legacy text without complex shaping; stacked tone marks may sit slightly close to vowels.
