# Plan: every ability, re-evaluated as a whole

Status: **planning. Nothing here is built.** AB-01, AB-02, AB-03, AB-06 and AB-12 are accepted. Round 1 accepted the mechanics for Rio, Vale, Brondo and Fenn; round 2 (the captain matrix below) covers all ten captains, their themes and looks, and waits for Jack's review. Review page: https://claude.ai/artifact/2EyWdxgTiSXnN71r8nXHC4. Tracker: #1011 (rescoped from four Star Pitch proposals to the whole set, Jack, 2026-09-25). Folds in #1010 (Spin Check). Rules that stay in force: [plan-pitching-hitting.md](plan-pitching-hitting.md) PH-16 and its refinements, spec §12–§13, and principle 2 in [00-decisions](../spec/00-decisions.md).

## What Jack asked for

"Re-evaluate all the abilities as a whole in one large pass", instead of patching Charmball, Skullball, Fogball and Phonyball one at a time.

## The rules every ability already answers to

These are accepted. The pass designs inside them.

1. **The two-second rule** (§13): a skill bends one rule for ≤ 2 s, then baseball resumes. The bend is the ball's path or speed, a fielder's body, or the terrain.
2. **A Star Pitch keeps the ordinary timing window and never turns contact into a miss** (PH-16-R1, R18, R19). Its challenge is readable ball behaviour: speed and path.
3. **A Star Swing needs real contact**; an individual swing may change its contact area (PH-16-R2), which no swing does yet.
4. **One Star Pitch and one Star Swing per character**, held LB plus the normal action, locked at release (PH-16-R9, R10, R11, R17). Tiers low 1 / mid 2 / top 3; top tier is captain-only (PH-16-R7, R8).
5. **Specials supplement the ordinary duel**: they never guarantee a hit or an out, and each keeps readable counterplay (PH-16 decision A).
6. **A play is decided by geometry**, never a roll or a caption (AGENTS.md; principle 2 for parks). §8.6 keeps one exception: "a fixed drop chance on the catch is allowed for *skills*".

## Where we are today

Ten captains, 27 role players. Each captain has a Star Pitch, a Star Swing and a field ability. Role players carry one of three generic pitches, one of three generic swings and one of eight field abilities.

### Star Pitches

