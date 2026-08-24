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
}
