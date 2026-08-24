using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class FieldAssistTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void ExhibitionStartsCpuOnTheGloveTrainingStartsThePlayer()
    {
        Assert.False(FieldAssist.PlayerStartsOnGlove(false));
        Assert.True(FieldAssist.PlayerStartsOnGlove(true));
        Assert.Equal(0.35, FieldAssist.StickTake);
        Assert.Equal(0.35, _content.Feel.FieldAssistStick);
        Assert.False(FieldAssist.StickTakesGlove(0, 0, FieldAssist.StickTake, false));
        Assert.False(FieldAssist.StickTakesGlove(0.1, 0.1, FieldAssist.StickTake, false));
        Assert.True(FieldAssist.StickTakesGlove(0.4, 0, FieldAssist.StickTake, false));
        Assert.True(FieldAssist.StickTakesGlove(0, 0, FieldAssist.StickTake, true));
    }

    [Fact]
    public void CpuHopperResolvesWithoutASouthPress()
    {
        var match = Match.Exhibition(_content, "rio", "ashlord", seed: 7);
        Assert.True(match.Top);
        var hopper = new AtBatResult(ContactQuality.Solid, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
        var field = match.ResolveFielding(hopper);
        Assert.True(field.Kind is PlayKind.GroundOut or PlayKind.Single or PlayKind.FlyOut, field.Kind.ToString());
        Assert.NotNull(field.Fielder);
    }

    [Fact]
    public void CoverSpotsAreTheBagsNotTheDirtPads()
    {
        Assert.Equal(Diamond.First, FieldAssist.CoverSpot("1B"));
        Assert.Equal(Diamond.Second, FieldAssist.CoverSpot("2B"));
        Assert.Equal(Diamond.Third, FieldAssist.CoverSpot("3B"));
        Assert.Equal(Diamond.Home, FieldAssist.CoverSpot("C"));
        Assert.Equal(Diamond.Rubber, FieldAssist.CoverSpot("P"));
    }
}