| Captain | Pitch | Tier | Today | Note |
| --- | --- | --- | --- | --- |
| Rio | Heatball | top | ×1.15 speed; the catcher's glove smokes | Its payoff (the burn-hop) belongs to the heat *swing*. **Accepted** |
| Vale | Charmball | mid | ×0.9 speed, a constant side-to-side wobble | Placeholder since PH-16-R18. **Accepted** |
| Zig | Prismball | mid | Late break, ghost images |. **Second look** |
| Brondo | Phonyball | mid | Decoy: one side early, switches late | Roll removed (PH-16-R19); the switch is the whole effect. **Accepted** |
| Konga | Caskball | top | ×0.85 speed; knockback on the catch (0.55 s) |. **Replaced: new options below** |
| Ashlord | Skullball | top | ×1.2 speed, nothing else | Placeholder since PH-16-R18; a top-tier price for speed alone. **Second look** |
| Fenn | Fogball | mid | ×0.82 speed, nothing else | Placeholder since PH-16-R18. **Accepted** |
| Sable | Mirage Ball | mid | A fading twin to the far half of the zone | New (#1149) |
| Hollis | Rockfall | mid | Floats up to 2.2 ft, drops back onto its path | New (#1150) |
| Reed | Leapfrog | mid | Crawls, then leaps to arrive on time | New (#1151) |
| Role players | Star fastball / change / breaker | low | ×1.25 / ×0.7 / ×0.85 late break | |

### Star Swings

| Captain | Swing | Tier | Today | Note |
| --- | --- | --- | --- | --- |
| Rio | Heat swing | top | Exit ×1.15; burn patch slows a fielder ×0.45 for 2 s; a caught fly burn-hops (35 % drop) | Drop is a roll. **Accepted** |
| Vale | Heart swing | mid | Exit ×1.05; the nearest fielder pauses 0.8 s (frozen glove, 40 % drop) | Drop is a roll. **Accepted** |
| Zig | Shell swing | mid | Exit ×1.1; first bounce randomized ±30° | Same effect as Fenn's. **Second look** |
| Brondo | Phony swing | mid | Exit ×1.1; decoy ball, the real one shows at the apex (35 % drop) | Drop is a roll; Brondo's pitch is a decoy too. **Accepted** |
| Konga | Cask swing | top | Exit ×1.2; two decoy balls fall with it | A third decoy. **Replaced: new options below** |
| Ashlord | Furnace | top | Exit ×1.25; lava strip slows a fielder ×0.45 for 2 s | Same family as Rio's heat swing. **Second look** |
| Fenn | Staff swing | mid | Exit ×1.08; first bounce randomized ±30° | Same effect as Zig's. **Accepted** |
| Sable | Sidewinder | mid | First hop kicks ≤ 20° away from the chaser | New |
| Hollis | Updraft | mid | Fly rides the wind ×1.5 | New |
| Reed | Pond Skip | mid | Chopper whose first hop springs ×2.2 | New |
| Role players | Star grounder / fly / line | low | Launch 8° / 38° / 18°, exit ×1.2–1.25 | |

### Field abilities

| Ability | Carriers | Today |
| --- | --- | --- |
| Grow | Rio | +6 ft catch radius, window +0.08 |
| Lick Catch | Zig | +3 ft catch radius (same bonus rail as Grow) |
| Withdraw | Fenn | The same catch-radius bonus as Grow |
| Snap Throw | Vale, 2 role players | 0.22-s release after a clean received throw |
| Laser | Brondo, 3 role players | ×1.25 throw home with a live runner on third |
| Clamber | Konga, 2 role players | Rob ≤ 28 ft over at the wall |
| Spin Check | Ashlord, 2 role players | **Dead in live play**: `FieldAbilities.SpinCheck` turns a triple into a double as a caption and nothing calls it (#1010) |
| Sand Scoop | Sable | +8 ft ground reach below 1 ft; never bobbles |
| Long Toss | Hollis | 80 ft more range before the long-throw loss |
| Lily Leap | Reed | The normal jump rises 4.5 ft |
| Super Jump | 6 role players | Rob ≤ 18 ft over |
| Ball Dash | 3 role players | Carries the ball ×1.20 |
| Dive | 8 role players | +2 ft reach, 8 ft sideways |
| Burrow | 1 role player | Ignores park slows |

## What the whole view shows

- **Four placeholders.** Charmball, Skullball and Fogball are speed changes since their window penalties went; Phonyball's switch is its whole effect. Skullball costs a top-tier price for +20 % speed.
- **Rolls still decide plays in five places.** A catch off a Heatball or a Caskball, a phony-swing ball and a heart-swing ball are each dropped by chance (`fielding.drops`, `Match.RollDrop`). A shell-swing or cask-swing grounder "warps" by chance (`fielding.park.shellWarpChance`). They are legal under §8.6's skill exception, but they are the only rolls left that decide a play.
- **Shared effects blur the captains.**
  - Zig and Fenn have the same swing.
  - Rio and Ashlord both lay a slow patch.
  - Rio, Zig and Fenn have the same catch-radius bonus under three names.
  - Brondo's pitch and swing are both decoys, and so are Konga's swing and Sable's pitch.
- **No swing uses PH-16-R2.** A Star Swing may change its contact area; none does.
- **Spin Check does nothing** in a live play.
- **The newest three captains** (Sable, Hollis, Reed) already follow the pattern the pass wants. Each ability is a distinct, geometric bend with named counterplay.

## Decision matrix

The recommendation is not the decision.

| Id | Area | Question | Options | Recommend |
| --- | --- | --- | --- | --- |
| AB-01 | Rules | Do rolls still decide plays for skills? | A retire §8.6's skill exception: no roll decides a catch, a drop or a bounce · B keep it | **Accepted: A** (Jack, 2026-09-25) |
| AB-02 | Rules | Must each captain's abilities be distinct? | A no two captains share an effect · B shared effects with different numbers | **Accepted: every captain Star Pitch and Star Swing is distinct; role players may carry simpler, repeated abilities; field abilities are shared (AB-12)** (Jack, 2026-09-25) |
| AB-03 | Process | How is the pass run? | A one round per captain · B one round per ability kind · C all at once | **Accepted: C, all at once** (Jack, 2026-09-25): the proposal below |
| AB-04 | Pitches | The effect families a Star Pitch may use | A speed · path shape · decoy · a payload on the catch, each captain in a different one · B path only | A |
| AB-05 | Swings | The effect families a Star Swing may use | A launch/exit · ball behaviour after contact · a fielder's body · terrain · contact area (PH-16-R2) · decoy, each captain in a different one · B fewer families | A |
| AB-06 | Swings | Who gets a contact-area swing (PH-16-R2)? | A a duplicated swing · B per captain · C not yet | **Accepted: B, per captain** (Jack, 2026-09-25): Fenn and Rio below |
| AB-07 | Field | Spin Check (#1010) | A redesign as a live, geometric rail · B retire it | Settled by AB-12: not in the starting pool |
| AB-08 | Field | The three copies of the catch-radius bonus | Settled by AB-12: not in the starting pool | — |
| AB-09 | Tiers | Which captains hold top-tier specials? | A keep Rio, Konga, Ashlord · B re-tier | A (the proposal keeps them) |
| AB-10 | Role players | Generic low-tier specials | A keep the six generics · B faction-flavoured variants | A |
| AB-11 | Tutorials | Lessons | A every changed ability updates its lesson in the same child (the tutorial rule) | A |
| AB-12 | Field | Are field abilities per captain? | A distinct per captain · B one shared pool for captains and role players | **Accepted: B** (Jack, 2026-09-25): the pool starts with Snap Throw, Lick Catch, Laser and Clamber |
| AB-13 | Look | Do the captains' themes and looks follow their abilities? | A yes: each captain gets a theme, look lines and a tell language that match the two Star abilities, and moves away from any look that reads as another game's character · B abilities only | A, in the matrix (direction for the art sessions; art waits for its gates) |

## The captain matrix (round 2)

Jack, 2026-09-25: be more creative, add the other three captains, and match each captain's theme and look to the abilities. Accepted rows keep their mechanic and take the new theme and name; ids stay. Numbers are trial starting points. The theme is a direction for the art sessions; art still waits for its gates.

| Captain | Theme | Star Pitch (family) | Star Swing (family) | Field | Tier |
| --- | --- | --- | --- | --- | --- |
| Rio Sparks | The harbor's fireworks kid | Skyrocket (Late rise) · Accepted | Sparkler (Bigger sweet spot) · Accepted | Laser | Top |
| Queen Vale | The Aurora Rink's figure-skating champion | Charmball (Swelling sway) · Accepted | Spotlight (One fielder paused) · Accepted | Snap Throw | Mid |
| Zig | The funfair chameleon | Loop-the-Loop (Path spectacle) · New | Spinning Top (Ball stalls on its hop) · New | Lick Catch | Mid |
| Brondo | The rooftop hustler | Phonyball (Decoy switch) · Accepted | Double Deal (Decoy ball) · Accepted | Laser | Mid |
| Konga | The canopy's storm drummer | Vine Swing (Pendulum arc) · New | Lightning Liner (Jagged flight) · New | Clamber | Top |
| Ashlord | The forge warlord of the volcano | Anvil (Late drop) · New | Hot Iron (Hot ball on the glove) · New | Laser | Top |
| Elder Fenn | The old sea turtle of the cove | Sea Mist (Payload on the runner) · Accepted | Driftwood Reach (Taller contact area) · Accepted | Snap Throw | Mid |
| Sable | The desert coyote trickster | Mirage (Vanish mid-flight) · Changed | Quicksand (Terrain) · Changed | Snap Throw | Mid |
| Hollis | The summit mountaineer | Belay (Speed hitch) · Changed | Summit Gust (Carry at the apex) · Changed | Clamber | Mid |
| Reed | The marsh frog | Skipping Stone (Skips on the dirt) · Changed | Leapfrog (Hops over a glove) · Changed | Lick Catch | Mid |

### Rio Sparks: the harbor's fireworks kid

- **Look:** Red with gold spark trim; Fuse-cord laces on the fat sneakers; Soot smudges on the round cheeks; Bat throws a spark trail on a charge. Tell language: gold sparks, a rocket whistle, a pop.
- **Original-IP distance:** Fireballs read as the plumber's; fireworks already belong to Harbor nights (the homer fireworks).
- **Star Pitch: Skyrocket** (was Heatball), accepted. ×1.15 speed; over the last third it rises up to 1 ft above its aimed crossing. Tell: A spark tail and a rising whistle. Counterplay: Aim high and start early; the rise is always up.
- **Star Swing: Sparkler** (was Heat swing), accepted. Exit ×1.15; the Perfect ring of the contact oval is ×1.5 (a sparkler ring while charged). Tell: A sparkler ring on the contact oval. Counterplay: Real contact is still needed; a miss is a miss.
- **Field:** Laser (shared pool).

### Queen Vale: the Aurora Rink's figure-skating champion

- **Look:** Pink and ice blue skating jersey; Skate-blade cleats; An aurora ribbon sash that trails on every move; Ice tiara in place of the crown. Tell language: ribbon trails, crystal chimes, a rink spotlight.
- **Original-IP distance:** A pink princess in a crown is the mushroom kingdom's; a skating champion with an ice tiara and aurora ribbons is the rink's own.
- **Star Pitch: Charmball**, accepted. ×0.9 speed; the wobble swells to its widest at mid-flight, then settles onto the aimed crossing by 0.85 of the flight. Tell: The ribbon traces the sway. Counterplay: Wait out the sway; the settled ball is the real read.
- **Star Swing: Spotlight** (was Heart swing), accepted. Exit ×1.05; the rink spotlight dazzles the nearest fielder, who pauses 0.8 s. Tell: A spotlight cone on that fielder. Counterplay: Other fielders back the dazzled one up.
- **Field:** Snap Throw (shared pool).

### Zig: the funfair chameleon

- **Look:** Turret eyes that swivel on their own (they replace the goggle discs); A curled tail; Skin cycles through the Carnival's rainbow bands; A ticket-stub bandolier. Tell language: colour cycling, a coaster clack, a calliope toot.
- **Original-IP distance:** A small green tongue-catcher reads as the dinosaur sidekick; turret eyes, a curled tail and skin that cycles the fair's colours make a chameleon of the fair.
- **Star Pitch: Loop-the-Loop** (was Prismball), new. Mid-flight the ball runs one full vertical loop, 4 ft across, like a coaster; then it goes on to its aimed crossing on the ordinary time. Tell: A coaster-track trail and a clack. Counterplay: The loop is always at the same point; time the exit, not the loop.
- **Star Swing: Spinning Top** (was Shell swing), new. A grounder that lands and spins in place for 0.8 s, then rolls on at half speed; Zig's legs beat the late charge. Tell: The ball spins like a top and whistles. Counterplay: Charge at contact; the ball is standing still.
- **Field:** Lick Catch (shared pool).

### Brondo: the rooftop hustler

- **Look:** Gold and charcoal pinstripe; Gold-rimmed shades and a toothpick; Rolled sleeves on the cube torso; A deck of cards in the back pocket. Tell language: shuffling cards, a coin flip.
- **Original-IP distance:** A greedy yellow brute reads as the plumber's rival; a smooth card sharp with shades and a pinstripe vest does not.
- **Star Pitch: Phonyball**, accepted. Shows one side early, switches late. Tell: A card flip at the switch. Counterplay: Read the switch.
- **Star Swing: Double Deal** (was Phony swing), accepted. Exit ×1.1; one decoy ball flies beside the real one until the apex. Tell: The decoy is a card-back ball. Counterplay: Fielders read the real ball at the apex.
- **Field:** Laser (shared pool).

### Konga: the canopy's storm drummer

- **Look:** Brown with moss green; A leaf crown; Vines wound on the long arms; Drum marks on the chest that glow on a swing; Barrel Bat becomes an ironwood log. Tell language: a thunder roll, falling leaves, a vine.
- **Original-IP distance:** A big ape with barrels reads as the jungle king of the other game. The barrels go; vines, leaves and thunder stay.
- **Star Pitch: Vine Swing** (was Caskball), new. The ball starts well outside the zone and swings in on one pendulum arc from a pivot above; it crosses on the aimed spot on the ordinary time. Tell: A vine drawn from the pivot to the ball. Counterplay: The vine shows the pivot; the arc is exact.
- **Star Swing: Lightning Liner** (was Cask swing), new. Exit ×1.2 on a liner; the ball jags sideways twice, up to 3 ft each, and lands on the spot a straight line would. Tell: A bolt trail, then thunder. Counterplay: Play the landing, not the flight; a glove at the end of the line takes it.
- **Field:** Clamber (shared pool).

### Ashlord: the forge warlord of the volcano

- **Look:** Black iron and ember purple, with glowing cracks; A forge helm with a chimney (the horns go); A leather apron with glowing rivets (the cape goes); Anvil-heavy boots; tongs on the back; Furnace Club stays. Tell language: an anvil clang, a glow that cools to iron.
- **Original-IP distance:** A horned, spiked fire king reads as the turtle king. A blacksmith warlord with a forge helm, an apron and tongs keeps the menace and loses the resemblance.
- **Star Pitch: Anvil** (was Skullball), new. ×1.2 speed, glowing; at 70 % of the flight it clangs and turns to cold iron, then drops up to 1.5 ft below its aimed height by the plate. Tell: The clang and the colour change. Counterplay: The clang comes first; aim low.
- **Star Swing: Hot Iron** (was Furnace), new. Exit ×1.25; the ball stays molten for 2 s after contact. A glove that holds it more than 0.5 s in that time drops it at its feet. Tell: The ball glows and hisses until it cools. Counterplay: Throw it at once, or let it cool on a hop; a caught fly is still an out.
- **Field:** Laser (shared pool).

### Elder Fenn: the old sea turtle of the cove

- **Look:** Sage shell with barnacles; the shell is the brim; Cream beard; Driftwood staff, slung; A fishing-net shawl. Tell language: sea mist, a conch hum.
- **Original-IP distance:** A walking turtle with a spiked shell is the turtle king's army; a barnacled sea-turtle fisherman with a driftwood staff is the cove's own.
- **Star Pitch: Sea Mist** (was Fogball), accepted. ×0.82 speed; on contact a 12 ft mist disc settles on home for 2 s and the batter-runner moves ×0.8 inside it. Tell: Mist rolls out of the ball on contact. Counterplay: Hit it far enough that the extra step does not matter.
- **Star Swing: Driftwood Reach** (was Staff swing), accepted. Exit ×1.08; the contact oval is ×1.4 tall (the staff reaches). Tell: The oval stretches up and down. Counterplay: Real contact is still needed; wide pitches still beat it.
- **Field:** Snap Throw (shared pool).

### Sable: the desert coyote trickster

- **Look:** Coyote ears; A rust-striped poncho and a sand scarf; Bone-bead wristbands; Heat shimmer rises off the shoulders at rest. Tell language: heat shimmer, a dry rattle.
- **Original-IP distance:** A trickster coyote is folklore, not a game mascot. Keep the ears soft, not a cartoon road-runner chaser.
- **Star Pitch: Mirage** (was Mirage Ball), changed. The ball vanishes into heat shimmer for the middle third of its flight; its shadow keeps crossing the dirt; it reappears for the last third. Tell: The shimmer and the shadow. Counterplay: Follow the shadow; the ball comes back where it points.
- **Star Swing: Quicksand** (was Sidewinder), changed. A grounder's first landing opens an 8 ft sand swirl for 2 s; a fielder inside it moves ×0.5. Tell: The swirl spins on the grass. Counterplay: Go round it, or take the ball past it.
- **Field:** Snap Throw (shared pool).

### Hollis: the summit mountaineer

- **Look:** Navy and white with signal-red straps; A climbing harness and a coiled rope; Snow goggles pushed up; Frost on the shoulders. Tell language: a rope creak, a gust, a snow flurry.
- **Original-IP distance:** Original.
- **Star Pitch: Belay** (was Rockfall), changed. At mid-flight the ball catches on a rope and hangs 0.15 s, then runs on to the plate, making up the time. Tell: The rope snaps taut. Counterplay: Time the second half, not the first.
- **Star Swing: Summit Gust** (was Updraft), changed. At its apex the fly catches a mountain gust and carries 15 % farther along its own line, in any park and any wind. Tell: A snow flurry bursts at the apex. Counterplay: Outfielders back up when the flurry bursts; the new landing is shown by its shadow.
- **Field:** Clamber (shared pool).

### Reed: the marsh frog

- **Look:** Green with lotus pink; Reed-woven wristbands; Webbed cleats; A throat pouch that puffs on every star action. Tell language: a croak, ripples, splash rings.
- **Original-IP distance:** Original.
- **Star Pitch: Skipping Stone** (was Leapfrog), changed. The pitch skips twice on the dirt in front of the plate, each skip lower, and pops up into the zone on its aimed crossing on the ordinary time. Tell: A splash ring at each skip. Counterplay: The second skip sets the height.
- **Star Swing: Leapfrog** (was Pond Skip), changed. A liner that hops 5 ft over the first infielder's glove it reaches, then drops back to its line. Tell: A croak at the hop. Counterplay: An infielder playing back takes it after the hop; the outfield plays it clean.
- **Field:** Lick Catch (shared pool).

### Every row passes

- **Timing window.** Every Star Pitch keeps the ordinary timing window; a path or speed change is the whole challenge.
- **Contact is contact.** No pitch turns contact into a miss; no swing makes a miss into contact.
- **Geometry decides.** No roll anywhere. A drop, a stall or a slow comes from a rule number and a position.
- **Two seconds.** Every bend ends within 2 s of the pitch or the contact.
- **Counterplay.** Every row names what beats it.
- **Fair to the CPU.** An effect that hides or fakes the ball (Mirage, Phonyball, Double Deal) costs a CPU the same read it costs a player.
- **Distinct.** No two captains share a family; role players keep the generic specials.
- **Ids stay.** Content ids stay stable; a new name is a display name.

### Field abilities: one shared pool (AB-12)

Field abilities are not captain identity. Captains and role players draw from one pool; each character carries one, as a data row.

| Ability | Effect |
| --- | --- |
| Snap Throw | 0.22-s release after a clean *received* throw |
| Lick Catch | A tongue snap takes a ball just out of reach |
| Laser | ×1.25 throw home with a live runner on third |
| Clamber | Rob ≤ 28 ft over at the wall |

The pool starts with these four. The rest (Grow, Withdraw, Spin Check, Sand Scoop, Long Toss, Lily Leap, Super Jump, Ball Dash, Dive, Burrow) are out of the starting pool; one comes back only by a later decision.

## Build order (after the rounds)

| Child | Kind | Scope |
| --- | --- | --- |
| AB-C0 | Gameplay | The rules AB-01 and AB-02 settle: spec §8.6 and §13, and a validator that refuses a shared effect family if AB-02 is A |
| AB-C1 … C10 | Gameplay | One per captain: Star Pitch and Star Swing as accepted, data rows, scenario rows, the lesson |
| AB-C13 | Gameplay | The field pool: every character carries one of the four; the others leave the roster data, their lessons and scenario rows |
| AB-C11 | Presentation | Tells for every new effect: VFX slots first, then the book pages |
| AB-C14 | Art | One child per captain for the new theme: catalog slots and extras first, then stills; each waits for the gates `docs/decisions/plan-world.md` names |
| AB-C12 | Balance | Only when Jack starts it: prices and tiers against play |
