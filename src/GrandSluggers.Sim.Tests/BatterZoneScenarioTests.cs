using System.Reflection;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The knee-to-chest strike zone per batter (spec §4.4, CF-4, CH-06), plan rows SC-15 … SC-19.
///
/// <b>SC-15</b>: every captain's zone runs from its knee landmark to its chest landmark, at the fixed half-width.
/// <b>SC-16</b>: the same pitch aimed at the middle crosses the middle of each batter's own zone.
/// <b>SC-17</b>: the swing and the stance never move the zone; it is the rest body's.
/// <b>SC-18</b>: S-108 for every body that can bat: every family, no aim, crosses inside with a ball's radius to spare.
/// <b>SC-19</b>: one zone per batter, the same in either half, from either seat, for a human's pitch or the CPU's.
///
/// Every row reads the roster from data, so the next captain is checked the day its file lands.
/// </summary>
public sealed class BatterZoneScenarioTests
{
    readonly ContentCatalog _content = Shipped.Content;

    RulesTable R => _content.Rules;

    /// <summary>The real ball's radius (S-108's margin).</summary>
    const double BallRadiusFt = 0.125;

    IEnumerable<Character> Captains => _content.CaptainIds.Select(_content.Must);

    // ---------------------------------------------------------------------------------
    // SC-15  Knee to chest, fixed width
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC15_EveryCaptainsZoneRunsFromTheKneeToTheChestAtTheFixedWidth()
    {
        Assert.Equal(7, Captains.Count());
        foreach (var who in Captains)
        {
            var zone = StrikeZoneGeometry.For(who, R);
            var (knee, chest, _) = Silhouette.Landmarks(who);
            // No shipped captain leans on the safety net: the zone is the body's, exactly.
            Assert.Equal(knee, zone.Bottom);
            Assert.Equal(chest, zone.Top);
            Assert.Equal(0.92, zone.HalfWidth);
            Assert.Equal(StrikeZoneGeometry.HalfWidth, zone.HalfWidth);
            Assert.Equal(HomeSet.PlateW / 2, zone.HalfWidth, 12);
        }

        // Taller body, taller zone; the width never moves.
        var ordered = Captains.OrderBy(c => c.Proportions.Height).Select(c => StrikeZoneGeometry.For(c, R)).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            Assert.True(ordered[i].Height >= ordered[i - 1].Height);
            Assert.True(ordered[i].Top >= ordered[i - 1].Top);
        }

