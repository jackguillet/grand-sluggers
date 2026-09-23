using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// P7 (#569): the difficulty ladder is a match's rung over shared tables; the star meter and the MVP
/// read stars.json; items are field effects with geometry and no roll; the park's night window is park data.
/// </summary>
public sealed class BalanceTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    static readonly PitchCommand Paint = new("fastball", 0, false);
    static readonly SwingCommand Swing = new(true, 0, 0, false);

    // ---------------------------------------------------------------------------------
    // Difficulty ladder (§16 cpu.json)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AMatchPlaysAtItsOwnRungOverTheSharedTables()
    {
        var normal = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1);
        var hard = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1, difficulty: "hard");
        var easy = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1, difficulty: "EASY");

        Assert.Same(_content.Rules, normal.Rules);
        Assert.Equal("normal", normal.Difficulty);
        Assert.Equal("hard", hard.Difficulty);
        Assert.Equal("easy", easy.Difficulty);
        Assert.Equal(_content.Rules.Cpu.Hard.MakeableMarginSec, hard.Rules.Cpu.Active.MakeableMarginSec);
        Assert.Equal(_content.Rules.Cpu.Easy.RunnerMarginSec, easy.Rules.Cpu.Active.RunnerMarginSec);
        // Every other table is the catalog's own: one set of rules, one rung.
        Assert.Same(_content.Rules.Fielding, hard.Rules.Fielding);
        Assert.Same(_content.Rules.Running, hard.Rules.Running);
        Assert.Same(_content.Rules.Batting, easy.Rules.Batting);
        // An unknown rung is the default, never a crash.
        Assert.Equal("normal", Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1, difficulty: "brutal").Difficulty);
    }

    [Fact]
    public void TheTitleCyclesTheLadderNextToTheInnings()
    {
        Assert.Equal(new[] { "easy", "normal", "hard" }, CpuRules.Levels);
        Assert.Equal("normal", CpuRules.Next("easy"));
        Assert.Equal("hard", CpuRules.Next("normal"));
        Assert.Equal("easy", CpuRules.Next("hard"));
        Assert.Equal("3 INNINGS  ·  NORMAL", CarnivalFront.TitleSetup(3, "normal"));
        Assert.Equal("9 INNINGS  ·  HARD", CarnivalFront.TitleSetup(9, "hard"));
        Assert.Equal("NORMAL", CarnivalFront.DifficultyLabel("nope"));
        // The rung says what it changes (#612): the CPU's skill. Since #860 (PH-17) it never touches a
        // pad's swing window, so the title prints no window multiplier (#876).
        Assert.Equal("3 INNINGS  ·  EASY  ·  CPU SKILL", CarnivalFront.TitleSetup(3, "easy", _content.Rules));
        Assert.Equal("3 INNINGS  ·  NORMAL  ·  CPU SKILL", CarnivalFront.TitleSetup(3, "normal", _content.Rules));
        Assert.Equal("9 INNINGS  ·  HARD  ·  CPU SKILL", CarnivalFront.TitleSetup(9, "hard", _content.Rules));
        foreach (var level in CpuRules.Levels)
            Assert.DoesNotContain("WINDOW", CarnivalFront.TitleSetup(3, level, _content.Rules));
    }

    /// <summary>
    /// The shipped rule since #860 (PH-17, one fixed challenge; Jack accepted the shared window in
    /// the <c>trials/pitch5</c> window on September 22, 2026: "trial was good."): a pad and the CPU
    /// are judged in the same window on every rung. The rung's <c>humanWindowMul</c> and the
    /// split-window half that it widened (<c>TheRungWidensOnlyAPadsSwingWindow</c>) were retired by #887.
    /// </summary>
    [Theory]
    [InlineData("easy")]
    [InlineData("normal")]
    [InlineData("hard")]
    public void OnTheShippedRootTheRungLeavesAPadsSwingWindowAlone(string level)
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1, difficulty: level);
        var pitch = new PitchCommand("fastball", 0, false);
        var table = match.SwingWindowFrames(pitch);
        Assert.Equal(_content.Rules.Batting.Window.Frames, table, 8);

        // The same press, just past the half window, is a whiff for the CPU and for a pad alike.
        var cpu = new SwingCommand(true, 0, table / 2 + 0.3, false);
        var cpuEv = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1, difficulty: level).Play(pitch, cpu);
        var padEv = Match.Exhibition(_content, "rio", "ashlord", innings: 3, seed: 1, difficulty: level).Play(pitch, cpu with { Human = true });
        Assert.Equal(PlayKind.SwingMiss, cpuEv.Kind);
        Assert.Equal(PlayKind.SwingMiss, padEv.Kind);
    }

    [Fact]
    public void HardCpuReadsSharperThanEasy()
    {
        var r = _content.Rules;
        var easy = r.AtLevel("easy").Cpu.Active;
        var hard = r.AtLevel("hard").Cpu.Active;
        Assert.True(easy.ReactionMul > 1 && hard.ReactionMul < 1);
        Assert.True(easy.MakeableMarginSec > hard.MakeableMarginSec);
        Assert.True(easy.PerfectStealChance < hard.PerfectStealChance);
        Assert.Equal(1.0, r.Cpu.Active.TimingSigmaMul);
    }

    // ---------------------------------------------------------------------------------
    // Stars and the MVP (§12 stars.json)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AHomerCreditsTheTableNotALiteral()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var batter = match.Batter;
        var m = _content.Rules.Stars.Mvp;
        Assert.True(match.BeginAtBat(Paint, Swing, out _, out _));
        var homer = FlightFixtures.OverTheFence(match.Park, 30, 0); // past every rob height (§8.4)
        var field = match.ResolveFielding(homer, match.PreviewHit(homer));
        var ev = match.FinishAtBat(Paint, Swing, homer, field);
        Assert.Equal(PlayKind.HomeRun, ev.Kind);
        var mvp = match.Mvp();
        Assert.Equal(batter.Id, mvp.Who.Id);
        // The homer, its RBI, and the go-ahead RBI on top: the solo shot from a tie put the offense ahead.
        Assert.Equal(m.HomeRun + m.Rbi + m.GoAheadRbi, mvp.Points);
    }

    [Fact]
    public void AStrikeoutAndAWalkReadTheirRows()
    {
        var m = _content.Rules.Stars.Mvp;
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var pitcher = match.Pitcher;
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        var batter = match.Batter;
        for (var i = 0; i < 4; i++) match.Play(wild, take);
        Assert.Equal(batter.Id, match.First?.Id);
        Assert.Equal(m.Walk, match.Mvp().Points);
        Assert.Equal(batter.Id, match.Mvp().Who.Id);

        var strike = new PitchCommand("fastball", 0, false);
        var whiff = new SwingCommand(true, 0, 40, false);
        for (var i = 0; i < 3; i++) match.Play(strike, whiff);
        Assert.Equal(1, match.Outs);
        var mvp = match.Mvp();
        Assert.Equal(pitcher.Id, mvp.Who.Id);
        Assert.Equal(m.Strikeout, mvp.Points);
    }

    [Fact]
    public void TheStarGainsAndCostsComeFromTheTable()
    {
        var g = _content.Rules.Stars.Gains;
        Assert.True(g.DoublePlay > 0 && g.RobbedHomer > 0);
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var before = match.DefenseStars;
        var strike = new PitchCommand("fastball", 0, false);
        var whiff = new SwingCommand(true, 0, 40, false);
        for (var i = 0; i < 3; i++) match.Play(strike, whiff);
        Assert.Equal(Math.Min(_content.Rules.Stars.MeterMax, before + g.Strikeout), match.DefenseStars, 6);
        Assert.Equal(_content.Rules.Stars.Costs.Own, match.StarCost(match.Pitcher, match.Defense.Captain));
    }

    // ---------------------------------------------------------------------------------
    // Items are geometry (§12 batting.items)
    // ---------------------------------------------------------------------------------

    [Fact]
    public void AnItemAddsTimeAtTheBodyItLandsOnAndTheGeometryDecides()
    {
        // The hard grounder right at SS (the P4 double-play fixture): the shortstop scoops and throws the batter out.
        var clean = LiveGrounder(item: null, target: null);
        Assert.Equal(PlayKind.GroundOut, clean.Play.Kind);
        Assert.False(clean.ItemLanded);
        // A peel aimed at the shortstop lands at their feet after its flight (flySec) and slips them for slipSec:
        // the play takes longer by about the slip; whether the batter beats it is the race, not the item.
        var peeled = LiveGrounder("banana", "SS");
        Assert.True(peeled.ItemLanded);
        var slipTicks = (int)(_content.Rules.Batting.Items.SlipSec / Match.HeadlessTickSec);
        Assert.True(peeled.Ticks >= clean.Ticks + slipTicks / 2, $"peel {peeled.Ticks} vs clean {clean.Ticks} ticks: {peeled.Play.Caption}");
        // A rocket at the same body dazes it when it lands: the same time, on every seed, no roll.
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var rocketed = LiveGrounder("rocket", "SS", seed);
            var bare = LiveGrounder(null, null, seed);
            Assert.True(rocketed.ItemLanded);
            Assert.True(rocketed.Ticks >= bare.Ticks + slipTicks / 2, $"seed {seed}: rocket {rocketed.Ticks} vs {bare.Ticks}");
        }
        // A peel under a body nowhere near the ball changes nothing: the geometry, not the item, decides.
        var far = LiveGrounder("banana", "RF");
        Assert.True(far.ItemLanded);
        Assert.Equal(clean.Play.Kind, far.Play.Kind);
        Assert.Equal(clean.Ticks, far.Ticks);
    }

    [Fact]
    public void TheCpuThrowsItsItemWhenItWouldMatterNotOnARoll()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var park = match.Park;
        var fielding = new FieldingResolver(_content.Chemistry, _content.Rules);
        // Right at SS: the batter is out at first without help — the item is thrown.
        var atSs = FlightFixtures.Landing(park, 118, 4, -18);
        var preSs = fielding.Preview(atSs, park, match.Defense.Roster, match.Pitcher, new Random(1));
        var fieldSs = fielding.Resolve(atSs, park, match.Defense.Roster, match.Pitcher, new Random(1), pre: preSs);
        Assert.True(match.CpuWouldThrowItem(atSs, fieldSs));
        // Through the hole to the grass: already a hit — the CPU keeps its item.
        var through = FlightFixtures.Landing(park, 215, 8, -25);
        var preThrough = fielding.Preview(through, park, match.Defense.Roster, match.Pitcher, new Random(1));
        var fieldThrough = fielding.Resolve(through, park, match.Defense.Roster, match.Pitcher, new Random(1), pre: preThrough);
        Assert.False(match.CpuWouldThrowItem(through, fieldThrough));
        // Which item is a table: the dirt with a runner on is the POW, empty bases the peel, the air the rocket.
        Assert.Equal("pow", ErrorItems.CpuPick(BattedBallClass.Grounder, runnersOn: true));
        Assert.Equal("banana", ErrorItems.CpuPick(BattedBallClass.Grounder, runnersOn: false));
        Assert.Equal("rocket", ErrorItems.CpuPick(BattedBallClass.Fly, runnersOn: true));
        // The old rolls are gone from the table.
        Assert.Null(typeof(OffenseItemRules).GetProperty("CpuThrowChance"));
        Assert.Null(typeof(OffenseItemRules).GetProperty("RocketDazeChance"));
    }

    (PlayEvent Play, int Ticks, bool ItemLanded) LiveGrounder(string? item, string? target, int seed = 1)
    {
        var match = Match.Slice(_content, innings: 3, seed: seed);
        var hit = FlightFixtures.Landing(match.Park, 118, 4, -18);
        var preview = match.PreviewHit(hit);
        var field = match.ResolveFielding(hit, preview);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Paint, Swing, hit, preview, field, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        if (item is not null)
        {
            var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher, match.Defense.Gloves);
            live.Apply(LivePlayCommand.ApplyItem(item, map[target!], LivePlayCommandSource.Cpu));
        }
        LivePlayCommandResult? done = null;
        var ticks = 0;
        var landed = false;
        for (; ticks < 60 * 30 && done?.CompletedPlay is null; ticks++)
        {
            done = live.Apply(LivePlayCommand.Tick(Match.HeadlessTickSec, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            landed |= live.ItemLanded;
        }
        Assert.NotNull(done?.CompletedPlay);
        return (done!.CompletedPlay!, ticks, landed);
    }

    // ---------------------------------------------------------------------------------
    // No park window, day or night (§14; FD-11-R2)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Re-authored to FD-11-R2 (F4-d, #895). This row held P7's move of Crystal's night window out of a
    /// park-id branch into a park field (<c>nightContactWindowMul</c>, 0.85). Jack dropped the window on
    /// both roots — night keeps the stadium lights and changes no rule of the at-bat — so the field is gone
    /// from <see cref="Park"/> as well as the id branch, and night at the rink is the day window.
    /// </summary>
    [Fact]
    public void NoParkCarriesANightWindow()
    {
        Assert.Null(typeof(Park).GetProperty("NightContactWindowMul"));
        Assert.Null(typeof(ParkHazardRules).GetProperty("CrystalNightWindowMul"));
        Assert.Null(typeof(ParkHazards).GetMethod("ContactWindowMul"));
        var day = AtBatResolver.ContactWindowFrames(null, _content.Parks["crystal-rink"], false);
        var night = AtBatResolver.ContactWindowFrames(null, _content.Parks["crystal-rink"], true);
        Assert.Equal(day, night);
    }
}
