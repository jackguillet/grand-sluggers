using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The compact field and the ball that fits it (spec §0.3, 3c-1 and 3e): 80-ft basepaths, fences at 0.70 of the
/// full-size parks, drag 0.0040. One change, because drag is global and park dimensions are not — at drag 0.0040 the
/// best swing in the game carries 304 ft, so the heavier ball against full-size poles is a game with no home runs in
/// it, and the smaller parks with the lighter ball are a derby.
///
/// <para>
/// The full-size field these numbers came from is named here only where a rule is defined against it (the basepath
/// factor 80/90, the fence factor 0.70): a rule written as "the old number times a factor" is checked against the
/// literal it produces, so it cannot drift.
/// </para>
/// </summary>
[Trait("Kind", "Balance")]
public sealed class CompactFieldTests
{
    static readonly ContentCatalog Game = Shipped.Content;

    /// <summary>The basepath scale: 80 ft over the full-size 90.</summary>
    const double Infield = 80.0 / 90.0;

    static readonly string[] ParkIds =
        [ParkId.Canopy, ParkId.Crystal, ParkId.Ember, ParkId.Funfair, ParkId.Harbor, ParkId.Rooftop];

    /// <summary>
    /// 80-ft basepaths. The bags are the full-size rounded 63.64 / 127.28 multiplied by 80/90 and kept at two decimals;
    /// the mound follows the same factor, 53.78 ft.
    /// </summary>
    [Fact]
    public void TheInfieldIsTheEightyFootDiamond()
    {
        var infield = Game.Rules.Infield;
        Assert.Equal(80, infield.BaselineFt);
        Assert.Equal(53.78, infield.MoundFt);
        Assert.Equal(56.57, infield.CornerFt);
        Assert.Equal(113.14, infield.SecondFt);
        Assert.Equal(Math.Round(60.5 * Infield, 2), infield.MoundFt);
        Assert.Equal(Math.Round(63.64 * Infield, 2), infield.CornerFt);
        Assert.Equal(Math.Round(127.28 * Infield, 2), infield.SecondFt);

        // A corner rotated back off the diagonal is still the basepath, and second is still two corners.
        Assert.Equal(80, infield.CornerFt * Math.Sqrt(2), 2);
        Assert.Equal(infield.SecondFt, infield.CornerFt * 2, 6);

        // The process-wide diamond is the table's.
        Assert.Equal(80, Diamond.Baseline);
        Assert.Equal(53.78, Diamond.Mound);
    }

    /// <summary>
    /// The ground follows the bags. The drawn grass diamond and the back arc of the dirt are measured from second, the
    /// mound and the rubber, so they take the basepath scale. Path width, the bag pads, the home pad, the mound table and
    /// the warning track are bodies and equipment and stay in feet — the same rule that keeps wall heights at 8 ft. The
    /// cost is visible at the corners: the grass vertex clears the 12-ft bag pad by 0.13 ft. Pinned because that margin is
    /// a hairline, and because the validator deliberately does not refuse a table over it (a pad-clearance rule would
    /// refuse a smaller diamond that scales perfectly well).
    /// </summary>
    [Fact]
    public void TheGroundDressIsTheEightyFootDiamondsDress()
    {
        var infield = Game.Rules.Infield;
        Assert.Equal(44.44, infield.InnerHalfFt);
        Assert.Equal(81.78, infield.BackArcFt);
        Assert.Equal(Math.Round(50 * Infield, 2), infield.InnerHalfFt);
        Assert.Equal(Math.Round(92 * Infield, 2), infield.BackArcFt);

        // The dirt's far edge.
        Assert.Equal(135.56, infield.MoundFt + infield.BackArcFt, 2);
        Assert.Equal(ParkDiamond.DirtMaxZ(DiamondGeometry.Of(Rules.Default)), infield.MoundFt + infield.BackArcFt, 2);

        // The vertex-to-bag gap, and the pad clearance it leaves.
        Assert.Equal(12.13, infield.CornerFt - infield.InnerHalfFt, 2);
        Assert.Equal(0.13, infield.CornerFt - (infield.InnerHalfFt + ParkDiamond.BagPadR), 2);
    }

