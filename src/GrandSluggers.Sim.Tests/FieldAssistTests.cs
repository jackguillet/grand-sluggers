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

    [Fact]
    public void AfterThrowToSecondYouAreTheCoverAtSecond()
    {
        Assert.Equal("2B", FieldAssist.AfterThrowPos("SS", 2));
        Assert.Equal("1B", FieldAssist.AfterThrowPos("SS", 1));
        Assert.Equal("3B", FieldAssist.AfterThrowPos("LF", 3));
        Assert.Equal("C", FieldAssist.AfterThrowPos("RF", 4));
        Assert.Equal("SS", FieldAssist.AfterThrowPos("SS", 0));
        Assert.Equal("SS", FieldAssist.AfterThrowPos("SS", InPlay.CommitBag(0, hopperCaught: true, cutoff: true)));
        Assert.Equal("1B", FieldAssist.AfterThrowPos("SS", InPlay.CommitBag(0, hopperCaught: true, cutoff: false)));
        Assert.Equal("2B", FieldAssist.CoverKey(2));
        Assert.Equal("", FieldAssist.CoverKey(0));
    }

    [Fact]
    public void SwapGloveIsTowardStickOrNextNearestToBallNotDiamondOrder()
    {
        var at = new Dictionary<string, (double X, double Z)>
        {
            ["SS"] = Diamond.Positions["SS"],
            ["2B"] = Diamond.Positions["2B"],
            ["1B"] = Diamond.Positions["1B"],
            ["3B"] = Diamond.Positions["3B"],
            ["P"] = Diamond.Positions["P"],
        };
        var ball = Diamond.Positions["SS"];
        Assert.Equal("2B", FieldAssist.SwapGlove("SS", at, ball.X, ball.Z, 1, 0));
        Assert.Equal("3B", FieldAssist.SwapGlove("SS", at, ball.X, ball.Z, -1, 0));
        Assert.Equal("1B", FieldAssist.SwapGlove("P", at, ball.X, ball.Z, 1, -0.4));
        var nearSecond = (Diamond.Positions["2B"].X, Diamond.Positions["2B"].Z + 4);
        Assert.Equal("2B", FieldAssist.SwapGlove("SS", at, nearSecond.Item1, nearSecond.Item2, 0, 0));
        Assert.Equal("3B", FieldAssist.SwapGlove("SS", at, ball.X, ball.Z, 0, 0));
    }
}
