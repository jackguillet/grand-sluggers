using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The ball's hazards, live (§14; F4-c, FR-07, FD-08-R1, FD-09-R2; <c>SF-21</c>, <c>SF-27</c>). A ball that comes into a
/// redirect's mouth leaves out of another instance of its type, drawn from the match's seeded stream, with the row's share
/// of its speed on the same heading and the row's lift; a ball under a reward target's top pays once a play. The preview
/// plans the path as hit and nothing is foreseen. The fixtures add instances to Harbor as a <see cref="Park"/> record, where a
/// known ball will certainly meet them.
/// </summary>
public sealed class BallRedirectTests
{
    static readonly ContentCatalog Catalog = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    static Park Harbor => Catalog.MustPark("harbor-diamond");

    /// <summary>A grounder up the middle-left (−10°): where its path is on the ground at <paramref name="t"/> seconds.</summary>
    static (AtBatResult Hit, double X, double Z) GrounderAt(double t)
    {
        var hit = FlightFixtures.Hit(Harbor, 90, -3, -10);
        var path = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, -10, Harbor, Catalog.Rules);
        var (x, _, z) = BallFlight.PointAt(path, t, Catalog.Rules);
        return (hit, x, z);
    }

    static Park With(params Hazard[] extra) => Harbor with { Hazards = [.. Harbor.Hazards, .. extra] };

    sealed record Played(LivePlaySystem Live, FieldingPreview Preview, PlayEvent? Done, PlayTrace Trace, List<(double T, double X, double Y, double Z)> Ball);

    static Played Play(Match match, AtBatResult hit)
    {
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        live.Recording = true;
        var field = match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field,
            LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var ball = new List<(double, double, double, double)>();
        PlayEvent? done = null;
        for (var i = 0; i < 60 * 20 && done is null; i++)
        {
            done = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            ball.Add((live.ElapsedSeconds, live.BallX, live.BallY, live.BallZ));
        }
        return new Played(live, preview, done, live.TakeTrace(done), ball);
    }

    /// <summary>Where a fly is on its way down at <paramref name="heightFt"/>: the first sample past the apex at or below that height.</summary>
    static (double X, double Z) Descending(AtBatResult hit, double heightFt)
    {
        var path = BattedBall.Of(hit, Harbor, Catalog.Rules).Samples;
        var apex = Enumerable.Range(0, path.Count).MaxBy(i => path[i].Height);
        var s = path.Skip(apex).First(p => p.Height <= heightFt);
        return (s.X, s.Z);
    }

    static Match New(Park park, int seed = 1, bool night = false) =>
        new(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: seed, night: night);

    /// <summary>
    /// <c>SF-21</c>: a grounder rolled into a warp can at 0.5 s comes out of the other can, on the ground, on the same heading
    /// at 0.8 of its speed and 12 ft/s up; the entry is one typed fact and one trace mark naming the mouth and the exit; the
    /// ball was never at the ground between the two (it left play at the entry). The preview did not foresee it: its landing is
    /// the path as hit.
    /// </summary>
    [Fact]
    public void SF21_AGrounderIntoACanComesOutOfTheOtherWithTheRowsSpeedAndLift()
    {
        var (hit, ex, ez) = GrounderAt(0.5);
        var park = With(new Hazard(HazardType.WarpPipe, ex, ez, 2.8, "A"), new Hazard(HazardType.WarpPipe, 60, 150, 2.8, "B"));
        var plain = New(Harbor).PreviewHit(hit);
        var p = Play(New(park), hit);
        Assert.Equal((plain.LandingX, plain.LandingZ), (p.Preview.LandingX, p.Preview.LandingZ));
        Assert.False(p.Preview.Warped, "a park never warps the preview");

        var fact = Assert.Single(p.Live.RedirectsThisPlay);
        Assert.Equal((HazardType.WarpPipe, park.Hazards.Count - 2, park.Hazards.Count - 1), (fact.Type, fact.Hazard, fact.Exit));
        Assert.Equal((60.0, 150.0), (fact.ExitX, fact.ExitZ));
        var mark = Assert.Single(p.Trace.Marks!, m => m.Kind == PlayTraceMarkKind.BallRedirected);
        Assert.Equal((fact.Hazard, fact.Exit), (mark.Hazard!.Index, mark.Hazard.Exit));

        // The ball jumps from the mouth to the exit in one frame, and leaves it going up and on its old heading.
        var at = p.Ball.FindIndex(b => Math.Abs(b.T - fact.T) < 1e-9);
        Assert.True(at > 0 && at + 3 < p.Ball.Count);
        Assert.Equal((60.0, 150.0), (p.Ball[at].X, p.Ball[at].Z));
        var after = p.Ball[at + 2];
        Assert.True(after.Y > 0, "the exit lifts the ball");
        var heading = Math.Atan2(-Math.Sin(10 * Math.PI / 180), Math.Cos(10 * Math.PI / 180));
        var outHeading = Math.Atan2(after.X - 60, after.Z - 150);
        Assert.InRange(outHeading - heading, -0.05, 0.05);
    }

    /// <summary>
    /// <c>SF-21</c>, the draw: with three cans the exit is one of the other two, drawn from the match's stream, so the same
    /// seed always sends it to the same can and some other seed sends it to the other; a mouth is never its own exit.
    /// </summary>
    [Fact]
    public void SF21_TheExitIsASeededDrawAndTheSameSeedIsTheSameExit()
    {
        var (hit, ex, ez) = GrounderAt(0.5);
        var park = With(new Hazard(HazardType.WarpPipe, ex, ez, 2.8, "A"), new Hazard(HazardType.WarpPipe, 60, 150, 2.8, "B"),
            new Hazard(HazardType.WarpPipe, -70, 140, 2.8, "C"));
        var exits = new HashSet<int>();
        for (var seed = 1; seed <= 12; seed++)
        {
            var a = Assert.Single(Play(New(park, seed), hit).Live.RedirectsThisPlay);
            var b = Assert.Single(Play(New(park, seed), hit).Live.RedirectsThisPlay);
            Assert.Equal(a, b);
            Assert.NotEqual(a.Hazard, a.Exit);
            exits.Add(a.Exit);
        }
        Assert.Equal(2, exits.Count);
    }

    /// <summary>A type with one instance at the park redirects nothing: a mouth cannot be its own exit.</summary>
    [Fact]
    public void AMouthWithNoTwinRedirectsNothing()
    {
        var (hit, ex, ez) = GrounderAt(0.5);
        var p = Play(New(With(new Hazard(HazardType.WarpPipe, ex, ez, 2.8, "A"))), hit);
        Assert.Empty(p.Live.RedirectsThisPlay);
    }

    /// <summary>
    /// FD-09-R2: a fly coming down into a chomper at night is spat out of another one, and nobody is out by a mouth — the play
    /// is decided by the gloves and the ball, not by the mouth.
    /// </summary>
    [Fact]
    public void AFlyIntoAChomperIsSpatOutOfAnotherAndIsNobodysOut()
    {
        var hit = FlightFixtures.Landing(Harbor, 150, 30, 0);
        var (mx, mz) = Descending(hit, 10);
        var park = With(new Hazard(HazardType.Chomper, mx, mz, 6, "C"), new Hazard(HazardType.Chomper, -70, 150, 6, "L"));
        var p = Play(New(park, night: true), hit);
        var fact = Assert.Single(p.Live.RedirectsThisPlay, r => r.Type == HazardType.Chomper);
        Assert.Equal(park.Hazards.Count - 1, fact.Exit);
        Assert.NotNull(p.Done);
    }

    /// <summary>
    /// The reward target, live: a fly whose path passes under a sign's top inside its disc hits it once, as a typed fact and a
    /// mark; the same fly with the sign over its head does not.
    /// </summary>
    [Fact]
    public void AFlyUnderASignHitsItOnceAndAFlyOverItDoesNot()
    {
        var hit = FlightFixtures.Landing(Harbor, 190, 32, 5);
        var land = BattedBall.Of(hit, Harbor, Catalog.Rules);
        var (sx, sz) = Descending(hit, 20);
        var sign = new Hazard(HazardType.Billboard, sx, sz, 6, "star");
        var p = Play(New(With(sign)), hit);
        var reward = p.Live.RewardThisPlay;
        Assert.NotNull(reward);
        Assert.Equal(Harbor.Hazards.Count, reward!.Hazard);
        Assert.Single(p.Trace.Marks!, m => m.Kind == PlayTraceMarkKind.RewardHit);

        // At its apex the fly is far above any sign's top: a sign there is not hit.
        var path = land.Samples;
        var mid = path.MaxBy(x => x.Height)!;
        Assert.True(mid.Height > Catalog.Rules.Hazards.Of(HazardType.Billboard).TopFt);
        var high = Play(New(With(new Hazard(HazardType.Billboard, mid.X, mid.Z, 4, "star"))), hit);
        Assert.Null(high.Live.RewardThisPlay);
    }

    /// <summary>With hazards off the same grounder rolls on through where the cans were: the switch removes the instances.</summary>
    [Fact]
    public void HazardsOffTheSameGrounderRollsOn()
    {
        var (hit, ex, ez) = GrounderAt(0.5);
        var park = With(new Hazard(HazardType.WarpPipe, ex, ez, 2.8, "A"), new Hazard(HazardType.WarpPipe, 60, 150, 2.8, "B"));
        var off = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: 1, hazards: false);
        Assert.Empty(Play(off, hit).Live.RedirectsThisPlay);
    }
}
