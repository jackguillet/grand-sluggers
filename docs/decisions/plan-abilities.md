# Plan: every ability, re-evaluated as a whole

Status: **accepted; nothing here is built.** Every decision in the matrix is answered, and Jack accepted the abilities below as edited on the review page (2026-09-25). Tracker: #1011 (rescoped from four Star Pitch proposals to the whole set). Folds in #1010 (Spin Check, retired). Rules that stay in force: [plan-pitching-hitting.md](plan-pitching-hitting.md) PH-16 and its refinements, spec §12–§13, and principle 2 in [00-decisions](../spec/00-decisions.md). The cast, sidekicks, crews and park looks are in [plan-world.md](plan-world.md).

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
| AB-09 | Tiers | Which captains hold top-tier specials? | A keep Rio, Konga, Ashlord · B re-tier | **Accepted: tiers are retired** (Jack, 2026-09-25). A captain's Star Pitch and Star Swing cost 2 stars; a sidekick's generic specials cost 1. The price follows who carries the special, not a tier; the balance child may revisit the numbers. Spec §12's tier rows, `star-skills.json` `tier` and the top-tier validator go in AB-C0 |
| AB-10 | Sidekicks | Sidekicks' Star Pitches and Star Swings | A keep the six generics · B faction-flavoured variants | **Accepted: a sidekick pool, never a captain's ability** (Jack, 2026-09-25). Sidekicks' pitching and hitting specials come only from a generic pool (today Star Fastball, Change, Breaker; Star Grounder, Fly, Line); each species carries one pitch and one swing. Field abilities stay one pool for captains and sidekicks (AB-12) |
| AB-11 | Tutorials | Lessons | A every changed ability updates its lesson in the same child (the tutorial rule) | A |
| AB-12 | Field | Are field abilities per captain? | A distinct per captain · B one shared pool for captains and role players | **Accepted: B** (Jack, 2026-09-25): the pool is Snap Throw, Lick Catch, Laser, Relay Pivot and Wall Spring; Clamber is out. Canopy Yard's climb wall becomes a **park rule** (Jack, 2026-09-25): at that park any fielder at the wall climbs it and can rob a ball over it, whatever ability they carry; the height starts at Clamber's 28 ft as a trial number (C13) |
| AB-13 | Look | Do the captains' themes and looks follow their abilities? | A yes: each captain gets a theme, look lines and a tell language that match the two Star abilities, and moves away from any look that reads as another game's character · B abilities only | A, in the matrix (direction for the art sessions; art waits for its gates) |

## The accepted abilities

Jack accepted the review page as edited (2026-09-25). Numbers are trial starting points. A captain's Star Pitch and Star Swing cost 2 stars; a sidekick's cost 1 (AB-09). Content ids stay; the names are display names.

### Captains

| Captain (id) | Star Pitch | Star Swing | Field |
| --- | --- | --- | --- |
| Ronnie Sparks (`rio`) | **Skyrocket** (Extra Fast) | **Sparkler** (Bigger sweet spot) | Laser |
| Vale (`vale`) | **Aurora Ribbon** (Swelling sway) | **Follow Spot** (One fielder paused) | Snap Throw |
| Zig (`zig`) | **Loop-the-Loop** (Path spectacle) | **Spinning Top** (Ball stalls on its hop) | Lick Catch |
| Brondo (`brondo`) | **Phonyball** (Decoy switch) | **Double Deal** (Decoy ball) | Laser |
| Tambo (`konga`) | **Vine Swing** (Pendulum arc) | **Lightning Liner** (Jagged flight) | Wall Spring |
| Ashlord (`ashlord`) | **Anvil** (Late drop) | **Hot Iron** (Hot ball on the glove) | Laser |
| Elder Fenn (`fenn`) | **Undertow** (Payload on the runner) | **Driftwood Reach** (Taller contact area) | Snap Throw |
| Arroyo (`sable`) | **Mirage** (Vanish mid-flight) | **Dust Bowl** (Terrain) | Snap Throw |
| Hollis (`hollis`) | **Cable Car** (Speed hitch) | **Summit Gust** (Carry at the apex) | Wall Spring |
| Reed (`reed`) | **Skipping Stone** (Skips on the dirt) | **Lily Hop** (Hops over a glove) | Lick Catch |

#### Ronnie Sparks

