# Research: Mario Super Sluggers

*Mario Super Sluggers* (Wii, 2008) is the second *Super Mario Stadium* baseball game, after *Mario Superstar Baseball* (GameCube, 2005). Nintendo has not shipped a third. As of 2026 the series is a 17-year gap, and coverage still treats a Switch 2 sequel as an obvious hole.

This doc is a systems teardown so Grand Sluggers can steal the *shape* of the game, not the IP.

Sources: Super Mario Wiki (game, Chemistry, Star Skill, stadium pages), instruction booklet summaries, contemporary and retrospective reviews.

## What made it feel like itself

Five systems stacked on top of ordinary baseball:

1. **A huge cartoon roster with hidden roles.** ~40 unique characters (72 with color variants and Miis). Captains vs. role players. Stats on a 1–10 grid: Pitch, Bat, Field, Run. Almost nobody is a 10. Bowser/Petey/King K. Rool hit 10 Bat. Field never hits 9. The roster is *readable archetypes*, not a sim.
2. **Chemistry.** Pairwise good / bad / none. It is the draft puzzle.
3. **Star Skills.** A shared 5-star meter. Captains spend stars on unique pitches and swings. Role players spend stars on generic juice (fast / slow / break).
4. **Character fielding abilities.** Super Jump, Tongue Catch, Clamber, Laser Beam, Burrow, Teleport, etc. These are *who you are on defense*, not just a stat.
5. **Parks as gimmicks.** Nine stadiums. Mario Stadium is the only “real” diamond. The rest eat balls, freeze runners, tilt, catch fire, or go dark.

Plus: error items on chemistry at-bats, day/night variants, Challenge Mode recruiting, Toy Field (a party minigame park), unique bats as character cosmetics (Baby DK’s banana, Bowser’s spiked bat).

## Controls (Wii)

Three schemes: Remote vertical, Remote horizontal, Remote + Nunchuk.

Batting and pitching are **timing + charge**. Pull back to charge, swing/release on the ball. Curve by twisting. Changeup on a button. Fielding: move, catch, swing the remote to throw to a base.

We will not require motion. Gamepad timing is the default. Motion can be a later input profile.

## Chemistry (the distinctive system)

From *Mario Superstar Baseball*, expanded in Sluggers.

**Representation.** Each pair of characters has a hidden 0–100 affinity (Superstar Baseball). ≥90 (or “listed as good” in Sluggers) is good chemistry. ≤15 is bad. Everyone else is neutral.

**On the field**

| Relation | Effect |
| --- | --- |
| Good, throwing | Faster throw, purple trail, happy VO. Buddy Throw. |
| Good, outfield | Buddy Jump (leap off a partner to rob a homer). Buddy tackle → roll to partner → laser throw. |
| Good, batting | If on-deck has chemistry with the batter, an **error item** (shell, banana, bob-omb, mini-boo, POW, fireball) can be aimed at fielders after contact. |
| Good, baserunners | Charge-power multiplier: 1 buddy on base ×1.1, 2 ×1.25, 3 ×1.5 (Superstar Baseball tables). |
| Bad, throwing | Slow, off-line throws. Errors. Not always — RNG so it feels like a screw-up, not a rule. |
| None | Ordinary baseball. |

**On the draft screen**

Team Stars at game start = average chemistry of the roster *with the captain*.

| Avg chemistry with captain | Starting stars |
| --- | --- |
| ≥ 70 | 5 |
| 55–69 | 4 |
| 35–54 | 3 |
| 15–34 | 2 |
| 0–14 | 1 |
| 0 (all clones, cheat) | 0 |

A “dream team” of isolated stars starts the game starved for specials. A faction-pure roster starts loaded.

**Social graph, not a stat.** Good chemistry clusters: Mario crew, Kong family, Koopa army, babies with Yoshi, Boos with each other. Bad chemistry is rivalry (Mario/Bowser, Luigi/Waluigi) or type clash (babies vs. ghosts). Some characters are loners (Wario/Waluigi almost only like each other).

**Steal this.** A visible faction graph + a few cross-faction buddy pairs + rivalries. Starting stars from captain affinity. Buddy jump / buddy throw. Bad throws as comedy. Item drops on chemistry at-bats.

