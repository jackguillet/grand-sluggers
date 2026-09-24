using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The field's lessons (F8-c): each hazard lesson is earned by ordinary human commands at its own park, and fails when the
/// player skips the read — takes the ball before the hazard acts, runs through the volume, or leaves the pad dead.
/// </summary>
public sealed class HazardLessonTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    const double Frame = 1.0 / 60;
    TutorialSession Start(string id)
    {
        var run = new TutorialSession(_content, TutorialCatalog.Load(_content), id);
        run.Begin(); return run;
    }

    enum Read { Patient, Eager, Dead }

    [Theory]
    [InlineData("T-H01", "crystal-rink")]
    [InlineData("T-H02", "crystal-rink")]
    [InlineData("T-H03", "funfair-park")]
    [InlineData("T-H04", "canopy-yard")]
    public void EachLessonPlaysAtItsParkAndIsEarnedByTheRead(string id, string park)
    {
        var run = Start(id);
        Assert.Equal(park, run.Match.Park.Id);
        Drive(run, Read.Patient);
        Assert.True(run.Feedback?.Success == true, $"{id}: {run.Feedback} at {run.Elapsed}; play {run.LastPlay?.Kind}");
    }

    [Theory]
    [InlineData("T-H01")] [InlineData("T-H02")] [InlineData("T-H03")] [InlineData("T-H04")]
    public void DeadInputNeverEarnsTheLesson(string id)
    {
        var run = Start(id);
        Drive(run, Read.Dead);
        Assert.False(run.Feedback?.Success ?? false);
        Assert.Empty(run.Progress.Completed);
    }

    [Theory]
    [InlineData("T-H02", "slowed")]
    public void SkippingTheReadFails(string id, string code)
    {
        var run = Start(id);
        Drive(run, Read.Eager);
        Assert.False(run.Feedback?.Success ?? true, $"{id}: {run.Feedback}");
        Assert.Equal(code, run.Feedback!.Code);
    }

    /// <summary>
    /// Patient: wait for the hazard, then chase; on the fly lesson, bend the run around every volume. Eager: chase at once, and on
    /// the fly lesson run straight through. (A take lesson's ball reaches its hazard before any glove can, so skipping that read
    /// is the dead pad, where the assistance collects the ball.)
    /// </summary>
    static void Drive(TutorialSession run, Read read)
    {
        for (var i = 0; i < 1900 && run.Phase == TutorialPhase.Attempt; i++)
        {
            var live = run.Match.LivePlay;
            var pad = LivePadInput.Dead;
            if (read != Read.Dead && live.ElapsedSeconds > .4)
            {
                if (run.Lesson.Objective == "hazard-dodge-catch") { if (!live.HoldsBall) pad = Fly(run, live, around: read == Read.Patient); }
                else
                {
                    var acted = live.RedirectsThisPlay.Count > 0 || live.CaromsThisPlay.Count > 0
                        || run.Lesson.Objective == "manual-ground-possession";
                    if (!acted && read == Read.Patient) pad = new LivePadInput(StickY: -1);
                    if (acted || read == Read.Eager) pad = Toward(live.BallX - live.GloveX, live.BallZ - live.GloveZ, south: true);
                }
            }
            run.Tick(Frame, pad, LivePlayCommandSource.Human);
        }
    }

    static LivePadInput Fly(TutorialSession run, LivePlaySystem live, bool around)
    {
        var p = live.Preview!;
        var target = FlyCatch.ChaseTarget(p, run.Match.Rules, run.Match.Park);
        var dx = target.X - live.GloveX; var dz = target.Z - live.GloveZ;
        var len = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
        double sx = dx / len, sz = dz / len;
        if (around)
            foreach (var v in live.StatusVolumes)
            {
                var ox = live.GloveX - v.X; var oz = live.GloveZ - v.Z;
                var d = Math.Max(1e-6, Math.Sqrt(ox * ox + oz * oz));
                if (d < v.RadiusFt + 3) { sx += 1.5 * ox / d; sz += 1.5 * oz / d; }
            }
        var n = Math.Max(1e-6, Math.Sqrt(sx * sx + sz * sz));
        var mag = len < 3 ? .21 : 1;
        return new(StickX: sx / n * mag, StickY: sz / n * mag, SouthDown: live.ElapsedSeconds >= p.HangTimeSec - .6);
    }

    static LivePadInput Toward(double dx, double dz, bool south)
    {
        var length = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz));
        return new(StickX: dx / length, StickY: dz / length, SouthDown: south);
    }
}
