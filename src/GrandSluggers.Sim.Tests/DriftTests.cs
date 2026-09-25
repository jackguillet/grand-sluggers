using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The drift pattern (§14; the dust devils at Sunscorch Mesa, #1154, WD-09 C). A disc wanders the outfield on a path the play's
/// seeded phases fix; a ball in flight through it is pushed the row's feet square to its heading, away from the disc's middle. A
/// grounder and every body are untouched; the clear night air has none; hazards off removes them.
/// </summary>
public sealed class DriftTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static Park Harbor => Catalog.MustPark(ParkId.Harbor);
    static HazardTypeRules Devil => Catalog.Rules.Hazards.Of(HazardType.DustDevil);

    [Fact]
    public void TheDevilRowIsADriftAndTheMesaHasTwoByDayAndNoneAtNight()
    {
        Assert.Equal(HazardPattern.Drift, Devil.Pattern);
        Assert.Equal((3.0, 60.0, 14.0, 30.0, 10.0, 0.4), (Devil.FloorFt, Devil.TopFt, Devil.PeriodSec, Devil.TravelFt, Devil.PushFt, Devil.PushSec));
        var mesa = Catalog.MustPark(ParkId.Sunscorch);
        Assert.Equal(2, PlayedPark.Of(mesa, night: false, hazards: true, Catalog.Rules.Hazards).Hazards.Count(h => h.Type == HazardType.DustDevil));
        Assert.Empty(PlayedPark.Of(mesa, night: true, hazards: true, Catalog.Rules.Hazards).Hazards);
        Assert.Empty(PlayedPark.Of(mesa, night: false, hazards: false, Catalog.Rules.Hazards).Hazards);
    }

    [Fact]
    public void ThePathIsAFunctionOfTheClockAndThePhasesAndStaysWithinItsTravel()
    {
        var disc = new DriftDisc(0, HazardType.DustDevil, -60, 200, 10, 3, 60, 14, 30, 10, 0.4, 0.2, 0.7);
        for (var t = 0.0; t < 40; t += 0.25)
        {
            var (x, z) = disc.At(t);
            Assert.InRange(x, -90 - 1e-9, -30 + 1e-9);
            Assert.InRange(z, 182 - 1e-9, 218 + 1e-9);
            Assert.Equal((x, z), disc.At(t));
        }
        Assert.NotEqual(disc.At(3), (disc with { PhaseA = 0.5 }).At(3));
        var (cx, cz) = disc.At(5);
        Assert.True(disc.Takes(5, cx + 4, 20, cz));
        Assert.False(disc.Takes(5, cx + 4, 2, cz));    // under its floor: a grounder
        Assert.False(disc.Takes(5, cx + 4, 70, cz));   // over its top
    }

    [Theory]
    [InlineData(0.0, 1.0, 3.0, 0.0)]
    [InlineData(0.0, 1.0, -3.0, 0.0)]
    [InlineData(1.0, 1.0, 0.0, 2.0)]
    public void ThePushIsSquareToTheHeadingAndAwayFromTheMiddle(double vx, double vz, double offX, double offZ)
    {
        var (dx, dz) = Drifts.PushDir(vx, vz, offX, offZ, 0, 0);
        Assert.Equal(0, dx * vx + dz * vz, 9);
        Assert.Equal(1, Math.Sqrt(dx * dx + dz * dz), 9);
        Assert.True(dx * offX + dz * offZ >= 0, "away from the middle");
    }

    // ---------------------------------------------------------------------------------
    // Live
    // ---------------------------------------------------------------------------------

    static Park WithDevil(double x, double z, double radius) =>
        Harbor with { Hazards = [.. Harbor.Hazards, new Hazard(HazardType.DustDevil, x, z, radius, null)] };

    sealed record Played(PlayEvent? Done, List<(double T, double X, double Y, double Z, bool Held)> Ball,
        IReadOnlyList<BallPushed> Pushes, IReadOnlyList<DriftDisc> Discs);

    static Played Play(Park park, AtBatResult hit, int seed)
    {
        var match = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: seed);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, match.ResolveFielding(hit, preview),
            LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var discs = live.DriftDiscs.ToList();
        var ball = new List<(double, double, double, double, bool)>();
        List<BallPushed> pushes = [];
        PlayEvent? done = null;
        for (var i = 0; i < 60 * 20 && done is null; i++)
        {
            done = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (done is not null) break;
            ball.Add((live.ElapsedSeconds, live.BallX, live.BallY, live.BallZ, live.HoldsBall));
            pushes = [.. live.PushesThisPlay];
        }
        return new Played(done, ball, pushes, discs);
    }

    /// <summary>A deep fly to left-centre, and where it flies 45 % of the way to its landing: a wide disc stands there.</summary>
    static (AtBatResult Hit, double X, double Z) Fly()
    {
        var hit = FlightFixtures.Hit(Harbor, 96, 30, -12);
        var path = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, -12, Harbor, Catalog.Rules);
        var (x, _, z) = BallFlight.PointAt(path, BallFlight.HangTime(path, Catalog.Rules) * 0.45, Catalog.Rules);
        return (hit, x, z);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AFlyThroughTheDiscLandsDisplacedByTheRowsPushAndTheGlovesDecide(int seed)
    {
        var (hit, x, z) = Fly();
        var devil = Play(WithDevil(x, z, 45), hit, seed);   // wider than the wander: the fly meets it whatever the phases
        var plain = Play(Harbor, hit, seed);
        var push = Assert.Single(devil.Pushes);
        Assert.Equal(Devil.PushFt!.Value, push.PushedFt, 6);
        // Before the push the two balls are one; once it is spent the pushed ball flies exactly pushFt to the side of the plain one.
        var at = devil.Ball.FindIndex(b => b.T >= push.T - 1e-9);
        Assert.Equal(plain.Ball[at - 1].X, devil.Ball[at - 1].X, 9);
        var spent = devil.Ball.FindIndex(b => b.T >= push.T + Devil.PushSec!.Value + 2 * Frame);
        Assert.True(spent > at && !devil.Ball[spent].Held && !plain.Ball[spent].Held, "the push is spent before any glove holds the ball");
        var (ox, oz) = (devil.Ball[spent].X - plain.Ball[spent].X, devil.Ball[spent].Z - plain.Ball[spent].Z);
        Assert.Equal(Devil.PushFt!.Value, Math.Sqrt(ox * ox + oz * oz), 6);
        Assert.Equal(1, (ox * push.Dx + oz * push.Dz) / Devil.PushFt!.Value, 6);
        Assert.Equal(devil.Ball[spent].Y, plain.Ball[spent].Y, 9);   // sideways only: the height is the flight's
        Assert.NotNull(devil.Done);
    }

    [Fact]
    public void AGrounderThroughTheDiscIsNeverPushed()
    {
        var hit = FlightFixtures.Hit(Harbor, 90, -3, -10);
        var path = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, -10, Harbor, Catalog.Rules);
        var (x, _, z) = BallFlight.PointAt(path, 0.6, Catalog.Rules);
        for (var seed = 1; seed <= 4; seed++)
            Assert.Empty(Play(WithDevil(x, z, 45), hit, seed).Pushes);
    }

    [Fact]
    public void TheSameSeedWalksTheSamePathAndHazardsOffDrawsNothing()
    {
        var (hit, x, z) = Fly();
        var park = WithDevil(x, z, 45);
        var a = Play(park, hit, 5).Discs.Single();
        var b = Play(park, hit, 5).Discs.Single();
        Assert.Equal((a.PhaseA, a.PhaseB), (b.PhaseA, b.PhaseB));
        Assert.NotEqual((a.PhaseA, a.PhaseB), (Play(park, hit, 6).Discs.Single().PhaseA, Play(park, hit, 6).Discs.Single().PhaseB));
        var off = PlayedPark.Of(park, night: false, hazards: false, Catalog.Rules.Hazards);
        var none = Play(off, hit, 5);
        Assert.Empty(none.Discs);
        Assert.Empty(none.Pushes);
    }

    [Fact]
    public void TheCardSaysWhatADevilDoes()
    {
        var line = Front.CarnivalFront.HazardLine(HazardType.DustDevil, Catalog.Rules)!;
        Assert.Contains("pushed 10 ft sideways", line, StringComparison.Ordinal);
    }
}