**Do not steal.** Nintendo’s character list, the ♪ / squiggle UI as a 1:1 copy, Miis.

## Star Skills

- Shared meter, max 5 stars.
- Earn stars by executing well, and by hitting park features (e.g. a yellow Piranha Plant).
- Captains: unique Star Pitch *and* unique Star Swing. These are the trailer shots.
- Role players: one of three batting shapes (grounder / fly / line drive, extra velocity) and one of three pitches (fastball / changeup / breaker).
- Secondary captains on a team cost **two** stars to fire a unique skill (one if they strike).
- Perfect charged captain swing in Superstar Baseball could dump the whole meter into a guaranteed homer. Sluggers pulled that back. We should not ship a “spend 5, free homer” button.

**Captain specials in Sluggers (pattern language, not to copy 1:1)**

| Pattern | Example | What it does to baseball |
| --- | --- | --- |
| Elemental ball | Mario fire, Luigi tornado, Bowser bullet | The ball itself is a hazard / displacement |
| Screen / vision | Bowser Jr. graffiti | Batter or fielder cannot see |
| Decoy | Wario phony, Waluigi liar | Fake ball, late reveal |
| Area deny | Daisy flowers in the outfield | Terrain changes after the swing |
| Pull / swallow | Birdo suction / cannon | Ball path is illegal physics |
| Status | Peach hearts | Charm / freeze a fielder |
| Projectile payload | DK barrel, Diddy banana | Extra object on the field |

Role of the special: **break a baseball rule for two seconds, then baseball resumes.** That is the design test.

## Fielding abilities (who you are)

Not just stats. A move you *have*:

- Super Jump, Super Dive, Clamber (walls), Burrow, Teleport
- Tongue / suction / piranha / magical catch (catch from farther)
- Laser Beam / Quick Throw / Hammer Throw (throw variants)
- Enlarge, Spin Attack, Body Check, Angry Attack (contact defense)
- Ink Dive, Scatter Dive, Keeper Catch

**Steal this.** Every character has exactly one defensive verb. Captains’ verbs are flashier. Shared verbs inside a faction (Kong family all Clamber) teach the graph.

## Stats

Four numbers, 1–10, shown on the team screen:

- **Pitch** — velocity, break, stamina
- **Bat** — power *and* contact mixed into one readable number (Sluggers did this; we may split contact/power internally and show one)
- **Field** — range, hands, jump
- **Run** — speed on the bases and in the outfield

Pitcher stamina is a real resource: long outings, homers allowed, and star pitches fatigue the arm. A tired pitcher becomes erratic. You can swap (but then your fielding alignment changes).

Handedness is per character (bat side, throw side). Sluggers removed the GameCube option to flip it. Keep it authored.

## Parks

Nine (+ Toy Field). Each captain (almost) has a home park. Day/night on most. Night is not just a skybox: Peach Ice Garden goes dark except spotlights; Yoshi Park spawns Piranha Plants only at night; Daisy Cruiser tilts; Bowser Castle breathes fire.

| Park | Gimmick |
| --- | --- |
| Mario Stadium | None. The “real baseball” control park. Fireworks at night. |
| Peach Ice Garden | Ice rink. Freezies freeze you. Night: ceiling stars black out the stadium. |
| Yoshi Park | Amusement park. Warp pipes randomize grounders. Train on the warning track. Night: Piranha Plants eat balls. |
| Wario City | Rooftop / industrial. Urban obstacles, gem gimmicks. |
| DK Jungle | Vines, barrels, climbable walls. |
| Bowser Jr. Playroom | Day only. Chaotic toy terrain. |
| Bowser Castle | Night only. Lava, Podoboos, statue fire breath. |
| Luigi’s Mansion | Night only. Ghosts, lights. |
| Daisy Cruiser | Ship. Day/night. Deck tilt, Cheep Cheeps, Gooper Blooper. |
| Toy Field | Not a baseball game — party point-space minigame. |

**Steal this.** One clean diamond as the tutorial park. Every other park has *one primary gimmick* and a night variant. Unlock parks through play, not a shop full of DLC.

## Gear

Sluggers mixed three things:

