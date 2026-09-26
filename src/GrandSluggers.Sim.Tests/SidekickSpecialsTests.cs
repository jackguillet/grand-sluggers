using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The sidekick specials pool (AB-10, WD-27; spec §13): eight generic specials beside the six, each species carrying one pitch
/// and one swing that all its sidekicks share. Every effect is geometry on the ball's own path: the Dot crosses on its aim, the
/// Sinker's ball leaves lower, the Lob and the Sidearm keep the crossing and the clock, the Pull and the Opposite lean the
/// spray by the batter's hand, the Chopper's first hop springs, and the Drag Bunt comes to rest just inside the line.
/// </summary>
public sealed class SidekickSpecialsTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;
    static readonly string[] NewPitches = ["dot", "sinkball", "lob", "sidearm"];
    static readonly string[] NewSwings = ["pull", "opposite", "chopper", "drag-bunt"];

    // ---------------------------------------------------------------------------------
    // S-246  Star Dot: it crosses exactly on its aim
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("curveball", 0.4, -0.3, 0.5, 0.2)]
    [InlineData("slider", -0.6, 0.2, -1, -0.5)]
    [InlineData("sinker", 0, 0, 0.3, 0.8)]
    [InlineData("fastball", 0.2, 0.5, 0, 0)]
    public void S246_TheStarDotCrossesExactlyOnItsAim(string family, double aimX, double aimY, double breakX, double rubberX)
    {
        var rules = Game.Rules;
        var dot = Game.StarSkills.Pitch("dot")!;
        Assert.True(dot.Dot);
        Assert.Equal(1.05, dot.SpeedMul);
        var zone = StrikeZoneGeometry.For(Game.Must("rio"), rules);
        var star = new PitchCommand(family, 0.3, true, aimX, aimY, breakX, rubberX, Zone: zone);
        var (tx, ty) = PitchFlight.PlateTarget(aimX, aimY, zone);
        var aimed = (X: tx + rubberX * HomeSet.PitcherWalk + PitchFlight.BreakShiftFt(1, breakX, ChargeFeel.IsCharge(star.Charge01, rules) || rules.Pitching.Families.Of(family).BreakDamped, rules.Pitching.Flight), Y: ty);
        var cross = PitchFlight.Crossing(star, rules, "dot");
        Assert.Equal(aimed.X, cross.X, 9);
        Assert.Equal(aimed.Y, cross.Y, 9);
        // The plain pitch of a family with its own drop or sweep crosses elsewhere; the Dot does not.
        var plain = PitchFlight.Crossing(star with { Star = false }, rules);
        var row = rules.Pitching.Families.Of(family);
        if (row.DropFt != 0) Assert.NotEqual(plain.Y, cross.Y, 6);
        else Assert.Equal(plain.Y, cross.Y, 9);
        // The speed is ×1.05; the window is the ordinary one.
        var plainMph = AtBatResolver.PitchSpeedMph(star with { Star = false }, 5, rules, "dot", Game.StarSkills);
        Assert.Equal(plainMph * 1.05, AtBatResolver.PitchSpeedMph(star, 5, rules, "dot", Game.StarSkills), 9);
    }

    [Fact]
    public void S246_TheCpuArmLaysNoScatterOnAStarDot()
    {
        var rules = Game.Rules;
        var c = rules.Pitching.Cpu;
        var halfW = StrikeZoneGeometry.HalfWidth;
        var intents = new[] { 0, c.Locations.MiddleInFt, halfW + c.Locations.WasteOutFt, halfW - c.Locations.EdgeInsetFt };
        var (dotStars, plainStars) = (0, 0);
        foreach (var seed in new[] { 3, 11, 29 })
        {
            var dot = MatchPitchedBy("dot", seed);
            var plain = MatchPitchedBy("heatball", seed);
            for (var i = 0; i < 40; i++)
            {
                var p = dot.CpuPitcher.PitchByInputs(out var dotPlan);
                var q = plain.CpuPitcher.PitchByInputs(out var plainPlan);
                Assert.Equal(p.Star, q.Star);
                if (!p.Star) continue;
                // The Dot's intent is the named location itself; the same draw lands the other captain's star off it.
                Assert.Contains(intents, x => Math.Abs(Math.Abs(dotPlan.IntentX) - x) < 1e-9);
                dotStars++;
                if (intents.All(x => Math.Abs(Math.Abs(plainPlan.IntentX) - x) > 1e-9)) plainStars++;
            }
        }
        Assert.True(dotStars > 0, "the fixture throws some star pitches");
        Assert.Equal(dotStars, plainStars);
    }

    static Match MatchPitchedBy(string starPitch, int seed)
    {
        var (home, away) = PresetTeams.Pair(Game, "rio", "ashlord");
        var arm = home.Captain with { StarPitch = starPitch, Stats = home.Captain.Stats with { Control = 4 } };
        var roster = home.Roster.Select(c => c.Id == arm.Id ? arm : c).ToList();
        var match = Match.Exhibition(Game, home with { Captain = arm, Roster = roster, Starter = arm }, away, seed: seed);
        Assert.Equal(arm.Id, match.Pitcher.Id);
        return match;
    }

    // ---------------------------------------------------------------------------------
    // S-247  Star Sinker: a ball put in play off it leaves 6° lower
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(0.3, false)]
    [InlineData(0.0, true)]
    public void S247_ABallOffAStarSinkerLeavesSixDegreesLower(double crossingY, bool starSwing)
    {
        var sinker = Game.Must("rio") with { StarPitch = "sinkball" };
        var plain = Game.Must("rio") with { StarPitch = "no-such-pitch" };
        var batter = Game.Must("nico");
        var zone = StrikeZoneGeometry.For(batter, Game.Rules);
        var a = Resolve(sinker, batter, true, starSwing, zone.CenterY + crossingY);
        var b = Resolve(plain, batter, true, starSwing, zone.CenterY + crossingY);
        Assert.NotEqual(ContactQuality.Miss, a.Quality);
        Assert.Equal(6, Game.StarSkills.Pitch("sinkball")!.SinkDeg);
        Assert.Equal(b.LaunchDeg - 6, a.LaunchDeg, 6);
        Assert.Equal(b.SprayDeg, a.SprayDeg);
        Assert.Equal(b.ExitVeloMph, a.ExitVeloMph);
    }

    // ---------------------------------------------------------------------------------
    // S-248  Star Lob: a slow start on a high arc, the ordinary crossing on the ordinary instant
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("fastball", 0, 0)]
    [InlineData("changeup", 0.5, -0.4)]
    [InlineData("curveball", -0.7, 0.3)]
    public void S248_TheStarLobArrivesOnTheOrdinaryInstantAtTheOrdinaryCrossing(string family, double aimX, double aimY)
    {
        var rules = Game.Rules;
        var lob = Game.StarSkills.Pitch("lob")!;
        Assert.Equal(0.75, lob.Lob!.PaceMul);
        var star = new PitchCommand(family, 0, true, aimX, aimY);
        var plain = star with { Star = false };
        Assert.Equal(PitchFlight.Crossing(plain, rules), PitchFlight.Crossing(star, rules, "lob"));
        Assert.Equal(PitchFlight.Point(plain, 1, rules), PitchFlight.Point(star, 1, rules, "lob", skills: Game.StarSkills));
        // The clock: the speed is the ordinary one, so the flight time and the window are the plain pitch's.
        Assert.Equal(AtBatResolver.PitchSpeedMph(plain, 5, rules), AtBatResolver.PitchSpeedMph(star, 5, rules, "lob", Game.StarSkills));
        var release = PitchFlight.Point(plain, 0, rules);
        var plate = PitchFlight.Point(plain, 1, rules);
        var last = 0.0;
        for (var u = 0.01; u <= 0.99; u += 0.01)
        {
            var p = PitchFlight.Point(star, u, rules, "lob", skills: Game.StarSkills);
            var q = PitchFlight.Point(plain, u, rules);
            var along = (p.Z - release.Z) / (plate.Z - release.Z);
            Assert.True(along > last, "the lob never stops or runs back along its path");
            last = along;
            // Behind the plain ball all the way, and above it on the arc.
            Assert.True(along < (q.Z - release.Z) / (plate.Z - release.Z));
            Assert.True(p.Y > PitchFlight.Point(plain, lob.Lob.Progress(u), rules).Y);
        }
        // It leaves at three quarters of the pace and falls through the zone: its last stretch drops toward the plate.
        var start = (PitchFlight.Point(star, 0.001, rules, "lob", skills: Game.StarSkills).Z - release.Z) / (plate.Z - release.Z);
        Assert.Equal(0.75 * 0.001, start, 5);
        var mid = PitchFlight.Point(star, 0.5, rules, "lob", skills: Game.StarSkills);
        Assert.Equal(lob.Lob.ArcFt, mid.Y - PitchFlight.Point(plain, lob.Lob.Progress(0.5), rules).Y, 9);
        Assert.True(PitchFlight.Point(star, 0.98, rules, "lob", skills: Game.StarSkills).Y
                    > PitchFlight.Point(star, 1, rules, "lob", skills: Game.StarSkills).Y, "the lob falls into the zone");
    }

    // ---------------------------------------------------------------------------------
    // S-249  Star Sidearm: released 2 ft wider, the crossing unchanged
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("fastball", 0, 0, 0)]
    [InlineData("slider", 0.5, 0.2, -0.6)]
    [InlineData("changeup", -0.5, -0.3, 0.8)]
    public void S249_TheStarSidearmLeavesTwoFeetWiderAndCrossesOnTheSameSpot(string family, double aimX, double aimY, double rubberX)
    {
        var rules = Game.Rules;
        var star = new PitchCommand(family, 0, true, aimX, aimY, 0.4, rubberX);
        var plain = star with { Star = false };
        var hand = PitchFlight.Point(plain, 0, rules);
        var wide = PitchFlight.Point(star, 0, rules, "sidearm", skills: Game.StarSkills);
        Assert.Equal(2, Game.StarSkills.Pitch("sidearm")!.SidearmFt);
        Assert.Equal(hand.X + 2 * PitchFlight.SidearmSide(rules), wide.X, 9);
        Assert.True(Math.Abs(wide.X) > Math.Abs(hand.X), "wider, away from the middle");
        Assert.Equal(hand.Y, wide.Y, 9);
        Assert.Equal(hand.Z, wide.Z, 9);
        // The same crossing, to the last float step of the lerp that reaches it from the wider hand.
        var (px, py) = PitchFlight.Crossing(plain, rules);
        var (sx, sy) = PitchFlight.Crossing(star, rules, "sidearm");
        Assert.Equal(px, sx, 12);
        Assert.Equal(py, sy, 12);
        // The diagonal: the extra width shrinks evenly to nothing at the plate.
        foreach (var u in new[] { 0.25, 0.5, 0.75 })
            Assert.Equal(2 * PitchFlight.SidearmSide(rules) * (1 - u),
                PitchFlight.Point(star, u, rules, "sidearm", skills: Game.StarSkills).X - PitchFlight.Point(plain, u, rules).X, 9);
        // A hand the client passes moves out by the same 2 ft.
        var from = (1.0, 6.0, 55.0);
        Assert.Equal(1.0 + 2 * PitchFlight.SidearmSide(rules), PitchFlight.Point(star, 0, rules, "sidearm", from, Game.StarSkills).X, 9);
    }

    // ---------------------------------------------------------------------------------
    // S-250  Star Pull / Star Opposite: ±10° of spray by the batter's hand
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("pull", Hand.R, -10, 1.15)]
    [InlineData("pull", Hand.L, 10, 1.15)]
    [InlineData("opposite", Hand.R, 10, 1.1)]
    [InlineData("opposite", Hand.L, -10, 1.1)]
    public void S250_PullAndOppositeLeanTheSprayTenDegreesByTheBattersHand(string swing, Hand bats, double deg, double exitMul)
    {
        Assert.Equal(deg, Game.StarSkills.Swing(swing)!.PullSprayDeg(bats), 12);
        var pitcher = Game.Must("vale");
        var star = Game.Must("nico") with { Bats = bats, StarSwing = swing };
        var plain = star with { StarSwing = "no-such-swing" };
        var zone = StrikeZoneGeometry.For(star, Game.Rules);
        var a = Resolve(pitcher, star, false, true, zone.CenterY);
        var b = Resolve(pitcher, plain, false, true, zone.CenterY);
        Assert.Equal(a.Quality, b.Quality);
        Assert.NotEqual(ContactQuality.Sour, a.Quality);
        Assert.Equal(b.SprayDeg + deg, a.SprayDeg, 1);
        Assert.Equal(b.LaunchDeg, a.LaunchDeg);
        Assert.InRange(a.ExitVeloMph - b.ExitVeloMph * exitMul, -0.2, 0.2);
        // An ordinary swing of the same batter reads no lean.
        Assert.Equal(b.SprayDeg, Resolve(pitcher, star, false, false, zone.CenterY).SprayDeg);
    }

    // ---------------------------------------------------------------------------------
    // S-251  Star Chopper: launch −10°, the first bounce rises ×1.6
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(-12)]
    [InlineData(4)]
    [InlineData(20)]
    public void S251_TheStarChoppersFirstBounceRisesOnePointSixTimesAsFast(double spray)
    {
        var row = Game.StarSkills.Swing("chopper")!;
        Assert.Equal(-10, row.LaunchDeg);
        Assert.Equal(1.6, row.FirstHopBounceMul);
        var chop = Run(spray, "chopper");
        var plain = Run(spray, null);
        Assert.Empty(plain.Kicks);
        var hop = Assert.Single(chop.Kicks);
        Assert.Equal(1.6, hop.BounceMul);
        Assert.True(hop.T < 2, "the bend is inside two seconds");
        // The vertical speed off the hop, read on both paths over the first frame after it.
        var up = Rise(chop.Path!, hop.T);
        var plainUp = Rise(plain.Path!, hop.T);
        Assert.True(plainUp > 0);
        Assert.InRange(up / plainUp, 1.5, 1.7);
        Assert.NotNull(chop.Play);
    }

    static double Rise(IReadOnlyList<Sample> path, double t)
    {
        var a = BallFlight.PointAt(path, t, Game.Rules);
        var b = BallFlight.PointAt(path, t + Frame / 4, Game.Rules);
        return (b.Y - a.Y) / (Frame / 4);
    }

    static (List<FirstHopKicked> Kicks, IReadOnlyList<Sample>? Path, PlayEvent? Play) Run(double spray, string? swing)
    {
        var home = Game.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "soot", "vine", "moss", "hex");
        var away = Game.Team("Offense", "rio", "boom", "cinder", "grit", "kiwi", "nugget", "nico", "gull", "marlow");
        var match = Match.Exhibition(Game, home, away, 3, 1, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, 90, -10, spray, rules: match.Rules) with { StarSwingUsed = swing };
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0,
            LivePlayCommandSource.Cpu)).Snapshot.Active);
        var kicks = new List<FirstHopKicked>();
        IReadOnlyList<Sample>? path = null;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (play is not null) break;
            var kicked = live.Facts.OfType<FirstHopKicked>().ToList();
            kicks.AddRange(kicked);
            // The plain ball's path as it left the bat; the chopper's as the kick continued it.
            if (i == 0 && swing is null || kicked.Count > 0) path = live.Path;
        }
        return (kicks, path, play);
    }

    // ---------------------------------------------------------------------------------
    // S-252  Star Drag Bunt: rolls along the line and stops within 1 ft of fair
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(Hand.R, ParkId.Harbor, 0.0)]
    [InlineData(Hand.L, ParkId.Harbor, 0.0)]
    [InlineData(Hand.R, ParkId.Harbor, 0.35)]
    [InlineData(Hand.L, ParkId.Funfair, 0.0)]
    [InlineData(Hand.R, ParkId.Rooftop, 0.35)]
    [InlineData(Hand.L, ParkId.Rooftop, 0.0)]
    public void S252_TheDragBuntRollsAlongTheLineAndStopsJustFair(Hand bats, string parkId, double crossingOffset)
    {
        var row = Game.StarSkills.Swing("drag-bunt")!;
        Assert.Equal(0.5, row.DragBunt!.InsetFt);
        var batter = Game.Must("tumble") with { Bats = bats };
        Assert.Equal("drag-bunt", Game.Must("tumble").StarSwing);
        var park = Game.Parks[parkId];
        var zone = StrikeZoneGeometry.For(batter, Game.Rules);
        var hit = new AtBatResolver(Game.Chemistry, Game.Rules, Game.StarSkills).Resolve(
            Input(Game.Must("vale"), batter, false, true, zone.CenterY + crossingOffset), park, new Random(1));
        Assert.NotEqual(ContactQuality.Miss, hit.Quality);
        Assert.Equal(BattedBallClass.Bunt, hit.Class);
        Assert.True(hit.InPlay && !hit.Foul, $"a {hit.Quality} drag bunt is fair");
        Assert.Equal(Game.Rules.Batting.Bunt.Response.ExitMph.For(hit.Quality), hit.ExitVeloMph);
        Assert.Equal(row.LaunchDeg, hit.LaunchDeg);
        var side = SwingDragBunt.Side(bats);
        Assert.Equal(bats == Hand.R ? -1 : 1, side);
        var ball = BattedBall.Of(hit, park, Game.Rules);
        var rest = ball.Samples[^1];
        var inside = SwingDragBunt.InsideLineFt(rest.X, rest.Z, side);
        Assert.InRange(inside, 1e-9, 1.0);
        // Along the line: every point of the roll is fair, and the ball runs up its own side of the field.
        Assert.All(ball.Samples.Skip(1), s => Assert.True(SwingDragBunt.InsideLineFt(s.X, s.Z, side) > -1e-6));
        Assert.True(Math.Sign(rest.X) == side);
    }

    // ---------------------------------------------------------------------------------
    // S-253  Species carry the specials; the validator refuses the rest
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S253_EverySidekickCarriesItsSpeciesSpecialsFromTheGenericPool()
    {
        foreach (var c in Game.Characters.Values.Where(c => !c.Captain))
        {
            var sp = Game.Species[c.Species];
            Assert.Equal(sp.StarPitch, c.StarPitch);
            Assert.Equal(sp.StarSwing, c.StarSwing);
        }
        Assert.All(Game.Species.Values, s =>
        {
            Assert.Equal(StarSkills.GenericKind, Game.StarSkills.Pitch(s.StarPitch)!.Kind);
            Assert.Equal(StarSkills.GenericKind, Game.StarSkills.Swing(s.StarSwing)!.Kind);
        });
        // Fourteen generics, the six and the eight, and every new one is some species' own.
        Assert.Equal(7, Game.StarSkills.Pitches.Values.Count(p => p.Kind == StarSkills.GenericKind));
        Assert.Equal(7, Game.StarSkills.Swings.Values.Count(p => p.Kind == StarSkills.GenericKind));
        foreach (var id in NewPitches) Assert.Contains(Game.Species.Values, s => s.StarPitch == id);
        foreach (var id in NewSwings) Assert.Contains(Game.Species.Values, s => s.StarSwing == id);
        // The parks differ: not every faction's three species carry the three build defaults.
        Assert.True(Game.Species.Values.Select(s => s.Faction).Distinct().Count(f =>
            Game.Species.Values.Where(s => s.Faction == f).Any(s => NewPitches.Contains(s.StarPitch) || NewSwings.Contains(s.StarSwing))) >= 6);
    }

    [Fact]
    public void S253_ASidekickNamingASpecialOrASpeciesCarryingACaptainsIsRefusedByName()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeArray("characters/role-players.json", rows =>
        {
            rows[0]!["starPitch"] = "fastball";
            rows[1]!["starSwing"] = "line";
        });
        fixture.ChangeObject("world/species.json", json =>
        {
            var rows = json["species"]!.AsArray();
            rows.First(r => r!["id"]!.GetValue<string>() == "walrams")!["starPitch"] = "heatball";
            rows.First(r => r!["id"]!.GetValue<string>() == "rollers")!["starSwing"] = "heat-swing";
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("sidekick 'nico' names starPitch 'fastball'; a sidekick carries its species' specials", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("sidekick 'pip' names starSwing 'line'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("species 'walrams' starPitch 'heatball' is a captain's special", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("species 'rollers' starSwing 'heat-swing' is a captain's special", StringComparison.Ordinal));
    }

    [Fact]
    public void S253_TheNewNumbersAreValidatedInRangeAndOnTheRightKind()
    {
        using var fixture = new ContentFixture();
        fixture.ChangeObject("abilities/star-skills.json", json =>
        {
            json["pitches"]!["sinkball"]!["sinkDeg"] = 0;
            json["pitches"]!["lob"]!["lob"] = new JsonObject { ["paceMul"] = 1, ["arcFt"] = 5 };
            json["pitches"]!["sidearm"]!["sidearmFt"] = 4;
            json["pitches"]!["dot"]!["pullDeg"] = 5;
            json["swings"]!["pull"]!["pullDeg"] = 30;
            json["swings"]!["opposite"]!["dot"] = true;
            json["swings"]!["drag-bunt"]!["dragBunt"] = new JsonObject { ["insetFt"] = 2 };
            json["swings"]!["chopper"]!["launchDeg"] = -20;
            json["swings"]!["drag-bunt"]!["firstHopBounceMul"] = 0.8;
            json["pitches"]!["lob"]!["firstHopBounceMul"] = 1.6;
        });
        var errors = ContentDataValidator.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains("star pitch 'sinkball' sinkDeg must be greater than 0", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'lob' lob needs", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'sidearm' sidearmFt must be greater than 0 and at most 3", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'dot' cannot carry pullDeg", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'pull' pullDeg must be non-zero and at most 20", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'opposite' cannot carry dot", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'drag-bunt' dragBunt insetFt must be greater than 0 and at most 1", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'chopper' launchDeg must be between -15 and 60", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star swing 'drag-bunt' firstHopBounceMul must be greater than 1 and at most 3", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("star pitch 'lob' cannot carry firstHopBounceMul", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------

    static AtBatResult Resolve(Character pitcher, Character batter, bool starPitch, bool starSwing, double crossingY) =>
        new AtBatResolver(Game.Chemistry, Game.Rules, Game.StarSkills).Resolve(
            Input(pitcher, batter, starPitch, starSwing, crossingY), Game.Parks[ParkId.Harbor], new Random(1));

    static AtBatInput Input(Character pitcher, Character batter, bool starPitch, bool starSwing, double crossingY) =>
        new(pitcher, batter, null, [], false, false, 0, starPitch, starSwing, null, 80, CrossingY: crossingY);
}
