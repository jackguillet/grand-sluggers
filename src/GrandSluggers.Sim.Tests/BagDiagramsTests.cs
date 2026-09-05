using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BagDiagramsTests
{
    [Fact]
    public void RunningDiagramsUseTheSharedBagMapAndSchemeOnlyPresses()
    {
        Assert.Equal(3, BagDiagrams.Running.Count);
        Assert.Equal(BagDiagrams.Kind.BagMap, BagDiagrams.Running[0].Kind);
        Assert.Equal(BagDiagrams.Kind.Advance, BagDiagrams.Running[1].Kind);
        Assert.Equal(BagDiagrams.Kind.Return, BagDiagrams.Running[2].Kind);

        for (var bag = 1; bag <= 4; bag++)
            Assert.Equal(FieldAssist.BagPip(bag), BagDiagrams.Pip(bag));

        Assert.Equal("RIGHT", BagDiagrams.Direction(1));
        Assert.Equal("UP", BagDiagrams.Direction(2));
        Assert.Equal("LEFT", BagDiagrams.Direction(3));
        Assert.Equal("DOWN", BagDiagrams.Direction(4));
        Assert.Equal("HOME", BagDiagrams.BagName(4));

        Assert.Equal("D-PAD", BagDiagrams.Press(BagDiagrams.BagMap, InputScheme.Pad));
        Assert.Contains("1", BagDiagrams.Press(BagDiagrams.BagMap, InputScheme.Keys));
        Assert.DoesNotContain("South", BagDiagrams.Press(BagDiagrams.BagMap, InputScheme.Keys));
        Assert.Equal("LB", BagDiagrams.Press(BagDiagrams.Advance, InputScheme.Pad));
        Assert.Equal(",", BagDiagrams.Press(BagDiagrams.Advance, InputScheme.Keys));
        Assert.Equal("RB", BagDiagrams.Press(BagDiagrams.Return, InputScheme.Pad));
        Assert.Equal(".", BagDiagrams.Press(BagDiagrams.Return, InputScheme.Keys));

        Assert.Equal([new BagDiagrams.Route(1, 2), new(2, 3), new(3, 4)], BagDiagrams.Advance.Routes);
        Assert.Equal([new BagDiagrams.Route(1, 4), new(2, 1), new(3, 2)], BagDiagrams.Return.Routes);
    }

    [Fact]
    public void DiagramCardsFitTheBookAndLeaveACaptionBand()
    {
        var first = BagDiagrams.Card(0, 1280, 800);
        var middle = BagDiagrams.Card(1, 1280, 800);
        var last = BagDiagrams.Card(2, 1280, 800);
        var band = BagDiagrams.LineBand(1280, 800);

        Assert.True(first.W > 300);
        Assert.True(middle.X > first.X);
        Assert.True(last.X > middle.X);
        Assert.True(band.Y >= first.Y + first.H);
    }
}
