# Curveball, slider and sinker — the P1-d proposal

**Status: proposed, not accepted.** These numbers live in the trial overlay `trials/pitch5` and
nowhere else. The shipped root is unchanged. Jack judges them in the trial window (PH-20-R1), which
is not possible yet: the mound is not wired to a seat until P1-f.

Issue [#818](https://github.com/jackguillet/grand-sluggers/issues/818). Decisions: PH-02-R2 (the
five families and their roles), PH-03 (height is the family's), PH-04 (bounded live steering stays),
PH-05-R1 (a charge costs steering correction, never characteristic movement), PH-15-R6 (natural
movement and player steering are distinct), PH-20-R1 (new pitch numbers live in a trial).
Spec: §4.2, §4.3, §16. Tests: `PitchFamilyTrialScenarioTests`, S-107 … S-113.
Evidence: [`pitch-families-p1d.json`](pitch-families-p1d.json), sealed by source hash, regenerated
with `dotnet run --project tools/pitch-family-probes -- --write` and verified with `--check`.

Run it:

```
GRAND_SLUGGERS_TRIAL=trials/pitch5 dotnet run --project src/GrandSluggers.Cli -- match --seed 7
```

**Sibling report:** [`cpu-pitcher-p1g.md`](cpu-pitcher-p1g.md) — P1-g (#823) turns the CPU pitcher's
switch on in this same overlay, so the CPU throws these three families with the inputs a hand has
(PH-18-R1). The two are judged together in sitting 1.

## The proposal

Two new fields on a family row. `sweepFt` is the family's **natural sweep**: feet the crossing ends
off the straight line from the rubber, positive toward the pitcher's **glove side**, mirrored by the
throwing hand. `sweepFrom` is where in the flight it starts to show; from there it grows as the
square of the flight that is left, so it is nothing early and all of itself at the plate.

| field | curveball | slider | sinker | why |
| --- | --- | --- | --- | --- |
| `mph` | 72.5 | 78 | 81 | The speed order the roles ask for, fastball > sinker > slider > curveball > changeup, laid out evenly inside the shipped 68.8 – 86 envelope so no family is a near-twin of another. |
| `chargeMph` | 4 | 5 | 6 | Between the changeup's 3 and the fastball's 8, in the same order: the harder the pitch, the more a charge is worth on it, and the order never crosses at full charge. |
| `hump` | 0.8 | 0.15 | 0.3 | The arc. The curveball's is the library's biggest and the only one that lifts a path above the hand it left; the slider's is the smallest, which is what makes it the flat one; the sinker's is a hair under the fastball's 0.35 so the two look alike for most of the way. |
| `hangUntil` | 0.55 | 0.8 | 0.75 | Where the shape stops holding its height and starts dumping. Early for the arc, late for the ride. |
| `hangRate` | 0.42 | 0.95 | 0.88 | How much of the descent happens before that. The sinker's 0.88 is what keeps it on the fastball's line (an exact ride would be 0.869 for this drop); the slider's 0.95 is nearly a straight line. |
| `dumpRate` | 1.72 | 1.26 | 1.37 | What is left, spent after the seam. Each row arrives a whisker early (1.005, 1.012, 1.003 of the descent), the way the shipped changeup does at 1.048. |
| `dropFt` | 0.9 | 0.3 | 0.55 | Feet below the fastball's crossing. The curveball matches the shipped changeup exactly, so **no new family crosses lower than anything already in the game**; the slider barely drops, the sinker is between. |
| `sweepFt` | +0.32 | +0.58 | −0.2 | The slider's is the big one and the reason to throw it; the curveball's is a third of the zone and secondary to its drop; the sinker's is small and the other way — the arm side — which is what a two-seam run is. |
| `sweepFrom` | 0.42 | 0.45 | 0.55 | Late enough that half the sweep is still in the last fifth of the flight, early enough that the coverable margin (relationship 5) stays positive in the worst legal case. The sinker's is latest because its sweep is smallest and therefore cheapest to hide. |
| `breakDamped` | false | false | false | All three steer like a fastball. **Consequence:** the player's full 0.46 ft of stick is available on top of the sweep when the pitch is uncharged, which is exactly the worst case relationship 5 measures; a charge damps the stick to 0.046 ft and leaves the sweep alone (PH-05-R1, PH-15-R6). |
| `staminaCost` | 2 | 2 | 1 | Effort on top of `stamina.pitchCost`, between the fastball's 0 and the changeup's 3. **Consequence:** at Pitch 5 (a 90-point pool, 4 per pitch) an all-curveball inning costs half again as much arm as an all-fastball one, and the changeup stays the single most expensive pitch in the library (PH-08: ordinary pitching stays economical). |
| `offSpeed` | **true** | false | false | The rule: off-speed when the family's base mph falls in the slowest third of the shipped envelope (below 74.53). The curveball at 72.5 is 2.03 mph inside it; the slider at 78 is 3.47 outside and the sinker at 81 is 6.47 outside. **Consequence:** an early swing on the curveball gets the sour-slap pop band (§5.2) and the CPU batter's fooled-late error (§5.9); the slider and the sinker are hard pitches and get neither. |

## The plots

Side view is height against distance; top view is world X against distance. All five families, the
strike zone drawn at the plate, Pitch 5 and no steering.

| | right-handed pitcher | left-handed pitcher |
| --- | --- | --- |
| side | [`side-R.svg`](pitch-families-p1d/side-R.svg) | [`side-L.svg`](pitch-families-p1d/side-L.svg) |
| top | [`top-R.svg`](pitch-families-p1d/top-R.svg) | [`top-L.svg`](pitch-families-p1d/top-L.svg) |

## The seven relationships, measured

**1. Speed order and the shipped envelope.** Every family, at every Pitch stat, charge and release,
is slower than the fastball and faster than the changeup, in the order the roles ask for.

| family | slowest legal (Pitch 1, no charge) | fastest legal (Pitch 10, full charge, Nice!) |
| --- | --- | --- |
| fastball | 86.90 mph, 0.9731 s | 108.15 mph, 0.7819 s |
| sinker | 81.90 mph, 1.0325 s | 100.80 mph, 0.8389 s |
| slider | 78.90 mph, 1.0717 s | 96.60 mph, 0.8754 s |
| curveball | 73.40 mph, 1.1521 s | 89.78 mph, 0.9419 s |
| changeup | 69.70 mph, 1.2132 s | 84.84 mph, 0.9967 s |

Air time never touches a clamp. The shipped fastball's fastest is 0.7819 s against an `airMinSec` of
0.78 and the shipped changeup's slowest is 1.2132 s against an `airMaxSec` of 1.28, so a family
between them is between those too — no new pitch leans on the clamp, and none leans on it harder
than the shipped pair already does.

**2. Strike-capable and reachable.** From the middle of the rubber with no steering, at Pitch 5:

| family | crossing (x, y) ft | keeps a ball inside the zone | oval distance, R batter | L batter |
| --- | --- | --- | --- | --- |
| fastball | (0, 2.55) | yes | 0.000 | 0.000 |
| changeup | (0, 1.65) | yes | 0.818 | 0.818 |
| curveball | (+0.32, 1.65) | yes | 0.873 | 0.923 |
| slider | (+0.58, 2.25) | yes | 0.616 | 0.820 |
| sinker | (−0.20, 2.00) | yes | 0.567 | 0.535 |

Signs are a right-hander's; a left-hander's mirror exactly. "Keeps a ball inside the zone" is the
crossing plus a ball radius (0.125 ft) still inside the zone's 0.92 ft half-width and 1.45 – 3.65 ft
band. Oval distance is 1 on the drawn nice boundary, so every unsteered crossing in the library is
inside the nice oval of a batter standing where the box starts them, with an ordinary bat and no
charge.

**On the margin used.** The sim has no ball radius. `Baseball.cs` draws the ball 0.62 ft across and
says in its own summary that a real ball is about 0.25 ft; the drawn ball is a readability mesh and
decides how big the ball looks, not where it is. So the margin here is the real ball's radius,
0.125 ft. A drawn-ball margin (0.31 ft) would refuse the shipped changeup, which crosses 0.20 ft
above the zone floor.

**3. Height paths.** Measured over the whole flight, one arm (height does not know the hand):

| family | hump above its own chord | peak to plate | lost over the last two fifths | strays from the fastball's height before u = 0.75 | and dips beyond that by |
| --- | --- | --- | --- | --- | --- |
| fastball | 0.350 | 3.650 | 1.796 | 0 | 0 |
| changeup | 2.200 | 4.550 | 3.949 | 1.313 | −0.413 |
| curveball | 2.243 | 4.680 | 3.876 | 1.402 | −0.502 |
| slider | 0.265 | 3.950 | 1.843 | 0.255 | +0.045 |
| sinker | 0.605 | 4.200 | 2.270 | 0.076 | +0.474 |

The curveball arcs (it is the only family in the library whose path climbs after release — peak
6.330 ft at u = 0.200, against a 6.2 ft release) and travels farther up-and-down than anything else.
The slider is the flattest of the three by chord excursion, 0.265 ft against the sinker's 0.605 and
the curveball's 2.243. The sinker rides: it is never more than 0.076 ft off the fastball's height
through the first three quarters — the others are 0.255 and 1.402 off — and then leaves that line by
a further 0.474 ft, which is the dip.

Two honest notes. The "hump above its own chord" column is the one place the curveball only just
wins (2.243 against the shipped changeup's 2.200): the changeup's extreme hang is a hump by another
name. The tests therefore assert the hump term and the climb, not that column. And the changeup
still loses the most height over the last two fifths (3.949 ft against the curveball's 3.876); the
curveball's claim is the arc, not out-dropping a pitch whose whole identity is the dump.

**4. Sweep.** Slider +0.58 ft, curveball +0.32 ft, both toward the glove side; sinker −0.20 ft,
toward the arm side; fastball and changeup exactly 0. The mirror by throwing hand is **exact**, not
approximate: the sign is ±1 and negating a double is exact, so the sweep term at every point of the
flight satisfies `x_R == −x_L` bit for bit, and so does the whole crossing from the middle of the
rubber (where the unswept crossing is exactly +0.0). Off the middle of the rubber the two hands are
still mirror images about the same delivery's unswept line, but the flight rounds `base + sweep`
once per hand, so that claim is arithmetic to ~1e-12 rather than bitwise; the test says which is
which and why.

The glove side is +X for a right-hander. Derived from the diamond, not remembered: home is the
origin and first base is at `infield.cornerFt` on +X (a `[Positive]` rule, so it is +X in every data
root there can be), and a right-hander on the rubber faces home with first base on the glove hand's
side. So a right-hander's slider runs away from a right-handed batter and his sinker runs in on one,
which is what these pitches are for.

**5. Readable and coverable.** Worst legal case: the family's whole sweep plus the player's whole
stick the same way, Pitch 10, Nice! release, batter centred with an ordinary bat and no charge, and
the batter does not move until the **sweep alone** has taken the ball 0.125 ft off the line it was
flying. Reach is the box walk (1.6 box-units per second × 2.4 ft per box-unit = 3.84 ft/s) for
whatever is left of the flight, plus the nice oval's half-width **at the crossing's own height**.
Margin is reach minus the lateral distance to the crossing; every one is positive.

| family | charge | air | crossing x | sweep shows at u | window | walk | margin, R batter | L batter |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| curveball | — | 0.988 s | +0.780 ft | 0.782 | 0.215 s | 0.825 ft | **+0.649** | **+0.477** |
| curveball | full | 0.942 s | +0.366 ft | 0.782 | 0.205 s | 0.787 ft | +1.024 | +0.852 |
| slider | — | 0.926 s | +1.040 ft | 0.705 | 0.273 s | 1.047 ft | +1.018 | **+0.729** |
| slider | full | 0.875 s | +0.626 ft | 0.705 | 0.258 s | 0.991 ft | +1.375 | +1.086 |
| sinker | — | 0.895 s | −0.660 ft | 0.906 | 0.084 s | 0.324 ft | **+0.313** | +0.573 |
| sinker | full | 0.839 s | −0.246 ft | 0.906 | 0.079 s | 0.304 ft | +0.707 | +0.967 |

A right-handed arm; a left-handed one mirrors, with the two batter columns swapped. The tightest
case in the whole set is the sinker's +0.313 ft — a right-handed batter covering a right-hander's
uncharged sinker, whose sweep is the smallest and therefore the last to show. The uncharged row is
always the harder one, because a charge buys speed by spending steering.

This is a conservative reading twice over. Only the sweep opens the window; the stick's own
mid-flight bend reaches 0.125 ft by about u = 0.35, so a batter watching the real ball starts
earlier than this gives them credit for. And the batter is assumed to start dead centre, which is
the worst place to be for a pitch that ends on one side.

**Sensitivity, stated rather than buried.** If the threshold is the *drawn* ball's radius (0.31 ft)
instead of the real one, the curveball's 0.32 ft sweep barely exceeds it at all and is only
"visible" in the last 1 % of the flight, and its margin goes negative. That is a property of the
threshold, not of the pitch: any sweep near the threshold is undetectable by that measure however
early it starts. A sitting is the right place to settle it, because it is a question about eyes.

**6. Charge.** The sweep is identical at charge 0 and charge 1 — the same bits, at every point of
the flight, not only at the plate — while the stick's shift goes from 0.46 ft to 0.046 ft. A charge
costs steering correction and never characteristic movement (PH-05-R1).

**7. The flags.** In the table above, each with its consequence.

## What this is not

- **No sitting has happened.** The mound is not wired to a seat until P1-f, so nobody has thrown one
  of these pitches. Nothing here is a feel judgment.
- **No human acceptance.** PH-20-R1 keeps that with Jack, in the trial window. This child writes
  `numeric_targets` nowhere and `human_acceptance` nowhere.
- **No balance claim.** The CPU pitcher never selects these families, so no whole-game rate moved;
  `cli match --seed 7` under the trial prints the same log as the shipped run, for that reason.
- **The shipped root is unchanged.** `cli match --seed 7` is byte-identical before and after, the
  #811 golden passes unedited, and the #708 evidence diff is source-hash lines only. Without
  `GRAND_SLUGGERS_TRIAL=trials/pitch5` the three families still stop the pitch by name.
- **D7 is untouched.** No shipped speed, `arcadeScale` or air-time value moved.

## Two findings, reported and not fixed

**No ordinary family crosses high.** Every family's `dropFt` is at or below the fastball's, so the
library has no pitch that crosses above mid-zone and nothing that plays like a riser. The lowest
crossing in the library is still 1.65 ft, the shipped changeup's, which the curveball now matches.
Whether the library wants a high pitch is a design question this child does not answer.

**A left-hander releases from a right-hander's side.** `PitchFlight.Release` uses one
`releaseHandX` (+1.55 ft) for every pitcher, so the ball leaves the same point whatever the arm —
and, since the glove side is +X for a right-hander, that point is on the *glove* side for a
right-hander and the *arm* side for a left-hander. The sweep now mirrors with the hand; the release
does not. #818 leaves it alone deliberately (the issue lists "whether a lefty's release point should
mirror" as left open), and it is worth noting that fixing it would move a shipped flight and so
needs its own argument and its own golden.