1. **Cosmetic signature bats** (banana, rattle, spiked club) — identity.
2. **Shop stat items** that last one game: Nice Bat (+contact), Power Bat, Charge Bat (always full charge), Lucky Glove (fewer errors, stronger arm), Dr. K (throw speed), Dash Spikes, Buddy Badge (chemistry with everyone), Error Booster.
3. **Error items** used mid-play from chemistry (shells, bananas, POW).

Grand Sluggers keeps signature bat stats as the loadout, while active batting
uses one common authored bat for a consistent grip and swing silhouette. Gloves
remain loadout and look. Buddy Badge is too strong as a default — keep it as a
rare challenge reward, not a draft crutch.

## Modes worth copying later

- **Exhibition** — pick captain, 8 others, park, innings. The game.
- **Challenge** — island hub, recruit by missions, captain traversal abilities (magnet, vines, manholes). This is a second game. Do not start here.
- **Toy Field / minigames** — party modes. After exhibition is fun.
- **MVP** — postgame scoring that celebrates plays, not just box score. Cheap, do it early.

## What reviews actually liked / hated

Liked: roster size, chemistry as a draft puzzle, motion batting that felt like tennis rather than a real stance (for some), gimmick parks, star skills as personality.

Hated / mixed: motion throwing in the outfield, some specials that steal agency from the other player (full-screen paint), Challenge Mode busywork, no European SKU.

**Design warning:** specials that *blind* or *softlock* the opponent are trailer candy and couch poison. Prefer specials that change the *ball* or the *field*, not the other player’s eyes.

## What we will not copy

- Nintendo characters, names, music, parks, or UI chrome
- Wii-only motion as a requirement
- Mii integration
- Superstar Matchup cutscenes that fire on arbitrary scoreboard states
- A 72-character launch roster. We start with 2 captains and grow to ~24, then ~40.

---

## Mechanics teardown (2026-09-12)

What the two games actually do moment to moment, gathered for [gameplay-spec.md](gameplay-spec.md). MSS = *Mario Super Sluggers* (Wii, 2008). MSB = *Mario Superstar Baseball* (GameCube, 2005). MSS has no public frame data; where only MSB numbers exist they are used as the proxy, since reviewers and the wiki call the games near-identical in play. Source keys are at the end. "UNVERIFIED" is recollection with no source found.

### Batting

- Normal swing: "a quick swing done without a windup… they create better contact." Charge swing: "swing when the charge is at maximum to unleash a mighty blow" (hold-and-release the same button). [MAN-W]
- The cursor moves **with the batter**; "you'll make better contact… and hit with more power if you strike it with the center of the cursor." [MAN-W] MSB: the cursor is an *Easy* option; a "Drop Spot" toggle shows where the ball lands. [MAN-GC]
- MSB stick: left/right pushes the angle; **up = ground balls more likely, down = pop flies**. L recenters the batter. [MH-BAT]
- MSB contact window: **9 frames for a slap, 7 for a charge**. Early contact **pulls**, late contact **pushes**; the direction table runs ≈ ±55° across the window, with the stick shifting ±10–15°. [MH-BAT][MH-HA][MH-GLOSS]
- MSB bat hitbox: five zones **sour / nice / perfect / nice / sour**, asymmetric per character, scaled by a contact multiplier; slap zones widen ×1.05/1.10/1.20 with 1/2/3 chemistry links on base; charge zones are narrower. PERFECT flashes on the sweet spot. [MH-CZ][GF-MSB]
- MSB exit velocity: slap sour 100–130 / nice 140–145 / perfect 145–150; charge sour 140–150 / nice 162–177 / perfect 160–170 (perfect charge carries: no added gravity). Final EV × (0.8 + 0.2 × power/100) × pitch factor (perfect charge pitch vs sour slap ×0.6; vs perfect charge ×1.1). Pull/push hitters ×1.05 / ×0.85 by side. A hidden pitcher stat dampens non-perfect contact. [MH-BAT][MH-PTI][MH-CB]
- Charge: 100% when the golden crater appears, holds ≈30 frames, then overcharges down. [MH-IP][MH-BS]
- Launch angle: five bands with probabilities by (vertical type × swing × zone); stick moves mass to the lowest/highest band. **A sour slap on a changeup or charge pitch is forced to a pop-up.** [MH-LA]
- Bunt: hold a button to square (MSS 1 / Z; MSB B). Foul bunt on two strikes is out. Stick left → third-base line, right → first-base line. [MAN-W][MH-BUNT] Bunting everyone is the MSS speedrun route against the CPU. [SR-MSS]
- MSB "5-star dinger": with 5 stars, charge + perfect = automatic homer, consumes all five. **MSS has no such rule** (no source; UNVERIFIED as an absence). [MH-BAT][MW-MSB]
- Star swing cost: captain 1; a captain on another team's roster 2; a *missed* star swing costs 1 for everyone. Non-captains get ground / fly / line. [MW-SS][MAN-GC][MH-BAT]
- MSS star swings all bend the *ball* or a *fielder* for a beat: burning ball (first fielder to touch it is burned), tornado carry, hearts that stun, egg that bounces (can bounce over for a ground-rule double), cannon egg that stuns then leaves a dive window, barrel, sharp curve, Bob-omb pop-up that fielders avoid until it lands, zig-zag grounder, three fireballs, decoy ball then real ball. [MW star-skill pages]

