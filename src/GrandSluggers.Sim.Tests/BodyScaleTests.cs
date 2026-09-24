using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BodyScaleTests
{
    readonly string _repo = Path.GetFullPath(Path.Combine(ContentCatalog.Load().Root.Shipped, ".."));
    readonly ContentCatalog _content = ContentCatalog.Load();

    static readonly string[] NotGrow =
        ["lick-catch", "super-jump", "dive", "laser", "clamber", "snap-throw", ""];

    [Fact]
    public void AGloveWithTheBallIsRestScaleForEveryCaptain()
    {
        Assert.True(BodyScale.Grow > BodyScale.Rest);
        foreach (var id in Shipped.CaptainIds)
        {
            var who = _content.Must(id);
            Assert.Equal(BodyScale.Rest, BodyScale.Live(
                who.FieldAbility, playGlove: true, holdBall: true, highlighted: true, hint: true));
            Assert.Equal(BodyScale.Rest, BodyScale.Live(
                who.FieldAbility, playGlove: true, holdBall: true, highlighted: false, hint: false));
            Assert.Equal(BodyScale.Rest, BodyScale.Of(
                grow: BodyScale.IsGrow(who.FieldAbility), holdBall: true, highlighted: true, hint: true));
        }
    }

    [Fact]
    public void GrowAbilityIsTheOnlyScaleUp()
    {
        Assert.Equal(BodyScale.Grow, BodyScale.Live("grow", playGlove: true, holdBall: false));
        Assert.Equal(BodyScale.Grow, BodyScale.Live(
            "grow", playGlove: true, holdBall: false, highlighted: true, hint: true));
        Assert.Equal(BodyScale.Rest, BodyScale.Live("grow", playGlove: false, holdBall: false, highlighted: true));
        Assert.Equal(BodyScale.Rest, BodyScale.Live("grow", playGlove: true, holdBall: true, highlighted: true));
        foreach (var ability in NotGrow)
        {
            Assert.Equal(BodyScale.Rest, BodyScale.Live(
                ability, playGlove: true, holdBall: false, highlighted: true, hint: true));
            Assert.Equal(BodyScale.Rest, BodyScale.Live(
                ability, playGlove: true, holdBall: true, highlighted: true, hint: true));
        }
        foreach (var id in Shipped.CaptainIds)
        {
            var who = _content.Must(id);
            var chasing = BodyScale.Live(who.FieldAbility, playGlove: true, holdBall: false, highlighted: true);
            Assert.Equal(BodyScale.IsGrow(who.FieldAbility) ? BodyScale.Grow : BodyScale.Rest, chasing);
        }
    }

    [Fact]
    public void HighlightAndHintNeverScaleTheToy()
    {
        Assert.Equal(BodyScale.Rest, BodyScale.Of(grow: false, highlighted: true, hint: false));
        Assert.Equal(BodyScale.Rest, BodyScale.Of(grow: false, highlighted: false, hint: true));
        Assert.Equal(BodyScale.Rest, BodyScale.Of(grow: false, highlighted: true, hint: true));
        Assert.Equal(BodyScale.Grow, BodyScale.Of(grow: true, highlighted: false, hint: false));
        Assert.Equal(BodyScale.Grow, BodyScale.Of(grow: true, highlighted: true, hint: true));
        Assert.Equal(BodyScale.Rest, BodyScale.Of(grow: true, holdBall: true, highlighted: true, hint: true));
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

    [Fact]
    public void ActorDirectorDoesNotGrowAGloveThatHoldsTheBall()
    {
        var src = File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime/ActorDirector.cs"));
        Assert.Contains("BodyScale.GrowOn", src, StringComparison.Ordinal);
        Assert.Contains("holdBall", src, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "SetGrow(who.FieldAbility == \"grow\" && highlighted)",
            src,
            StringComparison.Ordinal);
        Assert.Contains("SetGrow(false)", src, StringComparison.Ordinal);
    }
}
