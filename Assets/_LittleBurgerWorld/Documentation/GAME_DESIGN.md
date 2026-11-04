# Little Burger World – Game Design

A toddler-friendly (2–5, also 5–7) 3D toy burger restaurant for Android tablets and phones, landscape.
No timers, no failure, no angry customers, no lives, no ads, no purchases, no currency. Every action gets positive feedback.

## Screens

| Screen | Contents |
|---|---|
| **Boot** | Logo splash (1.2 s) → Main Menu. Creates the persistent services. |
| **Main Menu** | Logo, animated Benny the Bear, giant spinning burger, **LET'S PLAY!** (guided), **FREE PLAY**, sticker book, small parent (gear) button behind a grown-up gate. |
| **Restaurant** | Fixed isometric toy restaurant with all stations, bottom ingredient tray, top bar (hold-to-go-home, mode badge, hold-to-tidy in Free Play, replay + activities in Guided), picture prompt bubble, hint hand, celebration and sticker toasts. |

## Restaurant layout (one room, 8 × 8 units)

* **Back counter** (along the left back wall): fry station (happy fryer, potato basket, batch plate), fries-box holder, milkshake machine (3 flavour buttons, big dispense button, cup stack, whipped cream, straw jar).
* **Front island**: grill (2 cooking spots) and the burger board – the centre of the screen.
* **Serving counter** (along the right wall): meal tray; customers wait behind it after entering through the door.
* **Dining area**: two tables with stools, plants, window, bunting, pictures, clock and the original "Little Burger World" sign.

## Core interactions (all toddler-tuned)

| Gesture | Result |
|---|---|
| Drag an ingredient picture from the tray | A real 3D ingredient appears under the finger (lifted slightly above it so the finger never hides it), with a soft shadow showing where it will land. |
| Release over a station | Glowing ring highlights the target; the item hops and squashes into place with a click and sparkles. |
| Release over nothing / touch cancelled | The item hops back home (tray items pop back into the tray). Never lost, never "wrong". |
| **Tap** any ingredient button or food | It flies to the sensible place automatically: ingredient → board, raw patty → grill, fries → box, finished food → tray → customer. Children who cannot drag yet can still play everything. |
| Touch-and-hold | Also starts a drag (for slow fingers). |
| Tap a station button | Potato basket, flavour buttons, cups, big dispense button, cream, straws. |
| Tap Benny | He giggles and shows hearts. |
| Tap balloons | They pop into confetti (bonus after celebrations). |

## Stations

* **Burger board** – transform-based stacking (no physics), each layer hops into place; any order in Free Play. A top bun on ≥1 layer completes the burger: recipe matched (Basic, Cheeseburger, Veggie, Double, Special or a "custom" burger – all celebrated), spin + jingle + sparkles, then the whole burger is one draggable object.
* **Grill** – raw (pink) patty cooks side one (colour blend + steam + gentle sizzle), bounces "flip me", tap to flip, side two cooks, *ding* – cooked patty with grill marks. Never burns. Left alone it simply stays warm.
* **Fries** – tap the potato basket → potato hops into the smiling fryer → bubbles → golden batch pops onto the plate → drag/tap it to the paper box → box fills stick by stick → confetti → draggable fries. A new empty box appears automatically.
* **Milkshake** – pick strawberry / chocolate / vanilla, a cup slides under the nozzle (tap cups or it happens automatically), press the big button to fill, add whipped cream (with cherry) and a straw → celebration → draggable milkshake. Forgiving order: pressing the button without a cup places one; picking a flavour with an empty cup starts filling.
* **Tray** – slots for burger, fries, drink (+2 extra). A full meal gets a star burst. The whole tray can be served.

## Customers

Benny the Bear (original design: round head, big glossy eyes, small nose, smile, rosy cheeks, soft brown fur, red bow tie) plus Penny the Bunny, Milo the Cat (ginger tabby, whiskers, curly tail), Dotty the Dog (floppy ears, eye patch), Ellie the Elephant (big ears, trunk) and Bao the Panda. Milo and Dotty are available from the start; Penny, Ellie and Bao are unlocked by activities. Free Play picks unlocked animals at random. States: Idle → Entering → Waiting → ReceivingFood → Eating → Celebrating → Leaving. Request shown as a picture in a thought bubble. **Any** food is accepted happily (raw patties are the only thing not accepted – they go to the grill). Without food they wait happily forever, blinking, breathing and waving.

