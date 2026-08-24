using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CharacterCardTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void RioCardShowsFourStatsAndHeatball()
    {
        var rio = _content.Must("rio");
        var card = CharacterCard.Of(rio);
        Assert.Equal("rio", card.Id);
        Assert.Equal("Rio Sparks", card.Name);
        Assert.True(card.Captain);
        Assert.Equal(6, card.Stats.Pitch);
        Assert.Equal(7, card.Stats.Bat);
        Assert.Equal(6, card.Stats.Field);
        Assert.Equal(7, card.Stats.Run);
        Assert.Equal("Heatball", card.StarPitch);
        Assert.Equal("Heat Swing", card.StarSwing);
        Assert.Equal("Grow", card.FieldVerb);
        Assert.Equal(0.7, CharacterCard.BarFill(7), 3);
        Assert.Equal(0, CharacterCard.BarFill(-2), 3);
        Assert.Equal(1, CharacterCard.BarFill(12), 3);
    }

    [Fact]
    public void ValeCardIsNotRioAndShowsChemVsCaptain()
    {
        var vale = _content.Must("vale");
        var rio = _content.Must("rio");
        var card = CharacterCard.Of(vale, rio, _content.Chemistry);
        Assert.Equal("Charmball", card.StarPitch);
        Assert.Equal("Heart Swing", card.StarSwing);
        Assert.NotEqual("Heatball", card.StarPitch);
        Assert.NotEqual("Grow", card.FieldVerb);
        Assert.Equal(_content.Chemistry.Between(rio, vale), card.VsCaptain);
    }

    [Fact]
    public void TitleSplitsHyphenIds()
    {
        Assert.Equal("Lick Catch", CharacterCard.Title("lick-catch"));
        Assert.Equal("Snap Throw", CharacterCard.Title("snap-throw"));
        Assert.Equal("Super Jump", CharacterCard.Title("super-jump"));
    }
}
