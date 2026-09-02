using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FoulTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    AtBatInput Square(double sprayAim, double timing = 0, bool starSwing = false) =>
        new(_content.Must("vale"), _content.Must("rio"), _content.Must("nico"), [],
            "fastball", false, starSwing, timing, false, starSwing,
            _content.Bats["harbor-lumber"], 80, SprayAimDeg: sprayAim, PitchInZone: true);

    [Fact]
    public void SprayPastTheFoulLineIsFoulNotInPlay()
    {
        var park = _content.Parks["harbor-diamond"];
        var r = new AtBatResolver(_content.Chemistry).Resolve(Square(60), park, new Random(1));
        Assert.True(r.Foul);
        Assert.False(r.InPlay);
        Assert.False(r.HomeRun);
        Assert.True(AtBatResolver.IsFoul(r.SprayDeg));
        Assert.True(Math.Abs(r.SprayDeg) > AtBatResolver.FoulLineDeg);
        Assert.True(r.ExitVeloMph > 1);
    }

    [Fact]
    public void SquareContactUpTheMiddleIsFair()
    {
        var park = _content.Parks["harbor-diamond"];
        for (var seed = 0; seed < 40; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry).Resolve(Square(0), park, new Random(seed));
            Assert.False(r.Foul, $"seed {seed} spray {r.SprayDeg} labeled foul inside the lines");
            Assert.True(r.InPlay);
            Assert.False(AtBatResolver.IsFoul(r.SprayDeg));
        }
    }

    [Fact]
    public void PastThePoleIsFoulNotAHomer()
    {
        var park = _content.Parks["harbor-diamond"];
        var input = new AtBatInput(
            _content.Must("vale"), _content.Must("ashlord"), _content.Must("cinder"), [],
            "fastball", false, true, 0, false, true,
            _content.Bats["furnace-club"], 80, SprayAimDeg: 70, PitchInZone: true);
        var carry = 0.0;
        for (var seed = 0; seed < 20; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry).Resolve(input, park, new Random(seed));
            Assert.True(r.Foul, $"seed {seed} spray {r.SprayDeg}");
            Assert.False(r.HomeRun, $"seed {seed} homer in foul territory carry {r.CarryFt}");
            Assert.False(r.InPlay);
            carry = Math.Max(carry, r.CarryFt);
        }
        Assert.True(carry > park.RightFenceFt * 0.6, $"expected a real fly, best carry {carry}");
    }

    [Fact]
    public void FoulIsAStrikeUnlessTwo()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var pull = new SwingCommand(true, 0, 0, false, SprayAimDeg: 60);
        var batter = match.Batter.Id;

        var first = match.Play(paint, pull);
        Assert.Equal(PlayKind.Foul, first.Kind);
        Assert.Equal("Foul.", first.Caption);
        Assert.Equal(1, match.Strikes);
        Assert.Equal(0, match.Outs);
        Assert.Equal(batter, match.Batter.Id);

        var second = match.Play(paint, pull);
        Assert.Equal(PlayKind.Foul, second.Kind);
        Assert.Equal(2, match.Strikes);

        var third = match.Play(paint, pull);
        Assert.Equal(PlayKind.Foul, third.Kind);
        Assert.Equal(2, match.Strikes);
        Assert.Equal(0, match.Outs);
        Assert.Equal(batter, match.Batter.Id);
        Assert.False(InPlay.FairContactSendsBatter(third.AtBat));
    }

    [Fact]
    public void FullStickPullsDownTheLine()
    {
        Assert.InRange(AtBatResolver.SprayAimDeg(1), 36, AtBatResolver.FoulLineDeg);
        Assert.Equal(-AtBatResolver.SprayAimDeg(1), AtBatResolver.SprayAimDeg(-1));
        Assert.Equal(0, AtBatResolver.SprayAimDeg(0));
    }

    [Fact]
    public void CheapPullFliesIntoFoulTerritory()
    {
        var park = _content.Parks["harbor-diamond"];
        var fouls = 0;
        for (var seed = 0; seed < 80; seed++)
        {
            var r = new AtBatResolver(_content.Chemistry).Resolve(
                Square(AtBatResolver.SprayAimDeg(1), timing: 5), park, new Random(seed));
            if (r.Quality == ContactQuality.Miss) continue;
            if (r.Foul)
            {
                fouls++;
                Assert.True(Math.Abs(r.SprayDeg) > AtBatResolver.FoulLineDeg,
                    $"foul spray {r.SprayDeg} still inside the lines");
            }
            else
                Assert.False(AtBatResolver.IsFoul(r.SprayDeg));
        }
        Assert.True(fouls > 8, $"expected sitting-visible fouls off a full-stick cheap swing, got {fouls}");
    }

    [Fact]
    public void FairContactSendsTheBatterFoulDoesNot()
    {
        var fair = new AtBatResult(ContactQuality.Solid, true, false, 90, 18, 180, false, false, null, null, 12);
        var foul = fair with { InPlay = false, Foul = true, SprayDeg = 52 };
        Assert.True(InPlay.FairContactSendsBatter(fair));
        Assert.False(InPlay.FairContactSendsBatter(foul));
    }
}
