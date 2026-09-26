using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The CPU glove's decision table (§8.8) read off a view, with no live play behind it: the table decides from what it is shown,
/// and changes nothing. The live rows (S-41, S-50, the bunt rows, the relay) hold it in play; these hold it alone.
/// </summary>
public sealed class CpuFieldDeciderTests
{
    sealed class View : ICpuFieldView
    {
        public RulesTable Rules { get; init; } = Shipped.Content.Rules;
        public double Elapsed { get; init; } = 2;
        public double GloveX { get; init; }
        public double GloveZ { get; init; }
        public double Dash01 => 0;
        public InPlay.ForceState Forces { get; init; } = InPlay.ForceState.Empty;
        public FlyState Fly { get; init; }
        public IReadOnlyList<Runner> Runners { get; init; } = [];
        public int Outs { get; init; }
        public int HomeScore => 0;
        public int AwayScore => 0;
        public BattedBall? Ball => null;
        public AtBatResult? Hit => null;
        public int Reads { get; private set; }
        public Runner? RunnerAt(int fromBag) => null;
        public Runner? RunnerForBag(int bag) { Reads++; return null; }
        public double ThrowArrivalSec(int bag) => 1;
        public double WalkSec(int bag) => 1;
        public double ThrowReadySec(int bag) => 1;
        public double HoldLeftSec { get; init; } = double.PositiveInfinity;
    }

    [Fact]
    public void AnOutfielderWithNothingMakeableThrowsItIn()
    {
        var view = new View { GloveX = 0, GloveZ = 250 };
        Assert.True(FieldingResolver.OutfieldGrass(view.GloveX, view.GloveZ, view.Rules));
        Assert.Equal(new CpuFieldDecision(CpuFieldAction.ThrowIn), CpuFieldDecider.Decide(view));
    }

    [Fact]
    public void AnInfielderWithNobodyToPlayHolds()
    {
        var view = new View { GloveX = -20, GloveZ = 90 };
        Assert.False(FieldingResolver.OutfieldGrass(view.GloveX, view.GloveZ, view.Rules));
        Assert.Equal(CpuFieldDecision.Hold, CpuFieldDecider.Decide(view));
        Assert.True(view.Reads > 0, "the table read the bodies it was shown");
    }

    [Fact]
    public void TwoOutsWithNobodyToPlayHoldsToo()
    {
        var view = new View { GloveX = -20, GloveZ = 90, Outs = 2 };
        Assert.Equal(CpuFieldDecision.Hold, CpuFieldDecider.Decide(view));
    }
}
