using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// #715, found beside the hand-off coast: the coast (§8.9) keeps "the glove's velocity", measured as the last frame's
/// displacement over the frame. Under the <c>c80</c> copy's deliberate dive (#719) that last frame can be the lunge — about 10 ft
/// in one frame, 590 ft/s as a velocity — and a dive that missed with the ring handed to an outfielder on the next frame slid the
/// diver about 118 ft in the coast's 0.2 s, and further under the response law's brake (#718). A diver paying its recovery is on
/// the ground: it does not coast. Root-aware: the hybrid's ball in a plain process, the copy's own three under the trial root.
/// The shipped table had the same slide with a human seat — East lunges 10 ft for free, and Select or a released stick on the next
/// frame hands the ring off with the lunge as its velocity — so the guard also reads the dive's arm window (<c>DiveT</c>) there.
/// </summary>
[Trait("Rows", "compact")]
public sealed class DiveHandoffCoastTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);
    const double Frame = 1.0 / 60.0;
    static readonly string[] Defense = ["vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex"];

    /// <summary>Exit, launch, spray, the infielder who dives, the outfielder the ring goes to.</summary>
    public static TheoryData<double, double, double, string, string> MissedDives => TestRoot.Pick(
        new TheoryData<double, double, double, string, string> { { 80, 14, -18, "SS", "LF" } },
        // The 80-ft diamond needs a softer, higher ball: the 80-mph rope is the left fielder's from the start there.
        new TheoryData<double, double, double, string, string> { { 70, 20, -8, "SS", "LF" }, { 70, 20, -32, "SS", "LF" }, { 70, 20, 0, "2B", "CF" } });

    [Theory]
    [MemberData(nameof(MissedDives))]
    public void ADiverWhoseRingLeavesOnTheNextFrameStaysWhereHeLunged(double exit, double launch, double spray, string diver, string outfielder)
    {
        var home = Trial.Team("Defense", Defense[0], Defense[1..]);
        var away = Trial.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(Trial, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);

        (double X, double Z)? lunged = null;
        var handedAt = -1; var commitAt = -1; var owed = 0.0; var farthest = 0.0; var fastest = 0.0;
        var last = live.Fielders[diver];
        for (var i = 0; i < 60 * 12; i++)
        {
            // The completing frame resets the live field: nothing is read off it.
            if (live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay is not null) break;
            var at = live.Fielders[diver];
            if (lunged is null && live.Events.Contains(LiveEvent.DiveCommit))
            {
                Assert.Equal(diver, live.GlovePos);
                Assert.False(live.HoldsBall, "the dive missed");
                lunged = at;
                commitAt = i;
                owed = live.DiveRecoveryT;
            }
            else if (lunged is { } spot && live.DiveRecoveryT > 0 && live.DivingPos == diver)
            {
                if (handedAt < 0 && live.GlovePos != diver)
                {
                    Assert.Equal(outfielder, live.GlovePos);
                    handedAt = i;
                }
                farthest = Math.Max(farthest, Diamond.Dist(spot.X, spot.Z, at.X, at.Z));
                fastest = Math.Max(fastest, Diamond.Dist(last.X, last.Z, at.X, at.Z) / Frame);
            }
            last = at;
        }

        Assert.True(commitAt > 0, "the infielder never committed to a dive");
        Assert.True(owed > 0.4, "the dive's recovery is owed");
        Assert.Equal(commitAt + 1, handedAt);   // the defect's frame: the lunge was the glove's whole "velocity" when the ring left
        // The brake's drift after a lunge is under a foot; the coast was 9.8 ft a frame for twelve frames.
        Assert.True(farthest < 1.5, $"the diver slid {farthest:0.0} ft from where he lunged inside his recovery");
        var rated = Defense.Max(id => FieldingResolver.ChaseSpeedFt(Trial.Must(id), false, match.Rules));
        Assert.True(fastest <= rated, $"the diver moved at {fastest:0} ft/s on the ground; the fastest legs on this defense are rated {rated:0}");
    }

    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    /// <summary>
    /// The human's East on this process's own table (the shipped one in a plain process, where the dive is free and nothing is
    /// owed after it), then Select on the very next frame: the ring leaves the diver, and the diver does not leave with the lunge.
    /// </summary>
    [Fact]
    public void EastThenSelectOnTheNextFrameLeavesTheDiverWhereHeLunged()
    {
        // The rope is the left fielder's by then on the shipped diamond; the copy's softer ball is still the shortstop's.
        var (exit, launch, spray, diver) = TestRoot.Pick((80.0, 14.0, -18.0, "LF"), (70.0, 20.0, -8.0, "SS"));
        const int eastAt = 60;
        var run = RunHuman(exit, launch, spray, i => i == eastAt ? new LivePadInput(EastDown: true) : i == eastAt + 1 ? new LivePadInput(Swap: true) : LivePadInput.Dead);
        Assert.Equal((eastAt, diver), (run.LungeAt, run.Diver));
        Assert.Equal(eastAt + 1, run.HandedAt);
        Assert.True(run.MaxStepFt < 1.0, $"the diver moved {run.MaxStepFt:0.00} ft in a frame after his lunge; the slide was 10 ft a frame");
    }

    /// <summary>
    /// The shipped table: the stick runs the shortstop, East, and the stick is let go. The assistance hands the ring to the left
    /// fielder on the next frame, and the shortstop used to slide 124 ft. (The copy's diver owes his recovery: the rows above.)
    /// </summary>
    [Fact]
    public void EastThenAReleasedStickLeavesTheShippedDiverWhereHeLunged()
    {
        if (TestRoot.Compact) return;   // this process plays the copy: its stick is armed at neutral and its hand-off is the CPU rows' above
        const int eastAt = 30;
        var run = RunHuman(80, 14, -18, i => i < eastAt ? new LivePadInput(StickX: -0.3, StickY: 1.0)
            : i == eastAt ? new LivePadInput(StickX: -0.3, StickY: 1.0, EastDown: true) : LivePadInput.Dead);
        Assert.Equal((eastAt, "SS"), (run.LungeAt, run.Diver));
        Assert.Equal((eastAt + 1, "LF"), (run.HandedAt, run.HandedTo));
        Assert.True(run.MaxStepFt < 1.0, $"the diver moved {run.MaxStepFt:0.00} ft in a frame after his lunge; the slide was 9.5 ft a frame");
    }

    sealed record HumanRun(int LungeAt, string Diver, int HandedAt, string HandedTo, double MaxStepFt);

    /// <summary>A human glove on this process's table: the frame and body of the lunge, the frame the ring left it, and the diver's largest step in a frame over the half second after.</summary>
    static HumanRun RunHuman(double exit, double launch, double spray, Func<int, LivePadInput> pad)
    {
        var content = Control;
        var home = content.Team("Defense", Defense[0], Defense[1..]);
        var away = content.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray, rules: match.Rules);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, match.PreviewHit(hit), null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);

        var lungeAt = -1; var diver = ""; var handedAt = -1; var handedTo = ""; var maxStep = 0.0;
        (double X, double Z) last = default;
        for (var i = 0; i < 60 * 6 && (lungeAt < 0 || i <= lungeAt + 30); i++)
        {
            var ring = live.GlovePos;
            var was = live.Fielders[ring];
            if (live.Apply(LivePlayCommand.Tick(Frame, pad(i), LivePadInput.Dead, false, LivePlayCommandSource.Human)).CompletedPlay is not null) break;
            if (lungeAt < 0)
            {
                // The lunge: the ring's body thrown most of fielding.dash.diveLungeFt in the one frame, and the dive armed.
                var now = live.Fielders[ring];
                if (live.DiveT > 0 && live.GlovePos == ring && Diamond.Dist(was.X, was.Z, now.X, now.Z) > 5)
                    (lungeAt, diver, last) = (i, ring, now);
                continue;
            }
            var at = live.Fielders[diver];
            if (handedAt < 0 && live.GlovePos != diver) (handedAt, handedTo) = (i, live.GlovePos);
            maxStep = Math.Max(maxStep, Diamond.Dist(last.X, last.Z, at.X, at.Z));
            last = at;
        }
        return new HumanRun(lungeAt, diver, handedAt, handedTo, maxStep);
    }
}
