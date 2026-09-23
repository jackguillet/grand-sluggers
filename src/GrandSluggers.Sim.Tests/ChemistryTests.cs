using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ChemistryTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void SameFactionIsGood()
    {
        var rio = _content.Must("rio");
        var nico = _content.Must("nico");
        Assert.Equal(Chemistry.Good, _content.Chemistry.Between(rio, nico));
    }

    [Fact]
    public void AuthoredRivalsAreBad()
    {
        Assert.Equal(Chemistry.Bad, _content.Chemistry.Between("rio", "ashlord"));
    }

    [Fact]
    public void CrossFactionBuddyIsGood()
    {
        Assert.Equal(Chemistry.Good, _content.Chemistry.Between("rio", "vale"));
    }

    [Fact]
    public void UnrelatedIsNeutral()
    {
        Assert.Equal(Chemistry.Neutral, _content.Chemistry.Between("frost", "vine"));
    }

    [Fact]
    public void SparkTeamStartsWithMoreStarsThanMixedRivals()
    {
        var spark = PresetTeams.SparkAllStars(_content);
        var mixed = PresetTeams.MixedRivals(_content);
        var sparkStars = _content.Chemistry.StartingStars(spark);
        var mixedStars = _content.Chemistry.StartingStars(mixed);
        Assert.True(sparkStars > mixedStars, $"spark {sparkStars} vs mixed {mixedStars}");
        Assert.InRange(sparkStars, 4, 5);
        Assert.InRange(mixedStars, 1, 3);
    }

    [Fact]
    public void GoodThrowIsFasterAndClean()
    {
        var t = _content.Chemistry.FieldingThrow(_content.Must("rio"), _content.Must("nico"), new Random(1));
        Assert.Equal(Chemistry.Good, t.Relation);
        Assert.Equal(1.30, t.SpeedMul);
        Assert.False(t.Slanted);
    }

    [Fact]
    public void AnOnDeckBuddyNoLongerOffersAnItem()
    {
        // PH-16-R15 (#891): the on-deck item offer is removed; Rio and Nico are still good chemistry.
        Assert.Equal(Chemistry.Good, _content.Chemistry.Between(_content.Must("rio"), _content.Must("nico")));
        Assert.False(_content.Chemistry.ChemistryItemOffered(_content.Must("rio"), _content.Must("nico")));
        Assert.False(_content.Chemistry.ChemistryItemOffered(_content.Must("rio"), _content.Must("ashlord")));
    }
}
