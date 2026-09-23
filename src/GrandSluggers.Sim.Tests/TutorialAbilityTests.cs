using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class TutorialAbilityTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load();
    public static IEnumerable<object[]> Cases => from profile in new[] { "shipped" }
        from id in new[] { "T-F16", "T-A-burrow", "T-A-grow", "T-A-lick-catch", "T-A-withdraw" }
        select new object[] { profile, id };

    static (ContentCatalog, TutorialCatalog) Load(string profile)
    {
        Assert.Equal("shipped", profile);
        return (Shipped, TutorialCatalog.Load(Shipped));
    }

    static void Drive(TutorialSession run, string profile, LivePlayCommandSource source, bool move)
    {

        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (move && i > 0)
            {
                var target = live.Preview!.Grounder
                    ? (live.BallX, live.BallZ) : FlyCatch.ChaseTarget(live.Preview, run.Match.Park, run.Match.Rules);
                var who = live.Preview.Fielder;
                var bonus = FieldAbilities.CatchBonus(who, run.Match.Rules)
                    + (live.Preview.Grounder ? FieldAbilities.GroundRangeBonus(who, run.Match.Rules)
                        : FieldAbilities.FlyRangeBonus(who, run.Match.Rules));
                var ordinary = FieldingResolver.CatchRadiusFt(who, run.Match.Park, run.Match.Rules)
                    - FieldAbilities.CatchBonus(who, run.Match.Rules);
                target.Item1 += ordinary + bonus * .5;
                var dx = target.Item1 - live.GloveX;
                var dz = target.Item2 - live.GloveZ;
                var dist = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                pad = dist > 1
                    ? new(StickX: dx / dist, StickY: dz / dist, SouthDown: false)
                    : new(StickX: -dz / dist, StickY: dx / dist, SouthDown: false);
            }
            run.Tick(1.0 / 60, pad, source);
        }
    }

    [Theory, MemberData(nameof(Cases))]
    public void HumanUsesNamedReachBeyondOrdinaryGloveThreeTimesAndReplays(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, id); run.Begin();
        for (var n = 1; n <= 3; n++)
        {
            Drive(run, profile, LivePlayCommandSource.Human, move: true);
            Assert.True(run.Feedback!.Success, id + "/" + profile + ": " + run.Feedback.Detail + $" catcher={run.LastPlay?.Fielder?.Id} ability={run.Match.LivePlay.TutorialAbilityReachUsed} feat={run.LastPlay?.Outcome?.DefensiveFeat}");
            Assert.Equal(catalog.Setups.Single(s => s.Id == id).Skill, run.Match.LivePlay.TutorialAbilityReachUsed);
            Assert.Equal(n, run.Successes); Assert.Equal(n == 3, run.Passed);
            Assert.Equal(run.Feedback, TutorialSession.Replay(content, catalog, run.Recording()).Feedback);
            run.Retry();
        }
    }

    [Theory, MemberData(nameof(Cases))]
    public void CpuAndDemonstrationCannotOwnAbilityCatch(string profile, string id)
    {
        var (content, catalog) = Load(profile);
        var run = new TutorialSession(content, catalog, id); run.Begin();
        Drive(run, profile, LivePlayCommandSource.Cpu, move: true);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Equal(0, run.Successes);
        run = new TutorialSession(content, catalog, id); run.Begin(demonstration: true);
        Drive(run, profile, LivePlayCommandSource.Cpu, move: true);
        Assert.Equal("demonstration", run.Feedback!.Code);
        Assert.Equal(0, run.Successes);
    }
}