    /// <summary>
    /// The accepted table, spelled out rather than recomputed: a fence list derived by the same arithmetic the authoring
    /// used would move with it and prove nothing. One scale for all six keeps each park's identity and their order.
    /// </summary>
    [Theory]
    [InlineData(ParkId.Canopy, 218, 265, 223)]
    [InlineData(ParkId.Crystal, 224, 270, 224)]
    [InlineData(ParkId.Ember, 237, 286, 237)]
    [InlineData(ParkId.Funfair, 220, 273, 238)]
    [InlineData(ParkId.Harbor, 232, 280, 232)]
    [InlineData(ParkId.Rooftop, 223, 272, 225)]
    public void EachParkPlaysItsAcceptedFences(string id, int left, int center, int right)
    {
        var park = Game.Parks[id];
        Assert.Equal(left, park.LeftFenceFt);
        Assert.Equal(center, park.CenterFenceFt);
        Assert.Equal(right, park.RightFenceFt);
    }

    /// <summary>The parks keep their order, and wall heights did not scale, because bodies did not shrink.</summary>
    [Fact]
    public void TheParksKeepTheirOrderAndTheirWalls()
    {
        string[] bySize = [ParkId.Canopy, ParkId.Crystal, ParkId.Rooftop, ParkId.Funfair, ParkId.Harbor, ParkId.Ember];
        Assert.Equal(bySize, ParkIds.OrderBy(id => Game.Parks[id].CenterFenceFt).ToArray());
        Assert.Equal(8, Game.Parks[ParkId.Crystal].FenceHeightFt);
        Assert.Equal(12, Game.Parks[ParkId.Harbor].FenceHeightFt);
    }

    /// <summary>
    /// The heavier ball and the lip on the basepath scale: 155 × 80/90 = 137.78. A lip on the fence scale (108.50)
    /// would sit inside the middle infield and make <see cref="FieldingResolver.OutfieldGrass"/> call 2B and SS
    /// outfielders, which is why the lip takes the basepath scale while fair territory takes the fence's.
    /// </summary>
    [Fact]
    public void TheBallIsHeavierAndTheLipIsOnTheBasepathScale()
    {
        Assert.Equal(0.0040, Game.Rules.Flight.Drag);
        Assert.Equal(137.78, Game.Rules.Flight.Classes.InfieldLipFt);
        Assert.Equal(Math.Round(155 * Infield, 2), Game.Rules.Flight.Classes.InfieldLipFt);
    }

    /// <summary>
    /// The lip has a floor, and nothing in the rules enforces it: every infielder stands inside it and every outfielder
    /// past it. 2B and SS are the deep pair and set the floor; 137.78 clears them by 26.45 ft.
    /// </summary>
    [Fact]
    public void TheLipStaysOutsideEveryInfielder()
    {
        var lip = Game.Rules.Flight.Classes.InfieldLipFt;
        foreach (var pos in FieldingResolver.InfieldPursuitPositions)
            Assert.True(Radius(pos) < lip, $"{pos} at {Radius(pos):0.00} ft is outside the lip {lip}");
        Assert.Equal(111.33, Radius("2B"), 2);
        Assert.Equal(26.45, lip - Radius("2B"), 2);
        Assert.True(155 * 0.70 < Radius("2B"), "a lip on the fence scale sits inside the middle infield");
        foreach (var pos in new[] { "LF", "CF", "RF" })
            Assert.True(Radius(pos) > lip, $"{pos} should be past the lip");

        static double Radius(string pos)
        {
            var (x, z) = Diamond.Positions[pos];
            return Diamond.Dist(0, 0, x, z);
        }
    }

    /// <summary>The lip sits just outside the drawn dirt: 2.22 ft, the full-size 2.50 on the basepath scale.</summary>
    [Fact]
    public void TheLipAndTheDrawnDirtAgree()
    {
        var dirt = Game.Rules.Infield.MoundFt + Game.Rules.Infield.BackArcFt;
        Assert.Equal(2.22, Game.Rules.Flight.Classes.InfieldLipFt - dirt, 2);
        Assert.Equal(2.50 * Infield, Game.Rules.Flight.Classes.InfieldLipFt - dirt, 2);
    }

