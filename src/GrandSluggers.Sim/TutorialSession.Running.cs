namespace GrandSluggers.Sim;

/// <summary>Runner lessons observe the production offense pad and runner bodies after each live tick.</summary>
public sealed partial class TutorialSession
{
    sealed record RunnerBefore(string Id, int Bag, double Feet, RunnerPhase Phase, bool Held);
    string _lessonRunner = "";
    bool _runnerSelected;
    bool _runnerSent;
    bool _runnerHeld;
    bool _runnerReturned;
    bool _humanDashed;
    double _furthestRunnerFeet;

    void ResetRunningEvidence()
    {
        _lessonRunner = ""; _runnerSelected = false; _runnerSent = false;
        _runnerHeld = false; _runnerReturned = false; _humanDashed = false;
        _furthestRunnerFeet = 0;
    }

    RunnerBefore? CaptureRunnerBefore()
    {
        var runner = Match.RunnerAt(Lesson.Objective == "runner-send-halt-return" ? 2 : 0);
        return runner is null ? null : new(runner.Who.Id, runner.Bag, runner.Feet, runner.Phase, runner.Held);
    }

    void ObserveRunning(LivePadInput pad, bool owned, RunnerBefore? before, LivePlayCommandResult result)
    {
        if (before is null) return;
        var runner = Match.Runners.FirstOrDefault(r => r.Who.Id == before.Id);
        if (runner is null) return;
        _lessonRunner = before.Id;
        if (Lesson.Objective == "runner-send-halt-return")
        {
            // Each bit is a human order accepted by the same runner on the live basepath.
            if (owned && pad.KeysBag == 2 && Match.SelectedRunner?.Id == before.Id)
                _runnerSelected = true;
            if (owned && _runnerSelected && pad.StickBag == 3 && runner.HumanSent
                && runner.Phase == RunnerPhase.Advancing && runner.Feet > before.Feet)
                _runnerSent = true;
            if (owned && _runnerSent && pad.Freeze && runner.Held && runner.Feet > 0)
                _runnerHeld = true;
            if (owned && _runnerHeld && pad.StickBag == 2 && runner.Phase == RunnerPhase.Returning)
                _runnerReturned = true;
            if (_runnerReturned && runner.Bag == 2 && runner.Feet <= 1e-6
                && runner.Phase is RunnerPhase.Returning or RunnerPhase.OnBag)
                Finish(true, "runner-returned", "Your selected runner advanced, held, and returned safely to second.");
            return;
        }

        if (Lesson.Objective == "human-dash-run")
        {
            if (owned && pad.SouthDown && Match.LivePlay.Dash01 > 0) _humanDashed = true;
            _furthestRunnerFeet = Math.Max(_furthestRunnerFeet, runner.Feet);
            if (_humanDashed && _furthestRunnerFeet >= 30 && runner.Phase == RunnerPhase.Advancing)
                Finish(true, "dashed-to-first", "Your dash accelerated the batter-runner along the first-base path.");
        }
    }
}
