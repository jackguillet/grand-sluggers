using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class HudCalloutsTests
{
    [Fact]
    public void ScreenPageLabelsTheHudWeDraw()
    {
        Assert.Equal(2, HudCallouts.OnScreenPage.Count);
        Assert.Equal("plate", HudCallouts.Set.Shot);
        Assert.Equal("diamond-grounder", HudCallouts.InPlay.Shot);
        Assert.Contains(HudCallouts.Set.Marks, m => m.Id == "score" && m.Anchor == BroadcastHud.Standard.Score);
        Assert.Contains(HudCallouts.Set.Marks, m => m.Id == "count" && m.Anchor == BroadcastHud.Standard.Count);
        Assert.Contains(HudCallouts.Set.Marks, m => m.Id == "diamond" && m.Anchor == BroadcastHud.Standard.MiniDiamond);
        Assert.Contains(HudCallouts.Set.Marks, m => m.Id == "batter" && m.Anchor == BroadcastHud.Standard.BatterCard);
        Assert.Contains(HudCallouts.Set.Marks, m => m.Id == "pitcher" && m.Label.Contains("TIRED"));
        Assert.Contains(HudCallouts.InPlay.Marks, m => m.Id == "you" && m.Anchor == BroadcastHud.YouTell);
        Assert.Contains(HudCallouts.InPlay.Marks, m => m.Id == "item" && m.Anchor == BroadcastHud.ItemTell);
        Assert.Contains(HudCallouts.InPlay.Marks, m => m.Id == "landing" && m.Label.Contains("Landing"));
        Assert.Contains("YOU", BroadcastHud.ControlDisplay(true, "CF", "Rio Sparks"));
        Assert.Contains("TIRED", BroadcastHud.ArmLine(10));
        Assert.Contains("ITEM", BroadcastHud.ItemPointer(true, "Ashlord"));
        Assert.True(BroadcastHud.InFrame(BroadcastHud.YouTell, 1280, 800));
        Assert.True(BroadcastHud.InFrame(BroadcastHud.ItemTell, 1280, 800));
        foreach (var mark in HudCallouts.Set.Marks.Concat(HudCallouts.InPlay.Marks))
            Assert.False(HowToPlay.MixesHardware(mark.Label), mark.Id);
        var row = HudCallouts.Row(0, 1280, 800);
        Assert.True(row.W > 900);
        Assert.Equal("screen", HowToPlay.Must("screen").Id);
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("YOU"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("landing ring"));
    }
}