    /// <summary>
    /// The best swing without a star — Power 10, a perfect charge, full lift — carries 304 ft. It clears every park
    /// through the middle, and a charged perfect swing leaves every yard from Power 9 up; the biggest star swing clears
    /// centre from a middle bat.
    /// </summary>
    [Fact]
    public void TheBestSwingCarriesThreeHundredAndFourFeetAndStillLeavesEveryYard()
    {
        var b = Game.Rules.Batting;
        var exit = Exit(b, 10, b.Quality.Charge.Perfect);
        var launch = Launch(b, 10, charged: true, lift: b.Launch.StickDeg);
        Assert.Equal(122.5, exit);
        Assert.Equal(35.5, launch);
        Assert.Equal(304.0, BallFlight.CarryFeet(exit, launch, 0, Game.Rules), 1);

        foreach (var id in ParkIds)
        {
            var park = Game.Parks[id];
            for (var power = 9; power <= 10; power++)
            {
                var ball = BattedBall.Of(Exit(b, power, b.Quality.Charge.Perfect), Launch(b, power, charged: true, lift: b.Launch.StickDeg),
                    0, park, Game.Rules);
                Assert.True(ball.HomeRun, $"{id} charged perfect P{power} landed {ball.LandingDist:F0}");
            }

            var star = StarSkills.SwingExitMul("furnace", Game.StarSkills);
            Assert.Equal(1.25, star);
            var starBall = BattedBall.Of(Exit(b, 5, b.Quality.Charge.Perfect, star), StarSkills.SwingLaunchDeg("fly", Game.StarSkills) ?? 38,
                0, park, Game.Rules);
            Assert.True(starBall.HomeRun, $"{id} star swing landed {starBall.LandingDist:F0}");
        }
    }

    /// <summary>
    /// The outfield starts are not the park fence scale. Each body keeps its bearing and its fraction of the fence at
    /// that bearing: LF and RF keep 0.7203 of the fence at ∓23.75°, CF keeps 0.7625 of it. One set clears every wall.
    /// </summary>
    [Fact]
    public void TheOutfieldStartsClearEveryWall()
    {
        Assert.Equal((-77.09, 175.19), Game.Rules.Fielders.Spot("LF"));
        Assert.Equal((0, 213.5), Game.Rules.Fielders.Spot("CF"));
        Assert.Equal((77.09, 175.19), Game.Rules.Fielders.Spot("RF"));
        Assert.Equal((-77.09, 175.19), Diamond.Positions["LF"]);
        Assert.Equal((0, 213.5), Diamond.Positions["CF"]);

        var harbor = Game.Parks[ParkId.Harbor];
        Assert.Equal(213.5, FieldBounds.Clamp(harbor, 0, 213.5, Rules.Default).Z, 2);
        Assert.Equal(66.5, harbor.CenterFenceFt - 213.5, 2);

        var tightest = double.MaxValue;
        foreach (var id in ParkIds)
        foreach (var pos in new[] { "LF", "CF", "RF" })
        {
            var park = Game.Parks[id];
            var (x, z) = Game.Rules.Fielders.Spot(pos);
            Assert.True(FieldBounds.Inside(park, x, z, Rules.Default), $"{id} {pos} at ({x}, {z}) does not clear the wall");
            var fence = AtBatResolver.FenceAt(park, Math.Atan2(x, z) * 180 / Math.PI);
            tightest = Math.Min(tightest, fence - Diamond.Dist(0, 0, x, z));
        }
        Assert.Equal(51.50, tightest, 2);   // canopy-yard's centre field, the closest of the eighteen
    }

    /// <summary>
    /// The corner infielders stand 14.77 ft from their bags and the middle pair 38.23 ft from second — the full-size
    /// 16.62 and 43.01 on the basepath scale: the same defence, smaller.
    /// </summary>
    [Fact]
    public void TheInfieldersStandOffTheirBagsOnTheBasepathScale()
    {
        Assert.Equal(14.77, BagGap(Diamond.Positions["1B"], 1), 2);
        Assert.Equal(14.77, BagGap(Diamond.Positions["3B"], 3), 2);
        Assert.Equal(38.23, BagGap(Diamond.Positions["2B"], 2), 2);
        Assert.Equal(16.62 * Infield, BagGap(Diamond.Positions["1B"], 1), 2);
        Assert.Equal(43.01 * Infield, BagGap(Diamond.Positions["2B"], 2), 2);

        static double BagGap((double X, double Z) fielder, int bag)
        {
            var infield = Game.Rules.Infield;
            var (bx, bz) = bag switch
            {
                1 => (infield.CornerFt, infield.CornerFt),
                2 => (0.0, infield.SecondFt),
                3 => (-infield.CornerFt, infield.CornerFt),
                _ => (0.0, 0.0)
            };
            return Diamond.Dist(fielder.X, fielder.Z, bx, bz);
        }
    }

