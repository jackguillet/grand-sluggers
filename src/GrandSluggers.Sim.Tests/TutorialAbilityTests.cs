using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

[Trait("Rows", "compact")]
public sealed class TutorialAbilityTests
{
    static readonly ContentCatalog Shipped = ContentCatalog.Load();
    public static IEnumerable<object[]> Cases => from profile in new[] { "shipped", "c80" }
        from id in new[] { "T-F16", "T-A-burrow", "T-A-grow", "T-A-lick-catch", "T-A-withdraw" }
        select new object[] { profile, id };

    static (ContentCatalog, TutorialCatalog) Load(string profile)
    {
        var content = profile == "shipped" ? Shipped : ContentCatalog.Load(new DataRoot(Shipped.Root.Shipped,
            Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, "..", "trials", "c80"))));
        return (content, TutorialCatalog.Load(content));
    }

    static void Drive(TutorialSession run, string profile, LivePlayCommandSource source, bool move)
    {
        var offset = profile == "shipped" ? 16 : run.Lesson.Id is "T-F16" or "T-A-grow" ? 16 : 12;
        for (var i = 0; i < 1800 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (move && i > 0)
            {
                var target = live.Preview!.Grounder
                    ? (live.BallX, live.BallZ) : FlyCatch.ChaseTarget(live.Preview, run.Match.Park, run.Match.Rules);
                if (!live.Preview.Grounder) target.Item1 += offset;
                var dx = target.Item1 - live.GloveX;
                var dz = target.Item2 - live.GloveZ;
                var dist = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
                var south = live.Preview.Grounder || live.ElapsedSeconds >= live.Preview.HangTimeSec - .6;
                pad = dist > 1
                    ? new(StickX: dx / dist, StickY: dz / dist, SouthDown: south)
                    : new(StickX: -dz / dist, StickY: dx / dist, SouthDown: south);
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
            Assert.True(run.Feedback!.Success, id + "/" + profile + ": " + run.Feedback.Detail);
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