## Game modes

### Guided Play ("LET'S PLAY!")
Six picture cards (stars earned shown on each). Steps are communicated with a picture prompt bubble, a pulsing highlight and an animated hand that taps or drags. Each step also shows a one-line instruction and speaks it (English or Thai, following the language setting); the line repeats with the hand hint at most every 9 s, music dips while the voice talks, and "Hooray! You did it!" plays on completion.

1. **My First Burger** – bun, patty on grill, flip, patty on bun, top bun, Benny cheers.
2. **Add Some Cheese** – as above + cheese, then give it to Benny. Unlocks **pickles**.
3. **Make French Fries** – potato, fries into the box.
4. **Make a Milkshake** – flavour, cup, fill, cream, straw. Unlocks **Penny the Bunny**. (*Make French Fries* unlocks **Ellie the Elephant**, *Prepare a Complete Meal* unlocks **Bao the Panda**.)
5. **Prepare a Complete Meal** – burger, fries and milkshake on the tray.
6. **Serve Benny** – Benny walks in wanting a cheeseburger; make it and serve him.
7. **Milo's Snack** – make fries and give them to Milo the Cat.
8. **Dotty's Milkshake** – make a milkshake and give it to Dotty the Dog.

Never stuck: actions done early count immediately; hints repeat every few seconds; after several hints the game gently does the step itself; replay and back-to-cards buttons are always available.

### Free Play ("FREE PLAY")
Everything unlocked is available at once, any order, tall towers (12 layers), food can be put down anywhere on the counters, customers keep arriving with random picture requests and love whatever they get. **Tidy-up** is a hold-to-confirm button so a stray tap never wipes the child's work.

## Rewards (pressure-free)
Stars per activity (up to 3, earned by replaying), 10 stickers (first burger, cheese champ, fries fan, shake maker, meal master, bear buddy, tall tower ≥ 8 layers, balloon popper, animal friends = serve 4 different animals, super chef), unlocks (pickles, Penny, Ellie, Bao), celebration balloons. Nothing is ever taken away.

## Parent area
Gear button → grown-up gate (simple sum, three big answers; a wrong answer just shows a new question) → music / sounds / voice volumes, spoken-hint toggle, language (English / ไทย, data-driven), hold-to-reset progress, privacy information. No links, purchases or accounts.


## Replay features (v0.5–0.6)
* **New toppings:** fried egg, onion rings, ketchup and mustard (tray shrinks to fit any screen). New recipe: Sunny Egg Burger.
* **Ice cream cart** (dining area): tap the cones (or just a flavour), up to 3 scoops of strawberry / chocolate / vanilla, sprinkles, cherry on top. Servable from the first scoop. Animals sometimes order ice cream. Activity: *Ice Cream Time*.
* **Washing up:** every customer who finishes eating leaves a dirty plate (sink button shows the count, top-left). Rub (or tap) the plate with the sponge – splotches melt away with bubbles, clean plates hop onto the drying rack. An empty sink still offers 3 play plates. Activity: *Wash the Dishes*. Sticker: *Squeaky Clean* (5 plates).
* **Surprise capsules:** serving a customer or finishing an activity drops a free capsule into the machine (top-right, max 3 waiting – no currency, nothing to buy). Tap it: the capsule drops, pops open (by itself if the child just watches) and reveals one of 20 toys, which goes to the **Toy Shelf** on the main menu (unfound toys are silhouettes). New toys are always preferred until the set is complete. Sticker: *Toy Collector* (10 toys).
* **Restaurant moods:** each visit rotates Sunny → Birthday party (balloons, cake, party hats, confetti on every serve) → Rainy day (rain outside, puddles, gentle rain sound) → Cosy night (fairy lights, moon in the window). Tapping the window changes the mood. Gameplay is identical in every mood.
