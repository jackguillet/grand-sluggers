using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The runner on third's infield-back read (§9.9, #732): a grounder one of the four depth infielders meets at or behind his
/// own standard depth sends the runner; a ball met in front of it, or by the pitcher or the catcher, leaves the margin to
/// decide. The depth is the diamond's (<see cref="Diamond.Positions"/>), so the read holds on any basepath.
/// </summary>
public sealed class InfieldBackReadTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Theory]
    [InlineData("2B", 4, true)]
    [InlineData("SS", 4, true)]
    [InlineData("1B", 4, true)]
    [InlineData("3B", 4, true)]
    [InlineData("2B", -12, false)]
    [InlineData("SS", -12, false)]
    [InlineData("P", 0, false)]
    public void TheRunnerOnThirdGoesWhenTheFielderMeetsTheBallAtOrBehindHisOwnDepth(string pos, double deeperFt, bool goes)
    {
        var scenario = new Scenario(_content, seed: 1).Runner(3, 2).Outs(1);
        var match = scenario.Match;
        scenario.Contact();
        var runner = match.RunnerAt(3)!;
        var spot = Diamond.Positions[pos];
        var depth = Diamond.Dist(spot.X, spot.Z, Diamond.Home.X, Diamond.Home.Z);
        var scale = (depth + deeperFt) / depth;
        var (x, z) = (spot.X * scale, spot.Z * scale);
        // Met right away, so the margin home is short: only the infield-back read can send the runner.
        var ball = new BallSituation(false, false, 0, 0, x, z, 0.2, false, x, z, 60, Fielder: pos);
        var margin = RunnerAi.Margin(runner, 4, new RunnerAiContext(0, 1, int.MinValue, FlyState.None, ball, 0), match.Rules);
        Assert.True(margin <= match.Rules.Running.Cpu.ThirdHomeMarginSec + match.Rules.Cpu.Active.RunnerMarginSec,
            $"the fixture: margin home {margin:0.00} s is too short to send the runner on its own");

        RunnerAi.Decide(match.Runners, new RunnerAiContext(0, 1, int.MinValue, FlyState.None, ball, 0), _ => false, match.Rules);

        Assert.Equal(goes, runner.DestBag == 4);
    }
}
