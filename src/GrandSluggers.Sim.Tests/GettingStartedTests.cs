using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class GettingStartedTests
{
    [Fact]
    public void GettingStartedIsANumberedPathAndAModeTable()
    {
        Assert.Equal(["title", "field", "captains", "lineup", "pitch"], GettingStarted.Path.Select(s => s.Id));
        Assert.Equal(["title", "field", "select", "lineup", "plate"], GettingStarted.Path.Select(s => s.Shot));
        Assert.Equal(["exhibition", "training", "two-pads"], GettingStarted.Modes.Select(m => m.Id));
        Assert.Equal("Exhibition", GettingStarted.Modes[0].Title);
        Assert.Equal("Training", GettingStarted.Modes[1].Title);
        Assert.Equal("Two controllers", GettingStarted.Modes[2].Title);
        foreach (var step in GettingStarted.Path)
        {
            Assert.False(HowToPlay.NamesKeyboard(step.Caption), step.Id);
            Assert.StartsWith("how-to-start-", step.Picture);
            Assert.True(StillRequest.AllowedShots.Contains(step.Shot), step.Shot);
        }
        foreach (var mode in GettingStarted.Modes)
            Assert.False(HowToPlay.NamesKeyboard(mode.Line), mode.Id);
        Assert.Contains("South", GettingStarted.Path[0].Caption);
        Assert.Contains("Tutorials", GettingStarted.Modes[1].Line);
        Assert.Contains("Controller", GettingStarted.Modes[2].Line);
        var banned = new[] { "Challenge", "Toy Field", "minigame", "Records", "save file", "Wii", "disc" };
        var copy = GettingStarted.Path.SelectMany(s => new[] { s.Title, s.Caption })
            .Concat(GettingStarted.Modes.SelectMany(m => new[] { m.Title, m.Line }))
            .Concat(HowToPlay.Must("getting-started").Lines);
        foreach (var needle in banned)
            Assert.DoesNotContain(copy, l => l.Contains(needle, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Exhibition"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Training"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("View"));
        const float w = 1280, h = 800;
        var path = GettingStarted.PathRow(w, h);
        var table = GettingStarted.ModeTable(w, h);
        var band = GettingStarted.LineBand(w, h);
        Assert.Equal(ControlDiagram.Board(w, h).H, path.H);
        Assert.True(band.Y > table.Y + table.H - 1f);
        var first = GettingStarted.StepCell(0, w, h);
        var last = GettingStarted.StepCell(GettingStarted.Path.Count - 1, w, h);
        Assert.Equal(first.X, last.X);
        Assert.True(last.Y > first.Y);
        Assert.True(first.Y + first.H <= last.Y + 1f);
        Assert.Equal("getting-started-modes", HowToPlay.Must("getting-started-modes").Id);
    }
}
