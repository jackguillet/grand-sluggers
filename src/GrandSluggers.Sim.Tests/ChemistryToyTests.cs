using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ChemistryToyTests
{
    [Fact]
    public void StickersAreHeartsAndScribbles()
    {
        Assert.Equal(ChemistryToy.Heart, ChemistryToy.Sticker(Chemistry.Good));
        Assert.Equal(ChemistryToy.Scribble, ChemistryToy.Sticker(Chemistry.Bad));
        Assert.Equal(ChemistryToy.None, ChemistryToy.Sticker(Chemistry.Neutral));
    }

    [Fact]
    public void MiniDiamondPutsCatcherAtHomeAndCfDeep()
    {
        var c = ChemistryToy.MiniSpot("C");
        var p = ChemistryToy.MiniSpot("P");
        var cf = ChemistryToy.MiniSpot("CF");
        var first = ChemistryToy.MiniSpot("1B");
        var third = ChemistryToy.MiniSpot("3B");
        Assert.True(cf.V > p.V, $"CF {cf.V} should be deeper than P {p.V}");
        Assert.True(p.V > c.V, $"P {p.V} should be in front of C {c.V}");
        Assert.True(first.U > 0);
        Assert.True(third.U < 0);
        var of = ChemistryToy.GroupTokenSpot("OF");
        var inf = ChemistryToy.GroupTokenSpot("IF");
        Assert.True(of.V > inf.V);
        Assert.Equal("IF", TeamBuilder.GloveGroup("SS"));
        Assert.Equal("OF", TeamBuilder.GloveGroup("LF"));
    }

    [Fact]
    public void FilledStarsBounceBiggerThanEmpty()
    {
        Assert.True(ChemistryToy.StarFilled(0, 4));
        Assert.False(ChemistryToy.StarFilled(4, 4));
        var on = ChemistryToy.StarScale(0, 4, 0);
        var off = ChemistryToy.StarScale(4, 4, 0);
        Assert.True(on > off, $"filled {on} vs empty {off}");
        Assert.True(ChemistryToy.StarScale(1, 5, 0.4) > off);
    }

    [Fact]
    public void CompactDiamondKeepsCatcherInFrontOfCf()
    {
        var c = ChemistryToy.WorldSpot("C");
        var p = ChemistryToy.WorldSpot("P");
        var cf = ChemistryToy.WorldSpot("CF");
        var first = ChemistryToy.WorldSpot("1B");
        var third = ChemistryToy.WorldSpot("3B");
        Assert.True(cf.Z > p.Z, $"CF {cf.Z} should be deeper than P {p.Z}");
        Assert.True(p.Z > c.Z, $"P {p.Z} should be in front of C {c.Z}");
        Assert.True(first.X > 0);
        Assert.True(third.X < 0);
        Assert.True(cf.Z < 80, $"compact CF is still a wall ant z={cf.Z}");
        Assert.True(cf.Z < Diamond.Positions["CF"].Z * 0.3,
            $"compact CF {cf.Z} vs real {Diamond.Positions["CF"].Z}");
        var heart = ChemistryToy.HeartSpot(c, p);
        Assert.InRange(heart.Y, 2.4, 4.2);
        Assert.True(heart.Z > c.Z && heart.Z < p.Z);
    }

    [Fact]
    public void LineupCameraIsThreeQuarterOnTheToys()
    {
        Assert.True(ChemistryToy.CameraIsThreeQuarter(
            ChemistryToy.CamX, ChemistryToy.CamY, ChemistryToy.CamZ));
        var cam = (ChemistryToy.CamX, ChemistryToy.CamY, ChemistryToy.CamZ);
        var c = ChemistryToy.WorldSpot("C");
        var cf = ChemistryToy.WorldSpot("CF");
        var dC = Dist(cam, (c.X, 0, c.Z));
        var dCf = Dist(cam, (cf.X, 0, cf.Z));
        Assert.True(dC < dCf, $"catcher {dC} should be the near toy, CF {dCf}");
        Assert.True(dC < 28, $"highlighted toy is an ant dist={dC}");
    }

    static double Dist((double X, double Y, double Z) a, (double X, double Y, double Z) b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
