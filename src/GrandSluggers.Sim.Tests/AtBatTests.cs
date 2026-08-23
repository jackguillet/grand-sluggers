using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class AtBatTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    readonly Park _harbor;

    public AtBatTests() => _harbor = _content.Parks["harbor-diamond"];

    [Fact]
    public void PerfectTimingIsInPlay()
    {
        var r = Swing(timing: 0, bat: 10);
        Assert.Equal(ContactQuality.Perfect, r.Quality);
        Assert.True(r.InPlay);
        Assert.True(r.ExitVeloMph > 80);
    }

    [Fact]
    public void WideMissIsAStrike()
    {
        var r = Swing(timing: 20);
        Assert.Equal(ContactQuality.Miss, r.Quality);
        Assert.True(r.Strike);
        Assert.False(r.InPlay);
    }

    [Fact]
    public void PowerHitterCarriesFartherThanSlapHitter()
    {
        var ashlord = Swing(timing: 0, batterId: "ashlord", batId: "furnace-club");
        var vale = Swing(timing: 0, batterId: "vale", batId: "harbor-lumber");
        Assert.True(ashlord.CarryFt > vale.CarryFt, $"ash {ashlord.CarryFt} vs vale {vale.CarryFt}");
    }

    [Fact]
    public void HarborDiamondHasNoHazards()
    {
        Assert.Empty(_harbor.Hazards);
        Assert.Equal(400, _harbor.CenterFenceFt);
    }

    [Fact]
    public void CrystalRinkHasFreezeVolumes()
    {
        var rink = _content.Parks["crystal-rink"];
        Assert.Contains(rink.Hazards, h => h.Type == "freeze_volume");
    }

    [Fact]
    public void CrystalRinkIsIceGardenNotHarbor()
    {
        var rink = _content.Parks["crystal-rink"];
        Assert.Equal("crystal-rink", rink.Id);
        Assert.Equal("ice", rink.Surface);
        Assert.Equal("royal", rink.Faction);
        Assert.Contains(rink.Hazards, h => h.Type == "freeze_volume");
        Assert.All(rink.Hazards, h => Assert.Equal("freeze_volume", h.Type));
        Assert.DoesNotContain(rink.Hazards, h =>
            h.Type is "warp_pipe" or "billboard" or "ac_unit" or "barrel"
                or "lava_pit" or "fire_breath" or "climb_wall" or "statue");
        Assert.Equal("grass", _harbor.Surface);
        Assert.Empty(_harbor.Hazards);
        Assert.Equal("crystal-rink", PresetTeams.HomeParkId("vale"));
    }

    [Fact]
    public void FunfairParkIsCarnivalNotHarbor()
    {
        var fair = _content.Parks["funfair-park"];
        Assert.Equal("funfair-park", fair.Id);
        Assert.Equal("grass", fair.Surface);
        Assert.Equal("carnival", fair.Faction);
        Assert.Contains(fair.Hazards, h => h.Type == "warp_pipe");
        Assert.Contains(fair.Hazards, h => h.Type == "train");
        Assert.All(fair.Hazards.Where(h => h.Type == "warp_pipe"), h => Assert.False(string.IsNullOrWhiteSpace(h.Tag)));
        Assert.DoesNotContain(fair.Hazards, h =>
            h.Type is "freeze_volume" or "billboard" or "ac_unit" or "barrel"
                or "lava_pit" or "fire_breath" or "climb_wall" or "statue");
        Assert.Equal("funfair-park", PresetTeams.HomeParkId("zig"));
        Assert.Equal("grass", _harbor.Surface);
        Assert.Empty(_harbor.Hazards);
    }

    [Fact]
    public void RooftopCityIsUrbanRoofNotHarbor()
    {
        var roof = _content.Parks["rooftop-city"];
        Assert.Equal("rooftop-city", roof.Id);
        Assert.Equal("dirt", roof.Surface);
        Assert.Equal("goldrush", roof.Faction);
        Assert.Contains(roof.Hazards, h => h.Type == "billboard");
        Assert.Contains(roof.Hazards, h => h.Type == "ac_unit");
        Assert.Contains(roof.Hazards, h => h.Type == "billboard" && h.Tag == "star");
        Assert.DoesNotContain(roof.Hazards, h =>
            h.Type is "freeze_volume" or "warp_pipe" or "barrel"
                or "lava_pit" or "fire_breath" or "climb_wall" or "statue");
        Assert.Equal("rooftop-city", PresetTeams.HomeParkId("brondo"));
        Assert.Equal("grass", _harbor.Surface);
        Assert.Empty(_harbor.Hazards);
    }

    [Fact]
    public void CanopyYardIsJungleNotHarbor()
    {
        var yard = _content.Parks["canopy-yard"];
        Assert.Equal("canopy-yard", yard.Id);
        Assert.Equal("dirt", yard.Surface);
        Assert.Equal("canopy", yard.Faction);
        Assert.Contains(yard.Hazards, h => h.Type == "barrel");
        Assert.Contains(yard.Hazards, h => h.Type == "tree");
        Assert.Contains(yard.Hazards, h => h.Type == "climb_wall");
        Assert.DoesNotContain(yard.Hazards, h =>
            h.Type is "freeze_volume" or "warp_pipe" or "billboard" or "ac_unit"
                or "lava_pit" or "fire_breath" or "statue");
        Assert.Equal("canopy-yard", PresetTeams.HomeParkId("konga"));
        Assert.Equal("grass", _harbor.Surface);
        Assert.Empty(_harbor.Hazards);
    }

    [Fact]
    public void EmberKeepIsCourtyardNotHarbor()
    {
        var keep = _content.Parks["ember-keep"];
        Assert.Equal("ember-keep", keep.Id);
        Assert.Equal("ash", keep.Surface);
        Assert.Equal("ember", keep.Faction);
        Assert.Contains(keep.Hazards, h => h.Type == "lava_pit");
        Assert.Contains(keep.Hazards, h => h.Type == "fire_breath");
        Assert.Contains(keep.Hazards, h => h.Type == "statue");
        Assert.DoesNotContain(keep.Hazards, h =>
            h.Type is "freeze_volume" or "warp_pipe" or "billboard" or "ac_unit"
                or "barrel" or "climb_wall");
        Assert.Equal("ember-keep", PresetTeams.HomeParkId("ashlord"));
        Assert.Equal(408, keep.CenterFenceFt);
        Assert.Equal("grass", _harbor.Surface);
        Assert.Empty(_harbor.Hazards);
    }

    [Fact]
    public void CarryIncreasesWithExitVelo()
    {
        var slow = BallFlight.CarryFeet(80, 28, 0);
        var fast = BallFlight.CarryFeet(100, 28, 0);
        Assert.True(fast > slow);
        Assert.InRange(BallFlight.CarryFeet(95, 28, 0), 300, 450);
    }

    [Fact]
    public void RosterHasSixCaptains()
    {
        Assert.Equal(6, _content.Characters.Values.Count(c => c.Captain));
        Assert.True(_content.Characters.Count >= 16);
    }

    [Fact]
    public void BuntIsAShortGrounderNotAHomer()
    {
        var swing = Swing(timing: 0);
        var bunt = Swing(timing: 0, bunt: true);
        Assert.NotEqual(ContactQuality.Miss, bunt.Quality);
        Assert.True(bunt.LaunchDeg < 16, $"bunt launch {bunt.LaunchDeg}");
        Assert.True(bunt.ExitVeloMph < swing.ExitVeloMph, $"bunt {bunt.ExitVeloMph} vs swing {swing.ExitVeloMph}");
        Assert.False(bunt.HomeRun);
        Assert.True(bunt.CarryFt < 180, $"bunt carry {bunt.CarryFt}");
    }

    [Fact]
    public void InsideAimIsStillABallAfterLocation()
    {
        Assert.False(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, 0, false, 0.95, 0), 7));
        Assert.True(AtBatResolver.PitchInZone(new PitchCommand("fastball", 0, 0, false, 0.1, 0.1), 7));
    }

    [Fact]
    public void LateSwingGroundsMoreOftenThanSquare()
    {
        var late = 0;
        var square = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            if (Swing(timing: 6, seed: seed).LaunchDeg < 14) late++;
            if (Swing(timing: 0, seed: seed).LaunchDeg < 14) square++;
        }
        Assert.True(late > square, $"late {late} vs square {square}");
    }

    [Fact]
    public void SquareSwingsAreNotAllFlies()
    {
        var flies = 0;
        var hops = 0;
        for (var seed = 0; seed < 50; seed++)
        {
            var r = Swing(timing: 0, seed: seed);
            if (r.LaunchDeg < 14) hops++;
            else flies++;
        }
        Assert.True(hops > 0, "square contact should produce some grounders");
        Assert.True(flies > 0, "square contact should still produce flies");
        Assert.NotEqual(50, flies);
    }

    [Fact]
    public void StickUpBiasesAHopper()
    {
        var up = Swing(timing: 0, launchAim: 1, seed: 3);
        var down = Swing(timing: 0, launchAim: -1, seed: 3);
        Assert.True(up.LaunchDeg < down.LaunchDeg, $"up {up.LaunchDeg} vs down {down.LaunchDeg}");
        Assert.True(up.LaunchDeg < 14, $"stick-up should ground, launch {up.LaunchDeg}");
    }

    AtBatResult Swing(double timing, int? bat = null, string batterId = "rio", string batId = "harbor-lumber", bool bunt = false, double launchAim = 0, int seed = 1)
    {
        var batter = _content.Must(batterId);
        if (bat is int b)
            batter = batter with { Stats = batter.Stats with { Bat = b } };

        var input = new AtBatInput(
            Pitcher: _content.Must("ashlord"),
            Batter: batter,
            OnDeck: _content.Must("nico"),
            RunnersOn: [],
            PitchType: "fastball",
            ChargePitch: false,
            ChargeSwing: false,
            TimingErrorFrames: timing,
            UseStarPitch: false,
            UseStarSwing: false,
            Bat: _content.Bats[batId],
            PitcherStamina: 80,
            Bunt: bunt,
            LaunchAim: launchAim);

        return new AtBatResolver(_content.Chemistry).Resolve(input, _harbor, new Random(seed));
    }
}
