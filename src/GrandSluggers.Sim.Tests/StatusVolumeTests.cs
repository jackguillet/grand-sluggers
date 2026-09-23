using System.Reflection;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The status volume, live and per body (§14; FR-07, FD-08-R1, FD-08-R2; F4-b, #896). A body — a fielder or a runner —
/// that touches a <c>statusVolume</c> disc runs at <c>fielding.chase.frozenMul</c> (0.45) for the row's <c>slowSec</c>
/// (3.0 s, Jack's number) from the touch; staying inside keeps it slowed, and leaving and entering again starts the time
/// again. Nobody else is slowed, nothing is read from where the ball lands, and a slowed glove's catch is decided by the
/// glove and the ball, never by <c>fielding.drops.frozen</c>.
///
/// <para>
/// The fixtures that need a volume where a body will certainly run add one to Harbor as a <see cref="Park"/> record, which
/// the content validator never sees — the placement rule (FD-19, <c>SF-23</c>) keeps every catalog volume off the lanes,
/// so no catalog runner can reach one. The Rink rows play the catalog's own volumes. Tagged <c>Rows=compact</c>: every row
/// holds on the shipped root and on <c>trials/c80</c> (whose response law ramps a body into and out of the slow).
/// </para>
/// </summary>
public sealed class StatusVolumeTests
{
    static readonly ContentCatalog Catalog = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;
    const double SlowSec = 3.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    // ---------------------------------------------------------------------------------
    // The number is data (FD-08-R2): slowSec on the three volumes, validated
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>slowSec</c> is 3.0 on the three status volumes on both roots — Jack's number (FD-08-R2), not tuned per root —
    /// and absent from every other row; the JSON is the code fallback; the slow's factor is still <c>frozenMul</c> 0.45.
    /// </summary>
    [Fact]
    public void FD08R2_EveryStatusVolumeSlowsForThreeSecondsOnBothRootsAndNothingElseCarriesATime()
    {
        var trial = new DataRoot(Catalog.Root.Shipped, Path.GetFullPath(Path.Combine(Catalog.Root.Shipped, "..", "trials", "c80")));
        foreach (var root in new[] { Catalog.Root, trial })
        {
            var rules = RulesTable.Load(root);
            Assert.Equal(0.45, rules.Fielding.Chase.FrozenMul);
            foreach (var type in HazardType.All)
            {
                var row = rules.Hazards.Of(type);
                Assert.Equal(RulesTable.Defaults.Hazards.Of(type).SlowSec, row.SlowSec);
                if (row.Pattern == HazardPattern.StatusVolume) Assert.Equal(SlowSec, row.SlowSec);
                else Assert.Null(row.SlowSec);
            }
        }
        Assert.Equal(
            new[] { HazardType.FreezeVolume, HazardType.LavaPit, HazardType.FireBreath },
            HazardType.All.Where(t => Catalog.Rules.Hazards.Of(t).SlowSec is not null));
    }

    /// <summary>
    /// The row's time is a rule like any other: a volume with none, a time on a row that never slows anyone, a time that
    /// is not above zero, and a JSON <c>null</c> for it all stop the load and say which.
    /// </summary>
    [Theory]
    [InlineData("freezeVolume", "remove", "hazards.freezeVolume is a statusVolume and must author slowSec")]
    [InlineData("warpPipe", "3", "hazards.warpPipe.slowSec is 3, but only a statusVolume slows a body")]
    [InlineData("lavaPit", "0", "hazards.lavaPit.slowSec must be greater than 0; got 0")]
    [InlineData("fireBreath", "-1", "hazards.fireBreath.slowSec must be greater than 0; got -1")]
    [InlineData("fireBreath", "null", "hazards.fireBreath.slowSec must be a number; leave the key out rather than writing null")]
    public void FD08R2_ARowsTimeIsValidatedLikeEveryOtherRule(string row, string value, string expected)
    {
        using var fixture = new RulesCopy();
        fixture.Hazards(json =>
        {
            var obj = json[row]!.AsObject();
            if (value == "remove") obj.Remove("slowSec");
            else if (value == "null") obj["slowSec"] = null;
            else obj["slowSec"] = double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        });
        var errors = RulesTable.Validate(fixture.Root);
        Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root));
    }

    // ---------------------------------------------------------------------------------
    // The per-body timer, on its own
    // ---------------------------------------------------------------------------------

    static readonly StatusVolume Disc = new(0, HazardType.FreezeVolume, 0, 100, 8, SlowSec);

    /// <summary>
    /// <c>SF-20</c>, the clock: the touch slows the body for 3 s from the touch; staying inside keeps it slowed past them;
    /// leaving lets the time run out; entering again starts it again from the new touch; and each touch is one fact.
    /// </summary>
    [Fact]
    public void SF20_TheTouchSlowsForTheRowsTimeStayingInsideKeepsItAndEnteringAgainStartsItAgain()
    {
        var slows = new BodySlows();
        slows.Begin([Disc]);
        // Outside: nothing.
        Assert.Empty(slows.Read("CF", 0, 120, 0, immune: false));
        Assert.False(slows.Slowed("CF"));
        // The touch at 1.0 s: slowed until 4.0 s.
        var touch = Assert.Single(slows.Read("CF", 0, 107.5, 1.0, immune: false));
        Assert.Equal((Disc, 4.0), touch);
        Assert.True(slows.Slowed("CF"));
        // Still inside at 5.0 s, past the time: still slowed, and no second touch.
        Assert.Empty(slows.Read("CF", 0, 100, 5.0, immune: false));
        Assert.True(slows.Slowed("CF"));
        // Out at 5.5 s: the time ran out at 4.0, so the body runs free.
        Assert.Empty(slows.Read("CF", 0, 90, 5.5, immune: false));
        Assert.False(slows.Slowed("CF"));
        // In again at 6.0 s and out again at once: a new touch, slowed to 9.0 s whether it stays or not.
        Assert.Equal((Disc, 9.0), Assert.Single(slows.Read("CF", 0, 93, 6.0, immune: false)));
        Assert.Empty(slows.Read("CF", 0, 120, 6.1, immune: false));
        Assert.True(slows.Slowed("CF"));
        Assert.Empty(slows.Read("CF", 0, 120, 8.99, immune: false));
        Assert.True(slows.Slowed("CF"));
        Assert.Empty(slows.Read("CF", 0, 120, 9.0, immune: false));
        Assert.False(slows.Slowed("CF"));
        // A frame count, not a rounding: a body that touched at 0 and left is slowed for exactly 180 frames of 1/60 s.
        var clock = new BodySlows();
        clock.Begin([Disc]);
        clock.Read("SS", 0, 100, 0, immune: false);
        var frames = 0;
        for (var i = 1; i < 400; i++)
        {
            clock.Read("SS", 0, 200, i * Frame, immune: false);
            if (clock.Slowed("SS")) frames++;
        }
        Assert.Equal(179, frames); // frames 1 … 179 after the touch frame 0: 180 slowed frames in all
        // The other bodies were never read and are not slowed.
        Assert.False(slows.Slowed("LF"));
    }

    /// <summary><c>SF-20</c>: a Burrow body is never slowed and touches nothing; a park with no volume slows nobody.</summary>
    [Fact]
    public void SF20_AnImmuneBodyAndAParkWithNoVolumeSlowNobody()
    {
        var slows = new BodySlows();
        slows.Begin([Disc]);
        Assert.Empty(slows.Read("SS", 0, 100, 0, immune: true));
        Assert.False(slows.Slowed("SS"));
        var none = new BodySlows();
        none.Begin([]);
        Assert.Empty(none.Read("SS", 0, 100, 0, immune: false));
        Assert.False(none.Slowed("SS"));
    }

    // ---------------------------------------------------------------------------------
    // SF-20 live: the fielder who runs in, nobody else, the runner who strays in
    // ---------------------------------------------------------------------------------

    /// <summary>Harbor with one more hazard instance, the index it has in the park's list.</summary>
    static (Park Park, int Index) HarborWith(string type, double x, double z, double radius)
    {
        var harbor = Catalog.MustPark("harbor-diamond");
        var park = harbor with { Hazards = [.. harbor.Hazards, new Hazard(type, x, z, radius, null)] };
        return (park, park.Hazards.Count - 1);
    }

    sealed record Played(Match Match, LivePlaySystem Live, FieldingPreview Preview, PlayTrace Trace,
        List<Dictionary<string, (double X, double Z, bool Slowed)>> Bodies, List<IReadOnlyList<BodySlowed>> Touches,
        List<(double Feet, double Velocity, bool Slowed, int Bag)> Batter);

    /// <summary>A CPU live ball from contact to Time (or 20 s), with every fielder and the batter-runner recorded each frame.</summary>
    static Played Play(Match match, AtBatResult hit, LiveSeats? seats = null, Func<int, LivePlaySystem, LivePadInput>? pad = null)
    {
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        live.Recording = true;
        var human = seats is not null;
        var source = human ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;
        var field = human ? null : match.ResolveFielding(hit, preview);
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field,
            seats ?? LiveSeats.CpuOnly, 0, source)).Snapshot.Active);
        var batter = live.Runners.Single(r => r.IsBatter);
        var bodies = new List<Dictionary<string, (double X, double Z, bool Slowed)>>();
        var touches = new List<IReadOnlyList<BodySlowed>>();
        var run = new List<(double, double, bool, int)>();
        PlayEvent? done = null;
        for (var i = 0; i < 60 * 20 && done is null; i++)
        {
            var input = pad?.Invoke(i, live) ?? LivePadInput.Dead;
            done = live.Apply(LivePlayCommand.Tick(Frame, input, LivePadInput.Dead, false, source)).CompletedPlay;
            if (done is not null) break;
            bodies.Add(live.Fielders.ToDictionary(kv => kv.Key,
                kv => (kv.Key == live.GlovePos ? live.GloveX : kv.Value.X, kv.Key == live.GlovePos ? live.GloveZ : kv.Value.Z, live.IsSlowed(kv.Key))));
            touches.Add(live.Slows.ToArray());
            run.Add((batter.Feet, batter.Velocity, live.IsSlowed(batter), batter.Bag));
        }
        return new Played(match, live, preview, live.TakeTrace(done), bodies, touches, run);
    }

    /// <summary>
    /// <c>SF-20</c>: the glove that goes out for a shallow fly to centre (the centre fielder on the shipped root, the second
    /// baseman on the copy — whoever the preview sends) crosses a freeze volume set two fifths of the way along his run. He is
    /// slowed from the frame he stands in it, for 3 s from that touch (longer only while he is still inside), at 0.45 of the
    /// speed the chase asks; nobody else on the field is slowed; the touch is one typed fact and one trace mark naming him and
    /// the instance. The volume does not change the preview: the same ball at plain Harbor sends the same glove.
    /// </summary>
    [Fact]
    public void SF20_AFielderWhoRunsIntoAVolumeIsSlowedForThreeSecondsAndNoOtherBodyIs()
    {
        var harbor = Catalog.MustPark("harbor-diamond");
        var carry = Diamond.Positions["CF"].Z - 75;
        var plain = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), harbor, seed: 1)
            .PreviewHit(FlightFixtures.Landing(harbor, carry, 40, 0));
        var chaser = plain.Position;
        var from = Diamond.Positions[chaser];
        var (park, index) = HarborWith(HazardType.FreezeVolume,
            from.X + (plain.LandingX - from.X) * 0.4, from.Z + (plain.LandingZ - from.Z) * 0.4, 8);
        Assert.True(Diamond.Dist(from.X, from.Z, park.Hazards[index].X, park.Hazards[index].Z) > 12, "he starts outside it");
        var match = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: 1);
        var p = Play(match, FlightFixtures.Landing(park, carry, 40, 0));
        Assert.Equal(chaser, p.Preview.Position);
        Assert.Equal(plain, p.Preview with { Ball = plain.Ball });
        Assert.False(p.Preview.Frozen, "the preview reads no volume");

        var touch = Assert.Single(p.Live.SlowsThisPlay);
        Assert.Equal((chaser, index, HazardType.FreezeVolume, false), (touch.Pos, touch.Hazard, touch.Type, touch.IsRunner));
        Assert.Same(FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves)[chaser], touch.Who);
        Assert.Equal(touch.T + SlowSec, touch.UntilT, 9);
        // The touch is where the last frame left him: inside the disc, and the frame before he was not.
        var at = p.Touches.FindIndex(t => t.Count > 0);
        Assert.True(at > 0);
        var disc = ParkHazards.StatusVolumes(park, rules: match.Rules).Single();
        var before = p.Bodies[at - 1][chaser];
        Assert.True(disc.Contains(before.X, before.Z));
        Assert.False(disc.Contains(p.Bodies[at - 2][chaser].X, p.Bodies[at - 2][chaser].Z));

        // Slowed exactly from the touch to its time, or while inside; nobody else on any frame.
        for (var f = 0; f < p.Bodies.Count; f++)
        {
            var t = (f + 1) * Frame;
            foreach (var (pos, body) in p.Bodies[f])
            {
                if (pos != chaser)
                {
                    Assert.False(body.Slowed, $"{pos} at frame {f}");
                    continue;
                }
                var startT = f * Frame;
                var startAt = f == 0 ? Diamond.Positions[chaser] : (p.Bodies[f - 1][chaser].X, p.Bodies[f - 1][chaser].Z);
                var expected = startT + 1e-9 >= touch.T && (startT < touch.UntilT - 1e-9 || disc.Contains(startAt.Item1, startAt.Item2));
                Assert.True(expected == body.Slowed, $"{chaser} slowed {body.Slowed} at frame {f} (t {t:0.000})");
            }
        }

        // The slow is 0.45 of the asked chase: past the response law's brake, no slowed step is longer than that.
        var who = touch.Who;
        var asked = FieldingResolver.ChaseSpeedFt(who, chaser, p.Preview, match.Rules);
        var brakeFrames = (int)Math.Ceiling(match.Rules.Fielding.Chase.BrakeSec / Frame) + 1;
        var slowedSteps = new List<double>();
        for (var f = at + brakeFrames; f < p.Bodies.Count; f++)
        {
            if (!p.Bodies[f][chaser].Slowed) break;
            var a = p.Bodies[f - 1][chaser];
            var b = p.Bodies[f][chaser];
            slowedSteps.Add(Diamond.Dist(a.X, a.Z, b.X, b.Z) / Frame);
        }
        Assert.NotEmpty(slowedSteps);
        Assert.All(slowedSteps, v => Assert.True(v <= asked * match.Rules.Fielding.Chase.FrozenMul + 1e-6, $"{v} > 0.45 × {asked}"));
        // And he ran at it: the chase the slow scales is real, not a body standing still.
        Assert.Contains(slowedSteps, v => v >= asked * match.Rules.Fielding.Chase.FrozenMul * 0.9);

        // The trace carries the touch as a typed mark, and the frames he ran slowed.
        var mark = Assert.Single(p.Trace.Marks!, m => m.Kind == PlayTraceMarkKind.BodySlowed);
        Assert.Equal((chaser, touch.T, index), (mark.Fielder, mark.T, mark.Hazard!.Index));
        Assert.Null(mark.Runner);
        Assert.Equal(touch.UntilT, mark.Hazard.UntilT);
        Assert.Contains(p.Trace.Ticks, tick => tick.Fielders!.Any(x => x.Pos == chaser && x.Slowed == true));
        Assert.All(p.Trace.Ticks, tick => Assert.All(tick.Fielders!.Where(x => x.Pos != chaser), x => Assert.Null(x.Slowed)));
    }

    /// <summary>
    /// <c>SF-20</c>: the human glove steered into a volume, out of it, and back in: two touches, each slowing the body for
    /// 3 s from itself; the second starts the time again.
    /// </summary>
    [Fact]
    public void SF20_LeavingAndEnteringAgainStartsTheTimeAgain()
    {
        var ss = Diamond.Positions["SS"];
        // A disc just beside the short stop: two steps across it and back is the in-out-in.
        var (park, index) = HarborWith(HazardType.LavaPit, ss.X - 9, ss.Z, 4);
        var match = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: 1);
        // A high fly to the short stop's spot: he owns it, and it hangs while the stick walks him.
        var carry = Diamond.Dist(0, 0, ss.X, ss.Z);
        var spray = Math.Atan2(ss.X, ss.Z) * 180 / Math.PI;
        var neutral = 6;
        var disc = new StatusVolume(index, HazardType.LavaPit, ss.X - 9, ss.Z, 4, SlowSec);
        // West until he stands in the disc, east until he has been out of it for a sixth of a second, then west again.
        var phase = 0;
        var outside = 0;
        var p = Play(match, FlightFixtures.Landing(park, carry, 70, spray), HumanGlove, (i, live) =>
        {
            if (i < neutral || live.GlovePos != "SS") return LivePadInput.Dead;
            var inside = disc.Contains(live.GloveX, live.GloveZ);
            if (phase == 0 && inside) phase = 1;
            else if (phase == 1 && !inside && ++outside >= 10) phase = 2;
            return new LivePadInput(StickX: phase == 1 ? 1 : -1, StickY: 0);
        });
        Assert.Equal(2, phase);
        var touches = p.Live.SlowsThisPlay.Where(t => t.Pos == "SS").ToArray();
        Assert.True(touches.Length >= 2, $"{touches.Length} touches");
        Assert.All(touches, t => Assert.Equal(index, t.Hazard));
        Assert.All(touches, t => Assert.Equal(t.T + SlowSec, t.UntilT, 9));
        Assert.True(touches[1].T > touches[0].T);
        Assert.Equal(touches[1].T + SlowSec, touches[1].UntilT, 9);
        Assert.Equal(touches.Length, p.Trace.Marks!.Count(m => m.Kind == PlayTraceMarkKind.BodySlowed && m.Fielder == "SS"));
    }

    /// <summary><c>SF-20</c>: a Burrow fielder standing in a volume is not slowed and touches nothing (FieldAbilities.IgnoresParkSlow).</summary>
    [Fact]
    public void SF20_TheBurrowFielderIsNeverSlowed()
    {
        // Ember Court defends the top here, so Soot is on the field.
        var probe = new Match(Catalog, PresetTeams.SparkAllStars(Catalog), PresetTeams.EmberCourt(Catalog), Catalog.MustPark("harbor-diamond"), seed: 1);
        var map = FieldingResolver.Assign(probe.DefenseRoster, probe.Pitcher, probe.Defense.Gloves);
        var burrow = map.Single(kv => FieldAbilities.IgnoresParkSlow(kv.Value));
        var other = map.First(kv => kv.Key is not ("P" or "C") && !FieldAbilities.IgnoresParkSlow(kv.Value));
        var at = Diamond.Positions[burrow.Key];
        var near = Diamond.Positions[other.Key];
        var harbor = Catalog.MustPark("harbor-diamond");
        var park = harbor with
        {
            Hazards = [.. harbor.Hazards, new Hazard(HazardType.FreezeVolume, at.X, at.Z, 6, null), new Hazard(HazardType.FreezeVolume, near.X, near.Z, 6, null)]
        };
        var match = new Match(Catalog, PresetTeams.SparkAllStars(Catalog), PresetTeams.EmberCourt(Catalog), park, seed: 1);
        var p = Play(match, FlightFixtures.Landing(park, 200, 30, 20));
        // Both stood inside from the first frame; only the one without Burrow was slowed.
        Assert.DoesNotContain(p.Live.SlowsThisPlay, t => t.Pos == burrow.Key);
        Assert.All(p.Bodies, frame => Assert.False(frame[burrow.Key].Slowed));
        Assert.Equal(other.Key, p.Live.SlowsThisPlay[0].Pos);
        Assert.Equal(0.0, p.Live.SlowsThisPlay[0].T);
        Assert.True(p.Bodies[0][other.Key].Slowed);
    }

    /// <summary>
    /// <c>SF-20</c>, the runner: a volume on the path to first (a fixture — FD-19 keeps every catalog volume off the lanes)
    /// slows the batter-runner who crosses it, at his own speed × 0.45, for 3 s from the touch; the runner model applies it
    /// to his speed along the path, the one way his body moves.
    /// </summary>
    [Fact]
    public void SF20_ARunnerWhoStraysIntoAVolumeIsSlowedTheSameWay()
    {
        var first = Diamond.First;
        // Two thirds of the way down the line to first, radius 4: on his path out of the box.
        var (park, index) = HarborWith(HazardType.LavaPit, first.X * 2 / 3, first.Z * 2 / 3, 4);
        var match = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: 1);
        var batter = match.Batter;
        // A grounder to the left side: the batter runs it out.
        var third = Diamond.Positions["3B"];
        var p = Play(match, FlightFixtures.Landing(park, Diamond.Dist(0, 0, third.X, third.Z) * 0.9, 4, -30));
        var touch = Assert.Single(p.Live.SlowsThisPlay, t => t.IsRunner);
        Assert.Same(batter, touch.Who);
        Assert.Equal(index, touch.Hazard);
        Assert.Equal(touch.T + SlowSec, touch.UntilT, 9);
        Assert.DoesNotContain(p.Live.SlowsThisPlay, t => !t.IsRunner);
        var speed = RunnerSystem.SpeedFtPerSec(touch.Who, 0, match.Rules);
        var slowed = p.Batter.Where(b => b.Slowed && b.Velocity > 0).ToArray();
        Assert.NotEmpty(slowed);
        Assert.All(slowed, b => Assert.Equal(speed * match.Rules.Fielding.Chase.FrozenMul, b.Velocity, 9));
        // Before the touch he ran at his own speed.
        Assert.Contains(p.Batter, b => !b.Slowed && Math.Abs(b.Velocity - speed) < 1e-9);
        var mark = Assert.Single(p.Trace.Marks!, m => m.Kind == PlayTraceMarkKind.BodySlowed);
        Assert.Equal(touch.Who.Id, mark.Runner!.Id);
        Assert.Null(mark.Fielder);
        Assert.True(mark.Runner.Slowed);
    }

    // ---------------------------------------------------------------------------------
    // SF-20 at the catalog park, and the preview
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-20</c> at the Rink, by root, on the catalog's own volume: the centre fielder runs in on a fly toward the deep
    /// freeze volume and is slowed from the frame he enters it; nobody else is; the preview is not frozen, whoever the
    /// glove is and wherever the ball lands.
    /// </summary>
    [Fact]
    public void SF20_AtTheRinkTheChaserWhoEntersTheDeepVolumeIsTheOnlyBodySlowed()
    {
        var rink = Catalog.MustPark("crystal-rink");
        var deep = rink.Hazards
            .Select((h, i) => (h, i))
            .Where(x => Catalog.Rules.Hazards.Of(x.h.Type).Pattern == HazardPattern.StatusVolume)
            .OrderByDescending(x => Diamond.Dist(0, 0, x.h.X, x.h.Z))
            .First();
        var carry = Diamond.Dist(0, 0, deep.h.X, deep.h.Z);
        var spray = Math.Atan2(deep.h.X, deep.h.Z) * 180 / Math.PI;
        var match = Match.Slice(Catalog, seed: 1, parkId: "crystal-rink");
        // A high fly (50°): the second baseman who goes out for it is still under it when it comes down, in the disc.
        var p = Play(match, FlightFixtures.Landing(rink, carry, 50, spray));
        Assert.False(p.Preview.Frozen);
        Assert.NotEmpty(p.Live.SlowsThisPlay);
        Assert.All(p.Live.SlowsThisPlay, t => Assert.Equal(deep.i, t.Hazard));
        var slowedBodies = p.Live.SlowsThisPlay.Select(t => t.Pos).Distinct().ToArray();
        foreach (var frame in p.Bodies)
            foreach (var (pos, body) in frame)
                if (!slowedBodies.Contains(pos)) Assert.False(body.Slowed, pos);
    }

    // ---------------------------------------------------------------------------------
    // SF-22: no result by chance
    // ---------------------------------------------------------------------------------

    /// <summary>Counts every draw on the match's one stream without changing a value it hands out.</summary>
    sealed class CountingRandom(Random inner) : Random
    {
        public int Draws { get; private set; }
        public override int Next() { Draws++; return inner.Next(); }
        public override int Next(int maxValue) { Draws++; return inner.Next(maxValue); }
        public override int Next(int minValue, int maxValue) { Draws++; return inner.Next(minValue, maxValue); }
        public override double NextDouble() { Draws++; return inner.NextDouble(); }
        public override long NextInt64() { Draws++; return inner.NextInt64(); }
        public override long NextInt64(long maxValue) { Draws++; return inner.NextInt64(maxValue); }
        public override long NextInt64(long minValue, long maxValue) { Draws++; return inner.NextInt64(minValue, maxValue); }
        public override float NextSingle() { Draws++; return inner.NextSingle(); }
        public override void NextBytes(byte[] buffer) { Draws++; inner.NextBytes(buffer); }
        public override void NextBytes(Span<byte> buffer) { Draws++; inner.NextBytes(buffer); }
        protected override double Sample() { Draws++; return inner.NextDouble(); }
    }

    static readonly FieldInfo RngField = typeof(Match).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)!;

    static CountingRandom Count(Match match)
    {
        var counting = new CountingRandom((Random)RngField.GetValue(match)!);
        RngField.SetValue(match, counting);
        return counting;
    }

    /// <summary>
    /// <c>SF-22</c>, both roots, over a seed set: a fly to the short stop, who stands in a freeze volume from the crack and is
    /// slowed the whole play, is caught by the glove on every seed, and the live ball draws nothing from the match's stream —
    /// no <c>drops.frozen</c> roll. The heart swing, the special that still owns that table, is the control: the same ball
    /// under it draws the one drop roll, so the probe sees a draw when there is one.
    /// </summary>
    [Fact]
    public void SF22_ASlowedFieldersCatchIsDecidedByTheGloveAndTheBallWithNoDropRollDrawn()
    {
        var ss = Diamond.Positions["SS"];
        var (park, index) = HarborWith(HazardType.FreezeVolume, ss.X, ss.Z, 10);
        var carry = Diamond.Dist(0, 0, ss.X, ss.Z);
        var spray = Math.Atan2(ss.X, ss.Z) * 180 / Math.PI;
        var caught = 0;
        for (var seed = 1; seed <= 12; seed++)
        {
            var match = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: seed);
            var hit = FlightFixtures.Landing(park, carry, 70, spray);
            var preview = match.PreviewHit(hit);
            Assert.Equal("SS", preview.Position);
            Assert.False(preview.Frozen);
            var rng = Count(match);
            var p = Play(match, hit);
            Assert.Equal(0, rng.Draws);
            var touch = Assert.Single(p.Live.SlowsThisPlay);
            Assert.Equal(("SS", index, 0.0), (touch.Pos, touch.Hazard, touch.T));
            Assert.All(p.Bodies, frame => Assert.True(frame["SS"].Slowed));
            var outcome = p.Trace.Completed!;
            Assert.Contains(outcome.Outs, o => o.Type == OutType.Catch && o.Fielder is not null);
            caught++;
        }
        Assert.Equal(12, caught);

        // The control: the heart swing freezes the glove and rolls drops.frozen once.
        var control = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog), park, seed: 1);
        var heart = FlightFixtures.Landing(park, carry, 70, spray) with { StarSwingUsed = "heart-swing" };
        Assert.True(control.PreviewHit(heart).Frozen);
        var counted = Count(control);
        Play(control, heart);
        // The drop roll is the first draw; a dropped ball's live play may draw on after it (the pickup, the throw).
        Assert.True(counted.Draws >= 1, $"{counted.Draws} draws under the heart swing");
    }

    // ---------------------------------------------------------------------------------
    // Fixtures
    // ---------------------------------------------------------------------------------

    /// <summary>A throwaway copy of the shipped data root whose hazards.json a row can edit.</summary>
    sealed class RulesCopy : IDisposable
    {
        public RulesCopy()
        {
            Root = Path.Combine(Path.GetTempPath(), "grand-sluggers-896-" + Guid.NewGuid().ToString("N"));
            var source = Catalog.Root.Shipped;
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(Root, Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(Root, Path.GetRelativePath(source, file)));
        }

        public string Root { get; }

        public void Hazards(Action<JsonObject> change)
        {
            var path = Path.Combine(Root, RulesTable.Directory, "hazards.json");
            var json = JsonNode.Parse(File.ReadAllText(path), null, new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString());
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
