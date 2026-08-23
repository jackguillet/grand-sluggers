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

    AtBatResult Swing(double timing, int? bat = null, string batterId = "rio", string batId = "harbor-lumber")
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
            PitcherStamina: 80);

        return new AtBatResolver(_content.Chemistry).Resolve(input, _harbor, new Random(1));
    }
}
