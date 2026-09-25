# Research: character feel — builds, size, speed, motion, first art

> **Historical.** A finished report, kept for its evidence and reasoning; the contract is [character-motion.md](../../character-motion.md), [character-package.md](../../character-package.md) and [the decision plan](../../decisions/plan-characters.md). Where they disagree, the contract is right.

September 24, 2026. Research and decisions only. No runtime value changes. Jack picked all fourteen rows of the decision register (CH-01 … CH-14) on September 24. The next step is a plan whose epics own the work.

Sources are cited inline. Community reverse-engineering (mariobaseball.miraheze.org) covers the GameCube game only. Nobody has published the same data for Wii *Sluggers*. **UNVERIFIED** marks a claim without a primary source. Earlier packets that this builds on: [research-sluggers.md](../reference/research-sluggers.md), [research-game-feel-701-proportions.md](../game-feel/research-game-feel-701-proportions.md), [research-game-feel-701-movement.md](../game-feel/research-game-feel-701-movement.md), [silhouette-bible.md](../../silhouette-bible.md).

## 1. What ships today

| Channel | Today | Source |
| --- | --- | --- |
| Visible stats | Pitch / Bat / Field / Run, 1–10. Totals are not equal: Rio 26, Vale 26, Zig 23, Fenn 23, Ashlord 21, Brondo 20, Konga 20. | `data/characters/*.json:10-13` |
| Hidden stats | Contact, Power, Velocity, Movement, Control, Endurance, Arm, Hands all exist, all fall back to the parent stat, and no character authors them. | `src/GrandSluggers.Sim/Models.cs:30-120` |
| Body build | Height and Width change the 3D body (root scale × `ToyScale` 1.18). **Head, Arms and Torso are validated but do not change the 3D body.** They only size the portrait head. | `Silhouette.cs:13-35`, `Look.cs:100` |
| Head-to-body | The neutral rig is 4.81 units tall with a 0.90 head: **about 5.3 heads tall** for every captain. | `data/art/rig.json:405-412` |
| Height ladder (head top) | Zig 3.18 ft, Fenn 4.43, Rio 5.11, Brondo 5.45, Vale 7.04, Konga 7.38, Ashlord 8.17. Shortest to tallest is 2.57×. | `5.676 × height` |
| Size in play | None, except the Grow verb (×1.45). Strike zone is fixed at 1.45–3.65 ft, so Zig's head sits below the top of his own zone. Catch reach, HBP radius and catch height are the same for every body. | `StrikeZoneGeometry.cs:7-8`, `fielding.json:94,109` |
| Fielder speed | `12.4 + 1.12 × Run` ft/s: Konga 14.64, Zig 22.48 (1.54×). Outfield flies × 0.6. | `data/rules/fielding.json:2-7,30` |
| Runner speed | 80 ft ÷ `clamp(3.55 − 0.12 × Run)` s: Zig 2.47 s, Konga 3.31 s per base (1.34×). | `data/rules/running.json` |
| Acceleration | Rest → top 0.20 s, top → rest 0.10 s. The same for every body. | `fielding.json:42-43` |
| Motion | 22 clips. Every captain plays the same takes, the same stance, the same idle and the same cheer. The run loop plays at a fixed rate, whatever the speed. | `HeroActor.cs:336-377`, `Motion.cs:26` |
| Shading | "Toon" falls back to URP Lit, smoothness 0.04. Extras are all off (#687). | `Look.cs:178-186`, `skins.json` |

**A found defect.** The run clip needs more than 14 ft/s (`CartoonJuice.RunFtPerSec`). After the #718 speed rescale, a fly chase tops out at 13.49 ft/s (Zig, × 0.6). So **every outfielder chasing a fly plays the walk clip**, and Konga barely clears 14 on a grounder (`HeroActor.cs:399-401`). This is a presentation bug, not a decision. Filed as [#1111](https://github.com/jackguillet/grand-sluggers/issues/1111), a child of #209.

## 2. What the reference games do

### Mario Superstar Baseball (GameCube) and Super Sluggers (Wii)

- **Four visible bars, about fifteen hidden numbers.** Classes: Balance, Power, Speed, Technique. Hidden: slap and charge power, contact size, left/right bat range, pull/push, charge time, curve, curve control, arm, weight and catch radii ([Batter Stats](https://mariobaseball.miraheze.org/wiki/Batter_Stats), [Pitching Mechanics](https://mariobaseball.miraheze.org/wiki/Pitching_Mechanics), [Fielding Mechanics](https://mariobaseball.miraheze.org/wiki/Fielding_Mechanics)).
- **Specialists with a deep dump stat.** Bowser is 9/9/1/1 ([Mario Wiki](https://www.mariowiki.com/List_of_Bowser_profiles_and_statistics)). "Bad" characters get one elite hidden number: Waluigi's contact 90, Peach's curve control 90.
- **Heights are normalized.** The catch-cylinder heights run from Baby Mario 1.56 to Magikoopa 3.60 (2.3×). Bowser is only 1.23× Mario ([Fielding Mechanics](https://mariobaseball.miraheze.org/wiki/Fielding_Mechanics)).
- **Reach is authored, not measured off the mesh.** Peach is tall and has the smallest fly radius (0.91). DK's fly radius equals his height. Big bodies have a *smaller* ground radius.
- **The strike zone does not scale with the batter.** Batter size lives in contact width and bat range ([Pitching Mechanics](https://mariobaseball.miraheze.org/wiki/Pitching_Mechanics)).
- **A narrow speed gap.** Running top speed differs by 19 %, fielding by 30 %. **Slow characters accelerate faster** (0.0100 vs 0.0050 per frame). Stamina, mashing, handedness and dive range add the rest of the feel ([Running Mechanics](https://mariobaseball.miraheze.org/wiki/Running_Mechanics)).
- **Weight is knockback.** Five classes, factor 1.0 (Toad) to 0.5 (Bowser). Light bodies stay stunned twice as long.
- **One animation slot list, filled per character.** 105 slots, including pitcher happy, sad and taunt ([List of animations](https://mariobaseball.miraheze.org/wiki/List_of_animations)). Reviews praised the "humorously animated character models" (IGN).
- **The Bowser problem.** Competitive rule sets forced Bowser to captain, because power and arm stacked ([Tier Lists](https://mariobaseball.miraheze.org/wiki/Tier_Lists)).

### Other baseball games

| Game | Lever | Lesson |
| --- | --- | --- |
| Power Pros | One chibi body, floating hands and feet, letter grades | Identity from head, palette and grade. No limb gap to hide. |
| Wii Sports | Doll bodies. "Realistic in motion" ([Iwata Asks](https://www.nintendo.com/en-gb/Iwata-Asks/Iwata-Asks-Wii/Iwata-Asks-Wii-Sports/2-A-Question-of-Realism/2-A-Question-of-Realism-217824.html)) | Motion sells the body more than anatomy does. |
| Super Mega Baseball 1 → 2 | Moved away from big caricatures to "create space on the field for athleticism" ([GamingBolt](https://gamingbolt.com/super-mega-baseball-2-baseball-is-back-with-a-bang)) | Fat bodies crowd a small diamond. Test gaps, not stills. |
| Super Mega Baseball 4 | 7 body types, 75 traits; generic stances criticized | Traits are cheap variety. Shared stances erase identity. |
| Backyard Baseball | Pablo: smallest, roundest, best. Speed spans about 7× | Contrast plus voice makes an icon. |
| MLB Slugfest | Realistic bodies, absurd contact verbs | Put exaggeration in the contact beats. |
| Famista | Pino, a tiny mascot, is the fastest | Small = fast is an old, readable convention. |

MLB reference: sprint speed 23–31 ft/s (27 average), home to first 4.0–4.6 s on the scouting scale, pop time 2.0 s (1.85 elite), outfield arm 86 mph average ([Savant](https://baseballsavant.mlb.com/leaderboard/sprint_speed), [FanGraphs](https://blogs.fangraphs.com/scouting-explained-the-20-80-scouting-scale/)). Real spread is about ± 7–15 %, too small to read on a couch.

### Other Nintendo and party games

- **Mario Strikers: Battle League.** Five axes. Every character totals exactly 63. Gear trades one stat for another ([Game8](https://game8.co/games/Mario-Strikers-Battle-League/archives/378562)).
- **Mario Tennis.** Six named types. The Defensive type gets longer reach, which is a hidden size trait.
- **Mario Kart 8 Deluxe.** Light bodies get acceleration; heavy bodies get top speed and bump force ([Mario Wiki](https://www.mariowiki.com/Mario_Kart_8_Deluxe_in-game_statistics)).
- **Smash Ultimate.** Bowser runs faster than Mario. Heavies pay with a bigger hurtbox and get armor ([SmashWiki](https://www.ssbwiki.com/Weight)). Heavy does not have to mean slow.
- **Readability.** TF2 and Overwatch test every hero in flat black silhouette, and give each a unique idle stance ([Valve NPAR 2007](https://steamcdn-a.akamaihd.net/apps/valve/2007/NPAR07_IllustrativeRenderingInTeamFortress2.pdf), [80.lv](https://80.lv/articles/david-gibson-animating-mei-in-overwatch)). TF2 uses a warped diffuse with a rim light, so bodies read far from lights. Splatoon: big heads and hands exist so you can read state.
- **Toy proportions.** Chibi is 2–4 heads tall; realistic adults are 7–8 ([Clip Studio](https://tips.clip-studio.com/en-us/articles/4806)). Mario-cast ratios near 2.5–3.5 heads are UNVERIFIED estimates.
- **Movement weight.** Celeste reaches top speed in about 0.09 s. Super Mario 64 takes about 0.9 s. Classes differ in ramp, braking and push resistance, not only top speed.

## 3. Where we differ from Sluggers

| Topic | Sluggers | Grand Sluggers today |
| --- | --- | --- |
| Head-to-body | Big heads, toy (UNVERIFIED 2.5–3.5 heads) | 5.3 heads for everyone |
| Height ladder vs the hero | Bowser 1.23×, DK 1.28×, Baby 0.72× Mario | Ashlord 1.60×, Konga 1.44×, Zig 0.62× Rio |
| Build channels in 3D | Unique meshes | Height and width only |
| Size in play | Authored reach, weight, contact width | None (fixed zone, reach, radius) |
| Top-speed gap | 19 % run, 30 % field | 34 % run, 54 % field |
| Acceleration by body | Slow bodies ramp faster | Same for everyone |
| Hidden stats | About 15, authored | 8 slots, none authored |
| Motion identity | Per-character fill of a shared slot list | One shared set |

## 4. Decision register

Each row was a choice for Jack. **Rec** was the recommendation. **Pick** is Jack's decision, September 24, 2026. Notes under the table carry his words and the open questions they raise. Kind names the session that would own the work (`docs/agent-rails.md` §1). Rows that move speed or stats touch balance: they stop at the breakage suite until Jack starts a balance pass.

| Id | Question | Options | Rec | Pick | Kind |
| --- | --- | --- | --- | --- | --- |
| CH-01 | North-star silhouette | A Sluggers toy (big head, fat, readable) · B athletic cartoon (SMB2) · C chibi (Power Pros) | A | A | Art |
| CH-02 | Head-to-body ratio | A about 3 heads · B about 4 heads · C keep 5.3 | B, then judge | B | Art |
| CH-03 | Height ladder | A compress toward Sluggers (tallest ≤ 1.35× Rio, Zig ≥ 0.7×) · B keep · C exaggerate | A | A | Art |
| CH-04 | Build channels | A apply Head, Arms, Torso to the rig per captain (bone scale in data) · B height and width only · C unique meshes (banned now) | A | A | Art |
| CH-05 | Size in play | A authored per body class in data (reach, catch radii, contact width) · B visual only · C derived from the mesh | A | A | Gameplay |
| CH-06 | Strike zone | A fixed for every batter · B scaled to the batter | A | **B** (see note) | Gameplay |
| CH-07 | Visible stats | A four bars plus a class label · B four bars · C five bars | A | **B** (see note) | Presentation |
| CH-08 | Stat budget | A equal total per captain · B loose total, max one 9+ · C no rule | B | B | Gameplay |
| CH-09 | Hidden stats | A author the eight slots per captain now · B keep the fallback | A | A (see note) | Gameplay |
| CH-10 | Top-speed gap | A tight, about 1.25× · B keep 1.34–1.54× · C wide, 2× | A | A | Gameplay |
| CH-11 | Weight model | A heavy ramps slow, light ramps fast (Kart) · B heavy ramps fast, tops low (GC) · C same for all | A | A | Gameplay |
| CH-12 | Motion identity | A per-class style takes for run, idle, stance, windup, plus one captain signature beat · B shared takes plus stride rate from speed only · C unique per captain | A | A (see note) | Art |
| CH-13 | Juice by mass | A scale anticipation, hit-stop, squash and settle by weight class · B one juice for all | A | A | Presentation |
| CH-14 | First-pass shading and extras | A real toon with rim light, plus one signature extra per captain · B toon and rim, extras off · C keep Lit | B | B | Art |

Jack's notes on the picks:

- **CH-06, the strike zone scales vertically only.** "It should vertically scale, but horizontally the same." Jack set the landmarks on September 24: the zone runs from the batter's **knee** to the batter's **chest**. On September 25 he moved the bottom up to the batter's **mid-thigh** ("way too low" at the knee); the top stays at the chest. Its width over the plate stays fixed. The worked numbers and the aim rule are in [plan-characters.md](../../decisions/plan-characters.md).
- **CH-07, four bars, built from sub-stats.** Settled with Jack on September 24. He first chose five bars with a separate Arm bar, then took it back: "pitching is very different than fielding." Each bar is the rounded mean of its sub-stats. The sub-stats are the hidden numbers that CH-09 authors per character. Every sub-stat already exists as a code slot, so no new stat is needed.

  | Bar | Sub-stats | Code slot today |
  | --- | --- | --- |
  | **Bat** | contact, power | `Contact`, `Power` |
  | **Pitch** | power, stamina, control, break | `Velocity`, `Endurance`, `Control`, `Movement` |
  | **Field** | hands, throw speed | `Hands`, `Arm` |
  | **Run** | speed | `Run` |

  - **Pitching power and throwing speed are separate.** A pitcher's fastball comes from Pitch power. A fielder's throw comes from Field throw speed. A strong-armed outfielder is not automatically a hard-throwing pitcher.
  - **No accuracy and no range sub-stats.** Throws keep today's chemistry slant. Reach comes from the body class (CH-05).
  - **The Bowser guard.** The CH-08 cap (at most one bar at 9+) still applies. Test it in the balance pass.
- **CH-09, ten captains.** "I want 10 captains." The roster grows from seven captains to ten. Three new captains need factions, body classes and palettes. This is a roster decision; it does not unblock new captains before the rail work lands.
- **CH-12, plan for about fifteen motion styles.** "At least 7. We should plan for more including non-captains. So maybe 15?" The style layer is sized for about fifteen body classes, so role players can get their own class instead of always copying the captain.

Rationale for the recommendations:

- **CH-01 to CH-04.** Look already says "Sluggers weight". Today's 5.3-head body is closer to SMB2 than to a toy. Head, Arms and Torso already exist in data; wiring them is a rail, not a new rig. Compressing the ladder follows the SMB lesson that fat, tall bodies crowd an 80-ft diamond.
- **CH-05, CH-06.** Sluggers authors reach as data and keeps the zone fixed. That keeps pitching fair against a giant. It also lets a small captain earn a verb instead of free reach.
- **CH-08.** Strict equal totals flatten the Bowser-style specialist, which is the joke of the roster ("Specialization is the joke", `docs/roster.md`). One cap guards the Bowser problem instead.
- **CH-10, CH-11.** Sluggers keeps top speeds close and puts feel into the ramp. Kart's light-fast-ramp matches our cast reads (Zig light, Brondo heavy) better than the GameCube inverse. A narrower gap keeps Konga and Ashlord playable on the bases.
- **CH-12.** Every reference credits motion for identity. Seven class styles on the one rig and the one takes script is a rail. Stride rate from ground speed fixes the "every body runs alike" read cheaply, and it is still the sim's clock.
- **CH-14.** "Nothing too deep" for art: a real toon and rim light lift every captain at once. Extras stay off until they read as toys (#687).

## 5. Next steps

1. The plan is [plan-characters.md](../../decisions/plan-characters.md): epics CF-1 … CF-7 with scenarios SC-01 … SC-25.
2. First look sitting: dual stills of the seven captains at the new head ratio and ladder, in flat black and in palette.
