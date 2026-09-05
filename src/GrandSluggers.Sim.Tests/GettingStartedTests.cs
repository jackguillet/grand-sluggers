using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class GettingStartedTests
{
    [Fact]
    public void GettingStartedIsANumberedPathAndAModeTable()
    {
        Assert.Equal(["title", "captains", "field", "lineup", "pitch"], GettingStarted.Path.Select(s => s.Id));
        Assert.Equal(["title", "select", "field", "lineup", "plate"], GettingStarted.Path.Select(s => s.Shot));
        Assert.Equal(["exhibition", "training", "two-pads"], GettingStarted.Modes.Select(m => m.Id));
        Assert.Equal("Exhibition", GettingStarted.Modes[0].Title);
        Assert.Equal("Training", GettingStarted.Modes[1].Title);
        Assert.Equal("Two pads", GettingStarted.Modes[2].Title);
        foreach (var step in GettingStarted.Path)
        {
            Assert.False(HowToPlay.MixesHardware(step.PadCaption), step.Id);
            Assert.False(HowToPlay.MixesHardware(step.KeysCaption), step.Id);
            Assert.StartsWith("how-to-start-", step.Picture);
            Assert.True(StillRequest.AllowedShots.Contains(step.Shot), step.Shot);
        }
        foreach (var mode in GettingStarted.Modes)
        {
            Assert.False(HowToPlay.MixesHardware(mode.PadLine), mode.Id);
            Assert.False(HowToPlay.MixesHardware(mode.KeysLine), mode.Id);
        }
        Assert.Contains("South", GettingStarted.Caption(GettingStarted.Path[0], InputScheme.Pad));
        Assert.DoesNotContain("Space", GettingStarted.Caption(GettingStarted.Path[0], InputScheme.Pad));
        Assert.Contains("Space", GettingStarted.Caption(GettingStarted.Path[0], InputScheme.Keys));
        Assert.DoesNotContain("South", GettingStarted.Caption(GettingStarted.Path[0], InputScheme.Keys));
        Assert.Contains("West", GettingStarted.Line(GettingStarted.Modes[1], InputScheme.Pad));
        Assert.Contains("F", GettingStarted.Line(GettingStarted.Modes[1], InputScheme.Keys));
        Assert.DoesNotContain("West", GettingStarted.Line(GettingStarted.Modes[1], InputScheme.Keys));
        Assert.Contains("Gamepad", GettingStarted.Line(GettingStarted.Modes[2], InputScheme.Pad));
        Assert.Contains("player 1 only", GettingStarted.Line(GettingStarted.Modes[2], InputScheme.Keys));
        var banned = new[] { "Challenge", "Toy Field", "minigame", "Records", "save file", "Wii", "disc" };
        var copy = GettingStarted.Path.SelectMany(s => new[] { s.Title, s.PadCaption, s.KeysCaption })
            .Concat(GettingStarted.Modes.SelectMany(m => new[] { m.Title, m.PadLine, m.KeysLine }))
            .Concat(HowToPlay.Must("getting-started").Lines)
            .Concat(HowToPlay.Must("getting-started").KeyLines!);
        foreach (var needle in banned)
            Assert.DoesNotContain(copy, l => l.Contains(needle, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Exhibition"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Training"));
        Assert.Contains(HowToPlay.Must("getting-started").Lines, l => l.Contains("Esc"));
        const float w = 1280, h = 800;
        var path = GettingStarted.PathRow(w, h);
        var table = GettingStarted.ModeTable(w, h);
        var band = GettingStarted.LineBand(w, h);
        Assert.True(table.Y > path.Y + path.H - 1f);
        Assert.True(band.Y > table.Y + table.H - 1f);
        var first = GettingStarted.StepCell(0, w, h);
        var last = GettingStarted.StepCell(GettingStarted.Path.Count - 1, w, h);
        Assert.True(last.X > first.X);
        Assert.True(first.X + first.W <= last.X + 1f);
    }
}
