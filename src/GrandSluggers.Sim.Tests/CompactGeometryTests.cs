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

    static readonly string[] ParkIds =
        ["canopy-yard", "crystal-rink", "ember-keep", "funfair-park", "harbor-diamond", "rooftop-city"];

    // ---------------------------------------------------------------------------------
    // What the overlay carries
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Eight files, named. The tree is the declaration (#716), so this list is also the answer to
    /// "what is this trial changing?" — and a slice that quietly carried a ninth would show up here
    /// rather than in a trace nobody could attribute.
    /// </summary>
    [Fact]
    public void TheTrialCarriesEightFilesAndNoOthers()
    {
        Assert.Equal(
            [
                "parks/canopy-yard.json", "parks/crystal-rink.json", "parks/ember-keep.json",
                "parks/funfair-park.json", "parks/harbor-diamond.json", "parks/rooftop-city.json",
                "rules/flight.json", "rules/infield.json"
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

        // Ember Keep stays the biggest and Canopy Yard the smallest, before and after.
        var shippedOrder = ParkIds.OrderBy(id => Control.Parks[id].CenterFenceFt).ToArray();
        var trialOrder = ParkIds.OrderBy(id => Trial.Parks[id].CenterFenceFt).ToArray();
        Assert.Equal(shippedOrder, trialOrder);
        Assert.Equal("canopy-yard", trialOrder[0]);
        Assert.Equal("ember-keep", trialOrder[^1]);
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
    /// <para><b>Radii are not scaled.</b> A barrel is a physical object; it did not shrink, for the
    /// same reason a wall did not.</para>
    /// </summary>
    [Fact]
    public void HazardsMigrateByTheZoneTheySitInAndKeepTheirRadius()
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
                var scale = FieldingResolver.OutfieldGrass(was.X, was.Z) ? Outfield : Infield;
                Assert.Equal(was.Type, now.Type);
                Assert.Equal(was.Tag, now.Tag);
                Assert.Equal(was.Radius, now.Radius);
                Assert.Equal(Math.Round(was.X * scale, MidpointRounding.AwayFromZero), now.X, 6);
                Assert.Equal(Math.Round(was.Z * scale, MidpointRounding.AwayFromZero), now.Z, 6);
                Assert.True(now.Radius > 0 || was.Type == "train", where);
            }
        }

        // The eight Canopy hazards are the split the research describes: three barrels on the dirt,
        // five on the grass, and the lip decides which factor each one takes.
        var canopy = Control.Parks["canopy-yard"].Hazards;
        Assert.Equal(3, canopy.Count(h => !FieldingResolver.OutfieldGrass(h.X, h.Z)));
        Assert.Equal(5, canopy.Count(h => FieldingResolver.OutfieldGrass(h.X, h.Z)));

        // Rooftop's AC unit is the one the lip decides narrowly — 155.2 ft from home against a
        // 155-ft lip — so it takes the fence factor and lands at (28, 105) rather than (36, 133).
        var ac = Control.Parks["rooftop-city"].Hazards.Single(h => h.Type == "ac_unit");
        Assert.Equal(155.24, Diamond.Dist(0, 0, ac.X, ac.Z), 2);
        Assert.True(FieldingResolver.OutfieldGrass(ac.X, ac.Z));
        var migrated = Trial.Parks["rooftop-city"].Hazards.Single(h => h.Type == "ac_unit");
        Assert.Equal(28, migrated.X);
        Assert.Equal(105, migrated.Z);
        Assert.Equal(ac.Radius, migrated.Radius);

        // Harbor is the reference park and has no hazards, so it is unaffected by the rule either way.
        Assert.Empty(Trial.Parks["harbor-diamond"].Hazards);
    }

    /// <summary>
    /// Surgical on purpose: drag is the only field that moves. Cutting exit velocity instead would
    /// have reopened the exit table, the infield races and every accepted fielding anchor, so the
    /// check is the whole file rather than the one number — a trial writes whole files (#716), and a
    /// second field edited in passing would otherwise ride along unremarked.
    /// </summary>
    [Fact]
    public void TheTrialCarriesTheHeavierBallAndNothingElseInTheFlightTable()
    {
        Assert.Equal(0.0019, Control.Rules.Flight.Drag);
        Assert.Equal(0.0040, Trial.Rules.Flight.Drag);

        var shipped = Load(Path.Combine(Shipped, "rules", "flight.json"));
        var trial = Load(Path.Combine(Overlay, "rules", "flight.json"));
        Assert.Equal(0.0019, shipped["drag"]!.GetValue<double>());
        Assert.Equal(0.0040, trial["drag"]!.GetValue<double>());
        shipped.Remove("drag");
        trial.Remove("drag");
        Assert.Equal(shipped.ToJsonString(), trial.ToJsonString());

        static JsonObject Load(string path) =>
            JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();
    }

    /// <summary>
    /// No exit-table change: <c>batting.json</c> is not carried, so every contact leaves the bat at
    /// exactly the speed it leaves at today. This is what lets the slice claim that only drag moved.
    /// </summary>
    [Fact]
    public void TheExitTableIsUntouchedAtEveryPowerAndQuality()
    {
        var control = Control.Rules.Batting;
        var trial = Trial.Rules.Batting;
        Assert.False(Root.Overridden("rules", "batting.json"));
        Assert.False(Root.Overridden("rules", "running.json"));

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
    /// Ordinary uncharged contact stays inside a migrated park — with one exception, and it is one
    /// the compact profile inherited rather than created. A nice slap at Power 9 or 10 with full lift,
    /// pulled to the line, clears Funfair Park's left pole: 222 ft against a 220-ft pole, an 8-ft wall
    /// and the only 6 mph wind blowing straight out in the set. The same swing already clears the
    /// shipped Funfair line today (317 ft against 315), so the migration did not open that door.
    /// </summary>
    [Fact]
    public void TheOnlyNiceSlapThatClearsAPoleIsTheOneThatAlreadyDoesToday()
    {
        Assert.Equal(
            ["funfair-park P9 lift spray-44.9", "funfair-park P10 lift spray-44.9"],
            NiceSlapHomeRuns(Trial));
        Assert.Equal(
            ["funfair-park P10 lift spray-44.9"],
            NiceSlapHomeRuns(Control));

        static IReadOnlyList<string> NiceSlapHomeRuns(ContentCatalog cat)
        {
            var b = Control.Rules.Batting;
            var gone = new List<string>();
            foreach (var id in ParkIds)
                for (var power = 1; power <= 10; power++)
                    foreach (var lift in new[] { 0.0, b.Launch.StickDeg })
                        foreach (var spray in Sprays)
                            if (BattedBall.Of(Exit(b, power, b.Quality.Slap.Nice), Launch(b, power, false, lift), spray,
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

        Assert.True(liners > 300, $"only {liners} liners in the sweep");
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
    /// <b>The outfield did not migrate with the park, and no overlay can move it.</b> The nine
    /// defensive starts are <see cref="Diamond.Positions"/> literals, and the three outfield ones are
    /// not even park-relative today — LF and RF at (±110, 250), CF at (0, 305), in every park. Against
    /// a 400-ft centre field that is 95 ft of room in front of the wall; against a migrated 280-ft one
    /// there is none, and <see cref="FieldBounds.Clamp"/> pins CF eight feet off the fence. Nothing can
    /// land behind an outfielder pinned to the wall, which is why extra-base hits fall away in a trial
    /// run and why no trace-level read of the park migration means anything until the starts follow.
    ///
    /// <para>The research names them for C80 — LF (−77.09, 175.19), CF (0, 213.5), RF (77.09, 175.19) —
    /// and they belong to the fielding chain (#718 on). This test is the marker: it will fail the day
    /// they move, which is the point.</para>
    /// </summary>
    [Fact]
    public void TheOutfieldStartsAreStillTheNinetyFootLiteralsAndClampToACompactWall()
    {
        Assert.Equal((-110, 250), Diamond.Positions["LF"]);
        Assert.Equal((0, 305), Diamond.Positions["CF"]);
        Assert.Equal((110, 250), Diamond.Positions["RF"]);

        var (cfX, cfZ) = Diamond.Positions["CF"];
        var shippedHarbor = FieldBounds.Clamp(Control.Parks["harbor-diamond"], cfX, cfZ);
        Assert.Equal(305, shippedHarbor.Z, 1);

        var compactHarbor = FieldBounds.Clamp(Trial.Parks["harbor-diamond"], cfX, cfZ);
        Assert.Equal(272, compactHarbor.Z, 0);
        Assert.True(Trial.Parks["harbor-diamond"].CenterFenceFt - compactHarbor.Z < 10,
            "CF is pinned against the compact wall with no room behind it");
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

    // ---------------------------------------------------------------------------------

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