- **Star Pitch: Skyrocket** (was Heatball). ×1.15 speed; over the last third it rises up to 1 ft above its aimed crossing. Tell: A tail of gold portal sparks and a rising whistle. Counterplay: Aim high and start early; the rise is always up.
- **Star Swing: Sparkler** (was Heat swing). Exit ×1.15; the Perfect ring of the contact oval is ×1.5 (a sparkler ring while charged). Tell: A ring of portal sparks on the contact oval. Counterplay: Real contact is still needed; a miss is a miss.

#### Vale

- **Star Pitch: Aurora Ribbon** (was Charmball). ×0.9 speed; the wobble swells to its widest at mid-flight, then settles onto the aimed crossing by 0.85 of the flight. Tell: An aurora ribbon traces the sway. Counterplay: Wait out the sway; the settled ball is the real read.
- **Star Swing: Follow Spot** (was Heart swing). Exit ×1.05; the rink's follow spot dazzles the nearest fielder, who pauses 0.8 s. Tell: The follow spot's cone lands on that fielder. Counterplay: Other fielders back the dazzled one up.

#### Zig

- **Star Pitch: Loop-the-Loop** (was Prismball). Mid-flight the ball runs one full vertical loop, 4 ft across, like a coaster; then it goes on to its aimed crossing on the ordinary time. Tell: A coaster-track trail and a clack. Counterplay: The loop is always at the same point; time the exit, not the loop.
- **Star Swing: Spinning Top** (was Shell swing). A grounder that lands and spins in place for 0.8 s, then rolls on at half speed; Zig's legs beat the late charge. Tell: The ball spins like a top and whistles. Counterplay: Charge at contact; the ball is standing still.

#### Brondo

- **Star Pitch: Phonyball** (was Phonyball). Shows one side early, switches late. Tell: A card flip at the switch. Counterplay: Read the switch.
- **Star Swing: Double Deal** (was Phony swing). Exit ×1.1; one decoy ball flies beside the real one until the apex. Tell: The decoy is a card-back ball. Counterplay: Fielders read the real ball at the apex.

#### Tambo

- **Star Pitch: Vine Swing** (was Caskball). The ball starts well outside the zone and swings in on one pendulum arc from a pivot above; it crosses on the aimed spot on the ordinary time. Tell: A vine drawn from the pivot to the ball. Counterplay: The vine shows the pivot; the arc is exact.
- **Star Swing: Lightning Liner** (was Cask swing). Exit ×1.2 on a liner; the ball jags sideways twice, up to 3 ft each, and lands on the spot a straight line would. Tell: A bolt trail, then thunder. Counterplay: Play the landing, not the flight; a glove at the end of the line takes it.

#### Ashlord

- **Star Pitch: Anvil** (was Skullball). ×1.2 speed, glowing; at 70 % of the flight it clangs and turns to cold iron, then drops up to 1.5 ft below its aimed height by the plate. Tell: The clang and the colour change. Counterplay: The clang comes first; aim low.
- **Star Swing: Hot Iron** (was Furnace). Exit ×1.25; the ball stays molten for 2 s after contact. A glove that holds it more than 0.5 s in that time drops it at its feet. Tell: The ball glows and hisses until it cools. Counterplay: Throw it at once, or let it cool on a hop; a caught fly is still an out.

#### Elder Fenn

- **Star Pitch: Undertow** (was Fogball). ×0.82 speed; on contact a 12 ft undertow ring washes out on home for 2 s and the batter-runner moves ×0.8 inside it. Tell: A wave ring washes out from home. Counterplay: Hit it far enough that the extra step does not matter.
- **Star Swing: Driftwood Reach** (was Staff swing). Exit ×1.08; the contact oval is ×1.4 tall (the staff reaches). Tell: The oval stretches up and down like a driftwood staff. Counterplay: Real contact is still needed; wide pitches still beat it.

#### Arroyo

- **Star Pitch: Mirage** (was Mirage Ball). The ball vanishes into heat shimmer for the middle third of its flight; its shadow keeps crossing the dirt; it reappears for the last third. Tell: The shimmer and the shadow. Counterplay: Follow the shadow; the ball comes back where it points.
- **Star Swing: Dust Bowl** (was Sidewinder). A grounder's first landing kicks up an 8 ft bowl of loose dust for 2 s; a fielder inside it moves ×0.5. Tell: A dust bowl swirls on the hard-pan. Counterplay: Go round it, or take the ball past it.

#### Hollis