### Pitching

- Normal (easier control) / charge ("wind up… release at maximum") / changeup (hold a modifier: "a floating pitch with good movement") / **curve applied after release** by twisting or tilting toward that side / star = two buttons. Pitcher moves left/right on the mound; a button resets. [MAN-W][MAN-GC]
- MSB curve: post-release, **direction only** (stick magnitude ignored); how fast the bend reaches full is a stat (8 → 2 frames). **Charge pitches and changeups get 2% of the curve stat** — essentially straight. Mound position ±0.4 m. Charge "Perfect Pitch": release within 15 frames of MAX → ×1.05 speed. [MH-PIT]
- MSB strike zone: x ∈ ±0.53, checked over a depth band; HBP tested against the batter's hitbox when the pitch is not a strike. [MH-PIT]
- Stamina (MSS booklet): "drops as the game continues… more quickly when the opposing team gets consecutive hits. When a pitcher is out of stamina, ball speed and control drop dramatically." Wiki: long outings, runs, star pitches, homers fatigue; a grand slam can empty it. [MAN-W][MW-MSS] MSB tired: curve and control ×0.01, charge speed −80%; star pitch refused at 0 stamina. Swap via pause. [MH-PIT][MAN-GC]
- Pickoff: MSS Z / A picks a base then throw; MSB B. **"It's not possible to get a runner out on a pure pick-off — the runner will always get back in time."** The pickoff catches runners already told to steal: they take off when the pitcher moves. [MAN-W][MH-PIT][MH-FLD]
- CPU pitcher (MSB): charge chance by class/difficulty (5–70%), changeup after charge (0–40%), perfect-pitch chance rises with strikes (30/50/60% on the hardest); star pitch only in named situations (+10% per strike); pickoff attempts 3–10%, lead runner except first-and-third (66% to first). [MH-PAI]

### Fielding

- Fielders auto-pursue; the nearest is selected (MSB: "usually… somewhat irregular"). Throw = direction + button (right 1B, up 2B, left 3B, down home); no direction = cutoff or "whatever base the fielder deems necessary"; a modifier throws to the cutoff man; another switches fielders. Shake/tap = dash. [MAN-W][MAN-GC][MH-THR]
- MSB **lockout after contact** before a fielder can move: P 25, C 40, 1B 16, 2B 15, 3B 18, SS 17, OF 50 frames; the camera cuts to the field at frame 25. [MH-FLD]
- MSB throw speed = arm × situation: 1.0 normal, 0.9 to nip a batter, 0.7–0.8 short non-critical, **0.3–0.35 lazy lob when every runner is ≥ 80% home in a rundown**, 0.25 underhand inside 6 m. **A throw to an uncovered base slows so someone can arrive; otherwise it drops.** [MH-THR]
- Jump (standing) 2.1 units, Super Jump 3.8; dive while dashing, auto-aimed, range = frames-to-landing × sprint in a 60° cone; wall jump / clamber rob heights; bobble 1–15% by class and centering; knockback on hard hits scales with weight. [MH-FLD]
- Buddy jump: two chem partners near the wall ("A twice"). Buddy toss: attack a ball to fire it back fast. Attack destroys an error item. [MAN-W][MW-MSS]
- Chemistry throws: good ≈ ×1.3 speed, purple trail, never to the cutoff; **bad = 20% chance of a slow, slanted throw**. "Only occurs in fielding, not consistently." [MW-CHEM][MH-THR][MH-CHEM]
- CPU fielders always cover every base (pitcher backfills); **cover movement is a constant speed regardless of stat, starting 14 frames after contact**; idle outfielders take support spots or back up 20 m behind a throw. Throw target by a per-runner "desperation" score and an "urgency" level: tag-ups within 45 m first, then home within ≈ 62–75 m, then third, second, else cutoff. [MH-FLD][MH-AUTO][MH-FAI]
- Error items (MSS): batter-and-next-batter chemistry; aimed with the pointer after the hit; shell, Bob-omb, banana, fireball, Mini Boo (ball invisible), POW (quake stun). Fielders can attack them. [MW-MSS][MAN-W]

