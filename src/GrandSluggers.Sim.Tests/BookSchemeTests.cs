using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BookSchemeTests : IDisposable
{
    public BookSchemeTests() => BookScheme.Reset();
    public void Dispose() => BookScheme.Reset();

    [Fact]
    public void LastInputPicksTheSchemeUntilYouToggle()
    {
        BookScheme.Observe(InputScheme.Keys);
        Assert.Equal(InputScheme.Keys, BookScheme.Current);
        Assert.False(BookScheme.Locked);
        BookScheme.Observe(InputScheme.Pad);
        Assert.Equal(InputScheme.Pad, BookScheme.Current);
        Assert.True(BookScheme.Select(InputScheme.Keys));
        Assert.True(BookScheme.Locked);
        BookScheme.Observe(InputScheme.Pad);
        Assert.Equal(InputScheme.Keys, BookScheme.Current);
        BookScheme.Open();
        Assert.False(BookScheme.Locked);
        BookScheme.Observe(InputScheme.Pad);
        Assert.Equal(InputScheme.Pad, BookScheme.Current);
    }

    [Fact]
    public void ToggleBarIsOnTheBookAndIsNotPageNav()
    {
        const float w = 1280, h = 800;
        var book = HowToPlay.BookPanel(w, h);
        var bar = BookScheme.ToggleBar(w, h);
        Assert.True(bar.X > book.X + book.W * 0.5f, "toggle sits on the right");
        Assert.True(bar.Y >= book.Y && bar.Y + bar.H < book.Y + 80);
        Assert.Equal(InputScheme.Pad, BookScheme.HitToggle(bar.X + 8, bar.Y + 8, w, h));
        Assert.Equal(InputScheme.Keys, BookScheme.HitToggle(bar.X + bar.W - 8, bar.Y + 8, w, h));
        Assert.Null(BookScheme.HitToggle(book.X + 20, book.Y + 120, w, h));
        Assert.Equal(0, HowToPlay.HitNav(bar.X + 8, bar.Y + 8, w, h, 4));
        Assert.Equal("Controller", BookScheme.Label(InputScheme.Pad));
        Assert.Contains("Keyboard", BookScheme.Label(InputScheme.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("South", BookScheme.Footer(InputScheme.Pad));
        Assert.DoesNotContain("Space", BookScheme.Footer(InputScheme.Pad));
        Assert.Contains("Space", BookScheme.Footer(InputScheme.Keys));
        Assert.DoesNotContain("South", BookScheme.Footer(InputScheme.Keys));
    }

    [Fact]
    public void ControlDiagramDoesNotMixPadAndKeys()
    {
        Assert.NotEmpty(ControlDiagram.PadParts);
        Assert.NotEmpty(ControlDiagram.KeysParts);
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Hardware == "South");
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Hardware.Contains("stick", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Offense.Contains("steal", StringComparison.OrdinalIgnoreCase)
            || c.Always.Contains("Move", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ControlDiagram.KeysCallouts, c => c.Hardware.Contains("Space"));
        Assert.Contains(ControlDiagram.KeysCallouts, c => c.Hardware.Contains("WASD"));
        Assert.Contains(ControlDiagram.KeysCallouts, c => c.Hardware.Contains("left click", StringComparison.OrdinalIgnoreCase));
        foreach (var c in ControlDiagram.PadCallouts.Concat(ControlDiagram.KeysCallouts))
        {
            Assert.False(ControlDiagram.MixesSchemes(c), c.Id);
            Assert.InRange(c.U, 0, 0.95f);
            Assert.InRange(c.V, 0, 0.95f);
            Assert.False(string.IsNullOrWhiteSpace(c.Hardware), c.Id);
            Assert.True(c.Offense.Length + c.Defense.Length + c.Always.Length > 0, c.Id);
        }
        foreach (var p in ControlDiagram.PadParts.Concat(ControlDiagram.KeysParts))
        {
            Assert.InRange(p.U, 0, 1);
            Assert.InRange(p.V, 0, 1);
            Assert.True(p.U + p.W <= 1.02f, p.Id);
            Assert.True(p.V + p.H <= 1.02f, p.Id);
        }
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Offense.Length > 0);
        Assert.Contains(ControlDiagram.PadCallouts, c => c.Defense.Length > 0);
        Assert.Equal("Offense", BookScheme.OffenseLabel);
        Assert.Equal("Defense", BookScheme.DefenseLabel);
        var cell = ControlDiagram.CalloutCell(0, InputScheme.Keys, 1280, 800);
        Assert.True(cell.H >= 56, $"control row too short {cell.H}");
        Assert.True(cell.W >= 400, $"control row too narrow {cell.W}");
    }

    [Fact]
    public void SchemeBadgesArePillsNotASentenceOnEveryPage()
    {
        Assert.Null(BookScheme.SeatBadge(InputScheme.Pad));
        Assert.Equal("Player 1 only", BookScheme.SeatBadge(InputScheme.Keys));
        Assert.Equal("Two controllers", BookScheme.PageBadge("two-pads", InputScheme.Pad));
        Assert.Equal("Two controllers", BookScheme.PageBadge("two-pads", InputScheme.Keys));
        Assert.Null(BookScheme.PageBadge("controls", InputScheme.Pad));
        var contents = HowToPlay.Must("contents");
        Assert.DoesNotContain(contents.Lines, l => l.Contains("player 1 only", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(contents.KeyLines!, l => l.Contains("player 1 only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RunningSpreadHasClosePlayAndTagCallouts()
    {
        Assert.Equal(2, BagDiagrams.Callouts.Count);
        Assert.Contains(BagDiagrams.Callouts, c => c.Title.Equals("Close play", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(BagDiagrams.Callouts, c => c.Title.Equals("Tag", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("SOUTH", BagDiagrams.CalloutPress(BagDiagrams.ClosePlay, InputScheme.Pad), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEFT CLICK", BagDiagrams.CalloutPress(BagDiagrams.ClosePlay, InputScheme.Keys), StringComparison.OrdinalIgnoreCase);
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
    public void ControlsPageSplitsPadAndKeys()
    {
        var page = HowToPlay.Must("controls");
        Assert.NotNull(page.KeyLines);
        var pad = page.Shown(InputScheme.Pad);
        var keys = page.Shown(InputScheme.Keys);
        Assert.Contains(pad, l => l.Contains("South"));
        Assert.DoesNotContain(pad, l => l.Contains("Space"));
        Assert.Contains(keys, l => l.Contains("Space") || l.Contains("left click"));
        Assert.DoesNotContain(keys, l => l.Contains("South"));
        Assert.DoesNotContain(pad, l => l.Contains("South") && l.Contains("Space"));
        Assert.Contains(pad, l => l.Contains("offense", StringComparison.OrdinalIgnoreCase));
        Assert.True(HowToPlay.Mentions("left click") || HowToPlay.Mentions("Left click"));
        Assert.InRange(page.KeyLines!.Count, 1, HowToPlay.KidLineMax);
        var board = ControlDiagram.Board(1280, 800);
        Assert.True(board.W > 900);
        Assert.True(board.H > 400);
    }
}
