using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec §7.3 (#625): the bunt defense. The batter's square is a typed fact on the swing
/// (<see cref="SwingCommand.SquareSec"/>); the corners crash and the middle covers before contact
/// (<see cref="BuntDefense"/>); the earliest of P / C / 1B / 3B fields (§8.2); the throw follows
/// §8.8's bunt rows. S-49 (a popped bunt caught by the catcher, the runner doubled off) is named
/// here, and §5.9's sac-bunt row runs as a live play. Every assertion reads the typed outcome:
/// the out's type, bag and runner, the cover the throw went to, the bodies at Time.
/// </summary>
public sealed class BuntScenarioTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    /// <summary>Exhibition's defense seat (D18): the CPU runs the glove until the stick takes it.</summary>
    static readonly LiveSeats HumanDefense = new(HumanBats: false, HumanPitches: true, PlayerMustField: false, Versus: false);
    static readonly LiveSeats HumanRunners = new(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);

    /// <summary>A bunt with the batter squared from SET (§5.8): the client's SET-to-plate time, batting.cpu.sacBuntSquareSec.</summary>
    static readonly SwingCommand SquaredBunt = new(true, 0, 0, false, Bunt: true, SquareSec: 1.8);
    /// <summary>The §7.3 scene: a bunt down the third-base line, dead about 25 ft out — the pitcher's ball from the rest spots, the third baseman's after the crash.</summary>
    const double BuntExit = 34, BuntLaunch = 5, BuntSpray = -40;
    /// <summary>S-49: a bunt popped straight up in front of the plate (§5.8's pop band), 29 ft out.</summary>
    const double PopExit = 24, PopLaunch = 65, PopSpray = 2;

    // ---------------------------------------------------------------------------------
    // The tell (§7.3): the square moves the bodies before contact
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheSquareCrashesTheCornersAndBringsTheMiddleToTheBags()
    {
        var match = Defense();
        var map = Gloves(match);
        var r = match.Rules;
        var b = r.Fielding.Bunt;
        var full = BuntDefense.Spots(map, 1.8, r);
        // The corners: crashFt toward the plate, on the line from their spot to home.
        foreach (var pos in new[] { "1B", "3B" })
        {
            var rest = Diamond.Positions[pos];
            var at = full[pos];
            Assert.Equal(b.CrashFt, Diamond.Dist(rest.X, rest.Z, at.X, at.Z), 2);
            Assert.Equal(Diamond.Dist(rest.X, rest.Z, 0, 0) - b.CrashFt, Diamond.Dist(at.X, at.Z, 0, 0), 2);
        }
        // The middle walks to the bags at the flat cover speed (§8.7, D11): 1.8 s in, the second baseman is still a few
        // feet short of first (the throw waits for the cover if it must, §8.5); by 2.5 s both are on their bags.
        var cover = r.Fielding.Cover;
        foreach (var (pos, bag) in new[] { ("2B", Diamond.First), ("SS", Diamond.Second) })
        {
            var rest = Diamond.Positions[pos];
            // The C80 copy (#718): a cover walks at the body's own pursuit speed (cover.chaseSpeedWeight 1), not the flat 28 ft/s.
            var walkFt = FieldingResolver.CoverSpeedFt(map[pos], r);
            var expectWalk = Math.Min(Diamond.Dist(rest.X, rest.Z, bag.X, bag.Z) - cover.StopFt, walkFt * 1.8);
            Assert.Equal(expectWalk, Diamond.Dist(rest.X, rest.Z, full[pos].X, full[pos].Z), 2);
        }
        // At the copy's own legs the second baseman's 51 ft takes 2.7 s: both are on their bags by 3.0 s there.
        var settled = BuntDefense.Spots(map, 3.0, r);
        Assert.True(Diamond.Dist(settled["2B"].X, settled["2B"].Z, Diamond.First.X, Diamond.First.Z) <= cover.StopFt + 1e-6);
        Assert.True(Diamond.Dist(settled["SS"].X, settled["SS"].Z, Diamond.Second.X, Diamond.Second.Z) <= cover.StopFt + 1e-6);
        // Everyone else stays: the pitcher on the rubber, the catcher behind the plate, the outfield.
        foreach (var pos in new[] { "P", "C", "LF", "CF", "RF" })
            Assert.Equal(Diamond.Positions[pos], full[pos]);
        // The crash is a run at the body's own chase speed (§8.1): a short square is a short crash, and the cover is still walking.
        var brief = BuntDefense.Spots(map, 0.3, r);
        var expect = Math.Min(b.CrashFt, FieldingResolver.ChaseSpeedFt(map["3B"], false, r) * 0.3);
        Assert.Equal(expect, Diamond.Dist(Diamond.Positions["3B"].X, Diamond.Positions["3B"].Z, brief["3B"].X, brief["3B"].Z), 2);
        Assert.True(Diamond.Dist(brief["2B"].X, brief["2B"].Z, Diamond.First.X, Diamond.First.Z) > cover.StopFt);
        // No square, nobody moves.
        Assert.All(BuntDefense.Spots(map, 0, r), kv => Assert.Equal(Diamond.Positions[kv.Key], kv.Value));
    }

    [Fact]
    public void TheBuntCoverMapIsTheMiddleBehindTheCrashAndTheBuntPoolIsTheFour()
    {
        var b = _content.Rules.Fielding.Bunt;
        var m = BuntDefense.CoverMap("3B", b);
        Assert.Equal(("2B", "SS", "P", "C"), (m[1], m[2], m[3], m[4]));
        var p = BuntDefense.CoverMap("P", b);
        Assert.Equal(("2B", "SS", "3B", "C"), (p[1], p[2], p[3], p[4]));
        Assert.Equal("P", BuntDefense.CoverMap("C", b)[4]);
        Assert.Equal(FieldingResolver.BuntPursuitPositions, FieldingResolver.PursuitPool(BattedBallClass.Bunt, false));
        Assert.Equal(FieldingResolver.FoulPursuitPositions, FieldingResolver.PursuitPool(BattedBallClass.Bunt, true));
        Assert.Equal(FieldingResolver.InfieldPursuitPositions, FieldingResolver.InfieldPool(BattedBallClass.Grounder));
        // The square alone (a slash) or the bunt alone (a late press) brings the middle in; a plain swing does not.
        Assert.True(BuntDefense.AlignmentOn(SquaredBunt, BattedBallClass.Bunt));
        Assert.True(BuntDefense.AlignmentOn(SquaredBunt with { Bunt = false }, BattedBallClass.Grounder));
        Assert.True(BuntDefense.AlignmentOn(Scenario.SwingAt(0, bunt: true), BattedBallClass.Bunt));
        Assert.False(BuntDefense.AlignmentOn(Scenario.Swing, BattedBallClass.Grounder));
    }

    [Fact]
    public void TheRulesTableCarriesTheBuntDefense()
    {
        var b = _content.Rules.Fielding.Bunt;
        Assert.Equal(25, b.CrashFt);
        Assert.Equal(new[] { "1B", "3B" }, b.Crash);
        Assert.Equal(("2B", "SS"), (b.CoverFirst, b.CoverSecond));
        Assert.Equal(new[] { "P", "C", "1B", "3B" }, b.Charge);
        Assert.Equal(30, b.SqueezeHomeFt);
        Assert.True(b.HardExitMph > 0);
        Assert.True(_content.Rules.Batting.Cpu.SacBuntSquareSec > 0);
    }

    // ---------------------------------------------------------------------------------
    // The scene: a squared bunt to the third-base side (§7.3), both seats
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheSquareMakesTheCrashingThirdBasemanTheGlove()
    {
        var match = Defense();
        Station(match, [1]);
        var hit = FlightFixtures.Hit(match.Park, 30, -4, BuntSpray, bunt: true);
        Assert.Equal(BattedBallClass.Bunt, hit.Class);
        // From the rest spots the pitcher's route is earliest; after the crash the third baseman's is (§8.2, D16).
        Assert.Equal("P", match.PreviewHit(hit).Position);
        Assert.Equal("3B", match.PreviewHit(hit, SquaredBunt).Position);
    }

    [Fact]
    public void SquaredBuntToThirdIsFieldedByTheCrashingThirdBasemanWithSecondCoveringFirst_CpuSeat()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        var hit = FlightFixtures.Hit(match.Park, BuntExit, BuntLaunch, BuntSpray, bunt: true);
        var preview = match.PreviewHit(hit, SquaredBunt);
        Assert.Equal("3B", preview.Position);
        var run = Run(match, hit, preview, SquaredBunt, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        AssertSacBuntRow(match, run, runner, "3B");
    }

    [Fact]
    public void SquaredBuntToThirdIsFieldedByTheCrashingThirdBasemanWithSecondCoveringFirst_HumanSeat()
    {
        // The human takes the crashing body with the stick (D18) and keeps it for the play: no hand-off takes a body
        // that still has a route (§8.9). The throw is theirs: first, the default.
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        var hit = FlightFixtures.Hit(match.Park, BuntExit, BuntLaunch, BuntSpray, bunt: true);
        var preview = match.PreviewHit(hit, SquaredBunt);
        Assert.Equal("3B", preview.Position);
        var thrown = false;
        // The C80 copy's pursuit stick (#718) takes the glove only after it has been seen at neutral: six dead frames first.
        // Without them the stick never takes the body, and the first human frame is the CPU's own pickup.
        var neutralSec = 6 * Frame;
        var run = Run(match, hit, preview, SquaredBunt, HumanDefense, LivePlayCommandSource.Human, fieldPad: live =>
        {
            if (thrown || live.Throwing) return LivePadInput.Dead;
            if (live.ElapsedSeconds < neutralSec - 1e-9) return LivePadInput.Dead;
            if (live.HoldsBall)
            {
                thrown = true;
                return new LivePadInput(KeysBag: 1, SouthDown: true);
            }
            var dx = live.BallX - live.GloveX;
            var dz = live.BallZ - live.GloveZ;
            var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
            return new LivePadInput(StickX: dx / len, StickY: dz / len);
        });
        Assert.True(run.TookAt >= 0, "the stick took the glove");
        Assert.NotEmpty(run.GlovesAfterTake);
        Assert.All(run.GlovesAfterTake, pos => Assert.Equal("3B", pos));
        AssertSacBuntRow(match, run, runner, "3B");
    }

    void AssertSacBuntRow(Match match, RunResult run, Character runner, string fielderPos)
    {
        var facts = run.Play.Outcome!;
        var outs = facts.OutsMade;
        // The batter is thrown out at first by the crashing corner (§7.3: first by default) …
        var o = Assert.Single(outs);
        Assert.Equal((OutType.ThrowOutAtFirst, 1, 0), (o.Type, o.Bag, o.FromBag));
        Assert.Equal(Gloves(match)[fielderPos].Id, o.Fielder!.Id);
        // … into the second baseman's glove: the cover behind the crash (§7.3, §8.7).
        var toFirst = Assert.Single(run.Throws);
        Assert.Equal((1, "2B", fielderPos), (toFirst.Bag, toFirst.CoverPos, toFirst.FromPos));
        // The sac: the runner from first moved up, and the play stamps the bunt (§7.3).
        Assert.Contains(facts.Moves, m => m.Runner.Id == runner.Id && m.FromBag == 1 && m.ToBag == 2);
        Assert.Equal("BUNT", PlayStamp.Label(run.Play));
        Assert.True(run.Play.Swing.Bunt);
    }

    [Fact]
    public void ARunnerOnThirdHoldsOnABuntUnlessSent()
    {
        // §7.3: the runner from third goes at contact only if the offense sent them; the CPU runner holds through the throw to first.
        var match = Defense();
        Station(match, [1, 3]);
        var third = match.Third!;
        var hit = FlightFixtures.Hit(match.Park, BuntExit, BuntLaunch, BuntSpray, bunt: true);
        var preview = match.PreviewHit(hit, SquaredBunt);
        var run = Run(match, hit, preview, SquaredBunt, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu);
        var facts = run.Play.Outcome!;
        Assert.DoesNotContain(facts.Moves, m => m.Runner.Id == third.Id);
        Assert.DoesNotContain(facts.OutsMade, o => o.Runner.Id == third.Id);
        Assert.DoesNotContain(run.Throws, t => t.Bag == 4);
        Assert.Equal(0, run.Play.RunsScored);
    }

    // ---------------------------------------------------------------------------------
    // S-49: the popped bunt (§5.8, §10.5)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S49_SquaredBuntPoppedInFrontOfThePlateIsCaughtByTheCatcherAndTheRunnerSentIsDoubledOffFirst()
    {
        var match = Defense();
        Station(match, [1]);
        var runner = match.First!;
        var hit = FlightFixtures.Hit(match.Park, PopExit, PopLaunch, PopSpray, bunt: true);
        Assert.Equal(BattedBallClass.Pop, hit.Class);
        var preview = match.PreviewHit(hit, SquaredBunt);
        // The catcher's route is earliest to a pop 29 ft out (§8.2): the corners are still 80 ft away after the crash.
        Assert.Equal("C", preview.Position);
        // The offense sends the runner at contact with the stick (§9.3); on a ball in the air that is the doubled-off risk (§10.5).
        var run = Run(match, hit, preview, SquaredBunt, HumanRunners, LivePlayCommandSource.Human,
            runPad: live => live.ElapsedSeconds < 0.1 ? new LivePadInput(KeysBag: 1, StickBag: 2) : LivePadInput.Dead);
        var outs = run.Play.Outcome!.OutsMade;
        Assert.Equal(2, outs.Count);
        Assert.Equal((OutType.Catch, 0), (outs[0].Type, outs[0].FromBag));
        Assert.Equal(Gloves(match)["C"].Id, outs[0].Fielder!.Id);
        // The throw back to first is a force back (§10.5) into the second baseman's glove, the bunt cover of first.
        Assert.Equal((OutType.Force, 1, 1, runner.Id), (outs[1].Type, outs[1].Bag, outs[1].FromBag, outs[1].Runner.Id));
        var back = Assert.Single(run.Throws);
        Assert.Equal((1, "2B", "C"), (back.Bag, back.CoverPos, back.FromPos));
        Assert.Equal("DOUBLE PLAY", PlayStamp.Label(run.Play));
    }

    // ---------------------------------------------------------------------------------
    // §5.9's sac-bunt row is a live play through the square
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheCpuSacBuntIsReadAtSetAndPlayedLive()
    {
        // Runner on first, no outs, a light bat, a close game (§5.9): the square is read at SET; at the plate plane a
        // strike is bunted and a ball is taken with the corners already in — the tell's cost.
        var squaredOnce = false;
        for (var seed = 1; seed <= 60 && !squaredOnce; seed++)
        {
            var match = Defense("pip", seed); // pip: Bat 3, the leadoff
            Station(match, [1]);
            Assert.True(match.Batter.Stats.Bat <= match.Rules.Batting.Cpu.SacBuntBatMax);
            var squared = match.CpuBatter.SquaresBunt();
            Assert.Equal(squared, match.CpuBatter.SquaresBunt()); // idempotent for the pitch
            Assert.Equal(squared, match.CpuBatter.Squared);
            if (!squared) continue;
            squaredOnce = true;
            var pitch = match.PreparePitch(Scenario.PitchAt(0, StrikeZoneGeometry.CenterY));
            var ball = match.CpuBatter.Swing(match.PreparePitch(Scenario.PitchAt(2.5, StrikeZoneGeometry.CenterY)));
            Assert.False(ball.Swing);
            Assert.True(ball.SquareSec > 0);
            var swing = match.CpuBatter.Swing(pitch);
            Assert.True(swing.Swing && swing.Bunt && swing.SquareSec > 0);
            var play = match.Play(pitch, swing);
            Assert.True(play.Swing.Bunt);
            Assert.False(match.CpuBatter.Squared); // spent on this pitch
            Assert.Contains(play.AtBat.Class, new[] { BattedBallClass.Bunt, BattedBallClass.Pop, BattedBallClass.Foul });
            if (play.Kind is PlayKind.Foul or PlayKind.Strikeout or PlayKind.SwingMiss) continue;
            // A live play (§10.6): typed bodies at Time, a glove from the bunt's four, never a table row.
            Assert.NotEmpty(play.Outcome!.BodiesAtTime);
            var gloves = Gloves(match);
            Assert.NotNull(play.Fielder);
            var pos = gloves.First(kv => kv.Value.Id == play.Fielder!.Id).Key;
            if (play.AtBat.Class == BattedBallClass.Bunt)
                Assert.Contains(pos, FieldingResolver.BuntPursuitPositions);
        }
        Assert.True(squaredOnce, "some seed squares");
    }

    [Fact]
    public void TheCpuBatterDoesNotSquareOutsideTheRow()
    {
        var match = Defense("cinder"); // cinder: Bat 8
        Station(match, [1]);
        Assert.False(match.CpuBatter.SquaresBunt());
        var light = Defense("pip");
        Assert.False(light.CpuBatter.SquaresBunt()); // nobody on
        Station(light, [1, 2]);
        Assert.False(light.CpuBatter.SquaresBunt()); // not first only
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    public sealed class ThrowRecord
    {
        public int Bag;
        public string CoverPos = "";
        public string FromPos = "";
    }

    public sealed class RunResult
    {
        public PlayEvent Play = null!;
        public List<ThrowRecord> Throws = new();
        public int TookAt = -1;
        public List<string> GlovesAfterTake = new();
    }

    RunResult Run(Match match, AtBatResult hit, FieldingPreview preview, SwingCommand swing, LiveSeats seats, LivePlayCommandSource seat,
        Func<LivePlaySystem, LivePadInput>? fieldPad = null, Func<LivePlaySystem, LivePadInput>? runPad = null)
    {
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, swing, hit, preview, null, seats, 0, seat)).Snapshot.Active);
        Assert.Equal(swing.SquareSec, live.SquareSec);
        var result = new RunResult();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var field = fieldPad?.Invoke(live) ?? LivePadInput.Dead;
            var run = runPad?.Invoke(live) ?? LivePadInput.Dead;
            var r = live.Apply(LivePlayCommand.Tick(Frame, field, run, false, seat));
            if (live.Events.Contains(LiveEvent.ThrowPop))
                result.Throws.Add(new ThrowRecord { Bag = live.ThrowBag, CoverPos = live.CoverPos, FromPos = live.ThrowFromPos });
            if (result.TookAt < 0 && live.PlayerFielding) result.TookAt = i;
            if (result.TookAt >= 0 && !live.Throwing && !live.HoldsBall && live.Active) result.GlovesAfterTake.Add(live.GlovePos);
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        result.Play = play!;
        return result;
    }

    static Dictionary<string, Character> Gloves(Match match) =>
        FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);

    /// <summary>Home nine on defense: vale on the mound, pewter behind the plate, lace at first, frost at second, basil at third, ashlord at short; the away leadoff bats.</summary>
    Match Defense(string leadoff = "cinder", int seed = 1)
    {
        var home = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = _content.Team("Offense", "zig", leadoff, "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        return Match.Exhibition(_content, home, away, 3, seed);
    }

    /// <summary>Station the away order's next hitters on the given bags (bag 1 gets the second hitter, and so on).</summary>
    static void Station(Match match, int[] bags)
    {
        var roster = match.Away.Roster.ToList();
        foreach (var bag in bags)
            Assert.True(match.StationRunner(bag, roster[bag + 1]), $"station bag {bag}");
    }
}