### Baserunning

- MSS remote-only: baserunning is **automatic** except dash. Nunchuk: one button sends all / steals, the other sends all back, tap the opposite to halt, stick + button for one runner. **Steal: pick a base and press before the pitch; "the runner will start stealing when the pitcher releases the ball."** [MAN-W] MSB: Y advance/steal, X return; X cancels a steal before the windup. [MAN-GC][MH-RUN]
- MSB steal timing: runner leaves at **frame 40** of the windup; input in the **first 15 frames = "Perfect Steal"**, leaves at frame 15 (gold STEAL). Runner top speed ×⅔ while the pitch is in the air. [MH-RUN][SC-GC]
- **No lead-offs in either game.** Runners stand on the bag. (Consistent with the pickoff rule above; UNVERIFIED as a stated rule.)
- Batter-runner starts 31 frames after contact from where they stood; a lefty reaches first ≈ 32 frames sooner. Speed stat → home-to-first ≈ 213–259 frames; mashing adds ≤ 0.02/frame; runner stamina exists. [MH-RUN]
- **Tag-ups**: on a caught fly the game auto-sends runners back; they cannot leave until the ball is *firmly* caught; then advance manually ("Tag up and go!" is an MSS mission). A lone runner cannot cross home on a fly with < 2 outs until it resolves. Runners within 0.3 base-lengths cannot pass each other. [MH-RUN][MW-MSS]
- **Close plays (MSS booklet)**: "When a runner is approaching **third or home** along with the throw… wait for a button icon, then press it as quickly as you can. If the offensive player presses first, safe; if the defensive player presses first, out." Running abilities are used during close plays. MSB had no prompt; its close play was a **body check** roll (23–70% by weight class). [MAN-W][MH-RUN][MW-MSB]
- Sliding is automatic; a rounding runner never slides. [MH-RUN]
- Emergent plays the reference allows: delayed double steal (first-and-third), fake squeeze, running off the line in a rundown to dodge a lazy lob. [MH-FAI][MH-THR]
- CPU runners: keep going if time-to-base < throw time − 20 frames (30 on easy), bonus once past 40%; else turn back with a 12–20% mistake chance. CPU steals ≤ 2% base × class × speed; perfect-steal chance 0/20/40/50% by difficulty. [MH-RAI][MH-BAI]

### Match rules

- Options (MSB manual): first bat, star skills on/off, innings, **mercy: "end a game if the score differential is 10 runs at the end of an inning"**; **extra innings: "up to three extra innings regardless"**; bottom half skipped if home leads. Inning choices 1/3/5/7/9. MSS picks first bat by roulette. [MAN-GC][MH-BAI][MW-MSS][TVT-MSB]
- Walks and HBP exist and count for MVP and Star Chance. No DH, balk, dropped third strike, infield fly, or intentional walk in any source (UNVERIFIED as absences). [MH-PIT][MH-MVP]
- Ground-rule doubles exist (lava pits, short ship walls, egg bounce). [MH-STAD][MW-Cruiser][MW-Egg]
- Stars: 5 cap; starting stars from captain chemistry (≥70 → 5, 55–69 → 4, 35–54 → 3, 15–34 → 2, else 1). MSB "Star Chance" at-bats give a star to whichever side wins the at-bat; MSS earns from "well executed plays" and park features. [MH-CHEM][MAN-GC][MW-MSS][MH-STAD]
- MVP (MSB): walk-off HR → hitter; no-hitter → pitcher; walk-off hit → hitter; else points HR 10, winning pitcher 5, go-ahead RBI 5, big play 5, K 3, RBI 3, hit/walk/HBP/SB 1. MSS adds close plays and item use. [MH-MVP][MW-MSS]
- Park gimmicks are field rules with geometry (Freezies on touch, warp pipes on the ball, Piranha Plants that eat and spit, manholes, floor arrows, cannon barrels, gas flowers, fire puddles, gravestone ghosts, deck tilt). [MW stadium pages]

