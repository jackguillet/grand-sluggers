# Pitching and hitting: a reference-informed design discussion

> **Historical.** A finished report, kept for its evidence and reasoning; the contract is [gameplay-spec §4 and §5](../../spec/04-pitching.md) and [the decision plan](../../decisions/plan-pitching-hitting.md). Where they disagree, the contract is right.

Research date: September 20, 2026 (America/Los_Angeles). Session kind: **Gameplay research/documentation**. Inspected Harbor revision: `05471a6038f9e21d66957016df3f7cb3b1b6842a`. This report accompanies the [decision register](../../decisions/plan-pitching-hitting.md); it changes no runtime rule and accepts no new mechanic.

Jack's brief: preserve what he loves about Mario Super Sluggers while considering a more robust pitching/hitting system. “More robust” could mean richer tactical choices, more precise execution, or more consistent and understandable outcomes. Those are different investments. PH-01 asks which should lead.

## Evidence and limits

**Primary documentation** below establishes advertised controls and behavior, not measured frame windows or hidden formulas. Nintendo booklets hosted by archival sites remain Nintendo-authored primary sources. The Show comparison deliberately names the **2025 edition**; this is not a claim about the latest edition. Super Mega Baseball: Extra Innings supplies its documented mechanics; SMB4 supplies separately verified difficulty/practice features. Do not silently transfer edition-specific details.

No new reference play session, annotated video measurement, or Harbor standalone sitting was performed for this report. Community datamine pages failed to load during this session. Earlier community numbers in [research-sluggers.md](../reference/research-sluggers.md) remain inherited research, not freshly verified evidence. In particular, do not assign GameCube's reported 9/7-frame windows, curve coefficients, or inferred flight times to Wii Super Sluggers.

## Mario Superstar Baseball — GameCube, 2005

**Documented:** A performs an ordinary swing; holding/releasing charges it. The batter can reposition, reset, influence hit direction, bunt, or use a Star Swing. Pitching similarly offers normal and charged deliveries, a changeup, mound movement, post-release sideways break, and Star Pitches. The batting cursor is an EASY control option. Team Stars fund specials, with Star Chance at-bats replenishing the winning side. [GC, printed pp. 8–17]

**Design reading:** positioning and timing create interaction with very few basic commands. Charging exposes a commitment that the other player can exploit. Character and resource choices can deepen that duel without demanding a separate precision minigame. The optional cursor also separates visible assistance from the underlying contact model.

**Open evidence:** exact contact-zone shapes, overcharge behavior, timing bands, fatigue penalties, and competitive exploits require accessible datamine evidence or controlled observation. This report does not independently verify the historical five-star-homer claim. Grand Sluggers' existing D6 prohibition remains authoritative regardless.

## Mario Super Sluggers — Wii, 2008

**Documented:** upright Remote, Nunchuk, and sideways button controls coexist. Sideways play moves batter/pitcher horizontally, resets with Down, and offers normal/charged actions. The batting cursor travels with the batter; central contact improves the hit. Normal swings favor contact, charged swings favor power. Pitchers can steer after release; normal pitches favor control, while charged pitches, changeups, and character-specific Star Pitches provide alternatives. [WII, printed pp. 3–9]

**Design reading:** the essential reference is the player’s repeated read–commit–respond loop. A gamepad can preserve that loop without motion controls. Preserve the visible contest between a moving ball and a player-positioned hitting region when considering extra depth.

**Difference worth keeping explicit:** the GameCube manual presents its cursor as an assistance option; the Wii booklet directly teaches the batter-linked cursor. The Wii schemes also change how much supporting play the player controls. Shared action names do not establish identical physics, frame windows, automation, or competitive balance.

## Super Mega Baseball — execution and adjustable assistance

