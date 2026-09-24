using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The ordinary movement/status interaction (#715, the 3e deferred-work boundary): the park's slow, re-authored to FD-08-R1
/// and FD-08-R2 by F4-b (#896). A body that touches a freeze volume, a lava pit or the statue's breath runs at
/// <c>fielding.chase.frozenMul</c> of its speed for the row's <c>slowSec</c> (3 s, Jack's number, not yet played) from the
/// touch — the body that touched it, not every chaser, and not for a play decided at the landing mark. It is the one status
/// the ordinary loop carries, it is the same rule on the shipped table and on the <c>c80</c> copy, and these rows hold it
/// against the copy's new movement: the slow is a restriction on how fast a body moves and on nothing else
/// (F693-02-mixed-status-action-readiness, its ordinary form), so the response law ramps to the slowed speed, both seats run
/// it, and what the take costs the hands is the table's own function of the ball with no term for the slow. The statuses
/// that come from special attacks are excluded from 3e with the specials and have no rows here; the heart swing's
/// play-wide slow and its <c>drops.frozen</c> roll are left exactly as they were.
/// </summary>
public sealed class ParkSlowRowsTests
{
    readonly ContentCatalog _content = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    public void TheParkSlowIsOneMultiplierOnBothTables()
    {
        var chase = Rules.Default.Fielding.Chase;
        Assert.Equal(0.45, chase.FrozenMul);
        var rio = _content.Must("rio");
        Assert.Equal(FieldingResolver.ChaseSpeedFt(rio, false, rules: Rules.Default) * chase.FrozenMul, FieldingResolver.ChaseSpeedFt(rio, true, rules: Rules.Default), 9);
        Assert.Equal(chase.FrozenMul, BodySlows.Mul(true, rules: Rules.Default));
        Assert.Equal(1.0, BodySlows.Mul(false, rules: Rules.Default));
        // Burrow is the one body the park cannot slow (§8.1), on both tables.
        Assert.True(FieldAbilities.IgnoresParkSlow(_content.Must("soot")));
        Assert.False(FieldAbilities.IgnoresParkSlow(rio));
    }

    /// <summary>
    /// A high fly (50°) into the Rink's deep freeze volume, by root: (10, 187) shipped, (7, 131) on the copy (#732). It was
    /// (10, 180) / (7, 126) until FD-19-R1 moved the volume outward along its own bearing off the copy's second-base pad
    /// (F4-e, #862). The hang is long enough that the glove that goes out for it runs into the disc before it comes down.
    /// </summary>
    static (double Carry, double Spray) RinkFly => (141.2, 3.06);

    /// <summary>
    /// FD-08-R1 / FD-08-R2, both seats: the glove that runs the fly into the Rink's deep volume runs at the table's chase speed
    /// until it touches the disc, and at <c>frozenMul</c> of it once it has (after the response law's brake on the copy). The
    /// ball landing in the disc slows nobody: the preview is not frozen, and the same ball at Harbor never slows anyone.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AGloveThatTouchesTheRinksVolumeRunsAtFrozenMulOfItsSpeed(bool human)
    {
        var (carry, spray) = RinkFly;
        var rink = Chase("crystal-rink", carry, 50, spray, human);
        Assert.False(rink.Preview.Frozen, "the landing mark slows nobody (FD-08-R1)");
        Assert.True(rink.Touch is not null, "the fixture: the glove runs into the deep freeze volume");
        var rules = rink.Rules;
        var asked = FieldingResolver.ChaseSpeedFt(rink.Who!, rink.Pos, rink.Preview, rules);
        // The body reaches exactly the speed the table asks of it, free and then slowed: the response law ramps to each.
        Assert.Equal(asked, rink.TopFree, 6);
        Assert.Equal(asked * rules.Fielding.Chase.FrozenMul, rink.TopSlowed, 6);

        var harbor = Chase("harbor-diamond", carry, 50, spray, human);
        Assert.False(harbor.Preview.Frozen);
        Assert.Null(harbor.Touch);
        Assert.Equal(0, harbor.SlowedFrames);
    }

    /// <summary>
    /// FD-08-R2: what the take costs the hands has no term for the slow. The short stop stands in a lava pit from the crack
    /// (a fixture at Harbor: FD-19 keeps every catalog volume off his spot), so he is slowed when he takes the grounder hit at
    /// him; the recoil he owes is the table's own function of the ball and his hands.
    /// </summary>
    [Fact]
    public void TheSlowIsMovementOnlyWhatTheTakeCostsHasNoTermForIt()
    {
        var ss = Diamond.Positions["SS"];
        var harbor = _content.MustPark("harbor-diamond");
        var park = harbor with { Hazards = [.. harbor.Hazards, new Hazard(HazardType.LavaPit, ss.X, ss.Z, 12, null)] };
        var match = new Match(_content, PresetTeams.EmberCourt(_content), PresetTeams.SparkAllStars(_content), park, seed: 1);
        var hit = FlightFixtures.Hit(park, 100, -12, Math.Atan2(ss.X, ss.Z) * 180 / Math.PI);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder);
        Assert.False(preview.Frozen);
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var rules = match.Rules;
        var live = match.LivePlay;
        var field = match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly)).Snapshot.Active);
        var took = "";
        var slowedAtTake = false;
        var owed = double.NaN;
        var incoming = double.NaN;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 20 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame)).CompletedPlay;
            if (took != "" || !live.Events.Contains(LiveEvent.Glove)) continue;
            took = live.GlovePos;
            slowedAtTake = live.IsSlowed(took);
            owed = live.RecoilT;
            incoming = live.IncomingFtPerSec;
        }
        Assert.NotNull(play);
        Assert.Equal("SS", took);
        Assert.True(slowedAtTake, "the fixture: the glove that takes the ball stands in the pit");
        // What the take costs is the table's own function of the ball's incoming speed and the hands (#720), and the slow is not in it.
        var expected = FieldingResolver.RecoilSec(map[took], incoming, rules);
        Assert.Equal(expected, owed, 6);
    }

    sealed record Run(FieldingPreview Preview, RulesTable Rules, string Pos, Character? Who, BodySlowed? Touch,
        double TopFree, double TopSlowed, int SlowedFrames);

    /// <summary>
    /// The play glove's chase on this ball until it holds it (or 10 s). The body measured is the first glove a status volume
    /// slows (else the preview's): its fastest frame-to-frame speed while it wore the ring before the touch, its fastest once
    /// slowed and past the response law's brake (the copy decelerates into the slow), and how many frames any glove ran
    /// slowed. The CPU's chaser is its route's, not always the preview's, so the body is read frame by frame.
    /// </summary>
    Run Chase(string parkId, double carry, double launch, double spray, bool human)
    {
        var match = Match.Slice(_content, seed: 1, parkId: parkId);
        var hit = FlightFixtures.Landing(match.Park, carry, launch, spray);
        var preview = match.PreviewHit(hit);
        var rules = match.Rules;
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var live = match.LivePlay;
        var seats = human ? HumanGlove : LiveSeats.CpuOnly;
        var source = human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;
        var field = human ? null : match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, seats, 0, source)).Snapshot.Active);
        var plant = FlyCatch.ChaseTarget(preview, rules, match.Park);
        // The copy's pursuit stick (#718) takes the glove only after it has been seen at neutral: six dead frames first.
        var neutral = 6;
        var frames = new List<(string Glove, double Step, bool Slowed)>();
        BodySlowed? touch = null;
        var touchAt = -1;
        var last = (live.GlovePos, live.GloveX, live.GloveZ);
        for (var i = 0; i < 600 && live.Active; i++)
        {
            var pad = LivePadInput.Dead;
            if (human && i >= neutral)
            {
                var dx = plant.X - live.GloveX;
                var dz = plant.Z - live.GloveZ;
                var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                pad = new LivePadInput(StickX: dx / len, StickY: dz / len);
            }
            live.Apply(LivePlayCommand.Tick(Frame, pad, LivePadInput.Dead, false, source));
            if (!live.Active || live.HoldsBall) break;
            var glove = live.GlovePos;
            if (touch is null && live.Slows.FirstOrDefault(t => t.Pos == glove) is { } mine)
            {
                touch = mine;
                touchAt = frames.Count;
            }
            var step = glove == last.GlovePos ? Diamond.Dist(last.GloveX, last.GloveZ, live.GloveX, live.GloveZ) / Frame : 0;
            frames.Add((glove, step, live.IsSlowed(glove)));
            last = (glove, live.GloveX, live.GloveZ);
        }
        var pos = touch?.Pos ?? preview.Position;
        var brake = (int)Math.Ceiling(rules.Fielding.Chase.BrakeSec / Frame) + 1;
        var before = touchAt < 0 ? frames : frames.Take(touchAt).ToList();
        var topFree = before.Where(f => f.Glove == pos).Select(f => f.Step).DefaultIfEmpty(0).Max();
        var topSlowed = touchAt < 0 ? 0 : frames.Skip(touchAt + brake).Where(f => f.Glove == pos && f.Slowed).Select(f => f.Step).DefaultIfEmpty(0).Max();
        Assert.True(topFree > 0, "the glove ran");
        return new Run(preview, rules, pos, map[pos], touch, topFree, topSlowed, frames.Count(f => f.Slowed));
    }
}
