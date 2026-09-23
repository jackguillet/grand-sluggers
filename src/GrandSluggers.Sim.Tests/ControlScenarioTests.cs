using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec §8.9 (control: who you are on defense, D16–D18) as scenarios, Appendix B.8: S-94 … S-100 (#633, #667). Who wears the YOU
/// ring is an event — contact, the ball on the grass, a loose ball, a release, a Select press — never a per-frame re-pick;
/// a hand-off never takes a body that still has a route to the ball; the body the ring left coasts
/// <c>chase.handoffCoastSec</c> and nobody teleports; Select is locked for <c>chase.swapLockSec</c> and dead while
/// holding the ball. Headless on <see cref="LivePlaySystem"/>, the human seat's pad scripted per frame, both seats where
/// the row lives on one (S-94 / S-95).
/// </summary>
public sealed class ControlScenarioTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    /// <summary>A body's step in one frame at the fastest chase (Run 9, dash) is under this: any larger move is a teleport.</summary>
    const double StepCapFt = 1.5;

    // ---------------------------------------------------------------------------------
    // S-94  Dead stick: SS wears the ring from contact, the CPU runs the glove, nobody throws for you
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]   // 1P HOME pitches the top
    [InlineData(false)]  // 1P AWAY pitches the bottom
    public void S94_DeadStickTheRingSitsOnShortFromContactTheCpuScoopsAndNobodyThrows(bool pad1Home)
    {
        var (match, seats) = HumanDefense(pad1Home);
        var hit = GrounderToShort(match);
        var ring = new List<string>();
        var humanBeforeScoop = false;
        var everThrowing = false;
        var caughtAt = -1.0;
        var play = Run(match, seats, hit, (_, _) => LivePadInput.Dead, (live, _) =>
        {
            if (!live.Active) return;
            ring.Add(live.GlovePos);
            if (caughtAt < 0 && live.HoldsBall) caughtAt = live.ElapsedSeconds;
            if (caughtAt < 0 && live.PlayerFielding) humanBeforeScoop = true;
            everThrowing |= live.Throwing;
        });

        Assert.NotEmpty(ring);
        Assert.All(ring, pos => Assert.Equal("SS", pos));
        Assert.True(FieldAssist.ShowYou(seats.HumanFields, ring[0]), "the YOU ring shows on the play glove, dead stick included");
        Assert.False(humanBeforeScoop, "the CPU ran the glove: the human never took it (D18)");
        Assert.True(caughtAt > 0, "the CPU chase scooped it");
        Assert.False(everThrowing, "they do not throw for you (S-33)");
        Assert.NotNull(play);
        Assert.Equal(PlayKind.Single, play!.Kind);
        Assert.Empty(play.Outcome!.OutsMade);
        Assert.Equal(1, play.Outcome.BatterToBag);
    }

    // ---------------------------------------------------------------------------------
    // S-95  The stick past the threshold at 0.5 s takes SS; a dead stick afterwards still chases; South throws
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S95_StickPastTheThresholdAtHalfASecondOwnsShortADeadStickStillChasesAndSouthThrows(bool pad1Home)
    {
        var (match, seats) = HumanDefense(pad1Home);
        var hit = GrounderToShort(match);
        var take = match.Content.Feel.FieldAssistStick;
        var tookAt = -1.0;
        var caughtAt = -1.0;
        var releaseGlove = "";
        var releaseFrom = "";
        var throwBag = 0;
        var chasedAfterDead = false;
        var distBefore = double.NaN;
        var play = Run(match, seats, hit,
            (live, i) =>
            {
                var t = i * Frame;
                if (live.HoldsBall && !live.Throwing) return new LivePadInput(KeysBag: 1, SouthDown: true);
                if (t >= 0.5 && t < 0.5 + Frame * 3) return TowardBall(live);
                return LivePadInput.Dead;
            },
            (live, i) =>
            {
                if (!live.Active) return;
                var t = live.ElapsedSeconds;
                if (tookAt < 0 && live.PlayerFielding) tookAt = t;
                if (caughtAt < 0 && live.HoldsBall) caughtAt = t;
                // A dead stick after the take (from 0.55 s) still runs the route: the glove keeps closing on the ball.
                if (tookAt > 0 && caughtAt < 0 && t > 0.5 + Frame * 4)
                {
                    var d = Diamond.Dist(live.GloveX, live.GloveZ, live.BallX, live.BallZ);
                    if (!double.IsNaN(distBefore) && d < distBefore - 0.05) chasedAfterDead = true;
                    distBefore = d;
                }
                if (live.ThrowInFlight && releaseGlove == "")
                {
                    releaseGlove = live.GlovePos;
                    releaseFrom = live.ThrowFromPos;
                    throwBag = live.ThrowBag;
                }
            });

        Assert.InRange(tookAt, 0.5, 0.5 + Frame * 2);
        Assert.True(FieldAssist.StickTakesGlove(0.4, 0, take, false));
        Assert.True(caughtAt > tookAt, "the human's glove scooped it");
        Assert.True(chasedAfterDead, "a dead stick after the take still chases (CpuChases)");
        Assert.Equal("SS", releaseFrom);
        Assert.Equal(1, throwBag);
        Assert.Equal("1B", releaseGlove);
        Assert.NotNull(play);
        Assert.True(play!.Kind is PlayKind.GroundOut or PlayKind.Single, play.Kind.ToString());
    }

    // ---------------------------------------------------------------------------------
    // S-96  No hand-off while the human's 2B still has a route to a roller heading for the grass; 2B scoops
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]   // the human took 2B, then a dead stick lets the CPU run the route
    [InlineData(false)]  // the CPU seat, the same guard
    public void S96_ARollerReachesTheGrassBeforeSecondMeetsItAndSecondKeepsTheGloveWhileTheRouteReaches(bool human)
    {
        // The C80 copy: the lip is 137.78 ft and the legs are slower, so the shipped roller is past 2B before it can turn. The
        // high hopper at 9° is the one 2B runs down a foot onto the grass (138.7 ft, 2.06 s) before RF's route gets there (2.75 s).
        var (carry, launch, spray) = (95.0, -45.0, 14.0);
        S96_Row(human, carry, launch, spray);
    }

    void S96_Row(bool human, double carry, double launch, double spray)
    {
        var (match, seats) = human ? HumanDefense(true) : CpuDefense();
        // A ground ball through the right side: 2B's route meets it past the infield lip, before any outfielder's.
        var hit = FlightFixtures.Hit(match.Park, carry, launch, spray);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder);
        Assert.Equal("2B", preview.Position);
        var rules = match.Rules;
        var lip = rules.Flight.Classes.InfieldLipFt;
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var path = preview.Ball!.Samples;
        var start = Diamond.Positions["2B"];
        var route = FieldingPursuit.Plan(preview, match.Park, path, 0, start.X, start.Z,
            FieldingResolver.ChaseSpeedFt(map["2B"], "2B", preview, rules), rules, rules.Fielding.Reaction.LockoutSec("2B"));
        var outfield = FieldingPursuit.Choose(map, FieldingResolver.OutfieldPursuitPositions, preview, match.Park, path, null, 0, rules,
            FieldingResolver.CpuReactionLockouts(rules, null));
        Assert.True(route.Reachable, "the fixture: 2B reaches the roll");
        Assert.True(Diamond.Dist(0, 0, route.X, route.Z) >= lip, $"the fixture: 2B meets it on the grass ({Diamond.Dist(0, 0, route.X, route.Z):0} ft)");
        Assert.True(FieldingPursuit.Better(route, outfield.Route), "the fixture: 2B has the better route to this hop");

        var ring = new List<string>();
        var scoopedBy = "";
        var scoopDist = 0.0;
        var play = Run(match, seats, hit,
            (live, i) =>
            {
                var t = i * Frame;
                if (live.HoldsBall && !live.Throwing) return new LivePadInput(KeysBag: 1, SouthDown: true);
                if (human && t >= 0.3 && t < 0.3 + Frame) return TowardBall(live);
                return LivePadInput.Dead;
            },
            (live, _) =>
            {
                if (!live.Active || scoopedBy != "") return;
                ring.Add(live.GlovePos);
                if (live.HoldsBall)
                {
                    scoopedBy = live.GlovePos;
                    scoopDist = Diamond.Dist(0, 0, live.BallX, live.BallZ);
                }
            });

        Assert.All(ring, pos => Assert.Equal("2B", pos));
        Assert.Equal("2B", scoopedBy);
        Assert.True(scoopDist >= lip, $"the scoop was on the grass ({scoopDist:0} ft), where a position-only hand-off would already have moved the ring");
        Assert.NotNull(play);
        Assert.True(play!.Kind is PlayKind.GroundOut or PlayKind.Single, play.Kind.ToString());
    }

    // ---------------------------------------------------------------------------------
    // S-97  A liner over the shortstop's head: the hand-off goes to the outfielder by route when SS has no route; SS coasts; nobody teleports
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S97_LinerOverShortHandsOffByRouteWhenShortHasNoRouteShortCoastsAndNobodyTeleports()
    {
        var (match, seats) = HumanDefense(true);
        var hit = FlightFixtures.Hit(match.Park, 110, 10, -8);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Line);
        Assert.Equal("SS", preview.Position);
        var rules = match.Rules;
        Assert.True(FieldingResolver.OutfieldGrass(preview.LandingX, preview.LandingZ, rules), "the fixture: it lands on the grass past SS");
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var coast = rules.Fielding.Chase.HandoffCoastSec;
        Assert.Equal(0.2, coast);
        var coastFrames = (int)Math.Round(coast / Frame);
        var swapLock = rules.Fielding.Chase.SwapLockSec;
        var (ssX, ssZ) = Diamond.Positions["SS"];

        // Per frame: the glove, and every body where it stands.
        var frames = new List<(double T, string Glove, bool Human, Dictionary<string, (double X, double Z)> At)>();
        // Nobody reaches this liner in the air, so the outfield owns it from contact (§8.9: the plant is on the grass and the
        // infield has no route). The human takes SS with Select at 0.3 s, runs them at the landing through the lock, and lets go.
        var select = 0.3;
        var deadFrom = select + swapLock + Frame * 4;
        var play = Run(match, seats, hit,
            (live, i) =>
            {
                var t = i * Frame;
                if (live.HoldsBall && !live.Throwing) return new LivePadInput(KeysBag: 2, SouthDown: true);
                if (Near(t, select)) return Toward(live, ssX, ssZ, swap: true);
                if (t > select && t < deadFrom) return Toward(live, preview.LandingX, preview.LandingZ);
                return LivePadInput.Dead;
            },
            (live, _) =>
            {
                if (!live.Active) return;
                frames.Add((live.ElapsedSeconds, live.GlovePos, live.PlayerFielding, new Dictionary<string, (double X, double Z)>(live.Fielders)));
            }, maxFrames: 60 * 8);
        Assert.NotNull(play);

        Assert.True(FieldingResolver.IsOutfield(frames[0].Glove), $"from contact the ring is an outfielder's, not {frames[0].Glove}");
        var took = frames.FindIndex(f => f.Human);
        Assert.True(took > 0 && frames[took].Glove == "SS", "Select with the stick at SS took SS");
        var h = frames.FindIndex(took, f => f.Glove != "SS");
        Assert.True(h > took + 2, "the ring left SS");
        var before = frames[h - 1];
        var at = frames[h];
        Assert.True(FieldingResolver.IsOutfield(at.Glove), $"the hand-off goes to an outfielder, not {at.Glove}");
        // The moment: the first frame the chase evaluated it (the stick went dead after the lock) — and SS had no route to the ball then.
        Assert.InRange(at.T, deadFrom, deadFrom + Frame * 2);
        var ssBefore = before.At["SS"];
        var ssRoute = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, before.T, ssBefore.X, ssBefore.Z,
            FieldingResolver.ChaseSpeedFt(map["SS"], "SS", preview, rules), rules);
        Assert.False(ssRoute.Reachable, "SS had no route to the plant (D17)");
        // By route (D16), not by distance: the outfielder whose planned route meets the ball earliest, from where the bodies stood.
        var byRoute = FieldingPursuit.Choose(map, FieldingResolver.OutfieldPursuitPositions, preview, match.Park, preview.Ball.Samples,
            before.At, before.T, rules, FieldingResolver.CpuReactionLockouts(rules, preview.HangTimeSec));
        Assert.Equal(byRoute.Position, at.Glove);
        // Nobody teleports: every body's move across the hand-off frame and the coast is a step, and the new glove starts where it stood.
        for (var i = h; i <= Math.Min(frames.Count - 1, h + coastFrames + 1); i++)
            foreach (var kv in frames[i].At)
            {
                var was = frames[i - 1].At[kv.Key];
                Assert.True(Diamond.Dist(was.X, was.Z, kv.Value.X, kv.Value.Z) <= StepCapFt,
                    $"{kv.Key} moved {Diamond.Dist(was.X, was.Z, kv.Value.X, kv.Value.Z):0.0} ft in one frame at {frames[i].T:0.00}");
            }
        // SS coasts: the velocity of its last steered step, for chase.handoffCoastSec, then it stops where it is.
        var stepBefore = frames[h - 2].At["SS"];
        var vX = (ssBefore.X - stepBefore.X) / Frame;
        var vZ = (ssBefore.Z - stepBefore.Z) / Frame;
        var speed = Math.Sqrt(vX * vX + vZ * vZ);
        Assert.True(speed > 15, $"SS was running when the ring left ({speed:0.0} ft/s)");
        var ssAtHandoff = at.At["SS"];
        Assert.Equal(ssBefore, ssAtHandoff);
        var ssAfterCoast = frames[h + coastFrames].At["SS"];
        {
            // The response law (#718): the body keeps the coast's velocity for exactly chase.handoffCoastSec — every coast
            // frame at the coast's speed, none faster — and brakes over chase.brakeSec from the very next frame: each frame slower by
            // the brake rate of its rated speed, no standing frame between the coast and the brake, and at rest once the brake is spent.
            double StepFt(int i) => Diamond.Dist(frames[i - 1].At["SS"].X, frames[i - 1].At["SS"].Z, frames[i].At["SS"].X, frames[i].At["SS"].Z);
            var coasted = Diamond.Dist(ssAtHandoff.X, ssAtHandoff.Z, ssAfterCoast.X, ssAfterCoast.Z);
            Assert.InRange(coasted, speed * coast - 0.05, speed * coast + 0.05);
            for (var i = h + 1; i <= h + coastFrames; i++)
                Assert.Equal(speed * Frame, StepFt(i), 3);
            var off = Math.Abs((ssAfterCoast.X - ssAtHandoff.X) * vZ - (ssAfterCoast.Z - ssAtHandoff.Z) * vX) / speed;
            Assert.True(off < 0.05, $"SS coasts along its last heading ({off:0.00} ft off the line)");
            var brakeSec = rules.Fielding.Chase.BrakeSec;
            var rated = FieldingResolver.ChaseSpeedFt(map["SS"], false, rules);
            for (var k = 1; k <= 24 - coastFrames; k++)
                Assert.Equal(Math.Max(0, speed - k * Frame * rated / brakeSec) * Frame, StepFt(h + coastFrames + k), 3);
        }
    }

    // #636: the in-air D17. SS runs the one speed at a liner (§8.1: fielding.chase.infieldAirMul is for the stretched clock only),
    // so the rope it reaches with the plant 2 ft past the lip stays its own even though a position-only hand-off would fire.
    [Theory]
    [InlineData(true)]   // human, dead stick: the CPU runs SS
    [InlineData(false)]  // the CPU seat
    public void S97_ALinerTheShortstopReachesPastTheLipIsNeverHandedOffAndShortCatchesIt(bool human)
    {
        // On the 80-ft diamond there is no such ball: across the liners planted within 10 ft past the 137.78 ft lip, SS reaches
        // none (the nearest misses by 2.8 ft), so the position-only hand-off never argues with SS's route there. The row is the
        // deepest rope SS does reach (74 mph at 15°, planted 132.9 ft out): never handed off, and SS catches it. The tripwire
        // below says when a ball past the lip exists again; then this row takes it.
        var (exit, launch, spray) = (74.0, 15.0, -18.0);
        S97_PastTheLip_Row(human, exit, launch, spray, pastTheLip: false);
        Assert.NotEmpty(LinersShortReachesPastTheLip());
    }

    /// <summary>The liners to SS's side whose plant is on the grass and that SS's route reaches from its start: none on the 80-ft diamond.</summary>
    List<string> LinersShortReachesPastTheLip()
    {
        var (match, _) = CpuDefense();
        var rules = match.Rules;
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var start = Diamond.Positions["SS"];
        var found = new List<string>();
        for (var exit = 74.0; exit <= 78; exit += 1)
        for (var launch = 14.0; launch <= 18; launch += 0.5)
        foreach (var spray in new[] { -14.0, -18.0, -22.0 })
        {
            var preview = match.PreviewHit(FlightFixtures.Hit(match.Park, exit, launch, spray));
            if (!preview.Line || preview.Position != "SS") continue;
            var plant = FlyCatch.ChaseTarget(preview, match.Park, rules);
            if (!FieldingResolver.OutfieldGrass(plant.X, plant.Z, rules)) continue;
            var route = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, 0, start.X, start.Z,
                FieldingResolver.ChaseSpeedFt(map["SS"], "SS", preview, rules), rules, rules.Fielding.Reaction.LockoutSec("SS"));
            if (route.Reachable) found.Add($"{exit}/{launch}/{spray}");
        }
        return found;
    }

    void S97_PastTheLip_Row(bool human, double exit, double launch, double spray, bool pastTheLip)
    {
        var (match, seats) = human ? HumanDefense(true) : CpuDefense();
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Line);
        Assert.Equal("SS", preview.Position);
        var rules = match.Rules;
        var plant = FlyCatch.ChaseTarget(preview, match.Park, rules);
        Assert.True(FieldingResolver.OutfieldGrass(plant.X, plant.Z, rules) == pastTheLip, "the fixture: the plant is past the lip, where a position-only hand-off fires");
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var start = Diamond.Positions["SS"];
        var route = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, 0, start.X, start.Z,
            FieldingResolver.ChaseSpeedFt(map["SS"], "SS", preview, rules), rules, rules.Fielding.Reaction.LockoutSec("SS"));
        Assert.True(route.Reachable, "the fixture: SS reaches the plant");

        var ring = new List<string>();
        var caught = false;
        var play = Run(match, seats, hit, (_, _) => LivePadInput.Dead, (live, _) =>
        {
            // The catch completes the play on its own tick: read the cue before Active.
            if (live.Events.Contains(LiveEvent.Glove) || live.Caught) caught = true;
            if (!live.Active || caught) return;
            ring.Add(live.GlovePos);
        });

        Assert.NotEmpty(ring);
        Assert.All(ring, pos => Assert.Equal("SS", pos));
        Assert.True(caught, "SS caught it");
        Assert.NotNull(play);
        Assert.Equal(PlayKind.FlyOut, play!.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal(OutType.Catch, only.Type);
        Assert.Equal(map["SS"].Id, only.Fielder?.Id);
    }

    // #666: a liner is held on the live ball before the bounce (§7.6), not only under the landing ring at hang.
    [Fact]
    public void S97_SouthOnALinerAtTheInterceptIsACatchNotAScoop()
    {
        // The same rope on both roots. The C80 copy's pursuit stick (#718) takes the glove only after it has been seen at
        // neutral, so the pad there is dead for six frames before it runs SS at the intercept.
        S97_South_Row(88, 10, -18, 6);
    }

    void S97_South_Row(double exit, double launch, double spray, int neutralFrames)
    {
        var (match, seats) = HumanDefense(true);
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Line);
        Assert.Equal("SS", preview.Position);
        var start = Diamond.Positions["SS"];
        var intercept = LinerIntercept(preview, start, match.Rules);
        var plant = FlyCatch.ChaseTarget(preview, match.Park, match.Rules);
        Assert.True(Diamond.Dist(intercept.X, intercept.Z, plant.X, plant.Z) > match.Rules.Fielding.Catch.RadiusBaseFt,
            "the fixture: the intercept is short of the bounce, so a plant-only catch would miss it");

        var onTheBall = false;
        var play = Run(match, seats, hit,
            (live, i) =>
            {
                if (live.HoldsBall && !live.Throwing) return new LivePadInput(KeysBag: 1, SouthDown: true);
                if (i < neutralFrames) return LivePadInput.Dead;
                return Toward(live, intercept.X, intercept.Z) with { SouthDown = true };
            },
            (live, _) =>
            {
                if (!live.Active) return;
                var window = FieldingResolver.CatchWindowFt(preview.CatchRadius, false, false, match.Rules);
                if (live.ElapsedSeconds < preview.HangTimeSec && live.BallY > match.Rules.Fielding.Catch.InAirMinY
                    && Diamond.Dist(live.GloveX, live.GloveZ, live.BallX, live.BallZ) < window)
                    onTheBall = true;
            });

        Assert.True(onTheBall, "SS met the rope in the air");
        Assert.NotNull(play);
        Assert.Equal(PlayKind.FlyOut, play!.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal(OutType.Catch, only.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S97_ALinerCenterReachesIsFlyOut(bool human)
    {
        var (match, seats) = human ? HumanDefense(true) : CpuDefense();
        var hit = FlightFixtures.Hit(match.Park, 100, 16, 0);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Line);
        var cfStart = Diamond.Positions["CF"];
        var cf = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves)["CF"];
        var route = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, 0, cfStart.X, cfStart.Z,
            FieldingResolver.ChaseSpeedFt(cf, "CF", preview, match.Rules), match.Rules,
            match.Rules.Fielding.Reaction.LockoutSec("CF"));
        var standUp = FieldingResolver.CatchWindowFt(FieldingResolver.CatchRadiusFt(cf, match.Park, match.Rules), false, false, match.Rules);
        var diveWin = FieldingResolver.CatchWindowFt(FieldingResolver.CatchRadiusFt(cf, match.Park, match.Rules), true, false, match.Rules);
        Assert.True(route.MissFt < diveWin, $"the fixture: CF reaches standing or diving (miss {route.MissFt:0.0} < dive {diveWin:0.0}, stand-up {standUp:0.0})");

        var catcher = "";
        var play = Run(match, seats, hit, (_, _) => LivePadInput.Dead, (live, _) =>
        {
            if (live.Active && catcher == "" && (live.Caught || live.Events.Contains(LiveEvent.Glove)))
                catcher = live.GlovePos;
        });
        Assert.NotNull(play);
        Assert.Equal(PlayKind.FlyOut, play!.Kind);
        var only = Assert.Single(play.Outcome!.OutsMade);
        Assert.Equal(OutType.Catch, only.Type);
        Assert.Equal(cf.Id, only.Fielder?.Id);
        if (catcher != "")
            Assert.True(FieldingResolver.IsOutfield(catcher), $"CF holds it, not {catcher}");
    }

    // #669: a routine fly the glove plants under is stand-up (not DIVE). Same 1P / CPU.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ARoutineFlyTheGlovePlantsUnderIsStandUpNotADive(bool human)
    {
        var (match, seats) = human ? HumanDefense(true) : CpuDefense();
        var hit = FlightFixtures.Hit(match.Park, 88, 32, 0);
        Assert.Equal(BattedBallClass.Fly, hit.Class);
        var play = Run(match, seats, hit, (_, _) => LivePadInput.Dead, null);
        Assert.NotNull(play);
        Assert.Equal(PlayKind.FlyOut, play!.Kind);
        Assert.Equal(DefensiveFeat.None, play.Outcome!.DefensiveFeat);
        Assert.Equal("OUT", PlayStamp.Label(play.Kind, 1, 0, feat: play.Outcome.DefensiveFeat));
    }

    [Fact]
    public void S97_ALinerThatHasTouchedTheDirtIsAScoopNotAFlyOut()
    {
        var (match, seats) = CpuDefense();
        var hit = FlightFixtures.Hit(match.Park, 110, 10, -8);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Line);
        var start = Diamond.Positions[preview.Position];
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var route = FieldingPursuit.Plan(preview, match.Park, preview.Ball!.Samples, 0, start.X, start.Z,
            FieldingResolver.ChaseSpeedFt(map[preview.Position], preview.Position, preview, match.Rules), match.Rules,
            match.Rules.Fielding.Reaction.LockoutSec(preview.Position));
        Assert.False(route.Reachable, "the fixture: nobody holds it before the bounce");

        var bounced = false;
        var play = Run(match, seats, hit, (_, _) => LivePadInput.Dead, (live, _) =>
        {
            if (live.ElapsedSeconds >= preview.HangTimeSec && !live.Caught) bounced = true;
        });
        Assert.True(bounced, "the ball touched the dirt");
        Assert.NotNull(play);
        Assert.NotEqual(PlayKind.FlyOut, play!.Kind);
        Assert.DoesNotContain(play.Outcome!.OutsMade, o => o.Type == OutType.Catch);
    }

    // ---------------------------------------------------------------------------------
    // S-100  A liner that lands in RC (RF starts closer) and rolls to the wall is CF's, both seats (#667)
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void S100_ALinerThatBouncesInRightCenterAndRollsToTheWallIsCenters(bool human)
    {
        var (match, seats) = human ? HumanDefense(true) : CpuDefense();
        var hit = FlightFixtures.Hit(match.Park, 110, 10, 14);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Line);
        var rules = match.Rules;
        var live0 = BallFlight.PointAt(preview.Ball!.Samples, 0, rules);
        Assert.True(FieldingResolver.InAir(preview, live0.Y, 0, preview.HangTimeSec, rules),
            "the fixture: still up at contact, where Plan used to hand RF the bounce");
        var rf = Diamond.Positions["RF"];
        var cf = Diamond.Positions["CF"];
        Assert.True(Diamond.Dist(rf.X, rf.Z, preview.LandingX, preview.LandingZ) + 8
                    < Diamond.Dist(cf.X, cf.Z, preview.LandingX, preview.LandingZ),
            "the fixture: RF starts closer to the bounce");
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var ready = FieldingResolver.CpuReactionLockouts(rules, preview.HangTimeSec);
        var byRoute = FieldingPursuit.Choose(map, FieldingResolver.OutfieldPursuitPositions, preview, match.Park,
            preview.Ball.Samples, null, 0, rules, ready);
        Assert.Equal("CF", byRoute.Position);

        var ring = new List<string>();
        var scoopedBy = "";
        var scoopDist = 0.0;
        var play = Run(match, seats, hit,
            (live, _) =>
            {
                // Dead stick until the scoop; then South so Time can come (D18: the CPU does not throw for you).
                if (live.HoldsBall && !live.Throwing) return new LivePadInput(KeysBag: 2, SouthDown: true);
                return LivePadInput.Dead;
            },
            (live, _) =>
            {
                if (!live.Active) return;
                if (FieldingResolver.IsOutfield(live.GlovePos) && scoopedBy == "") ring.Add(live.GlovePos);
                if (scoopedBy == "" && live.HoldsBall)
                {
                    scoopedBy = live.GlovePos;
                    scoopDist = Diamond.Dist(0, 0, live.BallX, live.BallZ);
                }
            });

        Assert.NotEmpty(ring);
        Assert.All(ring, pos => Assert.Equal("CF", pos));
        Assert.Equal("CF", scoopedBy);
        Assert.True(scoopDist > FieldBounds.DistHome(preview.LandingX, preview.LandingZ) + 20,
            $"the scoop is the wall ({scoopDist:0} ft), not the bounce ({FieldBounds.DistHome(preview.LandingX, preview.LandingZ):0} ft)");
        Assert.NotNull(play);
        Assert.NotEqual(PlayKind.FlyOut, play!.Kind);
        Assert.DoesNotContain(play.Outcome!.OutsMade, o => o.Type == OutType.Catch);
    }

    // ---------------------------------------------------------------------------------
    // S-98  The throw from SS to first: the ring is on 1B at release, SS stays put, the stick steers 1B, Select does nothing with the ball
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S98_ThrowFromShortToFirstPutsTheRingOnFirstAtReleaseShortStaysPutAndTheStickSteersFirst()
    {
        // A runner on first keeps the play live after the ball lands at first, so the human's 1B has a beat with the ball.
        var (match, seats) = HumanDefense(true, s => s.Runner(1, 1));
        var hit = GrounderToShort(match);
        var thrown = false;
        var releaseGlove = "";
        var releaseFrom = "";
        var releaseCover = "";
        var releaseBag = 0;
        var ssAtRelease = (X: double.NaN, Z: double.NaN);
        var ssMovedAfterRelease = 0.0;
        var firstWithBall = (X: double.NaN, Z: double.NaN);
        var firstLater = (X: double.NaN, Z: double.NaN);
        var gloveWhileHolding = new List<string>();
        var framesHolding = 0;
        var steer = (X: 0.9, Y: 0.4);
        var play = Run(match, seats, hit,
            (live, i) =>
            {
                var t = i * Frame;
                if (live.Throwing) thrown = true;
                if (live.HoldsBall && !live.Throwing && !thrown) return new LivePadInput(KeysBag: 1, SouthDown: true);
                // After the throw: steer whoever wears the ring and press Select every few frames.
                if (thrown) return new LivePadInput(StickX: steer.X, StickY: steer.Y, Swap: i % 6 == 0);
                if (t >= 0.5) return TowardBall(live);
                return LivePadInput.Dead;
            },
            (live, _) =>
            {
                if (!live.Active) return;
                if (live.ThrowInFlight && releaseGlove == "")
                {
                    releaseGlove = live.GlovePos;
                    releaseFrom = live.ThrowFromPos;
                    releaseCover = live.CoverPos;
                    releaseBag = live.ThrowBag;
                    ssAtRelease = live.Fielders["SS"];
                    return;
                }
                if (releaseGlove == "") return;
                var ss = live.Fielders["SS"];
                ssMovedAfterRelease = Math.Max(ssMovedAfterRelease, Diamond.Dist(ss.X, ss.Z, ssAtRelease.X, ssAtRelease.Z));
                if (!live.Throwing && live.HoldsBall)
                {
                    gloveWhileHolding.Add(live.GlovePos);
                    framesHolding++;
                    if (framesHolding == 1) firstWithBall = live.Fielders["1B"];
                    if (framesHolding == 30) firstLater = live.Fielders["1B"];
                }
            });

        Assert.Equal("SS", releaseFrom);
        Assert.Equal(1, releaseBag);
        Assert.Equal("1B", releaseCover);
        Assert.Equal("1B", releaseGlove);
        Assert.True(ssMovedAfterRelease < 0.01, $"the thrower stays put: SS moved {ssMovedAfterRelease:0.00} ft after the release");
        Assert.True(framesHolding >= 30, "1B held the ball with the play still live");
        Assert.All(gloveWhileHolding, pos => Assert.Equal("1B", pos));
        // The stick now steers 1B: along the stick, at a glove's pace.
        var dx = firstLater.X - firstWithBall.X;
        var dz = firstLater.Z - firstWithBall.Z;
        var moved = Math.Sqrt(dx * dx + dz * dz);
        Assert.True(moved > 8, $"1B walked with the stick ({moved:0.0} ft in 29 frames)");
        var mag = Math.Sqrt(steer.X * steer.X + steer.Y * steer.Y);
        Assert.True((dx / moved) * (steer.X / mag) + (dz / moved) * (steer.Y / mag) > 0.98, "along the stick");
        Assert.NotNull(play);
        Assert.True(play!.Kind is PlayKind.GroundOut or PlayKind.Single, play.Kind.ToString());
    }

    // ---------------------------------------------------------------------------------
    // S-99  Select with the stick at CF takes CF; a second press inside the lock is ignored; with the ball, Select does nothing
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S99_SelectTowardCenterTakesCenterASecondPressInsideTheLockIsIgnoredAndTheLockThenLifts()
    {
        var (match, seats) = HumanDefense(true);
        var hit = GrounderToShort(match);
        var lock_ = match.Rules.Fielding.Chase.SwapLockSec;
        Assert.Equal(0.7, lock_);
        var first = 0.3;
        var second = first + lock_ * 0.5;
        var third = first + lock_ + Frame * 2;
        var gloveAt = new Dictionary<int, string>();
        var lockAfterFirst = double.NaN;
        var (cfX, cfZ) = Diamond.Positions["CF"];
        var (lfX, lfZ) = Diamond.Positions["LF"];
        Run(match, seats, hit,
            (live, i) =>
            {
                var t = i * Frame;
                if (Near(t, first)) return Toward(live, cfX, cfZ, swap: true);
                if (Near(t, second)) return Toward(live, lfX, lfZ, swap: true);
                if (Near(t, third)) return Toward(live, lfX, lfZ, swap: true);
                return LivePadInput.Dead;
            },
            (live, i) =>
            {
                if (!live.Active) return;
                gloveAt[i] = live.GlovePos;
                if (Near(i * Frame, first)) lockAfterFirst = live.SwapLock;
            }, maxFrames: (int)Math.Ceiling((third + 0.2) / Frame));

        var f1 = (int)Math.Round(first / Frame);
        var f2 = (int)Math.Round(second / Frame);
        var f3 = (int)Math.Round(third / Frame);
        Assert.Equal("SS", gloveAt[f1 - 1]);
        Assert.Equal("CF", gloveAt[f1]);
        Assert.Equal(lock_, lockAfterFirst, 3);
        Assert.Equal("CF", gloveAt[f2]);
        Assert.Equal("CF", gloveAt[f2 + 1]);
        Assert.Equal("LF", gloveAt[f3]);
        Assert.All(gloveAt.Where(kv => kv.Key > f1 && kv.Key < f3), kv => Assert.Equal("CF", kv.Value));
    }

    [Fact]
    public void S99_HoldingTheBallSelectDoesNothing()
    {
        var (match, seats) = HumanDefense(true);
        var hit = GrounderToShort(match);
        var (cfX, cfZ) = Diamond.Positions["CF"];
        var pressesWithBall = 0;
        var gloveWithBall = new List<string>();
        var play = Run(match, seats, hit,
            (live, i) =>
            {
                if (live.HoldsBall && !live.Throwing)
                {
                    // Six Select presses with the stick at CF (and dead), then the throw.
                    if (pressesWithBall < 6)
                    {
                        pressesWithBall++;
                        return pressesWithBall % 2 == 0 ? new LivePadInput(Swap: true) : Toward(live, cfX, cfZ, swap: true);
                    }
                    return new LivePadInput(KeysBag: 1, SouthDown: true);
                }
                return LivePadInput.Dead;
            },
            (live, _) =>
            {
                if (live.Active && live.HoldsBall && !live.Throwing) gloveWithBall.Add(live.GlovePos);
            });

        Assert.Equal(6, pressesWithBall);
        Assert.NotEmpty(gloveWithBall);
        Assert.All(gloveWithBall, pos => Assert.Equal("SS", pos));
        Assert.NotNull(play);
    }

    // ---------------------------------------------------------------------------------

    /// <summary>The flight sample nearest <paramref name="from"/> that is still above the hop — the intercept, not the bounce.</summary>
    static (double X, double Z) LinerIntercept(FieldingPreview preview, (double X, double Z) from, RulesTable rules)
    {
        var minY = rules.Fielding.Catch.InAirMinY;
        Sample? best = null;
        var bestD = double.MaxValue;
        foreach (var sample in preview.Ball!.Samples)
        {
            if (sample.Height <= minY) continue;
            var d = Diamond.Dist(from.X, from.Z, sample.X, sample.Z);
            if (d < bestD)
            {
                bestD = d;
                best = sample;
            }
        }
        Assert.NotNull(best);
        return (best.Value.X, best.Value.Z);
    }

    static bool Near(double t, double at) => Math.Abs(t - at) < Frame / 2;

    /// <summary>The hard grounder right at SS (the P4 fixture): SS is the play glove from contact in either half.</summary>
    static AtBatResult GrounderToShort(Match match)
    {
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder);
        Assert.Equal("SS", preview.Position);
        return hit;
    }

    /// <summary>The 1P human on defense: home pitches the top, away pitches the bottom (S-93).</summary>
    (Match Match, LiveSeats Seats) HumanDefense(bool pad1Home, Action<Scenario>? setup = null)
    {
        var seats = pad1Home ? Seats.One : Seats.AwayOne;
        var scenario = new Scenario(_content, seed: 1);
        var match = scenario.Match;
        if (!pad1Home) match.SkipToHomeHalf();
        setup?.Invoke(scenario);
        Assert.True(seats.HumanPitches(match.Top), "the human is on defense this half");
        scenario.Contact();
        var live = LiveSeats.For(seats, match.Top);
        Assert.True(live.HumanFields);
        return (match, live);
    }

    (Match Match, LiveSeats Seats) CpuDefense()
    {
        var scenario = new Scenario(_content, seed: 1);
        scenario.Contact();
        return (scenario.Match, LiveSeats.CpuOnly);
    }

    static LivePadInput TowardBall(LivePlaySystem live) => Toward(live, live.BallX, live.BallZ);

    /// <summary>A full stick from the glove toward (x, z).</summary>
    static LivePadInput Toward(LivePlaySystem live, double x, double z, bool swap = false)
    {
        var dx = x - live.GloveX;
        var dz = z - live.GloveZ;
        var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
        return new LivePadInput(StickX: dx / len, StickY: dz / len, Swap: swap);
    }

    /// <summary>
    /// Drive the live ball to Complete (or the frame cap) with the seat's pad scripted per frame; the observer sees the
    /// system after each tick. A CPU seat gets a dead pad whatever the script says (§0.4).
    /// </summary>
    static PlayEvent? Run(Match match, LiveSeats seats, AtBatResult hit, Func<LivePlaySystem, int, LivePadInput> pad,
        Action<LivePlaySystem, int>? observe, int maxFrames = 60 * 20)
    {
        var live = match.LivePlay;
        var preview = match.PreviewHit(hit);
        var field = seats.HumanFields ? null : match.ResolveFielding(hit, preview);
        var source = seats.HumanFields ? LivePlayCommandSource.Human : LivePlayCommandSource.System;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, seats, 0, source)).Snapshot.Active);
        PlayEvent? play = null;
        for (var i = 0; i < maxFrames && play is null; i++)
        {
            var p = seats.HumanFields ? pad(live, i) : LivePadInput.Dead;
            var r = live.Apply(LivePlayCommand.Tick(Frame, p, LivePadInput.Dead, false, source));
            observe?.Invoke(live, i);
            play = r.CompletedPlay;
        }
        return play;
    }
}