**Extra Innings, documented:** the hitter positions a swing target and chooses contact or charged power; the power value rewards charge timing. Pitchers choose a pitch type and target. Against CPU, normal pitching involves moving a reticle onto the target; power pitching also times its release. Its manual separately describes versus pitching, including changes during windup to deceive a human opponent. Do not copy the CPU-mode instructions as its universal multiplayer contract. [SMB-EI, Batting Mechanics / Pitching Mechanics]

**SMB4, documented separately:** difficulty can be adjusted by gameplay category. Practice keeps the sides batting/pitching, removes pitcher fatigue and Mojo/Fitness changes, and teaches actions through tutorials. [SMB4, Gameplay Features]

**Design reading:** borrow the separation between pitch selection, location, execution, and difficulty. A player should be able to describe which decision failed. Continuous reticle work is an option to evaluate, not a prerequisite for depth. Assistance should preserve the same baseball rules and make its effect explicit.

## MLB The Show 25 — how much control to ask of the hitter

**Documented:** Zone hitting combines precise PCI placement with swing timing. Directional hitting adds trajectory influence to timing; Timing uses the swing input alone. Normal/contact/power swings and bunts appear in these interfaces. [SHOW-H]

**Documented:** pitching interfaces include Pinpoint gestures synchronized to release, Pulse timing, Pure Analog, Meter, and Classic. Selection, location, and delivery execution are distinct tasks. [SHOW-P]

**Design reading:** there are two separate decisions here: how much control to expose and how demanding its execution should be. We can adopt richer pitch selection without also adopting gesture pitching, or choose spatial hitting without offering three different contact systems. Multiple interfaces increase tutorial and couch-fairness work. A “perfect” input should never promise a hit or homer independently of contact and defense.

## Wii Sports — the simplicity boundary

**Documented:** batting is a timed swing as the ball reaches the plate. Pitching supports location choice, delivery-speed variation, and fastball/curveball/screwball/splitter choices through a small input vocabulary. [SPORTS, printed p. 10]

**Design reading:** a small set of visibly different pitch shapes can already support deception. This is the useful lower-complexity comparison for PH-02. It does not establish the right pitch list, motion requirement, or assistance level for Grand Sluggers.

## What this suggests for Grand Sluggers

These are **author recommendations**, pending Jack's decisions:

1. **Keep a recognizable Mario-style duel.** Position, read, charge or stay quick, then commit. First explore richer choices inside this loop (PH-01).
2. **Give each ordinary pitch a job and a weakness.** A fast pitch challenges timing; a slow pitch punishes early commitment; a breaking pitch challenges coverage. Extra pitch names add little unless a hitter can read the difference and respond (PH-02–06).
3. **Resolve steering before adding a target system.** Pre-release location and post-release steering can coexist, but unconstrained control of both risks making every pitch an unanswerable late correction. Decide commitment and response opportunity together (PH-03–04).
4. **Resolve batting coverage before adding precision demands.** Retaining body-linked horizontal coverage, adding discrete height intent, and adding a free two-dimensional cursor produce different games. Do not stack all three by default (PH-09).
5. **Keep ordinary swings valuable.** A charge should exchange flexibility or forgiveness for power, rather than become the permanently correct choice. Evaluate taking pitches and two-strike survival alongside home runs (PH-10–14).
6. **Make failure legible.** Distinguish a mistimed swing, a reach/coverage miss, weak barrel contact, and an overcharged action. Feedback should describe the resolver's actual cause (PH-19).
7. **Treat CPU and two-pad play as the same design.** Visible preparation can create a mind game; leaking the exact target through a shared-screen overlay can destroy it. CPU decisions must use an explicitly chosen information policy (PH-06, PH-18).

“Robustness” should also mean reproducibility: the same timed inputs and sampled trajectory produce the same judgment, a shown ball outside the zone is not called inside, and new pitches use the existing trajectory/contact pipeline. There is no proposal for bat-mesh collision microphysics or a second input toolkit.

## Harbor baseline and existing decisions

The current [spec](../../gameplay-spec.md) is the authority. This is a snapshot of existing rules/data, **not a new endorsement of their tuning**:

