using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BodyScaleTests
{
    readonly string _repo = Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, ".."));

    [Fact]
    public void HighlightAndHintNeverScaleTheToy()
    {
        Assert.Equal(BodyScale.Rest, BodyScale.Of());
        Assert.Equal(BodyScale.Rest, BodyScale.Of(highlighted: true, hint: false));
        Assert.Equal(BodyScale.Rest, BodyScale.Of(highlighted: false, hint: true));
        Assert.Equal(BodyScale.Rest, BodyScale.Of(highlighted: true, hint: true));
    }

    [Fact]
    public void HeroActorUsesBodyScaleAndDoesNotScaleHighlight()
    {
        var src = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/HeroActor.cs"));
        Assert.Contains("BodyScale.Of", src, StringComparison.Ordinal);
        Assert.DoesNotContain("1.45f", src, StringComparison.Ordinal);
        Assert.DoesNotContain("1.18f", src, StringComparison.Ordinal);
        Assert.DoesNotContain("1.12f", src, StringComparison.Ordinal);
        Assert.DoesNotContain("_lit ?", src, StringComparison.Ordinal);
        Assert.DoesNotContain("_hint ?", src, StringComparison.Ordinal);
    }

    /// <summary>No field ability grows the toy (AB-12): the director never asks for a bigger body.</summary>
    [Fact]
    public void ActorDirectorNeverGrowsABody()
    {
        var src = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/ActorDirector.cs"));
        Assert.DoesNotContain("SetGrow", src, StringComparison.Ordinal);
    }
}
