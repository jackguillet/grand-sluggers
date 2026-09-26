using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Ashlord's two (spec §13): Anvil (<c>skullball</c>) is fast, clangs at 70 % of the flight and drops to a crossing below the
/// aimed one, and the umpire, the bat and the CPU judge that real crossing in the ordinary window; Hot Iron (<c>furnace</c>)
/// puts a molten ball in play that a glove may hold only half a second in its first two seconds, then drops at its feet — by
/// the clock, never a roll, the same for a CPU glove and a player's. A catch still counts. No lava on the warning track.
/// </summary>
public sealed class AshlordAbilityTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    static readonly BatterZone Ref = StrikeZoneGeometry.Reference;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    public void AshlordCarriesAnvilAndHotIronUnderTheirOldIds()
    {
        var ashlord = Game.Must("ashlord");
        Assert.Equal("skullball", ashlord.StarPitch);
        Assert.Equal("furnace", ashlord.StarSwing);
        var pitch = Game.StarSkills.Pitch("skullball")!;
        var swing = Game.StarSkills.Swing("furnace")!;
        Assert.Equal("Anvil", pitch.Name);
        Assert.Equal("Hot Iron", swing.Name);
        Assert.Equal(1.2, pitch.SpeedMul);
        Assert.Equal(new PitchDrop(1.5, 0.7), pitch.Drop);
        Assert.Equal(1.25, swing.ExitVeloMul);
        Assert.Equal(new HotBall(2.0, 0.5), swing.HotBall);
        Assert.Null(swing.Terrain);          // the lava strip is gone: the heat is in the ball, not on the track
        Assert.Null(pitch.Float);
        Assert.Null(pitch.Leap);
        Assert.DoesNotContain(Game.Characters.Values, c => c.Id != "ashlord" && (c.StarPitch == "skullball" || c.StarSwing == "furnace"));
        // Nobody else's row carries a drop or a hot ball: each captain's effect is its own (AB-02).
        Assert.DoesNotContain(Game.StarSkills.Pitches.Values, p => p.Id != "skullball" && p.Drop is not null);
        Assert.DoesNotContain(Game.StarSkills.Swings.Values, s => s.Id != "furnace" && s.HotBall is not null);
    }

    // ---------------------------------------------------------------------------------
    // S-226  Anvil: the plain path until the clang at 0.7, then down to 1.5 ft under it at the plate
    // ---------------------------------------------------------------------------------

    public static IEnumerable<object[]> Deliveries =>
    [
        [PitchFamily.Fastball, 0.0, 0.0, 0.0, 0.0],
        [PitchFamily.Changeup, 0.3, -0.4, 0.2, 0.0],
        [PitchFamily.Fastball, 1.0, 0.5, -0.3, 1.0],
        [PitchFamily.Fastball, 0.0, -0.2, 0.6, -0.7],
    ];

    [Theory]
    [MemberData(nameof(Deliveries))]
    public void S226_AnvilIsThePlainPathUntilTheClangThenDropsOneAndAHalfFeetByThePlate(
        string family, double charge, double aimX, double aimY, double breakX)
    {
        var rules = Game.Rules;
        var drop = Game.StarSkills.Pitch("skullball")!.Drop!;
        Assert.Equal(0, drop.Fall(0.7));
        Assert.Equal(1.5, drop.Fall(1.0));
        var star = new PitchCommand(family, charge, true, aimX, aimY, breakX, Zone: Ref);
        var plain = star with { Star = false };
        var last = 0.0;
        for (var i = 0; i <= 300; i++)
        {
            var u = i / 300.0;
            var a = PitchFlight.Point(star, u, rules, "skullball", skills: Game.StarSkills);
            var b = PitchFlight.Point(plain, u, rules);
            // Straight down, always: the iron never moves sideways or along the line.
            Assert.Equal(b.X, a.X);
            Assert.Equal(b.Z, a.Z);
            var fall = b.Y - a.Y;
            if (u <= 0.7) Assert.Equal(0, fall);
            Assert.True(fall >= last - 1e-12, $"u {u:F3}: the drop never climbs back ({fall} after {last})");
            last = fall;
        }
        Assert.Equal(1.5, last, 12);
        // Half-way from the clang to the plate the drop is a quarter of the whole: it falls late, like iron.
        var mid = PitchFlight.Point(plain, 0.85, rules).Y - PitchFlight.Point(star, 0.85, rules, "skullball", skills: Game.StarSkills).Y;
        Assert.InRange(mid, 0.37, 0.38);
    }

    [Fact]
    public void S226_TheClangIsASimFactAtSeventyPercentAndTheDropScalesWithTheZone()
    {
        var drop = Game.StarSkills.Pitch("skullball")!.Drop!;
        Assert.False(drop.Turned(0.69));
        Assert.True(drop.Turned(0.7));
        Assert.True(drop.Turned(1.0));
        var tall = new BatterZone(1.4, 4.2);
        var star = new PitchCommand(PitchFamily.Fastball, 0, true, 0, 0, 0, Zone: tall);
        var fall = PitchFlight.Crossing(star with { Star = false }, Game.Rules).Y - PitchFlight.Crossing(star, Game.Rules, "skullball").Y;
        Assert.Equal(1.5 * tall.VerticalScale, fall, 12);
    }

    // ---------------------------------------------------------------------------------
    // S-227  The umpire, the bat and the CPU judge the dropped crossing, in the ordinary window
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S227_TheRealCrossingIsTheDroppedOneForTheUmpireTheBatAndTheCpu()
    {
        var rules = Game.Rules;
        // Low in the zone: the plain pitch is a strike, the Anvil drops out of it for a ball.
        var low = new PitchCommand(PitchFamily.Fastball, 0, true, 0, -0.7, 0, Zone: Ref);
        var lowPlain = low with { Star = false };
        Assert.Equal(PitchFlight.Crossing(lowPlain, rules).Y - 1.5, PitchFlight.Crossing(low, rules, "skullball").Y, 12);
        Assert.True(AtBatResolver.PitchInZone(lowPlain, 5, rules));
        Assert.False(AtBatResolver.PitchInZone(low, 5, rules, "skullball"));
        // High over the zone: the plain pitch is a ball, the Anvil drops into it for a strike.
        var high = new PitchCommand(PitchFamily.Fastball, 0, true, 0, 1.0, 0, Zone: Ref);
        Assert.False(AtBatResolver.PitchInZone(high with { Star = false }, 5, rules));
        Assert.True(AtBatResolver.PitchInZone(high, 5, rules, "skullball"));
        // The contact aim is where the dropped ball crosses: what the bat meets and the CPU batter reads.
        Assert.Equal(PitchFlight.ContactAim(lowPlain, rules).Y - 1.5 / (PitchFlight.PlateScaleY * Ref.VerticalScale),
            PitchFlight.ContactAim(low, rules, "skullball").Y, 12);
        // A CPU pitcher aiming an Anvil at a spot aims over it and the drop lands it there.
        var aimed = PitchFlight.AimForCrossing(low, 0, 0, rules, "skullball");
        var landed = PitchFlight.ContactAim(aimed, rules, "skullball");
        Assert.Equal(0, landed.X, 9);
        Assert.Equal(0, landed.Y, 9);
    }

    [Fact]
    public void S227_AnvilIsFastButJudgedInTheOrdinaryWindow()
    {
        var rules = Game.Rules;
        var harbor = Game.Parks[ParkId.Harbor];
        Assert.Equal(AtBatResolver.ContactWindowFrames(null, harbor, false, rules, Game.StarSkills),
            AtBatResolver.ContactWindowFrames("skullball", harbor, false, rules, Game.StarSkills));
        Assert.Equal(1.2, StarSkills.PitchSpeedMul("skullball", Game.StarSkills));
        // The whole bend is inside the two-second rule: even the slowest flight is over well before 2 s.
        Assert.True(rules.Pitching.Flight.AirMaxSec <= 2.0);
    }

    // ---------------------------------------------------------------------------------
    // S-228  Hot Iron: half a second in the glove while molten, then it drops at the glove's feet
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S228_TheHoldIsCountedOnlyWhileTheBallIsMolten()
    {
        var hot = new HotBall(2.0, 0.5);
        Assert.False(hot.Drops(1.0, 1.5));             // exactly half a second is not more than it
        Assert.True(hot.Drops(1.0, 1.5 + Frame));
        Assert.False(hot.Drops(1.5, 3.0));             // a take at 1.5 s holds only half a second of molten ball
        Assert.True(hot.Drops(1.49, 2.1));             // … a take a hair earlier holds a hair more
        Assert.False(hot.Drops(-1, 1.9));              // nobody holds it
        Assert.Equal(0.25, hot.HoldLeft(1.0, 1.25), 12);
        Assert.Equal(double.PositiveInfinity, hot.HoldLeft(1.5, 1.6));
        Assert.Equal(0.5, hot.MoltenLeft(1.5), 12);
        Assert.Equal(0, hot.MoltenLeft(2.5));
    }

    /// <summary>
    /// A seat that owns the glove and holds a molten grounder instead of throwing: it drops at the glove's feet the first
    /// frame the hold passes half a second, the ball is loose, and that glove does not take it again until it cools. The
    /// same grounder off a star grounder is never dropped.
    /// </summary>
    [Theory]
    [InlineData(88, 0, -20, "SS")]
    [InlineData(88, 0, 20, "2B")]
    public void S228_AGloveThatHoldsTheMoltenBallDropsItAtItsFeetAndWaitsForItToCool(double exit, double launch, double spray, string pos)
    {
        var hot = Run(exit, launch, spray, "furnace", human: true);
        var plain = Run(exit, launch, spray, "ground", human: true);
        Assert.Empty(plain.Drops);
        Assert.Equal(pos, hot.Take.Pos);
        Assert.Equal(plain.Take.T, hot.Take.T, 9);   // the same ball to the same glove: the heat changes the hold, not the flight
        var drop = Assert.Single(hot.Drops);
        Assert.Equal(pos, drop.Pos);
        Assert.Equal("furnace", drop.SwingId);
        Assert.InRange(drop.T - hot.Take.T, 0.5, 0.5 + Frame + 1e-9);
        Assert.True(drop.T < 2.0);
        // At its feet, and loose.
        Assert.Contains(hot.Frames, f => f.T > drop.T - 1e-9 && f.T < drop.T + 2 * Frame && f.Loose);
        // The burned glove waits for the ball to cool.
        Assert.DoesNotContain(hot.Frames, f => f.T > drop.T && f.T < 2.0 && f.Holds && f.Pos == pos);
        Assert.NotNull(hot.Play);
    }

    [Fact]
    public void S228_ATakeThatCannotHoldHalfASecondOfMoltenBallIsNeverDropped()
    {
        var late = Run(75, 0, 30, "furnace", human: true);
        Assert.True(late.Take.T >= 1.5, $"the take at {late.Take.T:F2} s is late enough");
        Assert.Empty(late.Drops);
    }

    [Fact]
    public void S228_ACaughtLinerIsStillAnOutWhenItDropsAfterTheCatch()
    {
        // A runner on first keeps the play open after the catch; the shortstop holds the molten liner and it drops.
        foreach (var human in new[] { false, true })
        {
            var run = Run(95, 4, -20, "furnace", human, runnerOnFirst: true);
            var drop = Assert.Single(run.Drops);
            Assert.True(drop.T > run.Take.T);
            Assert.Equal(PlayKind.FlyOut, run.Play!.Kind);
        }
    }

    /// <summary>The CPU glove plays a molten grounder the way a good player does: it throws inside the hold, so nothing drops.</summary>
    [Theory]
    [InlineData(88, 0, -20)]
    [InlineData(88, 0, 20)]
    [InlineData(60, 0, 40)]
    [InlineData(70, 0, 38)]
    public void S228_TheCpuGloveThrowsTheMoltenGrounderBeforeItDrops(double exit, double launch, double spray)
    {
        var run = Run(exit, launch, spray, "furnace", human: false);
        Assert.Empty(run.Drops);
        Assert.Equal(PlayKind.GroundOut, run.Play!.Kind);
        Assert.Equal(Run(exit, launch, spray, "ground", human: false).Play!.Kind, run.Play.Kind);
    }

    [Fact]
    public void S228_TheBallGlowsUntilItCoolsAndOnlyOffHotIron()
    {
        var hot = Run(88, 0, -20, "furnace", human: true);
        Assert.All(hot.Frames.Where(f => f.T < 2.0 - 1e-9), f => Assert.True(f.Molten));
        Assert.All(hot.Frames.Where(f => f.T >= 2.0), f => Assert.False(f.Molten));
        Assert.DoesNotContain(Run(88, 0, -20, "ground", human: true).Frames, f => f.Molten);
    }

    sealed record FrameRead(double T, bool Holds, bool Loose, string Pos, bool Molten);

    static (List<HotBallDropped> Drops, (double T, string Pos) Take, List<FrameRead> Frames, PlayEvent? Play) Run(
        double exit, double launch, double spray, string swing, bool human, bool runnerOnFirst = false)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "ashlord", "boom", "cinder", "grit", "rio", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray, rules: match.Rules) with { StarSwingUsed = swing };
        var preview = match.PreviewHit(hit);
        if (runnerOnFirst) Assert.True(match.StationRunner(1, match.AwayOrder[1]));
        var live = match.LivePlay;
        var source = human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null,
            human ? HumanGlove : LiveSeats.CpuOnly, 0, source)).Snapshot.Active);
        var frames = new List<FrameRead>();
        (double T, string Pos) take = (-1, "");
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, source)).CompletedPlay;
            if (play is not null) break;
            if (take.T < 0 && live.HoldsBall) take = (live.ElapsedSeconds, live.GlovePos);
            frames.Add(new FrameRead(live.ElapsedSeconds, live.HoldsBall, live.LooseBall, live.GlovePos, live.Molten));
        }
        return (live.FactsThisPlay.OfType<HotBallDropped>().ToList(), take, frames, play);
    }

    // ---------------------------------------------------------------------------------
    // S-229  The CPU's table and the rows
    // ---------------------------------------------------------------------------------

    sealed class View : ICpuFieldView
    {
        public RulesTable Rules { get; init; } = Game.Rules;
        public double Elapsed => 1;
        public double GloveX { get; init; } = 40;
        public double GloveZ { get; init; } = 70;
        public double Dash01 => 0;
        public InPlay.ForceState Forces => InPlay.ForceState.Empty;
        public FlyState Fly => FlyState.None;
        public IReadOnlyList<Runner> Runners => [];
        public int Outs => 0;
        public int HomeScore => 0;
        public int AwayScore => 0;
        public BattedBall? Ball => null;
        public AtBatResult? Hit => null;
        public Runner? RunnerAt(int fromBag) => null;
        public Runner? RunnerForBag(int bag) => null;
        public double ThrowArrivalSec(int bag) => Ready;
        public double WalkSec(int bag) => Walk;
        public double ThrowReadySec(int bag) => Ready;
        public double Walk { get; init; } = 0.8;
        public double Ready { get; init; } = 1.2;
        public double HoldLeftSec { get; init; } = double.PositiveInfinity;
    }

    [Fact]
    public void S229_TheCpuThrowsAHotBallItCouldNotCarryToTheBagInTime()
    {
        // An ordinary ball: the legs are quicker, so the glove carries it.
        Assert.Equal(new CpuFieldDecision(CpuFieldAction.WalkTo, 1), CpuFieldDecider.PlayAt(new View(), 1));
        Assert.Equal(0.8, CpuFieldDecider.PlayArrivalSec(new View(), 1), 12);
        // A hot ball it could carry there inside its hold: the same walk.
        Assert.Equal(new CpuFieldDecision(CpuFieldAction.WalkTo, 1), CpuFieldDecider.PlayAt(new View { HoldLeftSec = 0.9 }, 1));
        // One that would drop on the way: the throw, and the table times the play by the throw.
        Assert.Equal(new CpuFieldDecision(CpuFieldAction.ThrowTo, 1), CpuFieldDecider.PlayAt(new View { HoldLeftSec = 0.4 }, 1));
        Assert.Equal(1.2, CpuFieldDecider.PlayArrivalSec(new View { HoldLeftSec = 0.4 }, 1), 12);
        // Nobody to throw to: the glove walks anyway and takes the drop, knowingly.
        var alone = new View { HoldLeftSec = 0.4, Ready = double.PositiveInfinity };
        Assert.Equal(new CpuFieldDecision(CpuFieldAction.WalkTo, 1), CpuFieldDecider.PlayAt(alone, 1));
    }

    [Fact]
    public void S229_TheDropIsAPitchsAndTheHotBallIsASwingsAndBothAreBounded()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["skullball"]!["drop"] = new JsonObject { ["dropFt"] = 2.5, ["from"] = 0.7 };
            json["pitches"]!["charmball"]!["drop"] = new JsonObject { ["dropFt"] = 1.0, ["from"] = 1.0 };
            json["pitches"]!["fogball"]!["hotBall"] = new JsonObject { ["moltenSec"] = 2.0, ["holdSec"] = 0.5 };
            json["swings"]!["furnace"]!["hotBall"] = new JsonObject { ["moltenSec"] = 3.0, ["holdSec"] = 0.5 };
            json["swings"]!["heart-swing"]!["hotBall"] = new JsonObject { ["moltenSec"] = 1.0, ["holdSec"] = 1.0 };
            json["swings"]!["shell-swing"]!["drop"] = new JsonObject { ["dropFt"] = 1.0, ["from"] = 0.5 };
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'skullball' drop needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'charmball' drop needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'fogball' cannot carry a hotBall", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'furnace' hotBall needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'heart-swing' hotBall needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'shell-swing' cannot carry a drop", StringComparison.Ordinal));
        // The shipped rows are clean.
        Assert.DoesNotContain(ContentDataValidator.Validate(Game.Root.Shipped), e => e.Contains("star-skills", StringComparison.Ordinal));
    }
}