- **D4:** cursor/contact position primarily determines quality; timing determines direction inside the contact window and a miss outside it. Code additionally demotes quality one tier at the outer timing rim; PH-10 retains that nuance for review. **D13:** judged input timing and rendered bat contact share an authored lead/warp contract.
- **D7:** pitch pace remains on hold until the named re-sit. `pitching.flight` currently clamps air time to **0.78–1.28 s**, with `arcadeScale = 2.05`. These are Harbor values, not measured Wii timings.
- **D12:** batter position recenters between pitches. Neither a different cursor nor persistent box position is approved by this brief.
- `data/rules/pitching.json`: nominal ordinary/changeup speeds **86 / 68.8 mph**, charge bonuses **8 / 3 mph**, maximum lateral break **0.46 ft**, charge/changeup break multiplier **0.10**, changeup crossing drop **0.9 ft**. Displayed/nominal speed is not the real-time reaction budget.
- `data/rules/batting.json`: baseline total slap/charge windows **9 / 7 frames at 60 Hz** (150 / about 117 ms), before stat/assist adjustments; authored lead **0.18 s**; timing spray span **±55°**, stick influence **±12°**. These are current constants, not a new reference measurement.
- `data/feel/table.json`: pitch/swing charge fill **0.55 / 0.45 s**, MAX hold **0.50 s**, decay **0.80 charge units/s**. Do not change these while merely recording a preferred direction.
- Primary code owners: `PitchFlight.cs` (trajectory and crossing), `AtBatResolver.cs` (window/contact/spray/speed), `AtBatFeel.cs` (charge and contact timing), `AtBatControl.cs` (screen-relative horizontal intent). `Match.cs` and CPU systems assemble commands. Rule changes belong in the existing systems and tables.

Coordinate existing #534 (plate reference/sitting), #563 (pitch/swing contract), #535 (zone judgment), #581 (pad hitting difficulty), #558 (authored takes), #693/#708/#715 (ball/field calibration), and #770 (tutorial foundation). The GitHub tutorial children may contain work newer than this fetched main revision; verify their merged state before implementation.

### Code map and qualifications at the inspected revision

- `src/GrandSluggers.Sim/AtBatFeel.cs:100`: shared charge action commits on release. `unity/Assets/Scripts/Runtime/AtBatDirector.cs:145`, `:373`, `:447` consume SET/flight/swing state. South pitches/swings; West modifies changeup/bunt; North arms stars. Human movement and pitch height are handled at `:159`, `:168`, `:314`: horizontal mound/box movement, shape-defined pitch height. The keyboard/mouse mapping is P1-only.
- `src/GrandSluggers.Sim/PitchFlight.cs:35`, `:47`, `:94`, `:104`: air time, base shapes, shared plate crossing and steering. `:132` provides compensated CPU endpoint aiming. A field existing in a command structure does not establish a human aiming verb.
- `src/GrandSluggers.Sim/AtBatFeel.cs:144`: batter-linked sweet-spot geometry. `AtBatResolver.cs:61` demotes contact quality one tier in the outer 10% of either half-window. `:179` owns window width; `:198` mirrors early/late spray by batting hand. PH-10 must resolve the precise quality contract rather than repeat an oversimplified D4 slogan.
- `src/GrandSluggers.Sim/Match.cs:933`, `:1963`: per-character stamina and additive action costs. Ordinary cost 4 plus changeup cost 3 means a simple changeup costs 7, before other modifiers. `:903`/`:924` apply tired speed/break/wobble effects.
- `unity/Assets/Scripts/Runtime/AtBatDirector.cs:421` commits the live CPU hitter about **0.30 s before plate arrival** (`leadSec .18 + decideLeadSec .12`), using the then-current trajectory; final trajectory still judges contact. Some existing §5.9 prose describes deciding from final crossing. PH-18 must distinguish live commitment, headless command construction and judgment instead of asserting either perfect omniscience or fully proven fairness.
- `src/GrandSluggers.Sim.Tests/AtBatScenarioTests.cs`: S-01–06 cover zone/pitch basics, S-07–13 contact/window/direction/charge, S-14–17 early input/HBP, S-18–19 bunts, S-25–26 stamina/swaps, S-27–29 CPU/scoring, S-30 Charge Bat. S-04 tests a supplied steered crossing and commitment clock separately; it is not a full live late-steering fairness test.

