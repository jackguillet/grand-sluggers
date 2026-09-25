# Plan: every ability, re-evaluated as a whole

Status: **planning. Nothing here is built.** AB-01, AB-02, AB-03, AB-06 and AB-12 are accepted. In the captain proposal Rio, Vale, Brondo and Fenn are accepted; Zig and Ashlord wait for a second look and Konga for a new proposal. Tracker: #1011 (rescoped from four Star Pitch proposals to the whole set, Jack, 2026-09-25). Folds in #1010 (Spin Check). Rules that stay in force: [plan-pitching-hitting.md](plan-pitching-hitting.md) PH-16 and its refinements, spec §12–§13, and principle 2 in [00-decisions](../spec/00-decisions.md).

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

## The proposal: every captain ability (AB-03 C)

One sitting reviews the twenty Star Pitches and Star Swings; field abilities are the shared pool (AB-12). Each row names the effect, what decides it (geometry) and the counterplay. **Kept** means today's rule stands; **Changed** replaces it; **New** fills a placeholder. Every roll in the old rows is gone (AB-01); no Star Pitch touches the timing window or turns contact into a miss (PH-16-R1). Numbers are trial starting points, not decisions.

### Star Pitches

| Captain | Pitch | Effect | Counterplay | |
| --- | --- | --- | --- | --- |
| Rio | Heatball (top) | ×1.15 speed, and over the last third of the flight it **rises** up to 1 ft above its aimed crossing: a riser with a flame trail | The trail shows the rise; aim high and time the fast ball | Changed: the drop roll goes. **Accepted** |
| Vale | Charmball (mid) | ×0.9 speed; the wobble **swells** to its widest at mid-flight, then settles onto the aimed crossing by 0.85 of the flight | Wait out the sway; the settled ball is the real read | Changed. **Accepted** |
| Zig | Prismball (mid) | Late sideways break with ghost images | Read the break past the ghosts | Kept. **Second look** |
| Brondo | Phonyball (mid) | Decoy: shows one side early, switches late | Read the switch | Kept (roll already gone). **Accepted** |
| Konga | Caskball (top) | ×0.85 speed and the barrel **climbs** late (`caskballRise`); a ball put in play off it is heavy: its exit ×0.9 | Square it up; a Perfect still flies | Changed: the drop roll goes, the heavy exit replaces it. **Replaced: new options below** |
| Ashlord | Skullball (top) | ×1.2 speed; a skull flashes at a fixed point late in the flight and the ball **drops** up to 1.5 ft below its aimed height by the plate (a guillotine) | The flash is the tell; read the drop, aim low | New. **Second look** |
| Fenn | Fogball (mid) | ×0.82 speed; on contact a **fog disc** (12 ft) settles on home for 2 s and the batter-runner moves ×0.8 inside it | Hit it far enough that the extra step does not matter | New. **Accepted** |
| Sable | Mirage Ball (mid) | A fading twin to the far half of the zone | Pick the real ball before the twin fades | Kept |
| Hollis | Rockfall (mid) | Floats up to 2.2 ft and drops back onto its path | The crossing is the ordinary one | Kept |
| Reed | Leapfrog (mid) | Crawls, then leaps to arrive on time | The arrival is the ordinary one | Kept |

### Star Swings

| Captain | Swing | Effect | Counterplay | |
| --- | --- | --- | --- | --- |
| Rio | Heat swing (top) | Exit ×1.15 and a **bigger sweet spot**: the Perfect ring of the contact oval ×1.5 (PH-16-R2) | Real contact is still required; a miss is a miss | Changed: the burn patch and the burn-hop go. **Accepted** |
| Vale | Heart swing (mid) | Exit ×1.05; the nearest fielder **pauses** 0.8 s | Other fielders back them up | Changed: the drop roll goes. **Accepted** |
| Zig | Shell swing (mid) | Exit ×1.1 on the ground; the first hop **bites**: the ball keeps 30 % of its speed after it (a spinning shell) | Charge it; a fielder playing in wins | Changed: the warp roll goes. **Second look** |
| Brondo | Phony swing (mid) | Exit ×1.1; one **decoy ball** flies beside the real one until the apex | Fielders read the real ball at the apex | Changed: the drop roll goes. **Accepted** |
| Konga | Cask swing (top) | Exit ×1.2; the fielder who takes it is **knocked back** 0.4 s with the ball (a barrel's weight) | A cutoff or a second glove covers the delay | Changed: the decoy fragments and the warp roll go. **Replaced: new options below** |
| Ashlord | Furnace (top) | Exit ×1.25; a **lava strip** on the warning track where it lands slows a fielder ×0.45 for 2 s | Play off the track; cut the ball off short | Kept (now the only terrain swing). **Second look** |
| Fenn | Staff swing (mid) | Exit ×1.08 and a **taller contact area** (the staff reaches): the oval ×1.4 tall (PH-16-R2) | Real contact is still required; wide pitches still beat it | Changed: the warp roll goes. **Accepted** |
| Sable | Sidewinder (mid) | The first hop kicks ≤ 20° away from the chaser | A ball caught before its hop never turns | Kept |
| Hollis | Updraft (mid) | The fly rides the park's wind ×1.5 | Calm parks; play the wind | Kept |
| Reed | Pond Skip (mid) | A chopper whose first hop springs ×2.2 | Glove it before it springs | Kept |

### Konga: new options (the first proposal is withdrawn)

| Kind | Option | Effect | Counterplay |
| --- | --- | --- | --- |
| Pitch | Barrel Lob | A high rainbow arc that drops steeply through the zone on the ordinary time; its shadow marks the crossing on the plate | Read the shadow; the steep fall punishes a late read, not a good one |
| Pitch | Heavy Barrel | A ball put in play off it staggers the batter-runner 0.5 s out of the box; a Perfect shrugs it off | Square it up |
| Swing | Ground Pound | On contact the stomp roots every infielder for 0.3 s | Outfielders are untouched; a ball in the air loses the effect |
| Swing | Rolling Barrel | A grounder that does not slow on the grass; it keeps its exit speed until a glove or the wall stops it | A body in front; the roll is straight and true, never a bad hop |

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
| AB-C12 | Balance | Only when Jack starts it: prices and tiers against play |
