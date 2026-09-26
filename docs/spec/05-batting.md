# 5. Batting

## 5.1 The four verbs

| Verb | Input | Swing |
| --- | --- | --- |
| Slap | Tap and release RT | Full swing, widest window, base power. Plays the `swing-slap` take: no windup, compact |
| Charge | Hold to MAX (`swingChargeSeconds` 0.45), release in the MAX band | The same window as a slap (PH-10-R1); the charge narrows the **cursor** instead, §5.2, PH-11-R1. More power (up to ×1.35 at MAX). Past the band the charge decays Plays the `swing-charge` take: the hold shows its windup, a bigger arc |
| Bunt | Hold West (third-base side) or North (first-base side) through the pitch | The bat squares toward the held side; the held bat meets the ball with no timed press (§5.8) |
| Star | **LT held** as RT is let go (§12, PH-16-R10, R11, R17) | The batter's Star Swing (§13). Costs its price even on a miss; unaffordable, the ordinary swing at the same timing |

Charge at MAX is a *charge* tell (rings line up; the swing shows MAX, the pitch its booklet word "Nice!"). Words about the contact — PERFECT / NICE / SOUR — come only from the typed zone once the bat meets the ball; a miss shows STRIKE. ✅ P1

**Cancel** (PH-13, PH-13-R1). A deliberate cancel discards an uncommitted swing load and its charge. The ordinary swing commits at the release of RT. Once committed it follows through: a cancel cannot take it back. A cancelled load never swings, so the pitch is taken and called. The hold that was cancelled cannot swing: its release commits nothing, and a new swing needs the button up and a fresh press, which starts from zero (no banked charge). The pitch has no cancel (PH-02-R3). One tick of `ChargeButton.Advance` applies, in order:

1. **Not accepting:** at rest; nothing carries over.
2. **Cancel:** an armed load, or a press on this tick, is discarded, **even when the release is on the same tick** (cancel beats release). If the button is still down, the state is `MustRelease`. A cancel with nothing loaded does nothing.
3. **Must release:** the tick's press, hold and release count for nothing; the button coming up clears the state. Only a press on a later tick loads again.
4. **Load:** press arms, hold fills, release commits (in SET the release disarms, §3).

**Conversion** (PH-13-R1). A bunt press during an uncommitted load is this cancel: the load and its charge are discarded and the bat squares toward the pressed side, with no separate cancel press. The plate's tick order for the swing and the bunt together is §5.8's.

**The cancel button** is **East** on the batting seat's own pad (controller seats are independent). The client steps `PlateButtons.Advance` every SET and flight tick with RT, the two bunt buttons and East. A cancel press while the plate accepts is the plate's verb whether or not a load was there: it is spent (`PlateButtonsState.CancelSpent`) until the button comes up, and no reader may take it as a dive, a dash or a Training skip meanwhile (`PlateButtons.CancelIsFree`; S-170).
A load cancelled by East / G lets go: the `swing-letgo` take walks the held load back to the stance and settles, starting on the load it discards (`Motion.LetGoStartAt`); a load a square replaces shows the square instead. Lesson T-B10 teaches it.

✅ Sim (S-150 … S-152, S-160 … S-163, S-186). ✅ P4-c client: the East input, the bunt button inputs and the lessons. ⚠️ Human gate: sitting 4.

## 5.2 The cursor (sweet spot) — quality

The reference model (D4): the bat is a hitbox along the swing plane, split into **five zones** — sour / nice / perfect / nice / perfect — and the ball meets one of them by *where it crosses*, not by when you pressed. The cursor on screen is that hitbox drawn on the plate.

- A gold oval **follows the batter**, never the pitch (booklet-confirmed). Box walk moves it in X by the same world distance as the body; its Y is the middle of the batter's zone and its half-height is half that zone (§4.4). The oval is the perfect+nice zones; the rim is sour. ✅ P1 (`SweetSpot`, world feet, `batting.cursor`)
- The oval is **tall enough that any strike is hittable**: its nice half-height is the zone half-height, and the sour rim is the barrel's rectangle `rimFraction` beyond it, so the corners of the frame are on the bat with the box centered. Quality is by the ellipse distance from the center: along X the bat barrel (nice half-axis `niceTipFt` 1.05 toward the tip, `niceHandleFt` 0.75 toward the hands — the handle side is shorter), along Y a ball at the top or bottom of the zone is at best nice.
  The perfect heart is `perfectFraction` (0.42) of the oval; a Star Swing whose row names `perfectRingMul` grows that heart for its own swing only (§13). The client draws exactly this outline (`SweetSpot.Outline`). ✅ P1 (S-05, S-06)
