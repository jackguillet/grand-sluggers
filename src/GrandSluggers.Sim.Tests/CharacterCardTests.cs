using Xunit;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.Sim.Tests;

public class CharacterCardTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void RioCardShowsFourStatsAndSkyrocket()
    {
        var rio = _content.Must("rio");
        var card = CharacterCard.Of(rio);
        Assert.Equal("rio", card.Id);
        Assert.Equal("Ronnie Sparks", card.Name);
        Assert.True(card.Captain);
        Assert.Equal(6, card.Stats.Pitch);
        Assert.Equal(7, card.Stats.Bat);
        Assert.Equal(6, card.Stats.Field);
        Assert.Equal(7, card.Stats.Run);
        Assert.Equal("Skyrocket", card.StarPitch);
        Assert.Equal("Sparkler", card.StarSwing);
        Assert.Equal("Grow", card.FieldVerb);
        Assert.Equal(Hand.R, card.Bats);
        Assert.Equal("BATS RIGHT", HowToPlay.CardBatHand(card.Bats));
        Assert.Equal(0.7, CharacterCard.BarFill(7), 3);
        Assert.Equal(0, CharacterCard.BarFill(-2), 3);
        Assert.Equal(1, CharacterCard.BarFill(12), 3);
    }

    [Fact]
    public void LineupCardNamesTheHandOfALeftHandedPoolBatter()
    {
        var zig = _content.Must("zig");
        var card = CharacterCard.Of(zig);
        Assert.Equal(Hand.L, card.Bats);
        Assert.Equal("BATS LEFT", HowToPlay.CardBatHand(card.Bats));
    }

    [Fact]
    public void ValeCardIsNotRioAndShowsChemVsCaptain()
    {
        var vale = _content.Must("vale");
        var rio = _content.Must("rio");
        var card = CharacterCard.Of(vale, rio, _content.Chemistry);
        Assert.Equal("Aurora Ribbon", card.StarPitch);
        Assert.Equal("Follow Spot", card.StarSwing);
        Assert.NotEqual("Skyrocket", card.StarPitch);
        Assert.NotEqual("Grow", card.FieldVerb);
        Assert.Equal(_content.Chemistry.Between(rio, vale), card.VsCaptain);
    }

    [Fact]
    public void TitleSplitsHyphenIds()
    {
        Assert.Equal("Lick Catch", CharacterCard.Title("lick-catch"));
        Assert.Equal("Snap Throw", CharacterCard.Title("snap-throw"));
        Assert.Equal("Super Jump", CharacterCard.Title("super-jump"));
        Assert.Equal("Heat Ball", CharacterCard.Title("heatball"));
        Assert.Equal("Charm Ball", CharacterCard.Title("charmball"));
        Assert.Equal("Fast Ball", CharacterCard.Title("fastball"));
    }
}
