using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The surge pattern (§14; the tide at Coconut Cove, #1153, WD-09 C). While a band's wave is in, on a clock the play's seeded
/// phase starts, a ball rolling inside the band drifts toward the nearer foul line — the row's pace, up to its carry, never
/// across the line. A ball in the air and every body are untouched; the gloves decide the play; hazards off removes it.
/// </summary>
public sealed class SurgeTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static Park Harbor => Catalog.MustPark(ParkId.Harbor);
    static HazardTypeRules Tide => Catalog.Rules.Hazards.Of(HazardType.Tide);

    [Fact]
    public void TheTideRowIsASurgeWithItsClockItsCarryAndAHighTide()
    {
        Assert.Equal(HazardPattern.Surge, Tide.Pattern);
        Assert.Equal((0.5, 12.0, 4.0, 12.0, 1.4), (Tide.HeightFt, Tide.PeriodSec, Tide.SurgeSec, Tide.CarryFt, Tide.NightRadiusMul));
        var cove = Catalog.MustPark(ParkId.Coconut);
        Assert.Equal(2, cove.Hazards.Count(h => h.Type == HazardType.Tide));
        Assert.All(cove.Hazards, h => Assert.True(Math.Abs(FieldBounds.SprayDeg(h.X, h.Z)) > 30, "a foul-side corner"));
        Assert.Equal(18 * 1.4, Surges.Of(cove, Catalog.Rules, night: true)[0].RadiusFt, 9);
        Assert.Equal(18, Surges.Of(cove, Catalog.Rules, night: false)[0].RadiusFt, 9);
    }

    [Fact]
    public void AWholeWaveCarriesTheWholeCarryAndTheClockRepeats()
    {
        var band = new SurgeBand(0, HazardType.Tide, -100, 150, 18, 0.5, 12, 4, 12);
        var carried = 0.0;
        for (var t = 0.0; t < 12; t += Frame)
            if (band.In(t, 0)) carried += band.StepFt(Frame);
        Assert.Equal(12, carried, 0);
        Assert.True(band.In(0.1, 0) && !band.In(4.1, 0) && band.In(12.1, 0));
        Assert.Equal(7.9, band.UntilIn(4.1, 0), 9);
        Assert.True(band.In(0.1, 9) == band.In(9.1, 0));
        Assert.True(band.Holds(-100, 0.4, 150) && !band.Holds(-100, 0.6, 150) && !band.Holds(-100, 0, 170));
    }

    [Theory]
    [InlineData(-120.0, 160.0)]
    [InlineData(118.0, 150.0)]
    [InlineData(-40.0, 200.0)]
    public void TheWayToTheLineIsSquareToTheNearerLine(double x, double z)
    {
        var (dx, dz, d) = Surges.TowardLine(x, z);
        var (lx, lz) = (x + dx * d, z + dz * d);
        Assert.Equal(45, Math.Abs(FieldBounds.SprayDeg(lx, lz)), 6);
        Assert.Equal(Math.Sign(x), Math.Sign(lx));
        Assert.True(Math.Abs(FieldBounds.SprayDeg(x + dx, z + dz)) > Math.Abs(FieldBounds.SprayDeg(x, z)), "toward the chalk");
    }

    // ---------------------------------------------------------------------------------
    // Live
    // ---------------------------------------------------------------------------------

    /// <summary>A liner down the right-field line that lands and rolls into the corner, and where it rolls 4.8 s in: the band is put there.</summary>
    static (AtBatResult Hit, double X, double Z) Roller()
    {
        var hit = FlightFixtures.Hit(Harbor, 105, 12, 38);
        var path = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, 38, Harbor, Catalog.Rules);
        var (x, _, z) = BallFlight.PointAt(path, 4.8, Catalog.Rules);
        return (hit, x, z);
    }

    sealed record Played(LivePlaySystem Live, PlayEvent? Done, List<(double T, double X, double Y, double Z, bool Held)> Ball,
        IReadOnlyList<BallCarried> Carries, double Phase);

    static Played Play(Park park, AtBatResult hit, int seed, bool night = false)
    {
        var match = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: seed, night: night);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        var field = match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field,
            LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var phase = live.SurgePhaseSec;
        var ball = new List<(double, double, double, double, bool)>();
        PlayEvent? done = null;
        List<BallCarried> carries = [];
        for (var i = 0; i < 60 * 20 && done is null; i++)
        {
            done = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (done is not null) break;   // the completing frame resets the field
            ball.Add((live.ElapsedSeconds, live.BallX, live.BallY, live.BallZ, live.HoldsBall));
            carries = [.. live.CarriesThisPlay];
        }
        return new Played(live, done, ball, carries, phase);
    }

    static Park WithTide(double x, double z, double radius = 18) =>
        Harbor with { Hazards = [.. Harbor.Hazards, new Hazard(HazardType.Tide, x, z, radius, null)] };

    /// <summary>The first seed whose phase brings the wave in while the roller is in the band.</summary>
    static (int Seed, Played Tide) CarriedSeed(Park park, AtBatResult hit)
    {
        for (var seed = 1; seed <= 60; seed++)
        {
            var played = Play(park, hit, seed);
            if (played.Carries.Count > 0) return (seed, played);
        }
        throw new InvalidOperationException("no seed in 60 brought the wave in while the roller was in the band");
    }

    [Fact]
    public void ARollerInTheBandDriftsTowardTheLineByTheRowsPaceAndNoFurtherThanItsCarry()
    {
        var (hit, x, z) = Roller();
        var park = WithTide(x, z);
        var (seed, tide) = CarriedSeed(park, hit);
        var plain = Play(Harbor, hit, seed);
        var carry = Assert.Single(tide.Carries);
        Assert.InRange(carry.CarriedFt, 1e-6, Tide.CarryFt! .Value + 1e-9);
        // Up to the carry the two balls are one ball; from it the tide's ball has moved square to the line by what it carried.
        var at = tide.Ball.FindIndex(b => b.T >= carry.T - 1e-9);
        Assert.True(at > 0);
        Assert.Equal(plain.Ball[at - 1].X, tide.Ball[at - 1].X, 9);
        Assert.Equal(plain.Ball[at - 1].Z, tide.Ball[at - 1].Z, 9);
        var (dx, dz, _) = Surges.TowardLine(carry.X, carry.Z);
        var last = Enumerable.Range(at, Math.Min(tide.Ball.Count, plain.Ball.Count) - at)
            .Where(i => !tide.Ball[i].Held && !plain.Ball[i].Held).DefaultIfEmpty(at).Max();
        var (ox, oz) = (tide.Ball[last].X - plain.Ball[last].X, tide.Ball[last].Z - plain.Ball[last].Z);
        var moved = Math.Sqrt(ox * ox + oz * oz);
        Assert.True(moved > 0.05, "the tide moved the ball");
        Assert.InRange(moved, 0, carry.CarriedFt + 1e-6);
        Assert.True((ox * dx + oz * dz) / moved > 0.999, "square toward the nearer line");
        // Never across the chalk, and the gloves still end the play.
        Assert.All(tide.Ball, b => Assert.True(Math.Abs(FieldBounds.SprayDeg(b.X, b.Z)) < 45 || b.Y > 0.01 || b.Held));
        Assert.NotNull(tide.Done);
    }

    [Fact]
    public void AFlyOverTheBandIsNeverCarriedAndTheSameSeedIsTheSameTide()
    {
        // A fly whose flight passes over the band at its middle: nothing while it is up, whatever the phase.
        var hit = FlightFixtures.Hit(Harbor, 95, 32, -30);
        var path = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, -30, Harbor, Catalog.Rules);
        var land = BallFlight.HangTime(path, Catalog.Rules);
        var (x, _, z) = BallFlight.PointAt(path, land * 0.5, Catalog.Rules);
        var park = WithTide(x, z, 30);
        for (var seed = 1; seed <= 12; seed++)
        {
            var played = Play(park, hit, seed);
            Assert.All(played.Carries, c => Assert.True(c.T > land, "carried while in the air"));
            Assert.Equal(played.Phase, Play(park, hit, seed).Phase);
        }
        Assert.Contains(Enumerable.Range(1, 12).Select(s => Play(park, hit, s).Phase).Distinct(), p => p > 0);
    }

    [Fact]
    public void HazardsOffAndAParkWithoutATideDrawNothingAndCarryNothing()
    {
        var (hit, x, z) = Roller();
        var park = WithTide(x, z);
        var off = PlayedPark.Of(park, night: false, hazards: false, Catalog.Rules.Hazards);
        Assert.Empty(Surges.Of(off, Catalog.Rules, night: false));
        var (seed, _) = CarriedSeed(park, hit);
        var none = Play(off, hit, seed);
        Assert.Empty(none.Carries);
        Assert.Equal(0, none.Phase);
        Assert.Equal(0, Play(Harbor, hit, seed).Phase);
    }

    [Fact]
    public void TheCardSaysWhenTheTideComesAndThatItReachesFartherAtNight()
    {
        var line = Front.CarnivalFront.HazardLine(HazardType.Tide, Catalog.Rules)!;
        Assert.Contains("every 12 s", line, StringComparison.Ordinal);
        Assert.Contains("toward the line", line, StringComparison.Ordinal);
        Assert.Contains("farther in at night", line, StringComparison.Ordinal);
    }
}