- **Star Pitch: Cable Car** (was Rockfall). At mid-flight the ball stops at a cable-car station for 0.15 s, then runs down its line to the plate, making up the time. Tell: A cable line and a bell at the stop. Counterplay: Time the run down the line, not the stop.
- **Star Swing: Summit Gust** (was Updraft). At its apex the fly catches a mountain gust and carries 15 % farther along its own line, in any park and any wind. Tell: A snow flurry bursts at the apex. Counterplay: Outfielders back up when the flurry bursts; the new landing is shown by its shadow.

#### Reed

- **Star Pitch: Skipping Stone** (was Leapfrog). The pitch skips twice on the dirt in front of the plate, each skip lower, and pops up into the zone on its aimed crossing on the ordinary time. Tell: A splash ring at each skip. Counterplay: The second skip sets the height.
- **Star Swing: Lily Hop** (was Pond Skip). A liner that hops 5 ft over the first infielder's glove it reaches, then drops back to its line. Tell: A lily pad flashes under the ball at the hop. Counterplay: An infielder playing back takes it after the hop; the outfield plays it clean.

### Sidekick specials (AB-10)

Sidekicks' Star Pitches and Star Swings come only from this pool; none is a captain's. Each species carries one pitch and one swing.

| Kind | Special | Effect |
| --- | --- | --- |
| pitch | Star Fastball | ×1.25 speed |
| pitch | Star Change | ×0.7 speed |
| pitch | Star Breaker | ×0.85 speed and a late break |
| pitch | Star Dot | ×1.05 speed and no aim scatter: it crosses exactly where it was aimed |
| pitch | Star Sinker | A ball put in play off it leaves 6° lower, so it is more often on the ground |
| pitch | Star Lob | ×0.75 speed on a high arc that falls through the zone on the ordinary time |
| pitch | Star Sidearm | Released 2 ft wider, so it crosses the zone on a diagonal |
| swing | Star Grounder | Launch 8°, exit ×1.2 |
| swing | Star Fly | Launch 38°, exit ×1.25 |
| swing | Star Line | Launch 18°, exit ×1.2 |
| swing | Star Pull | Exit ×1.15 and the ball goes 10° toward the pull line |
| swing | Star Opposite | Exit ×1.1 and the ball goes 10° toward the opposite field |
| swing | Star Chopper | Launch −10°; the first bounce rises ×1.6, high enough to beat out |
| swing | Star Drag Bunt | A squared bunt that rolls along the line and stops within 1 ft of fair |

### Field pool (AB-12), for captains and sidekicks

| Ability | Effect | Who may carry it |
| --- | --- | --- |
| Snap Throw | 0.22-s release after a clean received throw | Anyone |
| Lick Catch | A tongue snap takes a ball just out of reach | Tongue bodies: Zig, Reed and their role players |
| Laser | ×1.25 throw home with a live runner on third | Anyone |
| Relay Pivot | A cutoff fielder catches a throw and throws on in 0.15 s | Anyone |
| Wall Spring | Springs off any wall for +4 ft of reach | Anyone |

Lick Catch is the pressed 8 ft tongue snap (0.3 s, 0.8 s recovery), for tongue bodies only. Every other field ability is out of the game until a later decision. Canopy Yard's climb wall is a park rule (AB-12).

How it got here: round 1 set the rules (AB-01 to AB-12); round 2 proposed a matrix; rounds 3 and 4 redrafted every captain around the home parks and the story frame on a live review page (https://claude.ai/artifact/2EyWdxgTiSXnN71r8nXHC4), which Jack edited and accepted. The earlier drafts are in this file's git history.

## Build order

| Child | Kind | Scope |
| --- | --- | --- |
| AB-C0 | Gameplay | The rules: spec §8.6 (no skill roll), §12 (tiers out; a captain's special costs 2, a sidekick's 1) and §13; a validator that refuses two captains sharing an effect family and a sidekick carrying a captain's special |
| AB-C1 … C10 | Gameplay | One per captain: Star Pitch and Star Swing as accepted, data rows, scenario rows, the lesson |
| AB-C11 | Gameplay | Sidekick specials: the eight new generic specials, each species' pitch and swing, the lessons |
| AB-C13 | Gameplay | The field pool: Relay Pivot and Wall Spring built; every character carries one of the five; the rest leave the data, lessons and scenario rows; Canopy Yard's climb wall becomes a park rule |
| AB-C15 | Presentation | Tells for every new effect: VFX slots first, then the book pages |
| AB-C12 | Balance | Only when Jack starts it: prices against play |
