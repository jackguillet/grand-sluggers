using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public sealed class TutorialCatalogTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    TutorialCatalog Load() => TutorialCatalog.Load(_content);

    [Fact]
    public void EveryRuntimeControlAbilityItemAndStarIsMappedIndependently()
    {
        var c=Load();
        Assert.Empty(c.Validate(_content));
        Assert.Subset(c.Mechanics.SelectMany(m=>m.Sources).ToHashSet(),TutorialCatalog.RuntimeSources(_content).ToHashSet());
        Assert.True(c.Lessons.Count(l=>l.Status=="implemented") >= 31);
        foreach(var l in c.Lessons.Where(l=>l.Status=="implemented" && l.Id!="T-G06-C"))
            Assert.Contains(c.Profile,l.Profiles);
    }

    [Fact]
    public void RemovingAFeatureFromTheInventoryDoesNotHideItsRuntimeCoverageGap()
    {
        var c=Load();var m=c.Mechanics.First(m=>m.Sources.Contains("control:pitching/Changeup"));
        c.Mechanics[Array.IndexOf(c.Mechanics,m)] = m with { Sources=[] };
        Assert.Contains(c.Validate(_content),e=>e.Contains("unmapped runtime source control:pitching/Changeup"));
    }

    [Theory]
    [InlineData("mechanic")][InlineData("setup")][InlineData("objective")][InlineData("profile")]
    [InlineData("ability")][InlineData("prerequisite")][InlineData("controls")][InlineData("evidence")]
    public void DanglingOrUnimplementedLessonReferencesAreRejected(string field)
    {
        var c=Load();var i=Array.FindIndex(c.Lessons,l=>l.Id=="T-P01");var l=c.Lessons[i];
        c.Lessons[i]=field switch
        {
            "mechanic"=>l with { Mechanics=["not-a-mechanic"] },
            "setup"=>l with { Setup="missing" },
            "objective"=>l with { Objective="award-success" },
            "profile"=>l with { Profiles=["unknown"] },
            "ability"=>l with { Requires=["unknown"] },
            "prerequisite"=>l with { Prerequisites=["unknown"] },
            "controls"=>l with { Controls=["unknown"] },
            _=>l with { Tests=[] }
        };
        Assert.NotEmpty(c.Validate(_content));
    }

    [Fact]
    public void DuplicateAndNullRowsReturnUsefulErrors()
    {
        var c=Load();c.Lessons[1]=c.Lessons[0];
        Assert.Contains(c.Validate(_content),e=>e.Contains("duplicate lesson"));
        c=Load();c.Mechanics[0]=null!;
        Assert.Contains(c.Validate(_content),e=>e.Contains("null row"));
    }

    [Fact]
    public void CyclicPrerequisitesAndImpossibleFixtureNumbersFailValidation()
    {
        var c=Load();
        c.Lessons[0]=c.Lessons[0] with { Prerequisites=[c.Lessons[1].Id] };
        c.Lessons[1]=c.Lessons[1] with { Prerequisites=[c.Lessons[0].Id] };
        Assert.Contains(c.Validate(_content),e=>e.Contains("cycle"));
        c=Load();c.Setups[0]=c.Setups[0] with { TimeoutSec=double.NaN };
        Assert.Contains(c.Validate(_content),e=>e.Contains("invalid timeout"));
        c=Load();c.Setups.First(s=>s.Policy=="grounder").Balls["shipped"]=new(-1,0,4,0);
        Assert.Contains(c.Validate(_content),e=>e.Contains("invalid ball"));
    }

    [Fact]
    public void ANewUncoveredMechanicCannotInheritTheMigrationAllowance()
    {
        var c=Load();c.Mechanics[0]=c.Mechanics[0] with { Id="new-feature" };
        Assert.Contains(c.Validate(_content),e=>e.Contains("uncovered mechanic new-feature"));
        Assert.Contains(c.Validate(_content),e=>e.Contains("unowned migration debt new-feature"));
    }
}
