using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-1 (#717): the compact field and the ball that fits it, authored into the <c>c80</c> overlay
/// (#716). Eight files in one commit, because drag is global and park dimensions are not — at drag
/// 0.0040 the best swing in the game carries 304 ft, so drag alone against the shipped 330-ft poles
/// is a game with no home runs in it, and the parks alone are a derby.
///
/// <para>
/// What these tests can and cannot see. The overlay rides on the root the <em>environment</em>
/// resolves, so a catalog loaded here plays the trial's parks and the trial's drag while
/// <see cref="Diamond"/> — which reads the one process-wide <see cref="Rules.Default"/> — keeps the
/// shipped 90-ft infield. That is #711's design, not an oversight: the diamond is global so parallel
/// tests cannot pull it out from under each other. So the infield is checked here as authored data
/// and exercised by running <c>cli</c> under <c>GRAND_SLUGGERS_TRIAL</c>, which is a second process
/// and the comparison the trial README prescribes.
/// </para>
/// </summary>
public sealed class CompactGeometryTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static string Shipped => Control.Root.Shipped;
    static string Overlay => Path.GetFullPath(Path.Combine(Shipped, "..", "trials", "c80"));
    static readonly DataRoot Root = new(Control.Root.Shipped, Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80")));
    static readonly ContentCatalog Trial = ContentCatalog.Load(Root);

    /// <summary>The basepath scale: 80 ft over the historical 90.</summary>
    const double Infield = 80.0 / 90.0;

    /// <summary>The one fence scale, for all six parks (F693-04-park-migration).</summary>
    const double Outfield = 0.70;

    /// <summary>
    /// The migrated infield lip (<b>#728</b>): the shipped 155 ft on the basepath scale, rounded to
    /// the two decimals <c>data/</c> spells. Derived rather than typed, so it cannot drift from the
    /// scale the rest of this file checks.
    /// </summary>
    static readonly double MigratedLip = Math.Round(155 * Infield, 2);

    static readonly string[] ParkIds =
        ["canopy-yard", "crystal-rink", "ember-keep", "funfair-park", "harbor-diamond", "rooftop-city"];

    // ---------------------------------------------------------------------------------
    // What the overlay carries
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Thirteen files, named. The tree is the declaration (#716), so this list is also the answer to
    /// "what is this trial changing?" — and a slice that quietly carried a fourteenth would show up
    /// here rather than in a trace nobody could attribute. It was eight until #725 added the fielder
    /// starts, nine until #732 carried the fielding table for the hazard reach pad, ten until #722
    /// carried the CPU table for the throw reads, eleven until #732 carried the running table for
    /// the tag-up race, and twelve until #718 carried the role players so three of them could hold
    /// Ball Dash; the count is in the name so growing it is a rename somebody has to mean.
    /// </summary>
    [Fact]
    public void TheTrialCarriesThirteenFilesAndNoOthers()
    {
        Assert.Equal(
            [
                "characters/role-players.json",
                "parks/canopy-yard.json", "parks/crystal-rink.json", "parks/ember-keep.json",
                "parks/funfair-park.json", "parks/harbor-diamond.json", "parks/rooftop-city.json",
                "rules/cpu.json", "rules/fielders.json", "rules/fielding.json", "rules/flight.json", "rules/infield.json",
                "rules/running.json"
            ],
            Root.Overrides);
    }

    /// <summary>
    /// 80-ft basepaths. One file, because #711 made the infield global and every park shares it.
    /// The bags are the shipped rounded 63.64 / 127.28 multiplied by 80/90 and kept at the two
    /// decimals <c>data/</c> already spells them in; the mound follows the same factor, which is the
    /// 53.78 ft the research names for C80.
    /// </summary>
    [Fact]
    public void TheTrialInfieldIsTheEightyFootDiamond()
    {
        var trial = Trial.Rules.Infield;
        Assert.Equal(80, trial.BaselineFt);
        Assert.Equal(53.78, trial.MoundFt);
        Assert.Equal(56.57, trial.CornerFt);
        Assert.Equal(113.14, trial.SecondFt);

        // Each is the shipped number through the one factor, to the hundredth of a foot.
        var shipped = Control.Rules.Infield;
        Assert.Equal(90, shipped.BaselineFt);
        Assert.Equal(60.5, shipped.MoundFt);
        Assert.Equal(63.64, shipped.CornerFt);
        Assert.Equal(127.28, shipped.SecondFt);
        Assert.Equal(Math.Round(shipped.BaselineFt * Infield, 2), trial.BaselineFt);
        Assert.Equal(Math.Round(shipped.MoundFt * Infield, 2), trial.MoundFt);
        Assert.Equal(Math.Round(shipped.CornerFt * Infield, 2), trial.CornerFt);
        Assert.Equal(Math.Round(shipped.SecondFt * Infield, 2), trial.SecondFt);

        // A corner rotated back off the diagonal is still the basepath, and second is still two corners.
        Assert.Equal(80, trial.CornerFt * Math.Sqrt(2), 2);
        Assert.Equal(trial.SecondFt, trial.CornerFt * 2, 6);
    }

    /// <summary>
    /// <b>The ground follows the bags (#729).</b> The drawn grass diamond and the back arc of the
    /// dirt are measured from second, the mound and the rubber, so they take the basepath scale like
    /// everything else in this file. The rest of <see cref="ParkDiamond"/> — path width, the bag
    /// pads, the home pad, the mound table, the warning track — is bodies and equipment and stays in
    /// feet, which is the same rule that kept the wall heights at 8 ft.
    ///
    /// <para>
    /// What the migration buys, measured: the dirt's far edge comes in from 145.78 ft to 135.56,
    /// which is the shipped 152.50 through the same factor, and that is what puts the migrated lip
    /// back outside the dirt instead of 8 ft inside it.
    /// </para>
    ///
    /// <para>
    /// And what it repairs. Before this, the compact field drew a 90-ft dress on 80-ft bags, which
    /// left the grass vertex 6.57 ft from the bag it points at — <i>inside</i> the 12-ft bag pad, by
    /// 5.43 ft. The grass was drawn over the dirt the bag sits on. Migrated, the gap is 12.13 ft and
    /// the vertex clears the pad again, though only by 0.13 ft against the shipped 1.64: the pad is
    /// equipment and did not shrink. Pinned because that margin is a hairline, and because the
    /// validator deliberately does not refuse a table over it.
    /// </para>
    /// </summary>
    [Fact]
    public void TheTrialGroundDressIsTheEightyFootDiamondsDress()
    {
        var trial = Trial.Rules.Infield;
        var shipped = Control.Rules.Infield;

        Assert.Equal(44.44, trial.InnerHalfFt);
        Assert.Equal(81.78, trial.BackArcFt);
        Assert.Equal(50, shipped.InnerHalfFt);
        Assert.Equal(92, shipped.BackArcFt);
        Assert.Equal(Math.Round(shipped.InnerHalfFt * Infield, 2), trial.InnerHalfFt);
        Assert.Equal(Math.Round(shipped.BackArcFt * Infield, 2), trial.BackArcFt);

        // The dirt's far edge, on both fields, through the one factor.
        Assert.Equal(152.50, shipped.MoundFt + shipped.BackArcFt, 2);
        Assert.Equal(135.56, trial.MoundFt + trial.BackArcFt, 2);
        Assert.Equal(Math.Round((shipped.MoundFt + shipped.BackArcFt) * Infield, 2),
            trial.MoundFt + trial.BackArcFt, 2);

        // The vertex-to-bag gap, and the pad clearance it leaves. The pad is 12 ft on both fields.
        Assert.Equal(13.64, shipped.CornerFt - shipped.InnerHalfFt, 2);
        Assert.Equal(12.13, trial.CornerFt - trial.InnerHalfFt, 2);
        Assert.Equal(1.64, shipped.CornerFt - (shipped.InnerHalfFt + ParkDiamond.BagPadR), 2);
        Assert.Equal(0.13, trial.CornerFt - (trial.InnerHalfFt + ParkDiamond.BagPadR), 2);
        Assert.True(trial.InnerHalfFt + ParkDiamond.BagPadR < trial.CornerFt,
            "the compact grass vertex must still clear the bag pad it points at");

        // The defect this closes: the un-migrated dress on compact bags put the grass vertex 6.57 ft
        // from the bag, which is 5.43 ft inside the pad — grass drawn over the dirt the bag sits on.
        Assert.Equal(6.57, trial.CornerFt - shipped.InnerHalfFt, 2);
        Assert.Equal(-5.43, trial.CornerFt - (shipped.InnerHalfFt + ParkDiamond.BagPadR), 2);

        // The validator would NOT have said so, and that is deliberate. Its rule is vertex <= corner,
        // and 50 <= 56.57 holds; only a pad-clearance rule catches this, and a pad-clearance rule
        // would refuse a C70 trial that scaled perfectly well (35 + 12 = 47 against a 44.55 corner),
        // because the pad is equipment and does not scale. The test carries it instead of the table.
        Assert.True(shipped.InnerHalfFt <= trial.CornerFt,
            "the ordering rule passes on the un-migrated dress, which is why this test exists");
        Assert.True(shipped.InnerHalfFt + ParkDiamond.BagPadR > trial.CornerFt,
            "a pad-clearance rule is the only one that would have caught it");

        // #729's own claim, which #725 and #732 did not change: the dress rides in infield.json rather
        // than a file of its own. Named rather than counted, because the total is
        // TheTrialCarriesThirteenFilesAndNoOthers's to say — it went from eight to nine when #725 added
        // rules/fielders.json, to ten when #732 added rules/fielding.json, to eleven when #722 added
        // rules/cpu.json, to twelve when #732 added rules/running.json and to thirteen when #718 added
        // characters/role-players.json, and this line used to assert that count a second time.
        Assert.Equal(
            ["rules/cpu.json", "rules/fielders.json", "rules/fielding.json", "rules/flight.json", "rules/infield.json", "rules/running.json"],
            Root.Overrides.Where(f => f.StartsWith("rules/", StringComparison.Ordinal)));
    }

    /// <summary>
    /// The accepted table, spelled out rather than recomputed: a fence list derived by the same
    /// arithmetic the authoring used would move with it and prove nothing.
    /// </summary>
    [Theory]
    [InlineData("canopy-yard", 218, 265, 223)]
    [InlineData("crystal-rink", 224, 270, 224)]
    [InlineData("ember-keep", 237, 286, 237)]
    [InlineData("funfair-park", 220, 273, 238)]
    [InlineData("harbor-diamond", 232, 280, 232)]
    [InlineData("rooftop-city", 223, 272, 225)]
    public void EachParkPlaysItsAcceptedCompactFences(string id, int left, int center, int right)
    {
        var park = Trial.Parks[id];
        Assert.Equal(left, park.LeftFenceFt);
        Assert.Equal(center, park.CenterFenceFt);
        Assert.Equal(right, park.RightFenceFt);
    }

    /// <summary>
    /// One scale for all six is what preserves each park's identity and their order. Re-deriving each
    /// park from Harbor's ratios would flatten them into the same stadium, so every fence is the
    /// shipped fence through 0.70 — Harbor's poles excepted, where the accepted 232 wins over the
    /// 231 a flat multiply gives and that one foot is rounding.
    /// </summary>
    [Fact]
    public void OneScaleMovesEveryFenceAndTheParksKeepTheirOrder()
    {
        foreach (var id in ParkIds)
        {
            var shipped = Control.Parks[id];
            var trial = Trial.Parks[id];
            var expectedLeft = id == "harbor-diamond" ? 232 : (int)Math.Round(shipped.LeftFenceFt * Outfield, MidpointRounding.ToEven);
            var expectedRight = id == "harbor-diamond" ? 232 : (int)Math.Round(shipped.RightFenceFt * Outfield, MidpointRounding.ToEven);
            Assert.Equal(expectedLeft, trial.LeftFenceFt);
            Assert.Equal((int)Math.Round(shipped.CenterFenceFt * Outfield, MidpointRounding.ToEven), trial.CenterFenceFt);
            Assert.Equal(expectedRight, trial.RightFenceFt);
        }

        // Harbor is the only exception, and only at the poles: 330 x 0.70 is 231, the accepted number is 232.
        Assert.Equal(231, (int)Math.Round(Control.Parks["harbor-diamond"].LeftFenceFt * Outfield, MidpointRounding.ToEven));
        Assert.Equal(232, Trial.Parks["harbor-diamond"].LeftFenceFt);

        // The parks keep their order. Asserted against the literal ranking rather than against the
        // shipped one re-sorted: a monotone scale preserves order necessarily, so comparing the two
        // sorted lists restates the loop above and could not fail on its own.
        string[] bySize = ["canopy-yard", "crystal-rink", "rooftop-city", "funfair-park", "harbor-diamond", "ember-keep"];
        Assert.Equal(bySize, ParkIds.OrderBy(id => Control.Parks[id].CenterFenceFt).ToArray());
        Assert.Equal(bySize, ParkIds.OrderBy(id => Trial.Parks[id].CenterFenceFt).ToArray());

        // The tight pair is the one rounding could have swapped: 3 ft apart before, 2 ft after.
        Assert.Equal(3, Control.Parks["rooftop-city"].CenterFenceFt - Control.Parks["crystal-rink"].CenterFenceFt);
        Assert.Equal(2, Trial.Parks["rooftop-city"].CenterFenceFt - Trial.Parks["crystal-rink"].CenterFenceFt);
    }

    /// <summary>
    /// Wall heights do not scale, because bodies did not shrink. An 8-ft wall stays 8 ft and Crystal
    /// Rink and Funfair Park simply stay the friendlier parks they already are. Wind and the night
    /// window are not this slice's either.
    /// </summary>
    [Fact]
    public void WallsWindAndTheNightWindowAreTheShippedOnes()
    {
        foreach (var id in ParkIds)
        {
            var shipped = Control.Parks[id];
            var trial = Trial.Parks[id];
            Assert.Equal(shipped.FenceHeightFt, trial.FenceHeightFt);
            Assert.Equal(shipped.WindMph, trial.WindMph);
            Assert.Equal(shipped.WindDeg, trial.WindDeg);
            Assert.Equal(shipped.NightContactWindowMul, trial.NightContactWindowMul);
            Assert.Equal(shipped.Surface, trial.Surface);
            Assert.Equal(shipped.Faction, trial.Faction);
        }
        Assert.Equal(8, Trial.Parks["crystal-rink"].FenceHeightFt);
        Assert.Equal(12, Trial.Parks["harbor-diamond"].FenceHeightFt);
    }

    /// <summary>
    /// A hazard migrates by the scale of the zone it sits in: the basepath scale inside the infield
    /// lip, the fence scale beyond it. Leaving them at absolute positions would drift them relative
    /// to everything around them — an infield barrel would end up deeper into a shallower infield.
    /// The zone is decided by <see cref="FieldingResolver.OutfieldGrass"/>, the same radial lip the fielders
    /// use to decide who owns a ball there, so the rule is the game's own and not a second opinion.
    ///
    /// <para><b>Radii take the fence scale (#732, #730 decision 4).</b> #717 shipped them unscaled —
    /// "a barrel is a physical object; it did not shrink, for the same reason a wall did not" — and
    /// #730 measured the code instead: a barrel's capture test is <c>radius + pipeReachPadFt</c>, a
    /// fire breath's radius is × 1.6 at night, a freeze triggers on where the <i>ball</i> lands, and a
    /// <c>climb_wall</c> radius is never read at all. A radius is a trigger zone, not a body. Fair
    /// territory falls to 0.70² = 49% of shipped, so an unscaled zone roughly doubles its share of the
    /// field; × 0.70 preserves each hazard's share exactly, and unlike the positions it takes the
    /// fence factor everywhere, infield barrels included. The shipped radii stay on the record in
    /// <c>data/parks/</c>, and this compares against them.</para>
    /// </summary>
    [Fact]
    public void HazardsMigrateByTheZoneTheySitInAndTheirRadiiTakeTheFenceScale()
    {
        foreach (var id in ParkIds)
        {
            var shipped = Control.Parks[id].Hazards;
            var trial = Trial.Parks[id].Hazards;
            Assert.Equal(shipped.Count, trial.Count);
            for (var i = 0; i < shipped.Count; i++)
            {
                var was = shipped[i];
                var now = trial[i];
                var where = $"{id} hazard[{i}] {was.Type}";
                var scale = FieldingResolver.OutfieldGrass(was.X, was.Z, Control.Rules) ? Outfield : Infield;
                Assert.Equal(was.Type, now.Type);
                Assert.Equal(was.Tag, now.Tag);
                Assert.Equal(Math.Round(was.Radius * Outfield, 2), now.Radius, 6);
                Assert.Equal(Math.Round(was.X * scale, MidpointRounding.AwayFromZero), now.X, 6);
                Assert.Equal(Math.Round(was.Z * scale, MidpointRounding.AwayFromZero), now.Z, 6);
                Assert.True(now.Radius > 0 || was.Type == "train", where);
            }
        }

        // The eight Canopy hazards are the split the research describes: three barrels on the dirt,
        // five on the grass, and the lip decides which factor each one takes.
        var canopy = Control.Parks["canopy-yard"].Hazards;
        Assert.Equal(3, canopy.Count(h => !FieldingResolver.OutfieldGrass(h.X, h.Z, Control.Rules)));
        Assert.Equal(5, canopy.Count(h => FieldingResolver.OutfieldGrass(h.X, h.Z, Control.Rules)));

        // Rooftop's AC unit is the one the lip decides narrowly — 155.2 ft from home against the
        // SHIPPED 155-ft lip — so it takes the fence factor and lands at (28, 105) rather than
        // (36, 133). Two lips exist since #728, and the migration rule reads the shipped one: it asks
        // where a hazard stood on the historical field, not where it will stand on the compact one.
        // No shipped hazard has a radius in [137.78, 155), so re-deriving against the migrated lip
        // would produce byte-identical positions and no park file needs touching.
        var ac = Control.Parks["rooftop-city"].Hazards.Single(h => h.Type == "ac_unit");
        Assert.Equal(155.24, Diamond.Dist(0, 0, ac.X, ac.Z), 2);
        Assert.True(FieldingResolver.OutfieldGrass(ac.X, ac.Z, Control.Rules));
        var migrated = Trial.Parks["rooftop-city"].Hazards.Single(h => h.Type == "ac_unit");
        Assert.Equal(28, migrated.X);
        Assert.Equal(105, migrated.Z);
        Assert.Equal(6, ac.Radius);
        Assert.Equal(4.2, migrated.Radius);

        // Harbor is the reference park and has no hazards, so it is unaffected by the rule either way.
        Assert.Empty(Trial.Parks["harbor-diamond"].Hazards);
    }

    /// <summary>
    /// <b>The reach pad scales with the radii, and it is the part that matters (#732, #730 decision 4).</b>
    /// A barrel or pipe catches a ball inside <c>radius + park.pipeReachPadFt</c>, and the pad is
    /// larger than any barrel's radius. Scaling the radius alone would have taken a Canopy barrel's
    /// real catch from 13.00 ft to 11.50 — an 11.5% cut on a field that lost 30%. With the pad at 5.60
    /// the disc is 3.50 + 5.60 = 9.10 ft, exactly 0.70 of shipped. <c>emberNightFireMul</c> is
    /// dimensionless and does not move.
    ///
    /// <para>
    /// This is why <c>rules/fielding.json</c> enters the overlay here, whole, ahead of the two 3c
    /// chains that will write it (#727): every leaf that is not named below is the shipped value,
    /// checked by name so a copy that dropped a key or drifted a number is refused rather than read
    /// as authored. #722 slice 1 added the throw clock's five values to the named list.
    /// </para>
    /// </summary>
    [Fact]
    public void TheTrialFieldingTableIsTheShippedOneWithThePadOnTheFenceScale()
    {
        var shipped = Control.Rules.Fielding.Park;
        var trial = Trial.Rules.Fielding.Park;
        Assert.Equal(8, shipped.PipeReachPadFt);
        Assert.Equal(5.6, trial.PipeReachPadFt);
        Assert.Equal(Math.Round(shipped.PipeReachPadFt * Outfield, 2), trial.PipeReachPadFt);
        Assert.Equal(1.6, trial.EmberNightFireMul);
        Assert.Equal(shipped.EmberNightFireMul, trial.EmberNightFireMul);
        Assert.Equal(shipped.ShellWarpChance, trial.ShellWarpChance);

        var shippedLeaves = Leaves(Path.Combine(Shipped, "rules", "fielding.json"));
        var trialLeaves = Leaves(Path.Combine(Overlay, "rules", "fielding.json"));
        Assert.Equal(shippedLeaves.Keys, trialLeaves.Keys);
        var moved = shippedLeaves.Where(kv => trialLeaves[kv.Key] != kv.Value).Select(kv => kv.Key).ToList();
        // #732's pad; #722's throw clock (slice 1: release, travel, the long-throw loss, the bad pair's speed and
        // no slant; slice 2: the forced-relay ceiling set to never); #718's speed, reads and cover at contact, and its
        // retired universal sprint (Ball Dash's own multiple is 1.20 in both roots; the roster is what differs).
        // Nothing else in the table moves, and a new difference has to be named here.
        Assert.Equal(
            [
                "abilities.laserHomeOnly", "abilities.laserMul", "abilities.snapThrowMul",
                "catch.autoDive", "catch.diveRecoveryFieldCut", "catch.diveRecoverySec", "catch.jumpAirSec", "catch.jumpBufferSec", "catch.jumpReachFt", "catch.standUpReachFt",
                "chase.accelSec", "chase.baseFtPerSec", "chase.brakeSec", "chase.ftPerSecPerRun", "chase.infieldAirMul", "chase.minFtPerSec", "chase.outfieldAirMul",
                "chem.badSpeedMul", "chem.slantChance",
                "cover.chaseSpeedWeight", "cover.lockoutMul", "cover.startSec",
                "dash.chaseMul",
                "park.pipeReachPadFt",
                "reaction.catcherSec", "reaction.firstSec", "reaction.outfieldSec", "reaction.pitcherSec", "reaction.shortSec", "reaction.thirdSec",
                "stick.enterMag", "stick.leaveMag",
                "throw.baseFtPerSec", "throw.longThrowLossSec", "throw.onTheFlyFt", "throw.relayAutoContinue", "throw.relayBufferSec", "throw.releaseSec"
            ],
            moved);
        // #723: the abilities and the relay's ownership. snapReleaseSec does not move — 0.22 in both, inert beside the shipped release.
        Assert.Equal(("1.45", "1.25"), (shippedLeaves["abilities.laserMul"], trialLeaves["abilities.laserMul"]));
        Assert.Equal(("1.22", "1.0"), (shippedLeaves["abilities.snapThrowMul"], trialLeaves["abilities.snapThrowMul"]));
        Assert.Equal(("0", "1"), (shippedLeaves["abilities.laserHomeOnly"], trialLeaves["abilities.laserHomeOnly"]));
        Assert.Equal(shippedLeaves["abilities.snapReleaseSec"], trialLeaves["abilities.snapReleaseSec"]);
        Assert.Equal(("1", "0"), (shippedLeaves["throw.relayAutoContinue"], trialLeaves["throw.relayAutoContinue"]));
        Assert.Equal(("0", "0.25"), (shippedLeaves["throw.relayBufferSec"], trialLeaves["throw.relayBufferSec"]));
        // #718 (F693-02-ball-dash-carrier): the universal East-held sprint is retired on the trial; the ability's own multiple does not move.
        Assert.Equal(("1.35", "1.0"), (shippedLeaves["dash.chaseMul"], trialLeaves["dash.chaseMul"]));
        Assert.Equal(("1.20", "1.20"), (shippedLeaves["abilities.ballDashMul"], trialLeaves["abilities.ballDashMul"]));
        // #719 (slice 3): the normal jump — a physical arc on the trial, the arm window shipped; the rise and the air response are the accepted anchors in both.
        Assert.Equal(("0", "0.60"), (shippedLeaves["catch.jumpAirSec"], trialLeaves["catch.jumpAirSec"]));
        Assert.Equal(("0", "0.10"), (shippedLeaves["catch.jumpBufferSec"], trialLeaves["catch.jumpBufferSec"]));
        Assert.Equal(("8", "0"), (shippedLeaves["catch.jumpReachFt"], trialLeaves["catch.jumpReachFt"]));
        Assert.Equal(("2.0", "2.0"), (shippedLeaves["catch.jumpRiseFt"], trialLeaves["catch.jumpRiseFt"]));
        Assert.Equal(("0.10", "0.10"), (shippedLeaves["catch.jumpAirResponseMul"], trialLeaves["catch.jumpAirResponseMul"]));
        Assert.Equal(shippedLeaves["catch.jumpArmSec"], trialLeaves["catch.jumpArmSec"]);
        // #719 (slice 2): nobody dives for free — the automatic dive off, the recovery cost on.
        Assert.Equal(("1", "0"), (shippedLeaves["catch.autoDive"], trialLeaves["catch.autoDive"]));
        Assert.Equal(("0", "0.60"), (shippedLeaves["catch.diveRecoverySec"], trialLeaves["catch.diveRecoverySec"]));
        Assert.Equal(("0", "0.025"), (shippedLeaves["catch.diveRecoveryFieldCut"], trialLeaves["catch.diveRecoveryFieldCut"]));
        Assert.Equal(shippedLeaves["catch.diveArmSec"], trialLeaves["catch.diveArmSec"]);
        // #719 (slice 1): the authored stand-up reach, and the hit-class multipliers retired with it.
        Assert.Equal(("0", "6.0"), (shippedLeaves["catch.standUpReachFt"], trialLeaves["catch.standUpReachFt"]));
        Assert.Equal(("0.6", "1.0"), (shippedLeaves["chase.outfieldAirMul"], trialLeaves["chase.outfieldAirMul"]));
        Assert.Equal(("0.45", "1.0"), (shippedLeaves["chase.infieldAirMul"], trialLeaves["chase.infieldAirMul"]));
        Assert.Equal(shippedLeaves["catch.windowPadFt"], trialLeaves["catch.windowPadFt"]);
        Assert.Equal(shippedLeaves["catch.diveReachFt"], trialLeaves["catch.diveReachFt"]);
        // #718 (the human seat): the calibrated radial stick's gates; the calibration window is the accepted one in both roots.
        Assert.Equal(("0", "0.20"), (shippedLeaves["stick.enterMag"], trialLeaves["stick.enterMag"]));
        Assert.Equal(("0", "0.15"), (shippedLeaves["stick.leaveMag"], trialLeaves["stick.leaveMag"]));
        Assert.Equal(shippedLeaves["stick.calibrationSec"], trialLeaves["stick.calibrationSec"]);
        Assert.Equal(shippedLeaves["stick.centerOffsetMax"], trialLeaves["stick.centerOffsetMax"]);
        Assert.Equal(shippedLeaves["stick.sampleSpreadMax"], trialLeaves["stick.sampleSpreadMax"]);
        Assert.Equal(("21", "12.4"), (shippedLeaves["chase.baseFtPerSec"], trialLeaves["chase.baseFtPerSec"]));
        Assert.Equal(("0", "0.20"), (shippedLeaves["chase.accelSec"], trialLeaves["chase.accelSec"]));
        Assert.Equal(("0", "0.10"), (shippedLeaves["chase.brakeSec"], trialLeaves["chase.brakeSec"]));
        Assert.Equal(("0.83", "0.40"), (shippedLeaves["reaction.outfieldSec"], trialLeaves["reaction.outfieldSec"]));
        Assert.Equal(("0.23", "0"), (shippedLeaves["cover.startSec"], trialLeaves["cover.startSec"]));
        Assert.Equal(("1", "0"), (shippedLeaves["cover.lockoutMul"], trialLeaves["cover.lockoutMul"]));
        Assert.Equal(("0", "1"), (shippedLeaves["cover.chaseSpeedWeight"], trialLeaves["cover.chaseSpeedWeight"]));
        // secondSec was already 0.25 and does not move: the infield's one number is the number second already had.
        Assert.Equal(shippedLeaves["reaction.secondSec"], trialLeaves["reaction.secondSec"]);
        Assert.Equal(("200", "9999"), (shippedLeaves["throw.onTheFlyFt"], trialLeaves["throw.onTheFlyFt"]));
        Assert.Equal(("8", "5.6"), (shippedLeaves["park.pipeReachPadFt"], trialLeaves["park.pipeReachPadFt"]));
        Assert.Equal(("0.22", "0.30"), (shippedLeaves["throw.releaseSec"], trialLeaves["throw.releaseSec"]));
        Assert.Equal(("100", "88.89"), (shippedLeaves["throw.baseFtPerSec"], trialLeaves["throw.baseFtPerSec"]));
        Assert.Equal(("0", "0.60"), (shippedLeaves["throw.longThrowLossSec"], trialLeaves["throw.longThrowLossSec"]));
        Assert.Equal(("1.0", "0.90"), (shippedLeaves["chem.badSpeedMul"], trialLeaves["chem.badSpeedMul"]));
        Assert.Equal(("0.20", "0"), (shippedLeaves["chem.slantChance"], trialLeaves["chem.slantChance"]));

    }

    /// <summary>
    /// <b>The CPU table enters the overlay for the throw reads (#722 slice 2, decision 6 of #730).</b> Four
    /// numbers per rung: how much a relay must save before a fielder throws through the cutoff, and how
    /// much of the thrower's arm, of the fielder's relay and of the pair chemistry the CPU forecasts.
    /// The shipped rungs read none of it and set the bias to a value no relay can save, which is today's
    /// rule; the trial reads everything on normal and hard, half the arm and no relay on easy, and biases
    /// 0.3 / 0.1 / 0. Every other leaf — the windows, the reactions, the steal chances, the margins — is
    /// the shipped value, and the active rung is still normal.
    /// </summary>
    [Fact]
    public void TheTrialCpuTableIsTheShippedOneWithTheThrowReadsSwitchedOn()
    {
        var shipped = Leaves(Path.Combine(Shipped, "rules", "cpu.json"));
        var trial = Leaves(Path.Combine(Overlay, "rules", "cpu.json"));
        Assert.Equal(shipped.Keys, trial.Keys);
        Assert.Equal("\"normal\"", trial["level"]);
        var moved = shipped.Where(kv => trial[kv.Key] != kv.Value).Select(kv => kv.Key).ToList();
        Assert.Equal(
            [
                "easy.relayBiasSec", "easy.runnerReadsArm",
                "hard.readsChemistry", "hard.relayBiasSec", "hard.runnerReadsArm", "hard.runnerReadsRelay",
                "normal.readsChemistry", "normal.relayBiasSec", "normal.runnerReadsArm", "normal.runnerReadsRelay"
            ],
            moved);
        foreach (var rung in new[] { "easy", "normal", "hard" })
        {
            Assert.Equal("99", shipped[rung + ".relayBiasSec"]);
            Assert.Equal("0", shipped[rung + ".runnerReadsArm"]);
            Assert.Equal("0", shipped[rung + ".runnerReadsRelay"]);
            Assert.Equal("0", shipped[rung + ".readsChemistry"]);
        }
        Assert.Equal(("0.3", "0.5", "0", "0"), (trial["easy.relayBiasSec"], trial["easy.runnerReadsArm"], trial["easy.runnerReadsRelay"], trial["easy.readsChemistry"]));
        Assert.Equal(("0.1", "1", "1", "1"), (trial["normal.relayBiasSec"], trial["normal.runnerReadsArm"], trial["normal.runnerReadsRelay"], trial["normal.readsChemistry"]));
        Assert.Equal(("0", "1", "1", "1"), (trial["hard.relayBiasSec"], trial["hard.runnerReadsArm"], trial["hard.runnerReadsRelay"], trial["hard.readsChemistry"]));

        var rules = Trial.Rules.Cpu;
        Assert.Equal(0.3, rules.Easy.RelayBiasSec);
        Assert.Equal(0.1, rules.Normal.RelayBiasSec);
        Assert.Equal(0, rules.Hard.RelayBiasSec);
        Assert.Equal(99, Control.Rules.Cpu.Normal.RelayBiasSec);
        Assert.Equal(0, Control.Rules.Cpu.Hard.RunnerReadsRelay);
    }

    /// <summary>
    /// <b>The running table enters the overlay for the tag-up race (#732, decision 5 of #730), and its clock
    /// does not move.</b> The two carry gates go to never and the two race thresholds are authored; every
    /// other leaf — <c>bagSec</c> above all, the C80 anchor — is the shipped value, checked by name.
    /// </summary>
    [Fact]
    public void TheTrialRunningTableIsTheShippedClockWithTheTagUpRaceSwitchedOn()
    {
        var shipped = Leaves(Path.Combine(Shipped, "rules", "running.json"));
        var trial = Leaves(Path.Combine(Overlay, "rules", "running.json"));
        Assert.Equal(shipped.Keys, trial.Keys);
        var moved = shipped.Where(kv => trial[kv.Key] != kv.Value).Select(kv => kv.Key).ToList();
        Assert.Equal(
            ["cpu.tagSecondMinCarryFt", "cpu.tagThirdMinCarryFt", "cpu.tagUpHomeMarginSec", "cpu.tagUpThirdMarginSec"],
            moved);
        Assert.Equal(("200", "9999"), (shipped["cpu.tagThirdMinCarryFt"], trial["cpu.tagThirdMinCarryFt"]));
        Assert.Equal(("250", "9999"), (shipped["cpu.tagSecondMinCarryFt"], trial["cpu.tagSecondMinCarryFt"]));
        Assert.Equal(("99", "0.25"), (shipped["cpu.tagUpHomeMarginSec"], trial["cpu.tagUpHomeMarginSec"]));
        Assert.Equal(("99", "0.07"), (shipped["cpu.tagUpThirdMarginSec"], trial["cpu.tagUpThirdMarginSec"]));
        foreach (var key in shipped.Keys.Where(k => k.StartsWith("bagSec.", StringComparison.Ordinal)))
            Assert.Equal(shipped[key], trial[key]);
        Assert.Equal("3.55", trial["bagSec.baseSec"]);
    }

    /// <summary>
    /// The disc a barrel actually catches with, through <see cref="ParkHazards.WarpIfPipe"/> rather
    /// than arithmetic: a ball 12.9 ft from a shipped Canopy barrel warps and 13.1 does not; 9.0 ft
    /// from the compact one warps and 9.2 does not. Funfair's pipes go 12.00 → 8.40 the same way.
    /// The compact park read with the <i>shipped</i> table still warps at 9.2, which is the packet's
    /// point: the pad, not the radius, decides the catch. Ember's fire breath follows through
    /// <see cref="ParkHazards.InSlow"/> — 16 → 11.2 by day, and 25.60 → 17.92 at night because the
    /// multiplier stayed and the radius under it moved.
    /// </summary>
    [Fact]
    public void ABarrelsCaptureDiscIsSeventyPercentOfTheShippedOne()
    {
        var rng = new Random(730);
        foreach (var (id, type, discShipped) in new[] { ("canopy-yard", "barrel", 13.00), ("funfair-park", "warp_pipe", 12.00) })
        {
            var shippedPark = Control.Parks[id];
            var trialPark = Trial.Parks[id];
            var was = shippedPark.Hazards.First(h => h.Type == type);
            var now = trialPark.Hazards.First(h => h.Type == type);
            var discTrial = Math.Round(discShipped * Outfield, 2);
            Assert.Equal(discShipped, was.Radius + Control.Rules.Fielding.Park.PipeReachPadFt, 2);
            Assert.Equal(discTrial, now.Radius + Trial.Rules.Fielding.Park.PipeReachPadFt, 2);
            // The radius alone: the 13.00 → 11.50 cut the packet warned about (12.00 → 10.80 for a pipe).
            Assert.Equal(Math.Round(was.Radius * Outfield + 8, 2), now.Radius + Control.Rules.Fielding.Park.PipeReachPadFt, 2);

            Assert.True(ParkHazards.WarpIfPipe(shippedPark, was.X + discShipped - 0.1, was.Z, rng, Control.Rules).Warped, id + " shipped, inside the disc");
            Assert.False(ParkHazards.WarpIfPipe(shippedPark, was.X + discShipped + 0.1, was.Z, rng, Control.Rules).Warped, id + " shipped, outside the disc");
            Assert.True(ParkHazards.WarpIfPipe(trialPark, now.X + discTrial - 0.1, now.Z, rng, Trial.Rules).Warped, id + " compact, inside the disc");
            Assert.False(ParkHazards.WarpIfPipe(trialPark, now.X + discTrial + 0.1, now.Z, rng, Trial.Rules).Warped, id + " compact, outside the disc");
            Assert.True(ParkHazards.WarpIfPipe(trialPark, now.X + discTrial + 0.1, now.Z, rng, Control.Rules).Warped, id + " compact radius under the shipped pad still catches it");
        }

        var ember = Trial.Parks["ember-keep"];
        var fire = ember.Hazards.Single(h => h.Type == "fire_breath");
        var shippedFire = Control.Parks["ember-keep"].Hazards.Single(h => h.Type == "fire_breath");
        Assert.Equal(16, shippedFire.Radius);
        Assert.Equal(11.2, fire.Radius);
        Assert.Equal(25.60, shippedFire.Radius * Control.Rules.Fielding.Park.EmberNightFireMul, 2);
        Assert.Equal(17.92, fire.Radius * Trial.Rules.Fielding.Park.EmberNightFireMul, 2);
        Assert.True(ParkHazards.InSlow(ember, fire.X + 11.1, fire.Z, night: false, Trial.Rules));
        Assert.False(ParkHazards.InSlow(ember, fire.X + 11.3, fire.Z, night: false, Trial.Rules));
        Assert.True(ParkHazards.InSlow(ember, fire.X + 17.9, fire.Z, night: true, Trial.Rules));
        Assert.False(ParkHazards.InSlow(ember, fire.X + 18.0, fire.Z, night: true, Trial.Rules));
    }

    /// <summary>
    /// Surgical on purpose: drag and the infield lip are the only fields that move. Cutting exit
    /// velocity instead would have reopened the exit table, the infield races and every accepted
    /// fielding anchor, so the check is the whole file rather than the two numbers — a trial writes
    /// whole files (#716), and a third field edited in passing would otherwise ride along unremarked.
    ///
    /// <para>
    /// The allow-list is two entries, not one, because <b>#728</b> landed: drag is global and the lip
    /// is geometry. Each is asserted by value before it is stripped, so widening the list cannot hide
    /// a wrong number — only a named one.
    /// </para>
    /// </summary>
    [Fact]
    public void TheTrialCarriesTheHeavierBallAndTheMigratedLipAndNothingElseInTheFlightTable()
    {
        Assert.Equal(0.0019, Control.Rules.Flight.Drag);
        Assert.Equal(0.0040, Trial.Rules.Flight.Drag);
        Assert.Equal(155, Control.Rules.Flight.Classes.InfieldLipFt);
        Assert.Equal(MigratedLip, Trial.Rules.Flight.Classes.InfieldLipFt);

        var shipped = Load(Path.Combine(Shipped, "rules", "flight.json"));
        var trial = Load(Path.Combine(Overlay, "rules", "flight.json"));
        Assert.Equal(0.0019, shipped["drag"]!.GetValue<double>());
        Assert.Equal(0.0040, trial["drag"]!.GetValue<double>());
        Assert.Equal(155, shipped["classes"]!["infieldLipFt"]!.GetValue<double>());
        Assert.Equal(MigratedLip, trial["classes"]!["infieldLipFt"]!.GetValue<double>());
        shipped.Remove("drag");
        trial.Remove("drag");
        shipped["classes"]!.AsObject().Remove("infieldLipFt");
        trial["classes"]!.AsObject().Remove("infieldLipFt");
        Assert.Equal(shipped.ToJsonString(), trial.ToJsonString());

        static JsonObject Load(string path) =>
            JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();
    }

    /// <summary>
    /// <b>The lip has a floor, and nothing in the rules enforces it.</b> A lip below a middle
    /// infielder's radius makes <see cref="FieldingResolver.OutfieldGrass"/> answer true where 2B and
    /// SS stand, so the fielding rules start calling them outfielders and put them on the outfield
    /// read. <c>FlightRules.Validate</c> only orders the four launch bands and marks the lip positive,
    /// so this is the only place the floor is written down.
    ///
    /// <para>
    /// This is what ruled out the fence scale in <b>#730</b>: 155 × 0.70 = 108.50 ft sits inside the
    /// middle infield at both the shipped starts and the scaled ones. The accepted 137.78 clears
    /// today's floor by 12.53 ft and <b>#725</b>'s floor by 26.45 ft, so the guard held before and
    /// after the starts migrated — which is why the two issues did not have to be ordered. Since
    /// #725 the second arm reads the trial's authored starts instead of scaling the shipped ones
    /// here, and lands on the same 111.33 ft.
    /// </para>
    /// </summary>
    [Fact]
    public void TheMigratedLipStaysOutsideEveryInfielderBeforeAndAfterTheStartsMigrate()
    {
        var lip = Trial.Rules.Flight.Classes.InfieldLipFt;
        Assert.Equal(MigratedLip, lip);

        foreach (var pos in FieldingResolver.InfieldPursuitPositions)
        {
            // The shipped starts, which is what this process plays (#711).
            var (x, z) = Diamond.Positions[pos];
            Assert.True(Diamond.Dist(0, 0, x, z) < lip,
                $"{pos} at ({x}, {z}) is outside the migrated lip {lip}");

            // The trial's own, authored in #725 rather than scaled by this test.
            var (tx, tz) = TrialStart(pos);
            Assert.True(Diamond.Dist(0, 0, tx, tz) < lip,
                $"{pos} at ({tx}, {tz}) is outside the migrated lip {lip}");
        }

        // The two floors, named. 2B/SS are the deep pair and set both of them.
        Assert.Equal(125.25, Radius("2B"), 2);
        Assert.Equal(111.33, TrialRadius("2B"), 2);
        Assert.Equal(12.53, lip - Radius("2B"), 2);
        Assert.Equal(26.45, lip - TrialRadius("2B"), 2);

        // The authored start lands where scaling the shipped one by the basepath predicted it would.
        Assert.Equal(Radius("2B") * Infield, TrialRadius("2B"), 2);

        // The fence scale would have broken it, which is why #730 did not take it.
        Assert.True(155 * Outfield < Radius("2B"),
            "a lip on the fence scale sits inside the middle infield");

        // And the outfield is on the far side under both roots, which is the rule's whole point.
        foreach (var pos in new[] { "LF", "CF", "RF" })
        {
            Assert.True(Radius(pos) > lip, $"{pos} should be past the lip");
            Assert.True(TrialRadius(pos) > lip, $"{pos} should be past the lip under the trial too");
        }

        static double Radius(string pos)
        {
            var (x, z) = Diamond.Positions[pos];
            return Diamond.Dist(0, 0, x, z);
        }
    }

    /// <summary>
    /// No exit-table change: <c>batting.json</c> is not carried, so every contact leaves the bat at
    /// exactly the speed it leaves at today. This is what lets the slice claim that only drag moved.
    /// #717 also asserted here that <c>running.json</c> was not carried at all; #732 carries it for the
    /// tag-up race (decision 5 of #730) with every clock key byte-identical, so the claim that assertion
    /// protected — the runner clock is the C80 anchor and does not move — is now checked on the values.
    /// </summary>
    [Fact]
    public void TheExitTableIsUntouchedAtEveryPowerAndQuality()
    {
        var control = Control.Rules.Batting;
        var trial = Trial.Rules.Batting;
        Assert.False(Root.Overridden("rules", "batting.json"));
        Assert.True(Root.Overridden("rules", "running.json"));
        var bag = Control.Rules.Running.BagSec;
        var trialBag = Trial.Rules.Running.BagSec;
        Assert.Equal(
            (bag.BaseSec, bag.SecPerRun, bag.MinSec, bag.MaxSec, bag.DashMul, bag.BatterStartSec, bag.NoPassFt),
            (trialBag.BaseSec, trialBag.SecPerRun, trialBag.MinSec, trialBag.MaxSec, trialBag.DashMul, trialBag.BatterStartSec, trialBag.NoPassFt));
        Assert.Equal(3.55, trialBag.BaseSec);

        for (var power = 1; power <= 10; power++)
            foreach (var (c, t) in new[]
                     {
                         (control.Quality.Slap.Sour, trial.Quality.Slap.Sour),
                         (control.Quality.Slap.Nice, trial.Quality.Slap.Nice),
                         (control.Quality.Slap.Perfect, trial.Quality.Slap.Perfect),
                         (control.Quality.Charge.Sour, trial.Quality.Charge.Sour),
                         (control.Quality.Charge.Nice, trial.Quality.Charge.Nice),
                         (control.Quality.Charge.Perfect, trial.Quality.Charge.Perfect)
                     })
                Assert.Equal(
                    (control.Exit.BaseMph + power * control.Exit.MphPerPower) * c,
                    (trial.Exit.BaseMph + power * trial.Exit.MphPerPower) * t,
                    9);
    }

    // ---------------------------------------------------------------------------------
    // The coupling, and what the ball does once both halves land
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Why these are one commit. The best swing in the game without a star — Power 10, a perfect
    /// charge, full lift — carries 304 ft at the trial's drag, against five un-migrated parks whose
    /// shortest pole is 312 ft. Ship the drag alone and every park except a migrated Harbor has no
    /// home runs in it at all.
    /// </summary>
    [Fact]
    public void TheBestSwingCarriesThreeHundredAndFourFeetAndNoShippedParkWouldHoldAHomeRun()
    {
        var b = Control.Rules.Batting;
        var exit = Exit(b, 10, b.Quality.Charge.Perfect);
        var launch = Launch(b, 10, charged: true, lift: b.Launch.StickDeg);
        Assert.Equal(122.5, exit);
        Assert.Equal(35.5, launch);

        Assert.Equal(451.3, BallFlight.CarryFeet(exit, launch, 0, Control.Rules), 1);
        Assert.Equal(304.0, BallFlight.CarryFeet(exit, launch, 0, Trial.Rules), 1);

        // That carry clears nothing in the shipped parks: the shortest shipped pole is Canopy's 312.
        Assert.Equal(312, ParkIds.Min(id => Math.Min(Control.Parks[id].LeftFenceFt, Control.Parks[id].RightFenceFt)));
        foreach (var id in ParkIds)
            Assert.False(BattedBall.Of(exit, launch, 0, Control.Parks[id], Trial.Rules).HomeRun, id);

        // And it clears every migrated park through the middle, which is the other half of the couple.
        foreach (var id in ParkIds)
            Assert.True(BattedBall.Of(exit, launch, 0, Trial.Parks[id], Trial.Rules).HomeRun, id);
    }

    /// <summary>
    /// The change must not remove the home run. A charged perfect swing still leaves a migrated yard
    /// through the middle from Power 9 up, and a star swing still clears centre in all six. Power 8
    /// is the boundary and Ember Keep is where it sits: 285 ft against the deepest centre in the set.
    /// </summary>
    [Fact]
    public void AChargedPerfectStillLeavesTheYardAndAStarSwingStillClearsCentre()
    {
        var b = Control.Rules.Batting;
        foreach (var id in ParkIds)
        {
            var park = Trial.Parks[id];
            for (var power = 9; power <= 10; power++)
            {
                var ball = BattedBall.Of(
                    Exit(b, power, b.Quality.Charge.Perfect),
                    Launch(b, power, charged: true, lift: b.Launch.StickDeg),
                    0, park, Trial.Rules);
                Assert.True(ball.HomeRun, $"{id} charged perfect P{power} landed {ball.LandingDist:F0}");
            }

            // Furnace is the biggest star swing in the table (exitVeloMul 1.25); a middle bat using it
            // clears centre in every migrated park.
            var star = StarSkills.SwingExitMul("furnace", Trial.StarSkills);
            Assert.Equal(1.25, star);
            var starBall = BattedBall.Of(
                Exit(b, 5, b.Quality.Charge.Perfect, star),
                StarSkills.SwingLaunchDeg("fly", Trial.StarSkills) ?? 38,
                0, park, Trial.Rules);
            Assert.True(starBall.HomeRun, $"{id} star swing landed {starBall.LandingDist:F0}");
        }
    }

    /// <summary>
    /// <b>The slice's claim does not survive measurement.</b> #717 says ordinary uncharged contact
    /// stays inside a migrated park at every Power rating. It does not, and the shortfall is wider
    /// than a nice slap alone shows.
    ///
    /// A <i>perfect</i> slap is equally ordinary uncharged contact — the research's own derby probes
    /// list "perfect slap, lifted, Power 5" — and it is the ceiling of the uncharged swing at
    /// quality 1.00 against nice's 0.95. Sweeping only the nice column measures 95% of the door and
    /// reports the room is sealed. Sour (0.75) is dominated by perfect and adds nothing.
    ///
    /// Swept across both uncharged qualities, six parks, Power 1-10, both lifts and nine sprays,
    /// the migration opens <b>five new</b> ordinary home runs across <b>four</b> parks, one of them
    /// at Power 8 rather than only the top two ratings.
    /// </summary>
    [Fact]
    public void OrdinaryUnchargedContactClearsFivePolesTheShippedParksDoNot()
    {
        var b = Control.Rules.Batting;

        // The nice column on its own: one inherited, one created.
        Assert.Equal(
            ["funfair-park P10 lift spray-44.9"],
            HomeRuns(Control, b.Quality.Slap.Nice));
        Assert.Equal(
            ["funfair-park P9 lift spray-44.9", "funfair-park P10 lift spray-44.9"],
            HomeRuns(Trial, b.Quality.Slap.Nice));

        // The ceiling of ordinary contact, which is what the criterion is actually about.
        var control = HomeRuns(Control, b.Quality.Slap.Perfect);
        var trial = HomeRuns(Trial, b.Quality.Slap.Perfect);
        Assert.Equal(7, control.Count);
        Assert.Equal(12, trial.Count);

        Assert.Equal(
            [
                "canopy-yard P10 lift spray-35",
                "ember-keep P10 lift spray-44.9",
                "ember-keep P10 lift spray44.9",
                "funfair-park P8 lift spray-44.9",
                "rooftop-city P10 lift spray35"
            ],
            trial.Except(control).ToArray());

        // Nothing the shipped parks allow is closed by the migration: the trial is a superset.
        Assert.Empty(control.Except(trial));

        static IReadOnlyList<string> HomeRuns(ContentCatalog cat, double quality)
        {
            // From `cat`, not Control: batting.json is not overridden today, but a helper that mixes
            // one catalog's exit table with another's parks would lie the moment one was.
            var b = cat.Rules.Batting;
            var gone = new List<string>();
            foreach (var id in ParkIds)
                for (var power = 1; power <= 10; power++)
                    foreach (var lift in new[] { 0.0, b.Launch.StickDeg })
                        foreach (var spray in Sprays)
                            if (BattedBall.Of(Exit(b, power, quality), Launch(b, power, false, lift), spray,
                                    cat.Parks[id], cat.Rules).HomeRun)
                                gone.Add($"{id} P{power} {(lift > 0 ? "lift" : "flat")} spray{spray}");
            return gone;
        }
    }

    // ---------------------------------------------------------------------------------
    // What the heavier ball does to the two clocks the slice promised not to move
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <b>The slice's claim does not survive measurement.</b> #717 says a ball on the dirt is barely
    /// touched by drag and that grounder arrival times at infielders are unchanged. They are not: on
    /// the research's own three ground probes the heavier ball is late everywhere, by roughly a
    /// twentieth of a second at 60 ft and a third of a second by 100 ft, and it stops shorter. The
    /// numbers below are the #708 flight-probe table reproduced by the shipped integrator, so they
    /// are the research's own and not a new measurement. Reported rather than repaired: the rail on
    /// every 3c slice is that no expectation is edited to make it pass.
    /// </summary>
    [Theory]
    [InlineData(65, 5, 1.1052, 1.1807, 2.1119, 2.4590)]
    [InlineData(75, 12, 0.5917, 0.6326, 1.0255, 1.1477)]
    [InlineData(80, 8, 0.9037, 0.9658, 1.5662, 1.7522)]
    public void ABallOnTheDirtIsLateUnderTheHeavierBallAndLaterTheFartherItGoes(
        double exitMph, double launchDeg, double controlAtSixty, double trialAtSixty, double controlAtHundred, double trialAtHundred)
    {
        var control = BallFlight.Trajectory(exitMph, launchDeg, 0, Control.Rules);
        var trial = BallFlight.Trajectory(exitMph, launchDeg, 0, Trial.Rules);

        Near(controlAtSixty, Arrival(control, 60));
        Near(trialAtSixty, Arrival(trial, 60));
        Near(controlAtHundred, Arrival(control, 100));
        Near(trialAtHundred, Arrival(trial, 100));

        // The shape of it: late at the corners, later in the hole, and the delay grows with distance.
        Assert.True(Arrival(trial, 60) > Arrival(control, 60));
        Assert.True(Arrival(trial, 100) - Arrival(control, 100) > Arrival(trial, 60) - Arrival(control, 60));
    }

    /// <summary>
    /// The liner. Every liner's hang falls under the heavier ball — which is the direction the slice
    /// wanted, since a rope that used to clear the fence now lands in front of it. <b>It does not fall
    /// under 2.56 s.</b> The longest ordinary liner hangs 2.74 s in a migrated park, down from 3.01 s
    /// today, so the derived C80 alley closure is not met by drag alone. That closure assumes 18 ft/s
    /// pursuit, a 0.4-second read and a 6-ft reach — none of which is implemented yet (#718, #719), so
    /// this is a number for those slices to close, not one to edit here.
    /// </summary>
    [Fact]
    public void EveryLinerHangsLessUnderTheHeavierBall()
    {
        var b = Control.Rules.Batting;
        var liners = 0;
        foreach (var id in ParkIds)
            for (var power = 1; power <= 10; power++)
                foreach (var quality in new[] { b.Quality.Slap.Nice, b.Quality.Slap.Perfect })
                    // The authored loft plus the launch noise the swing actually carries
                    // (batting.launch.noiseDeg, half either way), which is what puts a rope at the top
                    // of the liner band. A sour is not in the sweep: the resolver forces it into the
                    // topper or pop band, so it never produces one.
                    for (var noise = -b.Launch.NoiseDeg / 2; noise <= b.Launch.NoiseDeg / 2; noise += 1)
                        foreach (var spray in new[] { -44.9, 0.0, 44.9 })
                        {
                            var exit = Exit(b, power, quality);
                            var launch = Math.Round(Launch(b, power, false, 0) + noise, 1);
                            if (launch < b.Launch.MinDeg || launch > b.Launch.MaxDeg) continue;
                            var today = BattedBall.Of(exit, launch, spray, Control.Parks[id], Control.Rules);
                            var trial = BattedBall.Of(exit, launch, spray, Trial.Parks[id], Trial.Rules);
                            if (today.Shape != BattedBallClass.Liner || trial.Shape != BattedBallClass.Liner) continue;
                            liners++;
                            Assert.True(trial.HangT < today.HangT,
                                $"{id} P{power} {exit} mph {launch} deg hung {trial.HangT:F3} against {today.HangT:F3}");
                        }

        // The real sweep is 2446. A floor of 300 would let it collapse to an eighth of its size and
        // still claim "every liner hangs less", so the count is pinned instead.
        Assert.Equal(2446, liners);
    }

    /// <summary>
    /// <b>The heavier ball does not get the liner under the alley closure on its own.</b> The longest
    /// ordinary liner — uncharged, no star, anywhere in the liner launch band — hangs 2.742 s in a
    /// migrated Harbor, down from 3.008 s today. The closure #717 names is 2.56 s, and it is the
    /// derived C80 LF–CF number at 18 ft/s pursuit, a 0.4-second read and a 6-ft reach; none of those
    /// is implemented yet (#718, #719). Reported rather than repaired: the rail on every 3c slice is
    /// that no expectation is edited to make it pass.
    /// </summary>
    [Fact]
    public void TheLongestOrdinaryLinerFallsToTwoSevenFourButNotUnderTheAlleyClosure()
    {
        var b = Control.Rules.Batting;
        var park = Control.Parks["harbor-diamond"];
        var compact = Trial.Parks["harbor-diamond"];
        var worstToday = 0.0;
        var worstTrial = 0.0;
        var classes = Control.Rules.Flight.Classes;
        // A liner is a rope: at least classes.linerMinExitMph off the bat, which no bat under Power 4
        // manages, and above the grounder band but under classes.linerMaxLaunchDeg.
        for (var power = 4; power <= 10; power++)
            foreach (var quality in new[] { b.Quality.Slap.Nice, b.Quality.Slap.Perfect })
                for (var launch = classes.GrounderMaxLaunchDeg; launch < classes.LinerMaxLaunchDeg; launch += 0.1)
                    foreach (var spray in new[] { -44.9, 0.0, 44.9 })
                    {
                        var exit = Exit(b, power, quality);
                        var deg = Math.Round(launch, 1);
                        if (exit < classes.LinerMinExitMph) continue;
                        var today = BattedBall.Of(exit, deg, spray, park, Control.Rules);
                        if (today.Shape == BattedBallClass.Liner) worstToday = Math.Max(worstToday, today.HangT);
                        var trial = BattedBall.Of(exit, deg, spray, compact, Trial.Rules);
                        if (trial.Shape == BattedBallClass.Liner) worstTrial = Math.Max(worstTrial, trial.HangT);
                    }

        Assert.Equal(3.008, worstToday, 3);
        Assert.Equal(2.742, worstTrial, 3);
        Assert.True(worstTrial > 2.56, "the 2.56 s alley closure is not met by drag alone — #718 and #719 own the rest");
    }

    // ---------------------------------------------------------------------------------
    // What this slice cannot carry, recorded where the next one will see it
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <b>The outfield migrated with the park (#725).</b> This test used to be the marker for what
    /// the slice could not carry: the nine defensive starts were <see cref="Diamond.Positions"/>
    /// literals and the three outfield ones were not park-relative at all, so against a migrated
    /// 280-ft centre field <see cref="FieldBounds.Clamp"/> pinned CF eight feet off the fence.
    /// Nothing can land behind an outfielder pinned to the wall, which is why no trace-level read of
    /// the park migration meant anything until the starts followed.
    ///
    /// <para>
    /// It said it would "fail the day they move". It could not have, and that is worth writing down:
    /// the trial's table never reaches this process, because <see cref="Diamond"/> reads the one
    /// shipped <see cref="Rules.Default"/> by #711's design. The three asserts would have stayed
    /// green while every sentence around them went false. It is rewritten here as the result, with
    /// the numbers it used to pin kept on the record.
    /// </para>
    ///
    /// <para>
    /// The trial's spots are <em>not</em> <see cref="Outfield"/>, the park fence scale. Each body
    /// keeps its bearing and its fraction of the fence <em>at that bearing</em>: LF and RF keep
    /// 0.7203 of a 379.16-ft fence, CF keeps 0.7625 of 400. The radial factors those imply are
    /// 0.7008 and 0.7000 — close, and not the same number — so no single scale describes them, and
    /// 110 × 0.70 = 77.00 is the wrong answer that looks right.
    /// </para>
    /// </summary>
    [Fact]
    public void TheOutfieldStartsMigratedAndClearEveryCompactWall()
    {
        // Shipped, unchanged: the numbers this test has always named.
        Assert.Equal((-110, 250), Diamond.Positions["LF"]);
        Assert.Equal((0, 305), Diamond.Positions["CF"]);
        Assert.Equal((110, 250), Diamond.Positions["RF"]);
        Assert.Equal(305, FieldBounds.Clamp(Control.Parks["harbor-diamond"], 0, 305).Z, 1);

        // The trial's own, from the accepted research (#730).
        Assert.Equal((-77.09, 175.19), Trial.Rules.Fielders.Spot("LF"));
        Assert.Equal((0, 213.5), Trial.Rules.Fielders.Spot("CF"));
        Assert.Equal((77.09, 175.19), Trial.Rules.Fielders.Spot("RF"));

        // The cost that is now closed, kept on the record: a 90-ft CF snapped to 272 against
        // harbor's compact 280-ft wall. The trial's CF stands where it is asked to, 66.5 ft short
        // of the fence — which is the room a ball needs to land behind an outfielder.
        var harbor = Trial.Parks["harbor-diamond"];
        Assert.Equal(272, FieldBounds.Clamp(harbor, 0, 305).Z, 0);
        Assert.Equal(213.5, FieldBounds.Clamp(harbor, 0, 213.5).Z, 2);
        Assert.Equal(66.5, harbor.CenterFenceFt - 213.5, 2);

        // Every park, both columns: all eighteen 90-ft starts stood outside the compact wall, and
        // none of the eighteen migrated ones does. One global set clears all six (#730 decision 3).
        var checkedPairs = 0;
        var tightest = double.MaxValue;
        foreach (var id in ParkIds)
        foreach (var pos in new[] { "LF", "CF", "RF" })
        {
            var park = Trial.Parks[id];
            var (sx, sz) = Diamond.Positions[pos];
            Assert.False(FieldBounds.Inside(park, sx, sz), $"{id} {pos} on the 90-ft spot used to be inside");

            var (x, z) = Trial.Rules.Fielders.Spot(pos);
            Assert.True(FieldBounds.Inside(park, x, z), $"{id} {pos} at ({x}, {z}) does not clear the wall");

            var fence = AtBatResolver.FenceAt(park, Math.Atan2(x, z) * 180 / Math.PI);
            tightest = Math.Min(tightest, fence - Diamond.Dist(0, 0, x, z));
            checkedPairs++;
        }

        Assert.Equal(18, checkedPairs);
        Assert.Equal(51.50, tightest, 2);   // canopy-yard's centre field, the closest of the eighteen
    }

    // ---------------------------------------------------------------------------------
    // The control
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// A checked-in trial is inert. The shipped root still plays the historical parks and the
    /// historical ball, so a run that did not name the overlay is the game that shipped.
    /// </summary>
    [Fact]
    public void TheShippedRootStillPlaysTheHistoricalParkAndBall()
    {
        Assert.Equal(0.0019, ContentCatalog.Load(new DataRoot(Shipped)).Rules.Flight.Drag);
        Assert.Equal(90, ContentCatalog.Load(new DataRoot(Shipped)).Rules.Infield.BaselineFt);
        Assert.Equal(330, Control.Parks["harbor-diamond"].LeftFenceFt);
        Assert.Equal(400, Control.Parks["harbor-diamond"].CenterFenceFt);
        Assert.Equal(378, Control.Parks["canopy-yard"].CenterFenceFt);
        Assert.Equal(58, Control.Parks["canopy-yard"].Hazards[0].Z);
    }

    /// <summary>
    /// <b>No trial park carries a key the shipped park does not.</b> The overlay's "override, never
    /// add" rule is enforced per <i>file</i>; inside a park file it is not. A rules table is
    /// protected — <c>RulesValidation.UnknownFields</c> refuses an unknown field — but
    /// <c>ParkDto</c> deserialization ignores unmapped members, so a speculative knob added to a
    /// trial park would load, be silently ignored by the sim, and read as authored.
    ///
    /// This checks the eight files this slice actually ships. The machinery gap itself belongs to
    /// #716 and is filed there.
    /// </summary>
    [Fact]
    public void NoTrialParkInventsAFieldTheShippedParkDoesNotHave()
    {
        foreach (var id in ParkIds)
        {
            var shipped = Keys(Path.Combine(Shipped, "parks", id + ".json"));
            var trial = Keys(Path.Combine(Overlay, "parks", id + ".json"));
            Assert.Equal(shipped, trial);
        }

        static IReadOnlyList<string> Keys(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            var keys = new List<string>();
            foreach (var field in doc.RootElement.EnumerateObject())
            {
                keys.Add(field.Name);
                if (field.Name != "hazards" || field.Value.ValueKind != JsonValueKind.Array) continue;
                var i = 0;
                foreach (var hazard in field.Value.EnumerateArray())
                {
                    foreach (var inner in hazard.EnumerateObject()) keys.Add($"hazards[{i}].{inner.Name}");
                    i++;
                }
            }
            keys.Sort(StringComparer.Ordinal);
            return keys;
        }
    }

    /// <summary>
    /// <b>Four hazards end up in a different zone than the one that scaled them.</b> The rule picks a
    /// factor from each hazard's <i>shipped</i> distance, and nothing checks where the result lands.
    /// Because the outfield contracts harder than the infield, four hazards that were outfield become
    /// infield — Canopy's deep tree by five hundredths of a foot.
    ///
    /// This is not a mis-application of the rule; it is the rule being one-way. It is recorded because
    /// the seam is <c>flight.classes.infieldLipFt</c>.
    ///
    /// <para>
    /// <b>Re-checked for #728.</b> The four assertions below are unchanged, and they should be: both
    /// sides of the comparison read the <i>shipped</i> lip, and that is the lip the migration rule
    /// used. Measured against the migrated 137.78 instead, the count is three, not four — Canopy's
    /// deep tree lands at (-49, 147), r = 154.95 ft, which is outside it. Three is exactly what #730's
    /// option table predicts for the accepted option, which is an independent check that 137.78 is the
    /// value that was decided.
    /// </para>
    /// </summary>
    [Fact]
    public void FourHazardsLandInADifferentZoneThanTheOneThatScaledThem()
    {
        var lip = Control.Rules.Flight.Classes.InfieldLipFt;
        var flipped = new List<string>();

        foreach (var id in ParkIds)
        {
            var shipped = Control.Parks[id].Hazards;
            var trial = Trial.Parks[id].Hazards;
            for (var i = 0; i < shipped.Count; i++)
            {
                var was = Diamond.Dist(0, 0, shipped[i].X, shipped[i].Z) >= lip;
                var now = Diamond.Dist(0, 0, trial[i].X, trial[i].Z) >= lip;
                if (was == now) continue;
                flipped.Add($"{id} {shipped[i].Type}");

                // Every flip runs outfield to infield. The outfield contracts harder, so a hazard can
                // fall inside the lip but never climb out of it.
                Assert.True(was && !now, $"{id} {shipped[i].Type} moved outward, which the scales forbid");
            }
        }

        Assert.Equal(
            ["canopy-yard tree", "crystal-rink freeze_volume", "ember-keep lava_pit", "rooftop-city ac_unit"],
            flipped);

        // The same count read against the migrated lip, so the narration above is a measurement.
        // Canopy's tree drops out: it lands at r = 154.95 ft, outside 137.78.
        var stillInside = new List<string>();
        foreach (var id in ParkIds)
        {
            var shipped = Control.Parks[id].Hazards;
            var trial = Trial.Parks[id].Hazards;
            for (var i = 0; i < shipped.Count; i++)
            {
                if (Diamond.Dist(0, 0, shipped[i].X, shipped[i].Z) < lip) continue;
                if (Diamond.Dist(0, 0, trial[i].X, trial[i].Z) >= MigratedLip) continue;
                stillInside.Add($"{id} {shipped[i].Type}");
            }
        }

        Assert.Equal(
            ["crystal-rink freeze_volume", "ember-keep lava_pit", "rooftop-city ac_unit"],
            stillInside);

        // Canopy's shallowest tree is the one that drops out: 221.36 ft shipped, 154.95 ft migrated,
        // inside the shipped 155 but outside 137.78.
        var tree = Trial.Parks["canopy-yard"].Hazards
            .Where(h => h.Type == "tree")
            .MinBy(h => Diamond.Dist(0, 0, h.X, h.Z))!;
        Assert.Equal(154.95, Diamond.Dist(0, 0, tree.X, tree.Z), 2);
        Assert.InRange(Diamond.Dist(0, 0, tree.X, tree.Z), MigratedLip, lip);
    }

    // ---------------------------------------------------------------------------------
    // Effects the slice does not own, measured so 3d does not mistake them for its anchors
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <b>The migrated lip cuts two thirds of the extra pops, and the two scales keep the rest.</b>
    /// <c>BattedBall</c> downgrades a fly landing inside <c>flight.classes.infieldLipFt</c> to a pop.
    /// #717 shipped the trial with the lip still at 155 ft against a 280-ft centre field, and the
    /// pops doubled: 489 → 976. <b>#728</b> moved it to 137.78 ft on the basepath scale and the count
    /// is 652 — 163 extra pops where there were 487.
    ///
    /// <para>
    /// The residual is not a miss; it is the compact profile's two scales. The lip follows the
    /// basepath at 0.8889 while fair territory follows the fence at 0.70, so the same radius still
    /// covers a larger share of a smaller field: 137.78 / 280 = 0.492 against the shipped
    /// 155 / 400 = 0.388. Closing it the rest of the way would mean a lip on the fence scale — 108.50
    /// ft — which #730 ruled out, because it puts 2B and SS outside the lip and makes the fielding
    /// rules call them outfielders. The number below is therefore the accepted answer, not a gap.
    /// </para>
    ///
    /// <para>The three counts stay on the record together so 3d can attribute the move.</para>
    /// </summary>
    [Fact]
    public void TheMigratedLipCutsTwoThirdsOfTheExtraPopsAndTheTwoScalesKeepTheRest()
    {
        var control = Shapes(Control);
        var trial = Shapes(Trial);

        // 489 shipped, 976 at #717's un-migrated lip, 652 now.
        Assert.Equal(489, control.Pop);
        Assert.Equal(652, trial.Pop);
        Assert.Equal(163, trial.Pop - control.Pop);
        Assert.True(trial.Pop - control.Pop < (976 - control.Pop) / 2.5,
            $"pops {control.Pop} -> {trial.Pop}: #717 measured 976, so the lip was the cause");

        // The flies did not vanish; they were reclassified. 4103 shipped, 3494 un-migrated, 3818 now.
        Assert.Equal(4103, control.Fly);
        Assert.Equal(3818, trial.Fly);

        // Every other shape is untouched by the lip: it only ever sorts a fly from a pop.
        Assert.Equal(489 + 4103, control.Pop + control.Fly);
        Assert.Equal(976 + 3494, trial.Pop + trial.Fly);

        static (int Pop, int Fly) Shapes(ContentCatalog cat)
        {
            var b = cat.Rules.Batting;
            var qualities = new[]
            {
                b.Quality.Slap.Nice, b.Quality.Slap.Perfect,
                b.Quality.Charge.Nice, b.Quality.Charge.Perfect
            };
            int pop = 0, fly = 0;
            foreach (var id in ParkIds)
                for (var power = 1; power <= 10; power++)
                    foreach (var charged in new[] { false, true })
                        foreach (var quality in qualities)
                            foreach (var lift in new[] { 0.0, b.Launch.StickDeg })
                                foreach (var spray in Sprays)
                                {
                                    var shape = BattedBall.Of(Exit(b, power, quality),
                                        Launch(b, power, charged, lift), spray, cat.Parks[id], cat.Rules).Shape;
                                    if (shape == BattedBallClass.Pop) pop++;
                                    else if (shape == BattedBallClass.Fly) fly++;
                                }
            return (pop, fly);
        }
    }

    /// <summary>
    /// <b>The migrated centre fielder starts inside a Funfair chomper.</b> An effect this slice does
    /// not own and does not repair. <c>ParkHazards.FunfairChompers</c> are C# literals, so they did
    /// not migrate with the park data (#717's gap) — the centre mouth is still at (0, 228) with an
    /// 18-ft radius. The shipped centre fielder stood 77.00 ft from that centre, 59.00 ft clear of
    /// the rim. The migrated one stands 14.50 ft from it, which is 3.50 ft <i>inside</i> the rim.
    ///
    /// <para>
    /// <c>ChompFly</c> is evaluated at the ball's landing point, not at the fielder, so nobody is
    /// frozen where they stand. What it means is narrower and stranger: on a Funfair night, a fly
    /// landing at the centre fielder's own feet is stamped an out by the hazard before his glove
    /// resolves. Recorded because it puts part of the trial's fly-out movement outside the geometry
    /// #725 controls, and a 3d reader would otherwise attribute all of it here.
    /// </para>
    /// </summary>
    [Fact]
    public void TheMigratedCentreFielderStartsInsideAFunfairChomper()
    {
        var funfair = Trial.Parks["funfair-park"];
        var centre = ParkHazards.FunfairChompers.Single(h => h.Tag == "C");
        var was = Control.Rules.Fielders.Spot("CF");
        var now = Trial.Rules.Fielders.Spot("CF");

        // Distance to the mouth's centre, then the clearance its 18-ft rim leaves.
        Assert.Equal(18, centre.Radius);
        Assert.Equal(77.00, Diamond.Dist(centre.X, centre.Z, was.X, was.Z), 2);
        Assert.Equal(14.50, Diamond.Dist(centre.X, centre.Z, now.X, now.Z), 2);
        Assert.Equal(59.00, Diamond.Dist(centre.X, centre.Z, was.X, was.Z) - centre.Radius, 2);
        Assert.Equal(-3.50, Diamond.Dist(centre.X, centre.Z, now.X, now.Z) - centre.Radius, 2);

        Assert.False(ParkHazards.ChompFly(funfair, night: true, was.X, was.Z));
        Assert.True(ParkHazards.ChompFly(funfair, night: true, now.X, now.Z));

        // Only at night, and only a fly: a grounder or a liner is never chomped.
        Assert.False(ParkHazards.ChompFly(funfair, night: false, now.X, now.Z));
        Assert.True(ParkHazards.ChompFly(funfair, night: true, now.X, now.Z, grounder: true) is false);

        // The corners are clear, so this is one body in one park.
        foreach (var pos in new[] { "LF", "RF" })
        {
            var spot = Trial.Rules.Fielders.Spot(pos);
            Assert.False(ParkHazards.ChompFly(funfair, night: true, spot.X, spot.Z), pos);
        }

        // And no other park has chompers at all, whatever a body stands on.
        foreach (var id in ParkIds.Where(p => p != "funfair-park"))
            Assert.False(ParkHazards.ChompFly(Trial.Parks[id], night: true, now.X, now.Z), id);
    }

    /// <summary>
    /// <b>The lip and the drawn dirt agree again, on the compact field as on the shipped one.</b>
    /// The lip is a rule and the dirt is a picture, and until #729 they were authored in different
    /// places: the lip in <c>flight.classes.infieldLipFt</c>, the dirt in <c>ParkDiamond.BackR</c>,
    /// a C# constant no overlay could reach. That is why they came apart, and the sequence is worth
    /// keeping because each step was somebody's slice.
    ///
    /// <para>
    /// Shipped, the lip sits 2.50 ft outside the farthest dirt. #717 pulled the dirt in with the
    /// mound and left the lip at 155, widening the gap to 9.22 ft. #728 moved the lip to 137.78 and
    /// flipped the sign — 8.00 ft <i>inside</i> the dirt, so a fly landing on drawn dirt past the
    /// lip was classified a fly. #729 put the dress in <c>infield.json</c> too, and the margin comes
    /// back to 2.22 ft outside: the shipped 2.50 × 8/9, exactly.
    /// </para>
    ///
    /// <para>
    /// Kept because the #730 packet predicted the wrong failure here. It named
    /// <c>ParkDiamond.TrackIsInsideTheWall</c> and <c>ParkDiamondTests</c> as the tests that would
    /// catch a lip moving without its dress. Both are <c>&gt;</c> comparisons against the lip, so
    /// lowering it only widens their margin — they cannot fail, and nothing else in the suite sees
    /// this. This does.
    /// </para>
    /// </summary>
    [Fact]
    public void TheLipAndTheDrawnDirtAgreeOnBothFields()
    {
        var shippedDirt = Control.Rules.Infield.MoundFt + Control.Rules.Infield.BackArcFt;
        var trialDirt = Trial.Rules.Infield.MoundFt + Trial.Rules.Infield.BackArcFt;
        Assert.Equal(152.50, shippedDirt, 2);
        Assert.Equal(135.56, trialDirt, 2);

        // ParkDiamond reads the process-wide shipped table, so it agrees with the control side.
        Assert.Equal(ParkDiamond.DirtMaxZ, shippedDirt, 2);

        // Both fields now put the lip just outside the dirt, and by the same factor.
        Assert.Equal(2.50, Control.Rules.Flight.Classes.InfieldLipFt - shippedDirt, 2);
        Assert.Equal(2.22, MigratedLip - trialDirt, 2);
        Assert.Equal(2.50 * Infield, MigratedLip - trialDirt, 2);

        // The two steps that got here, kept on the record so 3d can attribute them.
        var undressedDirt = Control.Rules.Infield.MoundFt * Infield + Control.Rules.Infield.BackArcFt;
        Assert.Equal(145.78, undressedDirt, 2);
        Assert.Equal(9.22, 155 - undressedDirt, 2);            // #717: the dirt moved, the lip did not
        Assert.Equal(-8.00, MigratedLip - undressedDirt, 2);   // #728: the lip moved, the dirt did not
    }

    /// <summary>
    /// <b>The corner infielders followed their bags (#725).</b> It was never only the outfield that
    /// did not move. The sharper cost was at the corners: with the starts still C# literals, 1B and
    /// 3B stood 26.41 ft from bags that had walked in without them, against 16.62 ft shipped — an
    /// error larger than the whole 6-ft stand-up catch reach #719 authors, and larger than the
    /// margins #718 measures cover arrival and the double-play feed against. 2B and SS already
    /// played deep enough that the shrink barely reached them.
    ///
    /// <para>
    /// This test pinned that cost until the migration landed. It now records that it is closed, and
    /// keeps the old numbers, because the distance between the two columns is the whole point of the
    /// slice. Note what it could <em>not</em> have done: it would not have gone red on its own, since
    /// <see cref="Diamond.Positions"/> answers from the shipped root in this process whatever the
    /// trial carries. It would simply have kept asserting 26.41 ft about a field that had moved.
    /// </para>
    /// </summary>
    [Fact]
    public void TheCornerInfieldersFollowedTheirBagsWhenTheStartsMigrated()
    {
        // Shipped, unchanged by any of this.
        Assert.Equal(16.62, BagGap(Diamond.Positions["1B"], 1, Control.Rules), 2);
        Assert.Equal(16.62, BagGap(Diamond.Positions["3B"], 3, Control.Rules), 2);
        Assert.Equal(43.01, BagGap(Diamond.Positions["2B"], 2, Control.Rules), 2);

        // What the overlay cost while the starts were literals: the gap this issue closed.
        Assert.Equal(26.41, BagGap(Diamond.Positions["1B"], 1, Trial.Rules), 2);
        Assert.Equal(26.41, BagGap(Diamond.Positions["3B"], 3, Trial.Rules), 2);
        Assert.Equal(42.28, BagGap(Diamond.Positions["2B"], 2, Trial.Rules), 2);
        Assert.True(BagGap(Diamond.Positions["1B"], 1, Trial.Rules)
                    - BagGap(Diamond.Positions["1B"], 1, Control.Rules) > 6.0,
            "the unmigrated corner error was larger than the whole stand-up reach");

        // Where the trial's own bodies stand now.
        Assert.Equal(14.77, BagGap(TrialStart("1B"), 1, Trial.Rules), 2);
        Assert.Equal(14.77, BagGap(TrialStart("3B"), 3, Trial.Rules), 2);
        Assert.Equal(38.23, BagGap(TrialStart("2B"), 2, Trial.Rules), 2);

        // The gap scaled with the diamond, which is what "the same defence, smaller" has to mean.
        Assert.Equal(16.62 * Infield, BagGap(TrialStart("1B"), 1, Trial.Rules), 2);
        Assert.Equal(43.01 * Infield, BagGap(TrialStart("2B"), 2, Trial.Rules), 2);

        static double BagGap((double X, double Z) fielder, int bag, RulesTable rules)
        {
            var infield = rules.Infield;
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

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Where the trial stands the body at <paramref name="pos"/>. Seven come from the overlay's
    /// <c>fielders.json</c> (#725); the pitcher is the trial's own rubber and the catcher is
    /// <see cref="HomeSet.CatcherZ"/>, which no root moves. This is the defence a
    /// <c>GRAND_SLUGGERS_TRIAL</c> process stands — not the one <see cref="Diamond.Positions"/>
    /// reports here, which is always the shipped root's.
    /// </summary>
    static (double X, double Z) TrialStart(string pos) => pos switch
    {
        "P" => (0, Trial.Rules.Infield.MoundFt),
        "C" => (0, HomeSet.CatcherZ),
        _ => Trial.Rules.Fielders.Spot(pos)
    };

    static double TrialRadius(string pos)
    {
        var (x, z) = TrialStart(pos);
        return Diamond.Dist(0, 0, x, z);
    }

    /// <summary>Every leaf of a rules file, "section.key" to its raw JSON text, comments skipped. Sorted so the key lists compare in one order and a missing or extra key names itself.</summary>
    static SortedDictionary<string, string> Leaves(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });
        var leaves = new SortedDictionary<string, string>(StringComparer.Ordinal);
        Walk(doc.RootElement, "");
        return leaves;

        void Walk(JsonElement element, string prefix)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var field in element.EnumerateObject())
                        Walk(field.Value, prefix.Length == 0 ? field.Name : prefix + "." + field.Name);
                    break;
                case JsonValueKind.Array:
                    var i = 0;
                    foreach (var item in element.EnumerateArray()) Walk(item, $"{prefix}[{i++}]");
                    break;
                default:
                    leaves[prefix] = element.GetRawText();
                    break;
            }
        }
    }

    static readonly double[] Sprays = [-44.9, -35, -22, -10, 0, 10, 22, 35, 44.9];

    static double Exit(BattingRules b, int power, double qualityMul, double starMul = 1.0) =>
        Math.Round((b.Exit.BaseMph + power * b.Exit.MphPerPower) * qualityMul * starMul, 1);

    static double Launch(BattingRules b, int power, bool charged, double lift) =>
        Math.Round(b.Launch.LoftBaseDeg + (power - 5) * b.Launch.LoftPerPower + (charged ? b.Charge.LoftDeg : 0) + lift, 1);

    /// <summary>The #708 table is published to six decimals; a thousandth of a second is the match.</summary>
    static void Near(double expected, double actual) =>
        Assert.InRange(actual, expected - 0.001, expected + 0.001);

    /// <summary>When the ball reaches this distance from home, on the play clock the fielders run on.</summary>
    static double Arrival(IReadOnlyList<Sample> path, double stationFt)
    {
        for (var i = 1; i < path.Count; i++)
        {
            if (path[i].Dist < stationFt) continue;
            var before = path[i - 1];
            var after = path[i];
            var u = (stationFt - before.Dist) / Math.Max(1e-9, after.Dist - before.Dist);
            return before.T + (after.T - before.T) * u;
        }
        return double.NaN;
    }
}