    /// <summary>
    /// No start stands in a Funfair mouth. The centre mouth is at (0, 160) with a 12.60-ft rim and the centre fielder
    /// stands 53.50 ft off it, 40.90 ft clear. Only at night, and only a fly.
    /// </summary>
    [Fact]
    public void TheCentreFielderIsClearOfTheFunfairChompers()
    {
        var funfair = PlayedPark.Of(Game.Parks[ParkId.Funfair], night: true, hazards: true, Game.Rules.Hazards);
        var funfairByDay = PlayedPark.Of(Game.Parks[ParkId.Funfair], night: false, hazards: true, Game.Rules.Hazards);
        var centre = funfair.Hazards.Single(h => h.Type == HazardType.Chomper && h.Tag == "C");
        var cf = Game.Rules.Fielders.Spot("CF");
        Assert.Equal((0.0, 160.0, 12.6), (centre.X, centre.Z, centre.Radius));
        Assert.Equal(53.50, Diamond.Dist(centre.X, centre.Z, cf.X, cf.Z), 2);
        Assert.Equal(40.90, Diamond.Dist(centre.X, centre.Z, cf.X, cf.Z) - centre.Radius, 2);

        // The mouths are redirects since FD-09-R2 (F4-c): a ball at a fielder's spot is in no mouth, a fly coming down
        // into the centre one is, by night only, and no other park has one there.
        BallHazards Mouths(Park park) { var b = new BallHazards(); b.Begin(park, true, Game.Rules); return b; }
        foreach (var pos in new[] { "LF", "CF", "RF" })
        {
            var spot = Game.Rules.Fielders.Spot(pos);
            Assert.Null(Mouths(funfair).Entered(spot.X, 6, spot.Z));
        }
        Assert.NotNull(Mouths(funfair).Entered(centre.X, 6, centre.Z));
        Assert.Null(Mouths(funfairByDay).Entered(centre.X, 6, centre.Z));
        foreach (var id in ParkIds.Where(p => p != ParkId.Funfair))
            Assert.Null(Mouths(PlayedPark.Of(Game.Parks[id], night: true, hazards: true, Game.Rules.Hazards)).Entered(centre.X, 6, centre.Z));
    }

    /// <summary>
    /// A barrel's capture disc is radius + reachPadFt, and the pad is the larger part of it, so the pad takes the fence
    /// scale with the radii: a Canopy barrel's disc is 3.50 + 5.60 = 9.10 ft, 0.70 of the full-size 13.00 (a Funfair
    /// warp can's 8.40, of 12.00).
    /// </summary>
    [Fact]
    public void ABarrelsCaptureDiscIsOnTheFenceScale()
    {
        foreach (var (id, type, disc) in new[] { (ParkId.Canopy, "barrel", 9.10), (ParkId.Funfair, "warp_pipe", 8.40) })
        {
            var park = Game.Parks[id];
            var hazard = park.Hazards.First(h => h.Type == type);
            Assert.Equal(5.6, Game.Rules.Hazards.Of(type).ReachPadFt);
            Assert.Equal(disc, hazard.Radius + Game.Rules.Hazards.Of(type).ReachPadFt, 2);
            var mouths = new BallHazards();
            mouths.Begin(park, false, Game.Rules);
            Assert.NotNull(mouths.Entered(hazard.X + disc - 0.1, 0, hazard.Z));
            Assert.Null(mouths.Entered(hazard.X + disc + 0.1, 0, hazard.Z));
        }

        // Ember's fire breath: 11.2 by day, 17.92 at night — the multiplier is dimensionless and the radius under it moved.
        var ember = Game.Parks[ParkId.Ember];
        var fire = ember.Hazards.Single(h => h.Type == "fire_breath");
        Assert.Equal(11.2, fire.Radius);
        Assert.Equal(17.92, fire.Radius * Game.Rules.Hazards.Of(HazardType.FireBreath).NightRadiusMul, 2);
        Assert.True(ParkHazards.InSlow(ember, fire.X + 11.1, fire.Z, Game.Rules, night: false));
        Assert.False(ParkHazards.InSlow(ember, fire.X + 11.3, fire.Z, Game.Rules, night: false));
        Assert.True(ParkHazards.InSlow(ember, fire.X + 17.9, fire.Z, Game.Rules, night: true));
        Assert.False(ParkHazards.InSlow(ember, fire.X + 18.0, fire.Z, Game.Rules, night: true));
    }

    static double Exit(BattingRules b, int power, double qualityMul, double starMul = 1.0) =>
        Math.Round((b.Exit.BaseMph + power * b.Exit.MphPerPower) * qualityMul * starMul, 1);

    static double Launch(BattingRules b, int power, bool charged, double lift) =>
        Math.Round(b.Launch.LoftBaseDeg + (power - 5) * b.Launch.LoftPerPower + (charged ? b.Charge.LoftDeg : 0) + lift, 1);
}
