using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class InPlayTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void EnergyScalesWithExitAndQuality()
    {
        var soft = Hit(ContactQuality.Cheap, 60);
        var hard = Hit(ContactQuality.Perfect, 100);
        Assert.True(InPlay.Energy(hard) > InPlay.Energy(soft),
            $"hard {InPlay.Energy(hard)} vs soft {InPlay.Energy(soft)}");
    }

    [Fact]
    public void HighEnergyBobblesMoreThanADyingRoller()
    {
        var rio = _content.Must("rio");
        var hard = Hit(ContactQuality.Perfect, 110);
        var dying = Hit(ContactQuality.Cheap, 40);
        var hardN = 0;
        var dyingN = 0;
        const int n = 80;
        for (var i = 0; i < n; i++)
        {
            if (InPlay.Bobbles(InPlay.Energy(hard), rio, new Random(i))) hardN++;
            if (InPlay.Bobbles(InPlay.Energy(dying), rio, new Random(i))) dyingN++;
        }
        Assert.True(hardN > dyingN, $"hard bobbles {hardN} vs dying {dyingN}");
        Assert.Equal(0, dyingN);
    }

    [Fact]
    public void FastBatterBeatsASlowThrow()
    {
        var dart = _content.Must("dart");
        var brick = _content.Must("brondo");
        Assert.True(InPlay.HomeToFirstSec(dart) < InPlay.HomeToFirstSec(brick),
            $"dart {InPlay.HomeToFirstSec(dart)} vs brondo {InPlay.HomeToFirstSec(brick)}");

        var hit = Hit(ContactQuality.Solid, 72, launch: 8, carry: 45);
        var slow = new ThrowResult(Chemistry.Bad, 0.55, false);
        var field = new FieldingResult(PlayKind.GroundOut, _content.Must("vale"), _content.Must("nico"),
            0.4, -40, 90, false, false, slow);
        Assert.True(InPlay.BatterBeatsThrow(dart, hit, field));

        var laser = new ThrowResult(Chemistry.Good, 1.6, false);
        var outPlay = field with { HangTimeSec = 1.6, LandingX = 50, LandingZ = 70, Throw = laser };
        Assert.False(InPlay.BatterBeatsThrow(brick, hit, outPlay));
    }

    [Fact]
    public void ScoopMissIsASingleNotASilentGroundOut()
    {
        var miss = new FieldingResult(PlayKind.Single, _content.Must("rio"), null, 0.8, 10, 40, false, false);
        Assert.NotEqual(PlayKind.GroundOut, miss.Kind);
        var hit = Hit(ContactQuality.Solid, 70, 8, 50);
        Assert.False(InPlay.BatterBeatsThrow(_content.Must("rio"), hit, miss));
    }

    static AtBatResult Hit(ContactQuality q, double exit, double launch = 22, double carry = 200) =>
        new(q, true, false, exit, launch, carry, false, false, null, null);
}
