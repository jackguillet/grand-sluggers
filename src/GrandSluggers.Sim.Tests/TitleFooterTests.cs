using Xunit;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The title's controls line is navigation, not onboarding (#806). It names every title verb, and nothing a first
/// pitch, a match or a finished or abandoned lesson sets can hide it.
/// </summary>
public class TitleFooterTests
{
    readonly string _repo = Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, ".."));

    [Fact]
    public void TheTitleFooterNamesEveryTitleVerb()
    {
        var footer = CarnivalFront.TitleFooter;
        Assert.Contains("Up/down choose", footer);
        Assert.Contains("South confirm", footer);
        Assert.Contains("View how to play", footer);
        Assert.Contains("Start options", footer);
        // The title is the root: there is nothing for East to go back to.
        Assert.DoesNotContain("East", footer);
        // Controllers are the only player input (§ controls).
        Assert.DoesNotContain("Space", footer);
        Assert.DoesNotContain("Esc", footer);
    }

    [Fact]
    public void NoPlayStateHidesTheTitleFooter()
    {
        var runtime = Path.Combine(_repo, "unity/Assets/Scripts/Runtime");
        Assert.Contains("CarnivalFront.TitleFooter", File.ReadAllText(Path.Combine(runtime, "HudView.cs")));
        foreach (var file in Directory.EnumerateFiles(runtime, "*.cs"))
        {
            var src = File.ReadAllText(file);
            Assert.DoesNotContain("hideHelp", src, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gs.trained", src);
        }
    }
}