### CPU (MSB datamine)

- Batter guesses the pitch zone during the windup (50–80% "same as last"), then re-reads after release with a chance to mistrack by a fixed offset; **the mistrack chance jumps if the pitcher moved on the mound since the last pitch** (30–95%). Fooled by a charge/changeup → swings 4–9 frames off. Charge vs slap: balanced 50%, power 80%, speed 30%, technique 10%. Bunts 0–25% early with a runner on first; never with power hitters. [MH-BAI]

### Feel

- MSB pitch flight ≈ 35–45 frames (0.6–0.75 s) to the plate at normal speed (derived from the speed tables, not stated). Camera cuts to the field 25 frames after contact; a captain star pitch pauses 50 frames for its emblem. [MH-PIT][MH-FLD]
- MSS HUD: next batter, score/inning, on-base mini diamond, B/S/O, chemistry indicator, batter and pitcher cards with the star gauge, poor-stamina icon; in play: control display over the fielder, the ball's landing circle, the item pointer. [MAN-W]
- Reviews: the sideways scheme has "too many commands for too few buttons"; remote-only auto-picks throw bases and drops stealing. [NWR][MAN-W]

### Sources

- MAN-W — Nintendo, *Mario Super Sluggers* instruction booklet (Wii): https://www.mariomayhem.com/downloads/mario_instruction_booklets/Mario_Super_Sluggers_-_ML1_Manual_-_WII.pdf
- MAN-GC — Nintendo, *Mario Superstar Baseball* instruction booklet (GameCube): https://www.mariomayhem.com/downloads/mario_instruction_booklets/Mario_Superstar_Baseball_-_Manual_-_GC.pdf
- MH-* — Mario Superstar Baseball community datamine wiki, https://mariobaseball.miraheze.org/wiki/ — pages Batting_Mechanics (BAT), Pitching_Mechanics (PIT), Throwing_Mechanics (THR), Running_Mechanics (RUN), Fielding_Mechanics (FLD), Fielding_Auto_Movement (AUTO), Chemistry (CHEM), Stadiums (STAD), Batting_AI_Logic (BAI), Pitching_AI_Logic (PAI), Fielding_AI_Logic (FAI), Baserunning_AI_Logic (RAI), Bunting (BUNT), MVP_Determination (MVP), Glossary (GLOSS), Cursed_Ball (CB), Pitch_Type_Impact_to_Exit_Velocity (PTI), Contact_Zones_Calculations (CZ), Initial_Power (IP), Batting_Launch_Angles (LA), Batting_Horizontal_Angles (HA), Batter_Stats (BS)
- MW-* — Super Mario Wiki: Mario_Super_Sluggers, Mario_Superstar_Baseball, Star_Skill, Chemistry, Laser_Beam, the star-skill pages (Fire_Swing, Tornado_Swing, Heart_Swing, Egg_Swing, Cannon_Swing, Barrel_Swing, Banana_Swing, Phony_Swing, Liar_Swing, Breath_Swing, Graffiti_Swing and the matching _Ball pages), the stadium pages
- SC-W / SC-GC — SuperCheats walkthroughs for MSS and MSB
- GF-MSB — GameFAQs MSB FAQ 43195 (search snippet only)
- SR-MSS — speedrun.com MSS guide (search snippet only)
- TVT-MSB — TV Tropes, *Mario Superstar Baseball* (search snippet only)
- NWR — Nintendo World Report review of MSS
