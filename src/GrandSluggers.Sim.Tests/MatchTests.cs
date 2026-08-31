using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class MatchTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void SliceGameFinishes()
    {
        var match = Match.Slice(_content, innings: 3, seed: 7);
        match.AutoPlayGame();
        Assert.True(match.Over);
        Assert.True(match.Log.Count > 10);
        Assert.InRange(match.Inning, 3, 5);
        var mvp = match.Mvp();
        Assert.False(string.IsNullOrWhiteSpace(mvp.Who.Name));
    }

    [Fact]
    public void FourBallsIsAWalk()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        PlayKind last = PlayKind.TakeBall;
        for (var i = 0; i < 4; i++)
            last = match.Play(wild, take).Kind;
        Assert.Equal(PlayKind.Walk, last);
        Assert.NotNull(match.First);
    }

    [Fact]
    public void PlayerHopperWithFirstOccupiedDoesNotTurnTwoUntilThrowTwoLands()
    {
        var (match, paint, swing, hit, field) = LiveHopperOnFirst();
        match.OpenLivePlay();
        var before = BroadcastHud.From(match);
        Assert.True(before.RunnerFirst);
        Assert.Equal(0, before.Outs);

        var force = match.StepThrow(2, runnerBeats: false, field.Fielder);
        Assert.True(force.Out);
        Assert.True(force.Force);
        Assert.False(force.TurnedTwo);
        Assert.Equal(1, match.Outs);
        Assert.Equal(1, match.LiveThrows);
        Assert.Null(match.First);
        Assert.DoesNotContain("turns two", force.Caption, StringComparison.OrdinalIgnoreCase);
        var mid = BroadcastHud.From(match);
        Assert.False(mid.RunnerFirst);
        Assert.Equal(1, mid.Outs);

        var ev = match.FinishAtBat(paint, swing, hit, field);
        Assert.Equal(1, match.Outs);
        Assert.NotNull(match.First);
        Assert.Contains("Force at second", ev.Caption);
        Assert.DoesNotContain("turns two", ev.Caption, StringComparison.OrdinalIgnoreCase);
        var after = BroadcastHud.From(match);
        Assert.True(after.RunnerFirst);
        Assert.Equal(1, after.Outs);
        Assert.Equal(after.Outs, match.Outs);
        Assert.Equal(after.RunnerFirst, match.First is not null);
    }

    [Fact]
    public void PlayerThrowTwoOnTimeTurnsTwo()
    {
        var (match, paint, swing, hit, field) = LiveHopperOnFirst();
        match.OpenLivePlay();
        match.StepThrow(2, runnerBeats: false, field.Fielder);
        Assert.Equal(1, match.Outs);
        var two = match.StepThrow(1, runnerBeats: false, field.Fielder);
        Assert.True(two.TurnedTwo);
        Assert.Equal(2, match.Outs);
        var pip = BroadcastHud.From(match);
        Assert.Equal(2, pip.Outs);
        Assert.False(pip.RunnerFirst);
        var ev = match.FinishAtBat(paint, swing, hit, field);
        Assert.Contains("turns two", ev.Caption);
        Assert.Equal(2, match.Outs);
        Assert.Null(match.First);
        Assert.Equal(BroadcastHud.From(match).Outs, match.Outs);
    }

    [Fact]
    public void PlayerThrowTwoLateIsForceOnly()
    {
        var (match, paint, swing, hit, field) = LiveHopperOnFirst();
        match.OpenLivePlay();
        match.StepThrow(2, runnerBeats: false, field.Fielder);
        var late = match.StepThrow(1, runnerBeats: true, field.Fielder);
        Assert.False(late.Out);
        Assert.True(late.BatterSafe);
        Assert.Equal(1, match.Outs);
        var ev = match.FinishAtBat(paint, swing, hit, field);
        Assert.Equal(1, match.Outs);
        Assert.NotNull(match.First);
        Assert.Contains("Force at second", ev.Caption);
        Assert.DoesNotContain("turns two", ev.Caption, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CpuDeadStickStillTurnsTwo()
    {
        var (match, paint, swing, hit, field) = LiveHopperOnFirst();
        Assert.False(InPlay.BatterBeatsThrow(match.Batter, hit, field));
        Assert.False(match.LivePlay);
        var ev = match.FinishAtBat(paint, swing, hit, field);
        Assert.Contains("turns two", ev.Caption);
        Assert.Equal(2, match.Outs);
        Assert.Null(match.First);
        var bug = BroadcastHud.From(match);
        Assert.Equal(2, bug.Outs);
        Assert.False(bug.RunnerFirst);
    }

    (Match Match, PitchCommand Pitch, SwingCommand Swing, AtBatResult Hit, FieldingResult Field) LiveHopperOnFirst()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        Assert.True(match.StationRunner(1, match.OnDeck!));
        Assert.NotNull(match.First);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        Assert.True(match.BeginAtBat(paint, swing, out var hit, out _));
        var laser = new ThrowResult(Chemistry.Good, 1.7, false);
        var field = new FieldingResult(PlayKind.GroundOut, match.Pitcher, match.Batter, 1.5, 48, 72, false, false, laser);
        return (match, paint, swing, hit, field);
    }

    [Fact]
    public void GrounderWithRunnerOnFirstForcesTheLead()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
        var leadId = match.First.Id;
        var paint = new PitchCommand("fastball", 0, 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        Assert.True(match.BeginAtBat(paint, swing, out var hit, out _));
        var laser = new ThrowResult(Chemistry.Good, 1.55, false);
        var field = new FieldingResult(PlayKind.GroundOut, match.Pitcher, match.Batter, 1.5, 48, 72, false, false, laser);
        match.FinishAtBat(paint, swing, hit, field);
        Assert.True(match.First is null || match.First.Id != leadId, "lead runner must be forced");
        Assert.True(match.Outs >= 1);
    }

    [Fact]
    public void HopperWithRunnerOnSecondIsATagNotAForce()
    {
        var match = Occupy(PlayKind.Double);
        Assert.NotNull(match.Second);
        Assert.Null(match.First);
        var runnerId = match.Second.Id;
        var paint = new PitchCommand("fastball", 0, 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        Assert.True(match.BeginAtBat(paint, swing, out var hit, out _));
        var laser = new ThrowResult(Chemistry.Good, 1.7, false);
        var field = new FieldingResult(PlayKind.GroundOut, match.Pitcher, match.Batter, 0.4, 42, 78, false, false, laser);
        Assert.False(InPlay.RunnerBeatsTag(match.Second, hit, field, 3));
        match.FinishAtBat(paint, swing, hit, field);
        Assert.True(match.Outs >= 1, "tag at third is an out");
        Assert.Null(match.Third);
        Assert.True(match.Second is null || match.Second.Id != runnerId);
    }

    [Fact]
    public void SlowTagThrowLetsTheRunnerTakeThird()
    {
        var match = Occupy(PlayKind.Double);
        var runnerId = match.Second!.Id;
        var paint = new PitchCommand("fastball", 0, 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        Assert.True(match.BeginAtBat(paint, swing, out var hit, out _));
        var looper = new ThrowResult(Chemistry.Bad, 0.4, false);
        var field = new FieldingResult(PlayKind.GroundOut, match.Pitcher, match.Batter, 0.35, 90, 40, false, false, looper);
        Assert.True(InPlay.RunnerBeatsTag(match.Second, hit, field, 3));
        match.FinishAtBat(paint, swing, hit, field);
        Assert.NotNull(match.Third);
        Assert.Equal(runnerId, match.Third.Id);
        Assert.NotNull(match.First);
        Assert.Equal(0, match.Outs);
    }

    [Fact]
    public void HopperWithRunnerOnThirdTagsAtHome()
    {
        var match = Occupy(PlayKind.Triple);
        Assert.NotNull(match.Third);
        Assert.Null(match.First);
        var runnerId = match.Third.Id;
        var paint = new PitchCommand("fastball", 0, 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        Assert.True(match.BeginAtBat(paint, swing, out var hit, out _));
        var laser = new ThrowResult(Chemistry.Good, 1.8, false);
        var field = new FieldingResult(PlayKind.GroundOut, match.Pitcher, match.Batter, 0.35, 20, 55, false, false, laser);
        Assert.False(InPlay.RunnerBeatsTag(match.Third, hit, field, 4));
        match.FinishAtBat(paint, swing, hit, field);
        Assert.True(match.Outs >= 1);
        Assert.True(match.Third is null || match.Third.Id != runnerId);
    }

    Match Occupy(PlayKind extra)
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        for (var i = 0; i < 24 && !match.Over; i++)
        {
            if (!match.BeginAtBat(paint, swing, out var hit, out _))
                continue;
            match.FinishAtBat(paint, swing, hit,
                new FieldingResult(extra, match.Pitcher, null, 1.4, 8, extra == PlayKind.Triple ? 310 : 250, false, false));
            if (extra == PlayKind.Double && match.Second is not null && match.First is null) return match;
            if (extra == PlayKind.Triple && match.Third is not null && match.First is null) return match;
        }
        Assert.Fail("could not occupy the bag with a hit");
        return match;
    }

    [Fact]
    public void ThreeLookingStrikesIsAStrikeout()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var take = new SwingCommand(false, 0, 0, false);
        PlayKind last = PlayKind.TakeStrike;
        for (var i = 0; i < 3; i++)
            last = match.Play(paint, take).Kind;
        Assert.Equal(PlayKind.Strikeout, last);
        Assert.Equal(1, match.Outs);
    }

    [Fact]
    public void PerfectAshlordSwingCanLeaveTheYard()
    {
        var park = _content.Parks["harbor-diamond"];
        var input = new AtBatInput(
            _content.Must("rio"), _content.Must("ashlord"), _content.Must("cinder"), [],
            "fastball", false, true, 0, false, true,
            _content.Bats["furnace-club"], 80, SprayAimDeg: 0, PitchInZone: true);
        var best = 0.0;
        for (var seed = 0; seed < 20; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry).Resolve(input, park, new Random(seed));
            if (r.HomeRun) best = Math.Max(best, r.CarryFt);
            best = Math.Max(best, r.CarryFt);
        }
        Assert.True(best > 280, $"best carry {best}");
    }

    [Fact]
    public void TrajectoryHasHangTime()
    {
        var samples = BallFlight.Trajectory(95, 28, 0);
        Assert.True(samples.Count > 10);
        Assert.InRange(BallFlight.HangTime(samples), 3.0, 6.5);
        var p = BallFlight.PointAt(samples, 0, 0.5);
        Assert.True(p.Y > 2);
        Assert.True(p.Z > 0);
    }

    [Fact]
    public void SparkStartsWithMoreStars()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.True(match.HomeStars >= match.AwayStars);
        Assert.Equal("Rio Sparks", match.Home.Captain.Name);
        Assert.Equal("Ashlord", match.Away.Captain.Name);
    }

    [Fact]
    public void CrystalRinkHasFreezeHazards()
    {
        var match = Match.Slice(_content, seed: 1, parkId: "crystal-rink");
        Assert.Equal("crystal-rink", match.Park.Id);
        Assert.Equal("ice", match.Park.Surface);
        Assert.True(ParkHazards.InFreeze(match.Park, 40, 70));
        Assert.False(ParkHazards.InFreeze(match.Park, 0, 0));
    }

    [Fact]
    public void SwapPitcherChangesTheMound()
    {
        var match = Match.Slice(_content, seed: 1);
        var first = match.Pitcher.Id;
        Assert.True(match.SwapPitcher());
        Assert.NotEqual(first, match.Pitcher.Id);
        Assert.True(match.PitcherStamina >= 35);
    }

    [Fact]
    public void CrystalGameFinishes()
    {
        var match = Match.Slice(_content, innings: 3, seed: 11, parkId: "crystal-rink");
        match.AutoPlayGame();
        Assert.True(match.Over);
        Assert.True(match.Log.Count > 8);
    }

    [Fact]
    public void FunfairHasWarpPipes()
    {
        var park = _content.Parks["funfair-park"];
        Assert.Equal("funfair-park", park.Id);
        Assert.Equal("grass", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe");
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe" && h.Tag == "A");
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe" && h.Tag == "B");
        Assert.Contains(park.Hazards, h => h.Type == "warp_pipe" && h.Tag == "C");
        var w = ParkHazards.WarpIfPipe(park, 20, 55, new Random(3));
        Assert.True(w.Warped);
        Assert.False(Math.Abs(w.X - 20) < 0.01 && Math.Abs(w.Z - 55) < 0.01);
    }

    [Fact]
    public void RooftopHasBillboards()
    {
        var park = _content.Parks["rooftop-city"];
        Assert.Equal("rooftop-city", park.Id);
        Assert.Equal("dirt", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "billboard");
        Assert.Contains(park.Hazards, h => h.Type == "ac_unit");
        Assert.True(ParkHazards.HitStarSign(park, -80, 240));
        Assert.False(ParkHazards.HitStarSign(park, 0, 0));
    }

    [Fact]
    public void CycleBatChangesLoadout()
    {
        var match = Match.Slice(_content, seed: 1);
        var first = match.HomeBat.Id;
        match.CycleBat(true);
        Assert.NotEqual(first, match.HomeBat.Id);
        var g = match.HomeGlove.Id;
        match.CycleGlove(true);
        Assert.NotEqual(g, match.HomeGlove.Id);
    }

    [Fact]
    public void FourParksFinishAGame()
    {
        foreach (var id in new[] { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city" })
        {
            var match = Match.Slice(_content, innings: 3, seed: 5, parkId: id);
            match.AutoPlayGame();
            Assert.True(match.Over, id);
        }
    }

    [Fact]
    public void CanopyYardHasBarrelsAndClimbWalls()
    {
        var park = _content.Parks["canopy-yard"];
        Assert.Equal("canopy-yard", park.Id);
        Assert.Equal("dirt", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "barrel");
        Assert.Contains(park.Hazards, h => h.Type == "tree");
        Assert.Contains(park.Hazards, h => h.Type == "climb_wall");
        var w = ParkHazards.WarpIfPipe(park, 22, 58, new Random(3));
        Assert.True(w.Warped);
        Assert.Equal("barrel cannon", ParkHazards.WarpName(park));
        Assert.True(ParkHazards.CanClamber(park, _content.Must("konga")));
        Assert.False(ParkHazards.CanClamber(park, _content.Must("rio")));
    }

    [Fact]
    public void EmberKeepLavaSlowsFielders()
    {
        var park = _content.Parks["ember-keep"];
        Assert.Equal("ember-keep", park.Id);
        Assert.Equal("ash", park.Surface);
        Assert.Contains(park.Hazards, h => h.Type == "lava_pit");
        Assert.Contains(park.Hazards, h => h.Type == "fire_breath");
        Assert.Contains(park.Hazards, h => h.Type == "statue");
        Assert.True(ParkHazards.InSlow(park, 38, 78));
        Assert.False(ParkHazards.InSlow(park, 0, 0));
        Assert.Equal("ember-keep", PresetTeams.HomeParkId("ashlord"));
        Assert.Equal("canopy-yard", PresetTeams.HomeParkId("konga"));
    }

    [Fact]
    public void ClamberRobsAJustOverFenceHomer()
    {
        var park = _content.Parks["canopy-yard"];
        var fence = AtBatResolver.FenceAt(park, 0);
        var hit = new AtBatResult(
            ContactQuality.Perfect, true, false, 102, 28, fence + 12, true, false, null, null);
        Assert.True(ParkHazards.CanClamberRob(park, _content.Must("konga"), hit));
        Assert.False(ParkHazards.CanClamberRob(park, _content.Must("ashlord"), hit));
        Assert.False(ParkHazards.CanClamberRob(_content.Parks["harbor-diamond"], _content.Must("konga"), hit));
    }

    [Fact]
    public void SixParksFinishAGame()
    {
        foreach (var id in new[]
                 { "harbor-diamond", "crystal-rink", "funfair-park", "rooftop-city", "canopy-yard", "ember-keep" })
        {
            var match = Match.Exhibition(_content, "konga", "ashlord", innings: 3, seed: 8, parkId: id);
            match.AutoPlayGame();
            Assert.True(match.Over, id);
        }
    }
}
