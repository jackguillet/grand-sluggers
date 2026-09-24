using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>The book teaches the one control scheme the game has: the gamepad.</summary>
public class BookControlsTests
{
    [Fact]
    public void FooterAndBadgesArePadOnly()
    {
        Assert.Contains("South", HowToPlay.BookFooter);
        Assert.Contains("East", HowToPlay.BookFooter);
        Assert.False(HowToPlay.NamesKeyboard(HowToPlay.BookFooter));
        Assert.Equal("Two controllers", HowToPlay.PageBadge("two-pads"));
        Assert.Null(HowToPlay.PageBadge("controls"));
        Assert.Null(HowToPlay.PageBadge("contents"));
    }

    [Fact]
    public void ControlDiagramNamesPadHardwareOnly()
    {
        Assert.NotEmpty(ControlDiagram.PadParts);
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Hardware.StartsWith("South"));
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Hardware.Contains("stick", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Offense.Contains("steal", StringComparison.OrdinalIgnoreCase)
            || c.Always.Contains("Move", StringComparison.OrdinalIgnoreCase));
        foreach (var c in ControlDiagram.PadCallouts)
        {
            foreach (var text in new[] { c.Hardware, c.Offense, c.Defense, c.Always })
                Assert.False(HowToPlay.NamesKeyboard(text), c.Id + ": " + text);
            Assert.InRange(c.U, 0, 0.95f);
            Assert.InRange(c.V, 0, 0.95f);
            Assert.False(string.IsNullOrWhiteSpace(c.Hardware), c.Id);
            Assert.True(c.Offense.Length + c.Defense.Length + c.Always.Length > 0, c.Id);
        }
        foreach (var p in ControlDiagram.PadParts)
        {
            Assert.InRange(p.U, 0, 1);
            Assert.InRange(p.V, 0, 1);
            Assert.True(p.U + p.W <= 1.02f, p.Id);
            Assert.True(p.V + p.H <= 1.02f, p.Id);
        }
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Offense.Length > 0);
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Defense.Length > 0);
        Assert.Equal("Offense", ControlDiagram.OffenseLabel);
        Assert.Equal("Defense", ControlDiagram.DefenseLabel);
        var cell = ControlDiagram.CalloutCell(0, 1280, 800);
        Assert.True(cell.H >= 56, $"control row too short {cell.H}");
        Assert.True(cell.W >= 400, $"control row too narrow {cell.W}");
    }

    [Fact]
    public void RunningSpreadHasClosePlayAndTagCallouts()
    {
        Assert.Equal(2, BagDiagrams.Callouts.Count);
        Assert.Contains(BagDiagrams.Callouts, c => c.Title.Equals("Close play", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(BagDiagrams.Callouts, c => c.Title.Equals("Tag", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("SOUTH", BagDiagrams.ClosePlay.Press, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("off", BagDiagrams.Tag.Line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("on a bag they are out", BagDiagrams.Tag.Line, StringComparison.OrdinalIgnoreCase);
        Assert.True(HowToPlay.Mentions("Close play"));
    }

    [Fact]
    public void ChapterMascotsAreRosterCaptains()
    {
        Assert.True(BookChapter.EveryPageHasARosterCaptain());
        Assert.Equal("rio", BookChapter.Captain("contents"));
        Assert.Equal("konga", BookChapter.Captain("running"));
        Assert.Equal("ashlord", BookChapter.Captain("fielding"));
        Assert.DoesNotContain(BookChapter.Captains.Values, id => id.Equals("mario", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ControlsPageIsPadOnly()
    {
        var page = HowToPlay.Must("controls").Lines;
        Assert.Contains(page, l => l.Contains("RT"));
        Assert.Contains(page, l => l.Contains("offense", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(page, HowToPlay.NamesKeyboard);
        Assert.False(HowToPlay.Mentions("left click"));
        Assert.False(HowToPlay.Mentions("keyboard"));
        Assert.False(HowToPlay.Mentions("mouse"));
        var board = ControlDiagram.Board(1280, 800);
        Assert.True(board.W > 900);
        Assert.True(board.H > 400);
    }
}
