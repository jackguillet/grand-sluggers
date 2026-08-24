using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class SchemeTests
{
    [Fact]
    public void EveryProductVerbHasPadAndKeyboard()
    {
        foreach (var id in new[]
        {
            "confirm", "charge", "star", "aim-run", "bags",
            "all-advance", "all-return", "steal", "changeup", "swap", "bunt",
            "call-time", "dash", "pickoff", "skip"
        })
        {
            var v = Scheme.Must(id);
            Assert.False(string.IsNullOrWhiteSpace(v.Pad), id);
            Assert.False(string.IsNullOrWhiteSpace(v.Keys), id);
            Assert.False(Scheme.IsDebug(v.Keys));
        }
        Assert.Equal("Space / Enter", Scheme.Keys("confirm"));
        Assert.Equal("Shift", Scheme.Keys("charge"));
        Assert.Equal("Q", Scheme.Keys("star"));
        Assert.Equal("WASD", Scheme.Keys("aim-run"));
        Assert.Equal("1 2 3 4", Scheme.Keys("bags"));
        Assert.Equal(",", Scheme.Keys("all-advance"));
        Assert.Equal(".", Scheme.Keys("all-return"));
        Assert.Equal("Z", Scheme.Keys("steal"));
        Assert.Equal("V", Scheme.Keys("changeup"));
        Assert.Equal("R", Scheme.Keys("swap"));
        Assert.Equal("V", Scheme.Keys("bunt"));
        Assert.Equal("H", Scheme.Keys("call-time"));
        Assert.Equal("South", Scheme.Pad("confirm"));
        Assert.Equal("LB", Scheme.Pad("all-advance"));
        Assert.Equal("RB", Scheme.Pad("all-return"));
        Assert.Equal("L3", Scheme.Pad("steal"));
    }

    [Fact]
    public void F1F2F3StayDebug()
    {
        Assert.True(Scheme.IsDebug("F1"));
        Assert.True(Scheme.IsDebug("F2"));
        Assert.True(Scheme.IsDebug("F3"));
        Assert.False(Scheme.IsDebug("Space / Enter"));
        Assert.False(Scheme.IsDebug("Z"));
        Assert.DoesNotContain(Scheme.Product, v => v.Keys.Contains("F1") || v.Keys.Contains("F2") || v.Keys.Contains("F3"));
    }
}
