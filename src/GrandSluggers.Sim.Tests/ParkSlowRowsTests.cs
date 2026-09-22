using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The ordinary movement/status interaction (#715, the 3e deferred-work boundary): the park's slow. A ball that lands in a
/// freeze volume, a lava pit or the statue's breath slows the glove that plays it to <c>fielding.chase.frozenMul</c> of its
/// speed for the play (§8.1). It is the one status the ordinary loop carries, it is the same rule on the shipped table and on
/// the <c>c80</c> copy, and these rows hold it against the copy's new movement: the slow is a restriction on how fast a body
/// moves and on nothing else (F693-02-mixed-status-action-readiness, its ordinary form), so the response law ramps to the
/// slowed speed, both seats run it, and what the take costs the hands is the table's own function of the ball with no term for
/// the slow. The statuses that come from special attacks are excluded from 3e with the specials and have no rows here.
/// </summary>
[Trait("Rows", "compact")]
public sealed class ParkSlowRowsTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    [Fact]
    public void TheParkSlowIsOneMultiplierOnBothTables()
    {
        var chase = Rules.Default.Fielding.Chase;
        Assert.Equal(0.45, chase.FrozenMul);
        var rio = _content.Must("rio");
        Assert.Equal(FieldingResolver.ChaseSpeedFt(rio, false) * chase.FrozenMul, FieldingResolver.ChaseSpeedFt(rio, true), 9);
        // Burrow is the one glove the park cannot slow (§8.1), on both tables.
        Assert.True(FieldAbilities.IgnoresParkSlow(_content.Must("soot")));
        Assert.False(FieldAbilities.IgnoresParkSlow(rio));
    }

    /// <summary>
    /// The fly into the Rink's deep freeze volume, by root: (10, 187) shipped, (7, 131) on the copy (#732). It was (10, 180) /
    /// (7, 126) until FD-19-R1 moved the volume outward along its own bearing off the copy's second-base pad (F4-e, #862).
    /// </summary>
    static (double Carry, double Spray) RinkFly => TestRoot.Pick((187.3, 3.06), (131.2, 3.06));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AGloveSlowedByTheParkRunsAtFrozenMulOfItsSpeed(bool human)
    {
        var (carry, spray) = RinkFly;
        var slowed = TopChaseSpeed("crystal-rink", carry, 30, spray, human, out var slowPre, out var slowPos, out var slowWho, out var rules);
        Assert.True(slowPre.Frozen, "the fixture: the ball lands in the freeze volume");
        var free = TopChaseSpeed("harbor-diamond", carry, 30, spray, human, out var freePre, out var freePos, out var freeWho, out _);
        Assert.False(freePre.Frozen, "the fixture: Harbor has no hazards");
        Assert.Equal((slowPos, slowWho.Id), (freePos, freeWho.Id));

        // The body reaches exactly the speed the table asks of it, slowed or not: the response law ramps to the asked speed.
        Assert.Equal(FieldingResolver.ChaseSpeedFt(slowWho, slowPos, slowPre, rules), slowed, 6);
        Assert.Equal(FieldingResolver.ChaseSpeedFt(freeWho, freePos, freePre, rules), free, 6);
        Assert.Equal(rules.Fielding.Chase.FrozenMul, slowed / free, 6);
    }

    /// <summary>
    /// The grounder into Ember's first-base-side lava pit, by root: (49, 100) shipped, (44, 89) on the copy (#732). It was
    /// (38, 78) / (34, 69), on the first-second lane, until FD-19-R1 moved the pit outward along its own bearing (F4-e, #862).
    /// </summary>
    static (double Carry, double Spray) EmberGrounder => TestRoot.Pick((111.4, 26.10), (99.3, 26.31));

    [Fact]
    public void TheSlowIsMovementOnlyWhatTheTakeCostsHasNoTermForIt()
    {
        var (carry, spray) = EmberGrounder;
        var match = Match.Slice(_content, seed: 1, parkId: "ember-keep");
        var hit = FlightFixtures.Landing(match.Park, carry, 4, spray);
        var preview = match.PreviewHit(hit);
        Assert.True(preview.Grounder);
        Assert.True(preview.Frozen, "the fixture: the grounder lands in the lava pit");
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var rules = match.Rules;
        var live = match.LivePlay;
        var field = match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly)).Snapshot.Active);
        var took = "";
        var owed = double.NaN;
        var incoming = double.NaN;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 20 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame)).CompletedPlay;
            if (took != "" || !live.Events.Contains(LiveEvent.Glove)) continue;
            took = live.GlovePos;
            owed = live.RecoilT;
            incoming = live.IncomingFtPerSec;
        }
        Assert.NotNull(play);
        Assert.NotEqual("", took);
        // What the take costs is the table's own function of the ball and the hands, and the slow is not in it: the copy reads
        // the ball's incoming speed (#720), the shipped table the hit's energy.
        var expected = rules.Fielding.Recoil.Active
            ? FieldingResolver.RecoilSec(map[took], incoming, rules)
            : InPlay.KnockbackSec(InPlay.Energy(hit, rules), map[took], rules);
        Assert.Equal(expected, owed, 6);
    }

    /// <summary>
    /// The fastest the play glove ran (ft/s, frame to frame) in the first second and a half of this ball: the chase at speed, before
    /// any dive or catch. <paramref name="pos"/> is the body that ran it (the CPU's chaser is its route's, not always the preview's).
    /// </summary>
    double TopChaseSpeed(string parkId, double carry, double launch, double spray, bool human,
        out FieldingPreview preview, out string pos, out Character who, out RulesTable rules)
    {
        var match = Match.Slice(_content, seed: 1, parkId: parkId);
        var hit = FlightFixtures.Landing(match.Park, carry, launch, spray);
        preview = match.PreviewHit(hit);
        rules = match.Rules;
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
        var live = match.LivePlay;
        var seats = human ? HumanGlove : LiveSeats.CpuOnly;
        var source = human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;
        var field = human ? null : match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, seats, 0, source)).Snapshot.Active);
        var plant = FlyCatch.ChaseTarget(preview, match.Park, rules);
        // The copy's pursuit stick (#718) takes the glove only after it has been seen at neutral: six dead frames first.
        var neutral = TestRoot.Pick(0, 6);
        var top = 0.0;
        pos = "";
        var last = (live.GlovePos, live.GloveX, live.GloveZ);
        for (var i = 0; i < 90 && live.Active; i++)
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
            var step = live.GlovePos == last.GlovePos ? Diamond.Dist(last.GloveX, last.GloveZ, live.GloveX, live.GloveZ) / Frame : 0;
            if (step > top)
            {
                top = step;
                pos = live.GlovePos;
            }
            last = (live.GlovePos, live.GloveX, live.GloveZ);
        }
        Assert.NotEqual("", pos);
        who = map[pos];
        return top;
    }
}