        // A role player stands in its captain's zone.
        foreach (var who in _content.Characters.Values)
            Assert.Equal(StrikeZoneGeometry.For(_content.Must(who.BodyType), R), StrikeZoneGeometry.For(who, R));
    }

    [Fact]
    public void SC15_TheClampIsASafetyNetEveryShippedBatterSitsInside()
    {
        var z = R.Pitching.Zone;
        foreach (var who in _content.Characters.Values)
        {
            var (knee, chest, _) = Silhouette.Landmarks(who);
            Assert.InRange(knee, z.BottomMinFt, z.BottomMaxFt);
            Assert.InRange(chest, z.TopMinFt, z.TopMaxFt);
            Assert.InRange(chest - knee, z.HeightMinFt, z.HeightMaxFt);
        }

        // The net catches a body outside it: the bottom and top land in their bands, the height in its band.
        var squat = StrikeZoneGeometry.Clamp(z.BottomMaxFt - 0.05, z.TopMinFt + 0.05, z);
        Assert.Equal(z.HeightMinFt, squat.Height, 9);
        var tiny = StrikeZoneGeometry.Clamp(0.1, 0.9, z);
        Assert.Equal((z.BottomMinFt, z.TopMinFt), (tiny.Bottom, tiny.Top));
        var giant = StrikeZoneGeometry.Clamp(3.0, 9.0, z);
        Assert.True(giant.Top <= z.TopMaxFt + 1e-9 && giant.Bottom >= z.BottomMinFt - 1e-9);
        Assert.InRange(giant.Height, z.HeightMinFt, z.HeightMaxFt);
        Assert.InRange(StrikeZoneGeometry.Clamp(0.1, 9.0, z).Height, z.HeightMinFt, z.HeightMaxFt);
    }

    [Fact]
    public void SC15_TheClampTableRefusesABandThatIsNotABand()
    {
        var errors = new List<string>();
        R.Pitching.Zone.Validate("pitching.json", errors);
        Assert.Empty(errors);
        (R.Pitching.Zone with { HeightMinFt = 5.0 }).Validate("pitching.json", errors);
        Assert.Contains(errors, e => e.Contains("zone.heightMinFt", StringComparison.Ordinal));
        errors.Clear();
        (R.Pitching.Zone with { TopMinFt = R.Pitching.Zone.BottomMaxFt }).Validate("pitching.json", errors);
        Assert.Contains(errors, e => e.Contains("zone.topMinFt", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // SC-16  The same pitch aimed middle crosses each batter's own middle
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC16_TheSamePitchAimedMiddleCrossesTheMiddleOfZigsZoneAndOfAshlords()
    {
        var zig = MatchWith("zig");
        var ashlord = MatchWith("ashlord");
        var zigZone = zig.BatterZone;
        var ashZone = ashlord.BatterZone;
        Assert.True(ashZone.Height > zigZone.Height + 1, $"Zig {zigZone} vs Ashlord {ashZone}");

        foreach (var family in PitchFamily.All)
        {
            // One command, aimed at the middle, thrown at two batters.
            var aimed = new PitchCommand(family, 0, false);
            var atZig = PitchFlight.Crossing(zig.PreparePitch(aimed), R);
            var atAsh = PitchFlight.Crossing(ashlord.PreparePitch(aimed), R);
            // The fastball crosses dead middle; every family keeps the same place in both zones.
            var zigShare = (atZig.Y - zigZone.CenterY) / zigZone.Height;
            var ashShare = (atAsh.Y - ashZone.CenterY) / ashZone.Height;
            Assert.Equal(zigShare, ashShare, 9);
            if (family == PitchFamily.Fastball)
            {
                Assert.Equal(zigZone.CenterY, atZig.Y, 9);
                Assert.Equal(ashZone.CenterY, atAsh.Y, 9);
            }
        }

        // The umpire, the CPU batter's read, the ring and the oval all read the same middle.
        foreach (var m in new[] { zig, ashlord })
        {
            var zone = m.BatterZone;
            var pitch = m.PreparePitch(new PitchCommand(PitchFamily.Fastball, 0, false));
            Assert.Equal(zone, pitch.Zone);
            Assert.True(AtBatResolver.PitchInZone(pitch, 5, R));
            Assert.Equal(zone.CenterY, SetTells.RubberRing(0, zone).Y);
            Assert.Equal(PitchFlight.Crossing(pitch, R), SetTells.Locator(new PitchCommand(PitchFamily.Fastball, 0, false), zone, R));
            var oval = SweetSpot.Oval(m.Batter, null, 0, 0, R);
            Assert.Equal(zone.CenterY, oval.CenterY);
            Assert.Equal(zone.HalfHeight, oval.HalfHeightFt);
            Assert.Equal(ContactQuality.Perfect, SweetSpot.Zone(0, m.Batter.Bats, 0, zone.CenterY, R, zone));
        }
    }

    [Fact]
    public void SC16_AStrikeOnOneBatterIsAStrikeOnEveryBatter()
    {
        // The aim frame stretches with the zone, so a delivery keeps its place and its call for every body.
        var pitches = new List<PitchCommand>();
        foreach (var family in PitchFamily.All)
        foreach (var aimY in new[] { -1.0, -0.8, -0.3, 0, 0.3, 0.8, 1.0 })
        foreach (var rubber in new[] { -1.0, 0, 0.6 })
            pitches.Add(new PitchCommand(family, 0, false, AimY: aimY, RubberX: rubber));
        foreach (var pitch in pitches)
        {
            var calls = _content.Characters.Values
                .Select(c => StrikeZoneGeometry.Contains(pitch with { Zone = StrikeZoneGeometry.For(c, R) }, R))
                .Distinct().ToList();
            Assert.Single(calls);
        }
    }

    // ---------------------------------------------------------------------------------
    // SC-17  The animation never moves the zone
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC17_MidSwingTheZoneIsTheRestZone()
    {
        // The zone function reads a character and a table: no pose, no swing clock, no stance.
        var reads = typeof(StrikeZoneGeometry).GetMethod(nameof(StrikeZoneGeometry.For))!.GetParameters()
            .Select(p => p.ParameterType).ToArray();
        Assert.Equal(new[] { typeof(Character), typeof(RulesTable) }, reads);

        foreach (var who in Captains)
        {
            var match = MatchWith(who.Id);
            var rest = match.BatterZone;
            // Walk the box, load a swing, swing through a foul: the batter is the same body, and so is the zone.
            Assert.True(match.WalkBatter(0.8));
            Assert.Equal(rest, match.BatterZone);
            var pitch = match.PreparePitch(new PitchCommand(PitchFamily.Fastball, 0, false));
            var swing = match.CpuBatter.Swing(pitch);
            Assert.Equal(rest, match.BatterZone);
            Assert.Equal(rest, pitch.Zone);
            var ev = match.Play(pitch, Scenario.SwingAt(-40));
            Assert.Equal(PlayKind.SwingMiss, ev.Kind);
            Assert.Equal(who.Id, match.Batter.Id);
            Assert.Equal(rest, match.BatterZone);
            _ = swing;
            // And it is the rest landmarks, the same numbers the still gate measures.
            var (knee, chest, _) = Silhouette.Landmarks(who);
            Assert.Equal((knee, chest), (rest.Bottom, rest.Top));
        }
    }

    // ---------------------------------------------------------------------------------
    // SC-18  S-108 for every body that can bat
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC18_EveryFamilyWithNoAimCrossesInsideEveryBattersZone()
    {
        foreach (var batter in _content.Characters.Values)
            AssertS108(StrikeZoneGeometry.For(batter, R), batter.Id);
    }

    [Fact]
    public void SC18_TheNextCaptainIsPitchableAnywhereOnTheLadderAndPastIt()
    {
        // A captain not yet authored: the CH-03 ladder's floor and cap (0.70× and 1.35× Rio), and bodies well
        // past both, which the safety net brings back. Every one of them keeps S-108.
        var rio = _content.Must("rio");
        foreach (var mul in new[] { 0.5, 0.70, 1.0, 1.35, 2.0 })
        {
            var body = rio with { Proportions = rio.Proportions with { Height = (float)(rio.Proportions.Height * mul) } };
            var zone = StrikeZoneGeometry.For(body, R);
            AssertS108(zone, $"rio x{mul}");
            // And every strike stays hittable with the box centered, on the ordinary bat.
            foreach (var bats in new[] { Hand.L, Hand.R })
                Assert.True(SweetSpot.CoversTheZone(bats, R, zone, SweetSpot.SwingBarrel(body with { Bats = bats }, null, 0, R)),
                    $"rio x{mul}: a strike is off the bat");
        }
    }

    void AssertS108(BatterZone zone, string who)
    {
        foreach (var family in PitchFamily.All)
        foreach (var throws in new[] { Hand.L, Hand.R })
        {
            var (x, y) = PitchFlight.Crossing(new PitchCommand(family, 0, false, Throws: throws, Zone: zone), R);
            var where = $"{who}/{family}/{throws}";
            Assert.True(Math.Abs(x) + BallRadiusFt <= zone.HalfWidth, $"{where}: x {x}");
            Assert.True(y - BallRadiusFt >= zone.Bottom, $"{where}: y {y} on the floor {zone.Bottom}");
            Assert.True(y + BallRadiusFt <= zone.Top, $"{where}: y {y} on the ceiling {zone.Top}");
            foreach (var bats in new[] { Hand.L, Hand.R })
                Assert.True(SweetSpot.Distance(0, bats, x, y, R, zone) <= 1, $"{where}: a {bats} oval does not reach ({x}, {y})");
        }
    }

    [Fact]
    public void S134_TheDrawnOvalIsTheJudgedOvalForEveryCaptain()
    {
        var resolver = new AtBatResolver(_content.Chemistry, R, _content.StarSkills);
        var park = _content.Parks[ExhibitionPick.DefaultPark];
        var pitcher = _content.Must("vale");
        foreach (var hitter in Captains)
        foreach (var charge in new[] { 0.0, 1.0 })
        foreach (var box in new[] { 0.0, -0.6 })
        {
            var oval = SweetSpot.Oval(hitter, null, charge, box, R);
            var zone = StrikeZoneGeometry.For(hitter, R);
            Assert.Equal(SweetSpot.WorldCenter(box, zone), (oval.CenterX, oval.CenterY));
            var pts = SweetSpot.Outline(oval);
            foreach (var (x, y) in pts)
                Assert.Equal(1.0, SweetSpot.Distance(box, hitter.Bats, oval.CenterX + x, oval.CenterY + y, R, zone, oval.BarrelScale), 9);
            for (var i = 0; i < pts.Count; i += pts.Count / 4)
            {
                var (x, y) = pts[i];
                Assert.Equal(ContactQuality.Nice, resolver.Resolve(
                    Swing(pitcher, hitter, charge, box, oval.CenterX + x * 0.98, oval.CenterY + y * 0.98), park, new Random(1)).Quality);
                Assert.Equal(ContactQuality.Sour, resolver.Resolve(
                    Swing(pitcher, hitter, charge, box, oval.CenterX + x * 1.02, oval.CenterY + y * 1.02), park, new Random(1)).Quality);
            }
        }
    }

    static AtBatInput Swing(Character pitcher, Character batter, double charge, double box, double x, double y) =>
        new(pitcher, batter, null, [], ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: 0,
            UseStarPitch: false, UseStarSwing: false, Bat: null, PitcherStamina: 100,
            Charge01: charge, BoxOffsetX: box, CrossingX: x, CrossingY: y);

    // ---------------------------------------------------------------------------------
    // SC-19  One zone per batter: both halves, both seats, 1P and 1v1
    // ---------------------------------------------------------------------------------

    [Fact]
    public void SC19_TheSameBatterHasTheSameZoneInEitherHalfFromEitherSeat()
    {
        foreach (var who in Captains)
        {
            var away = MatchWith(who.Id);
            var home = MatchWith(who.Id, home: true);
            Assert.True(away.Top);
            Assert.False(home.Top);
            Assert.Equal(who.Id, away.Batter.Id);
            Assert.Equal(who.Id, home.Batter.Id);
            Assert.Equal(away.BatterZone, home.BatterZone);

            // A human's pitch (the client builds it and the match prepares it) and the CPU's own carry the one zone.
            var human = home.PreparePitch(new PitchCommand(PitchFamily.Changeup, 1, false, RubberX: 0.3));
            var cpu = away.PreparePitch(away.CpuPitcher.Pitch());
            Assert.Equal(away.BatterZone, human.Zone);
            Assert.Equal(away.BatterZone, cpu.Zone);
            // A pitch prepared elsewhere, with some other zone, is laid on this batter's: no caller brings its own.
            var foreign = human with { Zone = new BatterZone(0.2, 0.5) };
            Assert.Equal(home.BatterZone, home.PreparePitch(foreign).Zone);
        }

        // No zone reader takes a seat, a pad, a side or a "human" flag: 1P and 1v1 cannot read two zones.
        var readers = new[] { typeof(StrikeZoneGeometry), typeof(SweetSpot), typeof(SetTells), typeof(PitchFlight) }
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.ReturnType == typeof(BatterZone) || m.GetParameters().Any(p => p.ParameterType == typeof(BatterZone)
                || p.ParameterType == typeof(BatterZone?)));
        foreach (var m in readers)
            foreach (var p in m.GetParameters())
            {
                Assert.NotEqual(typeof(Seats), p.ParameterType);
                Assert.NotEqual(typeof(LiveSeats), p.ParameterType);
                Assert.False(p.ParameterType == typeof(bool) && (p.Name ?? "").Contains("human", StringComparison.OrdinalIgnoreCase),
                    $"{m.DeclaringType!.Name}.{m.Name} reads a seat");
            }
    }

    // ---------------------------------------------------------------------------------
    // No live path reads the fallback zone
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AWholeGameNeverFallsBackToTheReferenceZone()
    {
        foreach (var (home, away, seed) in new[] { ("rio", "ashlord", 7), ("zig", "konga", 3) })
        {
            var match = Match.Exhibition(_content, home, away, innings: 3, seed: seed);
            StrikeZoneGeometry.FallbackReads = 0;
            match.AutoPlayGame();
            Assert.True(match.Over);
            Assert.Equal(0, StrikeZoneGeometry.FallbackReads);
            var pitched = match.Log.Where(e => e.Pitch.DeliveryPrepared).ToList();
            Assert.NotEmpty(pitched);
            foreach (var ev in pitched)
                Assert.Equal(StrikeZoneGeometry.For(ev.Batter, match.Rules), ev.Pitch.Zone);
        }
    }

    // ---------------------------------------------------------------------------------
    // helpers
    // ---------------------------------------------------------------------------------

    /// <summary>A match whose batter at the plate is <paramref name="batterId"/>, batting away (top) or home (bottom).</summary>
    Match MatchWith(string batterId, bool home = false, int seed = 1)
    {
        var batter = _content.Must(batterId);
        var captain = _content.Must(batterId == "rio" ? "zig" : "rio");
        var fillers = _content.Characters.Values
            .Where(c => !c.Captain && c.Id != batter.Id && c.Id != captain.Id)
            .OrderBy(c => c.Id, StringComparer.Ordinal).Take(7);
        var offense = new Team("Offense", captain, [captain, batter, .. fillers]);
        var defense = _content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        // Exhibition takes (home, away); the away side bats the top.
        var match = home
            ? Match.Exhibition(_content, offense, defense, 3, seed)
            : Match.Exhibition(_content, defense, offense, 3, seed);
        if (home) match.SkipToHomeHalf();
        Assert.Equal(batter.Id, match.Batter.Id);
        return match;
    }
}