These qualifications are recorded research findings, not silently repaired mechanics. The audit did not exhaustively verify bunt input ownership, all CPU paths, visual feedback, or runtime control parity.

## Research needed before selecting numbers

For each reference capture, record title, region/version, input scheme, character/hand, opponent, pitch/swing type, charge state, stars/chemistry, difficulty, and source/capture cadence. Separate input commitment, animation onset, release, visible tell, plate arrival, swing command, and actual contact. State timing uncertainty. A 30-fps upload cannot establish a one-frame window in a 60-Hz game.

Compare matched ordinary fast/slow/breaking pitches, normal/charge swings, inside/outside takes, early/on-time/late swings, and centered/edge contact. Use identical conditions when varying one choice. Do not infer world-space speed from an uncalibrated camera or substitute a wiki speed stat for milliseconds available to react.

Measure Harbor on a named standalone revision with pad, keyboard/mouse, and two pads, both batting hands, small/large captains, ordinary and star pitches. Track takes, whiffs, fouls, contact quality, pitch mix, charge usage, at-bat length, and input-to-visible-event agreement. Candidate thresholds and pitch timing remain unset until the corresponding tracker decisions are made. Preserve the existing S-29 and Harbor cohort commitments when a later change affects scoring; they do not replace human acceptance.

## Sources

- **GC:** [Nintendo, Mario Superstar Baseball booklet](https://www.gamesdatabase.org/Media/SYSTEM/Nintendo_GameCube/Manual/formated/Mario_Superstar_Baseball_-_2005_-_Nintendo.pdf), printed pp. 8–17 (PDF sheets 3–5).
- **WII:** [Nintendo, Mario Super Sluggers booklet](https://www.mariomayhem.com/downloads/mario_instruction_booklets/Mario_Super_Sluggers_-_ML1_Manual_-_WII.pdf), printed pp. 3–9 (PDF sheets 3–6). Nintendo's [manual index](https://en-americas-support.nintendo.com/app/answers/detail/a_id/16890/) also lists this title; the archived booklet was readable when the index's download failed.
- **SMB-EI:** [Super Mega Baseball: Extra Innings Xbox manual](https://dlassets-ssl.xboxlive.com/public/content/e91a9e60-dc92-416d-ae24-882e1e02b078/GameManual/fd5f5172-9ba4-432c-93ef-cdad92414871/de-DE/index.html), English content under this hosted locale path; Batting Mechanics and Pitching Mechanics.
- **SMB4:** [EA, Super Mega Baseball 4 accessibility resources](https://www.ea.com/able/resources/super-mega-baseball/super-mega-baseball-4), Gameplay Features / Difficulty / practice description.
- **SHOW-H:** [MLB The Show 25 official manual: hitting](https://mlb25.manual.theshow.com/en/controls-hitting.html).
- **SHOW-P:** [MLB The Show 25 official manual: pitching](https://mlb25.manual.theshow.com/en/controls-pitching.html).
- **SPORTS:** [Nintendo, Wii Sports booklet](https://csassets.nintendo.com/noaext/image/private/t_KA_PDF/Wii_Wii_Sports?_a=DATC1RAAZAA0), printed p. 10 (PDF sheet 5).
- **Legacy only:** [prior Mario teardown](../reference/research-sluggers.md) and [#693 decision register](../../decisions/plan-game-feel-693.md). Community pages linked there could not be re-read; this report does not renew their numerical claims.