- The **Contact** rating scales the barrel (`scalePerContact` 0.04 per point from 5) — this is the spatial forgiveness of PH-15-R7 and the only thing Contact is meant to buy; a charge **narrows** it (`chargeMul` 0.8; reference: charge zones are smaller than slap zones). **Runners on base never change the barrel**: there is no plate-level chemistry (PH-16-R14; there is no good-chemistry slap widening, `buddiesOnBase.widen*`). ✅ (S-11, S-30); ✅ (S-144); ✅ reads `Stats.Contact` (+ the bat's `contactMod`) rather than `Bat` (S-122)
- **This is the whole of what a charge costs and what Contact buys** (PH-11-R1, PH-15-R7): the timing half of both is gone, because every swing is judged in the one window of §5.3. ✅ (S-125)
- Vertical placement is earned by the pitch path at contact: a low crossing launches lower and a high crossing launches higher. Ordinary sour contact retains that launch; pitch type and quality do not force a topper or pop-up (S-12).
- **The drawn oval is the judged oval**. One sim function, `SweetSpot.Oval`, returns what the client draws for a swing: the center from the box walk (`WorldCenter`) and the half-extents — tip, handle and the zone's half height — from `SweetSpot.SwingBarrel`, the barrel the resolver judges with (Contact + the bat's `contactMod` clamped 1–10, the charge as it stands with the Charge Bat as a MAX, × the batter's body-class `contactWidthMul`, §8.1).
  The class widens the barrel along the bat only; the height stays the zone's, and no class takes a strike a batter reached with the plain barrel off the bat.
  `AtBatDirector.ShowCursor` hands the match's batter, bat, effective charge and box to it and `StrikeZone` draws the result; the client no longer clamps Contact or calls `BarrelScale` itself. Pinned across Contact 1–10, quick and charged, both hands, every bat and good-chemistry runners on base (which change nothing, S-144), through the resolver: a crossing just inside the drawn line is Nice and just outside it is Sour. ✅ (S-134)
- **A charge and Contact are placement only** (PH-11-R1, PH-15-R7). A charge multiplies the barrel by `chargeMul` and nothing else — same center, same half height, the same timing window and the same on-plane edge as a quick swing; the Charge Bat keeps the slap's barrel. Contact multiplies it by `1 + (Contact − 5) × scalePerContact` (floored at 0.5) and nothing else — the window is the same number at every Contact. ✅ P2-d (S-135, S-136)
- **Loading a swing does not slow the box walk** (PH-09-R1). The walk is `HomeSet.BoxWalkStep`: stick × frame × `BoxWalkPerSec` (1.6 box-offset units per second at full stick — the walk spans ±1 unit, `BatterWalk` 2.4 ft each — the client's rate since the box walk shipped, now named in the sim), with no charge argument. A seat holding a load to MAX and past it walks the same offset on every frame as a seat that never touches the button, and its committed swing carries the same box. Committed-swing movement and movement tuning are not selected by PH-09-R1 and are unchanged. ✅ P2-d (S-137)

| Zone | Slap exit (× base) | Charge exit (× base) | Tell |
| --- | --- | --- | --- |
| Perfect | 1.00 | 1.25 | PERFECT flash, crack, `solidFreeze` |
| Nice | 0.95 | 1.12 | NICE |
| Sour | 0.75 | 0.95 | dull thud; launch remains contact-driven |
| Off the bat | miss | miss | whiff |

The two columns are `batting.quality.slap` / `.charge`, interpolated by the effective charge (a decayed overcharge lands between them). The perfect charge is the ×1.25 of §5.5. Reference numbers behind the ratios: slap sour 100–130 / nice 140–145 / perfect 145–150; charge sour 140–150 / nice 162–177 / perfect 160–170, with the perfect charge carrying because it gets no added gravity.

## 5.3 Timing — the window and direction

`err` = (press time − (ball-plate time − `window.leadSec`)) in frames at 60 Hz (D13; `leadSec` 0.18, `AtBatMotion.SwingErrorFrames`). The take's `Contact` mark (`Motion.SwingContact`, 0.30 into the take) is an animation contract, not the judgment:
for a press inside the window the take is warped so the mark lands on the ball's plate time (`AtBatMotion.SwingContactSec`, `SwingClipTime`) — load → contact is compressed onto the span from the press to the plate, the follow-through plays at the take's own speed, the keys keep their order and the take never plays backward; a press inside the window but after the ball is on the plate (only a widened window) lands Contact at the press.
Outside the window the take plays at its natural 0.50 s and the bat misses the ball honestly. The warp is read at the press from the same window number the resolver judges (`Match.SwingWindowFrames`). ✅ (S-07, S-08, S-09; `SwingPresentationTests`)

The take is the swing that is judged: a charge (an effective charge at or above `match.charge.chargeAt` 0.55, the same test that narrows the barrel) plays `swing-charge`, anything else `swing-slap`; both share the Contact mark and the measured approach and contact keys, so the warp above is one rule for both. Both end on a held finish at 0.60 that stays up through the STRIKE stamp until SET, or through the contact freeze until the batter-runner is `feel.swingFinishStepFt` out of the box. ✅ (`SwingPresentationTests`, `MotionTests`, the swing matrix `finish` beat)

- **Window** — **one window for everyone** (PH-10-R1). ✅ (S-125 … S-127; `AtBatResolver.ContactWindowFrames`). The window is a total width: the bat is on the plane when |err| ≤ half of it. Outside it the bat is not on the plane: **miss**, strike. `window.frames` is the whole window for every hitter, a quick swing and a charged swing alike, and every human rung. **No Star Pitch changes the window** (PH-16-R1, PH-16-R18):
  a Star Pitch's challenge is its speed and its path, which the hitter can see, and it is judged in the same window as the plain pitch (S-190, S-191). The 5-frame floor still applies. No park or night term (FD-11-R2): Crystal's night multiplier is gone on both roots.
  Four accepted decisions say so: **PH-10-R1** (one shared window, **9 frames** at 60 Hz, ±4.5, 150 ms), **PH-11-R1** (a charge trades *placement* forgiveness, never timing), **PH-15-R7** (Contact is spatial forgiveness only), **PH-17** (one fixed challenge, so a difficulty rung may not widen a pad's window). 9 was the trial's start value, because it equalled the quick swing for an average hitter on the split window.
  The title's difficulty line reads `3 INNINGS · NORMAL · CPU SKILL`, because the rung changes the CPU and never a pad's window.
  - **There is no split window.** `batting.window.shared`, `slapFrames`, `chargeFrames`, `framesPerContact` and `humanWindowMul` are not in the data or the code; a table that still authors one is refused by name (S-126).
- Inside the window, timing does **not** change quality (D4). It changes **direction**: early contact **pulls**, late contact **pushes** (opposite field). Linear across the window: earliest frame ≈ 55° toward the pull line (`spray.timingDeg`), center ≈ straight at second, latest ≈ 55° toward the opposite line. The zone adds its spread (`spray.*SpreadDeg`), a pitch outside the zone adds `spray.outOfZoneSpanDeg`, and sour contact already pulled toward a line may skip past the chalk (`batting.foul`). ✅ P1 (S-07, S-08)
- **The stick at contact — an ordinary hit is geometry only** (PH-12). ✅ (S-13, S-128 … S-132; `AtBatResolver.StickShapesContact`). A swing that is neither a bunt nor a Star Swing ignores the stick at contact: its direction is the timing, the zone's spread, the out-of-zone spread and the sour pull above, and nothing else. **PH-12** option C: timing, contact position, pitch shape and swing type decide the flight.
  No preselected grounder / liner / lift approach replaces the stick (PH-12 rules one out). What stays: the stick still **walks the box** (§3, PH-09), so horizontal input still changes contact by moving the batter before the swing — never by a second spray — and Down still recenters in SET; the swing intent still carries the stick, and only the resolver ignores it.
  A **bunt** reads its held side and never the stick (§5.8), and a **Star Swing** keeps both stick terms until Phase 6 reviews each one, so `spray.stickDeg` and `launch.stickDeg` stay authored. The CPU batter follows (§5.9). PH-12 is human-accepted.
  - **There is no stick on an ordinary swing, and no switch for one.** A table that still authors `batting.geometryOnly` is refused by name (S-126).
- The batter's handedness mirrors the map. A right-handed batter who is early hits toward 3B. ✅ P1
- Quality still moves slightly with timing only through the *rim*: the outermost (1 − `squareFraction`) of each half-window reduces the cursor zone by one (perfect → nice, nice → sour) because the bat is not square. Never two zones; sour stays sour. ✅ P1

## 5.4 Height and the stick

- **Launch** = base by **Power** (`Stats.Power` plus the bat's `powerMod`) and charge (`launch.loftBaseDeg`, `loftPerPower`, `charge.loftDeg`) **+** the pitch height (`perFtOfHeight` per foot the crossing sits above the middle of the batter's zone, in reference-zone feet (§4.4): a low pitch launches lower) **+**, on a Star Swing only (until Phase 6), stick U/D at contact (`stickDeg`): **Up = over the top = grounder**, **Down = under = lift**. The reference maps up→grounder, down→fly on every swing; ours does so on a Star Swing only. ✅ (S-06)
- **An ordinary swing's launch has no stick term** (§5.3, PH-12): it is the Power and charge loft, signed pitch height and the noise. Ordinary sour contact uses the same continuous formula. A low pitch still launches lower than a high one and a charged swing still lofts more than a quick one (S-129); a Star Swing without an authored launch keeps the term until Phase 6 (S-131). ✅ (S-13, S-128, S-129)
- The same axis does **not** reset the box: Down-reset is a SET verb only, before the windup.
- Launch noise is ±`noiseDeg`/2 (±7°). Ordinary launch uses `loftBaseDeg` 16°, `loftPerPower` 1°, charged loft 2.5°, and `perFtOfHeight` **18° per foot** above/below the zone center, clamped to **−45° … 130°** (`minDeg`, `maxDeg`; only the under-the-ball term below reaches past the old 52° top). A downward launch can strike the dirt early; a shallow upward launch first bounces farther out; more loft carries above infield reach. The ordinary resolver never replaces this with a grounder/liner/pop band. Bunt and authored Star Swing responses retain their own contracts.
- **Under the ball — the pop behind the plate.** The part of the crossing above the barrel's nice top (the cursor center + `SweetSpot.HalfHeightFt`, the top of the batter's zone with the box centered; only the upper sour rim is there; the feet over are read in reference-zone feet, §4.4) is the bat meeting the bottom of the ball: it adds `launch.underBallDegPerFt` **200° per foot** to the launch, on top of the pitch-height term (`AtBatResolver.UnderTheBallDeg`; zero at or below the nice top).
  Across the rim's 0.36 ft the launch climbs through a steep pop in front of the plate, straight up at about 0.22 ft, and past 90° near the rim's top. **A launch past 90° is up and back over the plate**: the ball is carried as its supplement with the spray turned 180° (`AtBatResolver.PastVertical`: 100° at spray 10° is 80° at spray −170°), the same flight, so the class, the pool and the call read one ball. It is foul by the chalk (§5.6):
  the catcher (or a corner) may catch it in the air for a fly out; untouched it lands or meets the backstop (36 ft) and is foul, dead. The very top of the rim drives it into the backstop screen. Geometry and the ordinary launch noise decide it — no roll picks a foul back. Every batter at the rim's top goes back over the plate; no crossing at or under the nice top does (`FlightScenarioTests.UnderTheBall_*`, S-24c / S-24d).
- Sluggers' "scatter hit" (right stick at contact) is the reference's stick L/R. The reference guide calls it unreliable. An ordinary swing has no scatter hit at all (PH-12); a bunt and a Star Swing keep the stick L/R.

## 5.5 Exit velocity

`exit = base(power) × zone × charge × starSwing × pitch`.

- `base(power)` = `batting.exit.baseMph` + Power × `batting.exit.mphPerPower` (61 + 3.7 × Power; P7 lifted the base from 57 for the S-29 band and left the slope, so the whole lineup hits harder and the power hitter's homer stays inside the ≤ 2 per game mean). **Power** here is the rating `Stats.Power` (plus the bat's `powerMod`), not the `Bat` aggregate — it and the §5.4 loft are the only things Power drives. ✅ P2-a (S-122)

- `zone × charge`: the §5.2 table — 0 → the slap column; MAX → the charge column (**×1.25** on a perfect; reference: charge perfect 160–170 vs slap perfect 145–150, with less gravity). Below MAX interpolates; past the band the charge decays and the exit slides back toward the slap column. ✅ P1 (S-11, S-30)
- **No `buddies` term.** Good-chemistry runners on base used to multiply a charged swing's exit (×1.10 / 1.25 / 1.50, `buddiesOnBase.*Mul`) and widen a slap (§5.2). Jack removed plate-level chemistry (PH-16-R14): runners on base change neither the barrel nor the exit. ✅ (S-144)
- `pitch` (`batting.pitchFactor`): a charged pitch met with sour contact ×0.6; met with a perfect charge ×1.1 (reference "pitch type impact"). A high-**Movement** arm dampens non-perfect contact per point above 5 (nice ×0.9, sour ×0.75 at Movement 10) — the reference's hidden "cursed ball" made visible as the arm's stuff (Movement is the Break sub-stat, §2). ✅ P1, P3-a
- The Charge Bat gives a manual-MAX charge for free and keeps the narrow charge zones and the charge window off; it is never worse than a manual charge. ✅ (S-30). Its **window** clause is moot — nobody has a charge window to be spared — and its **spatial** clause is the whole item: it keeps the wide slap zones on a MAX charge, which is exactly the half PH-11-R1 says a charge trades. ✅ (S-30, S-125)
- Pull / push hitters (`data/characters/` optional `hitType`): ×1.05 to the named side, ×0.9 to the other. Optional; default mid.

## 5.6 Fair, foul, home run

- A ball is **foul** if it *lands* (or is first touched by a fielder) in foul territory, or rolls foul before passing a base without being touched. It is **fair** if it lands fair past the bases, or is touched fair, or leaves the park between the poles. The chalk is geometry, not a spray cutoff. ✅ P2:
  the untouched path's verdict is `BattedBall.Foul`, decided where the ball first lands past the bags, where it crosses the bag circle (90 ft) on a roll, where it rests, or where it touches a foul wall (S-20 … S-23). The wind bends the path, so the landing spray is not the spray at contact. There is no spin in the flight: a roll only curves with the wind. Touch-by-a-fielder is the live ball's call (P2 part b).
- **Home run**: the flight crosses the fence line above fence height between the poles. One rule, used by the flight, the fielding preview, and the landing ring. ✅ P2 (`BattedBall.HomeRun` from the fence crossing; the launch bands are gone).
- A ball that hits the wall is live (§7.9). A ball that bounces over is a ground-rule double (§1). ✅ P2
- **Foul ball** with fewer than 2 strikes adds a strike. With 2 strikes, nothing (except bunt). Runners return. The ball still flies so the camera can chase it; the stamp says FOUL when it lands. ✅ P2: a foul is a live ball the sim plays out (`LivePlaySystem`, `FairFoulCall`); it commits as `PlayKind.Foul` through `FinishInPlay` at the untouched path's verdict (plus the `flight.deadBall` beat) or at a touch on foul ground, and goes through `AfterPitch` like a take or a miss.
- A foul fly can be **caught** for an out (§7.11). ✅ P2 (S-24; the touch before the landing mark is the catch).

## 5.7 Hit by pitch — see §4.6.

## 5.8 Bunt

The bunt is a **held side** (PH-14-R2 … R6). Two face buttons square the bat: **West** toward third base, **North** toward first base. Left stick still walks the box (§5.4). ✅ P4-b in the sim (`BuntHold`, `PlateButtons`; S-19, S-130, S-153 … S-169).

- **Hold to square.** A bunt button down squares the bat toward its side while the plate accepts a bunt (SET and the pitch's flight). Among the bunt buttons that are down, the **latest press wins**. With both down, the one pressed on this tick wins; with both pressed on one tick, or both down and neither pressed, the side already held stays, and from no side it is the third-base side. Releasing the active bunt button while the other is held moves the bat to the other side. The side can change until contact. (S-153, S-154)
- **Release all to withdraw.** With no bunt button down the bat comes back: no swing, no bunt, nothing latched. A tap (down and up inside one tick) holds nothing. A withdrawn bat takes the pitch. (S-155)
- **Contact is geometric, with no timed press** (PH-14-R4). When the ball reaches the plate, a squared bat is the bunt (`SwingCommand.HeldBunt`): the bat is already on the plane, so the bunt has no timing error and no charge, whatever a caller writes. The cursor decides the quality (§5.2); a ball off the bat is a miss and a strike. Holding is not contact. (S-19, S-156, S-157)
- **The side leans the ball** (PH-14-R2). The spray is the side's lean, `bunt.sideDeg` (12°) toward that base (toward first positive, toward third negative), plus the spreads below. It is a bias, never a landing point or a fair ball. The stick shapes no bunt (§5.3). The side is a typed fact on the swing (`SwingCommand.BuntSide`) and on the play (`PlayEvent.Swing`). (S-130, S-158)
- **Contact fixes the side** (PH-14-R3). No bunt button after contact moves the ball or the side (`BuntHold.Contact`). (S-159)
- **Response by contact quality** (PH-14-R1). The bunt has its own response: better contact is the softer, more controlled bunt, never the ordinary swing's harder ball. Exit is `bunt.response.exitMph` by quality (Perfect 22, Nice 28, Sour 40 mph), with no Power, charge, star, pitch or tired-arm term. Launch is `bunt.launchMinDeg` + `launchSpanDeg` (3–12°).
  A sour bunt **pops** (the pop band) when the ball crossed above the bat's center and is chopped down with the sour pace below it; a sour chop is at or above `fielding.bunt.hardExitMph`, so the defense plays the lead force (§7.3). A crossing more than `bunt.popAboveCenterFt` above the zone center is a bunt pop whatever the quality. Spray is the lean + `bunt.response.spreadDeg` by quality (Perfect 10°, Nice 20°, Sour 44° total) + the out-of-zone spread. Timing never steers a bunt.
  Never a home run. The bunt table has no response switch and no swing-derived term: a table that authors `bunt.response.byContact`, `bunt.exitMul` or `bunt.spraySpanDeg` is refused by name. (S-167, S-168, S-169)
- **Foul bunt with two strikes is a strikeout.** ✅ P1 (S-18)
- **The plate on one tick** (`PlateButtons.Advance`, PH-13-R1): (1) **Committed**: a swing that committed on an earlier tick of this pitch follows through, and no bunt button squares until the next pitch. (2) **Bunt**: the side, as above. (3) **Swing**: while the bat is squared, the swing button is cancelled (§5.1):
  an armed load, or a press on this tick, is discarded with its charge, even when RT comes up on the same tick (the square beats the release), and a button still down must come up before a fresh press loads a new swing from zero. The explicit cancel (East) is the same cancel. So a bunt press during a load **converts** it to the bunt, the swing button while squared is not a swing, and a swing from a square needs every bunt button up and a fresh RT press. (S-160 … S-163)
- **A bunt button held for a bunt is no other verb** (PH-14-R6). A bunt button down at contact is spent: it counts as no verb (it does not square the next pitch, and no other reader may take it) until it comes up and is pressed again. Any reader of a bunt button asks `BuntHold.IsFree` first; the client marks contact with `PlateButtons.Contact` on the contact tick's input and keeps stepping the plate (not accepting) through the live ball so a release is seen. Today LT's other reader is the item modifier (LT + RB, LT + RT). (S-164)
- **One path.** Both seats, on two pads, and the CPU batter lay down the same held bunt: one seat's bunt buttons never move the other's bat, and a pad's held bunt and the CPU's with the same side, box and crossing are the same ball. (S-165)
- **The CPU sac bunt holds a side** (§5.9): the side is drawn once at SET with the square (`batting.cpu.sacBuntFirstSideChance` 0.5, first-base side, else third; `CpuBatter.BuntSide`), so the bat angle is a tell before the pitch. In the zone it is the held bunt toward that side; out of the zone it pulls the bat back and takes. (S-166)
- Bunt fielding: P, C, 1B, 3B charge (§7.3). Runner rules: sac bunt is a live play, not a table. ✅: the square is a typed fact on the swing (`SwingCommand.SquareSec`, how long the bat had been squared at the plate time); the defense reads it before the pitch (§7.3). No CPU fielder yet moves by the side.

**Client** (P4-c). West square toward third, North toward first, at the Input System's bunt button press point, on each seat's own pad; West no longer bunts. At the plate a squared bat is `PlateButtons.HeldBuntAtPlate`. The batter card names the held side (`BUNT 3B` / `BUNT 1B`, `BroadcastHud.BuntTell`) for a human and for the CPU.
The bat angles toward the held side: the squared take is `bunt-pull` when the side is the batter's pull field (third for a right-handed batter, first for a left-handed one) and `bunt-push` for the other field (`Motion.BuntClip`), so the defense reads the side in the barrel (PH-14-R3). Lessons T-B07 (either side, two strikes) and T-B11 (first-base side, runner on first).

✅ Sim. ✅ client: bindings, leak guards, editor gates (compiled; not run: Personal Unity cannot batchmode), book pair, lessons. ⚠️ The bat-angle cue. ⚠️ Human gate: sitting 4.

## 5.9 CPU batter

A table (`batting.json` `cpu`, `CpuBatter.Swing`), committed at the decision instant (§3) and read from **one crossing**: the flight **as it stands** at the commit — the break applied so far and no future steering (`CpuBatter.ReadPitch`, PH-18) — with the zone judged on that read. The read adds no draw. `CpuBatter.Swing` has no side effects: the box and the swing are the returned command. ✅ P1 (S-28); ✅ PH-18 (S-141 … S-143)

**Audit (what the CPU batter read before the commit read).** `DecideLeadSec` names *when* the CPU commits and `AtBatMotion.CommitCpuSwing` clamps the bat to that instant, but it never decided *what* was read: `CpuBatter.Swing` read `PitchFlight.Crossing(pitch)` (u = 1) of whatever command it got.
In the client (`AtBatDirector`) the call happens at `CpuDecisionTime`; with a human pitching, `_pitch.BreakX` is the stick so far, so that path already read the break as it stood — but projected with the late drift of that break, and the caller's `inZone` came from the same command. With a CPU pitcher the command carries the whole planned reach from release (§4.8, drawn frame by frame), and the headless `AutoPlay` / `cli match` hands `CpuBatter.Swing` the final prepared pitch:
both read the hidden destination. The commit read closes the second case and makes the first explicit. The timing source is unchanged and already keys to **Contact**, not Bat: σ = (11 − Contact) × `errorFramesPerBatStat` × `timingSigmaMul` (this audit decides nothing about it).

| Pitch | Count | Action |
| --- | --- | --- |
| In zone, middle third | any | Swing. Charge if count is 2-0, 3-1, 3-0 (Bat ≥ 6) or a runner is in scoring position with < 2 outs (Bat ≥ 7) |
| In zone, edge | < 2 strikes | Swing 65%, take 35% |
| In zone, edge | 2 strikes | Swing (protect) |
| Out of zone, near | < 2 strikes | Chase (18 − Bat) % |
| Out of zone, near | 2 strikes | Chase (30 − Bat) % |
| Out of zone, far | any | Take |
| Runner on 1st, 0 outs, Bat ≤ 5, trailing by ≤ 2 | | Sac bunt 35% — ✅: the square is read **at SET** (`CpuBatter.SquaresBunt`, once per pitch, the tell a human pitcher sees before the pitch, §7.3); at the plate plane a strike is bunted and a ball is taken with the corners already in (`batting.cpu.sacBuntSquareSec` is the headless square; the client passes its own clock). The side is drawn with the square (§5.8) |
| Star | ≥ 1 star, captain, runner on or 2 strikes | 20% |

**Tracking** (the reference model): the CPU batter's guess is the last crossing this offense saw; after release it re-reads the ball and centers the cursor on it `trackPerfectChance` (0.55) of the time, else the box stays at the guess plus a fixed offset (`mistrackMinFt` 0.11 – 0.41 ft, × the rung's `mistrackMul`, worse on easy). **If the pitcher moved on the rubber since the last pitch, the mistrack chance rises sharply** (`mistrackMovedChance` 0.7 vs `mistrackChance` 0.3).
That is why walking the rubber is a real verb against the CPU. Timing error σ = (11 − Bat) × `errorFramesPerBatStat` 0.62 frames × the rung's `timingSigmaMul`; when it did not track, a changeup pulls it `fooledMinFrames` 4–9 frames late and a charged pitch that many early. Zone classes: *middle* is the inner `middleFraction` of the frame; *near* is outside by at most `nearFt`. ✅ P1 (S-28)

**No forced-miss clamp against a human pitcher**: the human's meatball is punished by the same table; difficulty is a σ multiplier and the mistrack table (`data/rules/cpu.json` `difficulty` 0.8 / 1.0 / 1.3). ✅ P1 part a: `CpuSwingVsHuman`, its `batting.cpu.vsHuman` numbers, and the `cpuVsHumanTake` / `cpuVsHumanMiss` feel rolls are deleted; one table whoever pitches. ✅ the steal decision is its own once-per-at-bat read (`_cpuStealDecided`, §11.6), outside `CpuBatter.Swing`.

Charge vs slap by archetype (reference, `cpu.archetype`): balanced 50%, power 80%, speed 30%, technique 10% — derived from the character's **Power**/Run split (Power − Run ≥ `splitStat` = power, Run − Power ≥ `splitStat` = speed), with the technique gate on **Contact** and Run (both ≥ `techniqueMin`). ✅ P1, retargeted by P2-a

**Which trait each CPU read takes** (PH-15-R5; the rows above name `Bat` because it is the number a reader recognizes; each read takes a sub-stat, never the derived bar, §2). ✅ (S-122)

| Read | `Match` | Trait | Why |
| --- | --- | --- | --- |
| Chase on a near pitch, `(chase base − n)%` | `CpuBatter.Swing` | **Contact** | Laying off the pitch it cannot square up is contact judgment |
| Timing error σ, `(11 − n) × errorFramesPerBatStat` | `CpuBatter.Swing` | **Contact** | How far off the ball the bat arrives. ⚠️ a timing read; P2-b re-reads it |
| Forced charge, `n ≥ chargeBatMin` / `rispChargeBatMin` | `CpuBatter.Swing` | **Power** | Swinging for it on a hitter's count |
| Archetype split, `n − Run ≥ splitStat` | `CpuBatter.ChargeChance` | **Power** | A slugger against a speedster |
| Technique gate, `n ≥ techniqueMin && Run ≥ techniqueMin` | `CpuBatter.ChargeChance` | **Contact** | The hitter who both squares it up and beats it out slaps |
| Sac bunt, `n ≤ sacBuntBatMax` | `CpuBatter.SquaresBunt` | **Contact** | The weak-contact hitter gives himself up |
| Star swing, tracking, zone classes, the box | `CpuBatter.Swing` | — | No rating read at all; unchanged |

The rung multipliers (`timingSigmaMul`, `mistrackMul`) sit outside the trait and are the difficulty's, not the hitter's.

**The CPU batter's aims** (PH-12, PH-18). No human's stick shapes an ordinary swing, so the CPU batter holds none either: an ordinary CPU swing passes 0 / 0 and draws neither Gaussian (PH-18). A CPU Star Swing still draws a spray aim `Gauss() × spraySigmaDeg` (12°) and a launch aim `Gauss() × launchAimSigma` (0.45), and the sac bunt draws no aim and no timing error: it is the held bunt toward the side it drew with the square (§5.8).
Drawing no aim on an ordinary CPU swing keeps the seeded stream aligned ([`promote-stick.md`](../research/promote-stick.md)). ✅ (S-132)

**The CPU batter's timing σ is execution skill, not the window**. `(11 − Contact) × errorFramesPerBatStat × timingSigmaMul` is how far off the ball this bat *arrives*; `batting.window` is how far off the ball a swing may be and still meet it. They are two different things that both count frames, and the one window touches only the second: every hitter is judged in it, and the CPU still swings with its own error inside it.
So a CPU hitter with poor Contact still misses more, because its bat arrives further from the ball — it is not being given a narrower window. Whether that σ should key to Contact at all is the batting half of PH-18 and is P2-g's, not this section's. The rung multipliers above are CPU-only and are untouched by PH-17, which is about what a *pad* is given.
