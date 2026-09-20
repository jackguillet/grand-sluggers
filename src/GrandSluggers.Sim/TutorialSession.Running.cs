namespace GrandSluggers.Sim;

/// <summary>Runner lessons observe the production offense pad and runner bodies after each live tick.</summary>
public sealed partial class TutorialSession
{
    sealed record RunnerBefore(string Id, int Bag, double Feet, RunnerPhase Phase, bool Held, bool ForceSlide);
    string _lessonRunner = "";
    bool _runnerSelected;
    bool _runnerSent;
    bool _runnerHeld;
    bool _runnerReturned;
    bool _humanDashed;
    double _furthestRunnerFeet;
    bool _allSent;
    bool _allReturned;
    bool _humanSlide;
    bool _humanTaggedUp;

    void ResetRunningEvidence()
    {
        _lessonRunner = ""; _runnerSelected = false; _runnerSent = false;
        _runnerHeld = false; _runnerReturned = false; _humanDashed = false;
        _furthestRunnerFeet = 0;
        _allSent = false; _allReturned = false; _humanSlide = false; _humanTaggedUp = false;
    }

    RunnerBefore? CaptureRunnerBefore()
    {
        var bag = Lesson.Objective is "runner-send-halt-return" or "all-runner-return" ? 2
            : Lesson.Objective == "human-tag-up" ? 3 : 0;
        var runner = Match.RunnerAt(bag);
        return runner is null ? null : new(runner.Who.Id, runner.Bag, runner.Feet, runner.Phase, runner.Held, runner.ForceSlide);
    }

    void ObserveRunning(LivePadInput pad, bool owned, RunnerBefore? before, LivePlayCommandResult result)
    {
        if (Lesson.Objective == "human-tag-up")
        {
            if (before is not null) _lessonRunner = before.Id;
            var tagRunner = Match.Runners.FirstOrDefault(r => r.Who.Id == _lessonRunner);
            if (owned && pad.AllAdvance && Match.LivePlay.Caught && before is { Bag: 3, Feet: <= 1e-6 }
                && tagRunner is { HumanSent: true, Phase: RunnerPhase.Advancing })
                _humanTaggedUp = true;
            if (result.CompletedPlay is { } fly)
            {
                var scored = fly.RunsScored > 0 && fly.Outcome?.Moves.Any(m => m.Runner.Id == _lessonRunner
                    && m.FromBag == 3 && m.ToBag == 4 && fly.Scorers.Contains(m.Runner.Name)) == true;
                var catchOut = fly.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true;
                var success = _humanTaggedUp && scored && catchOut;
                Finish(success, success ? "tagged-up-scored" : "tag-up-missed",
                    success ? "Your runner waited for the catch, tagged at third, and scored."
                        : "Wait for the catch, then send the runner from third toward home.");
            }
            return;
        }
        if (before is null) return;
        var runner = Match.Runners.FirstOrDefault(r => r.Who.Id == before.Id);
        if (runner is null) return;
        _lessonRunner = before.Id;
        if (Lesson.Objective == "all-runner-return")
        {
            var ahead = Match.RunnerAt(3);
            if (ahead is null) return;
            if (owned && pad.AllAdvance && Match.SendAll && runner.HumanSent && ahead.HumanSent
                && runner.Phase == RunnerPhase.Advancing && ahead.Phase == RunnerPhase.Advancing)
                _allSent = true;
            if (owned && _allSent && pad.AllReturn && !Match.SendAll
                && runner.Phase == RunnerPhase.Returning && ahead.Phase == RunnerPhase.Returning)
                _allReturned = true;
            if (_allReturned && runner.Bag == 2 && ahead.Bag == 3
                && runner.Feet <= 1e-6 && ahead.Feet <= 1e-6
                && runner.Phase is RunnerPhase.Returning or RunnerPhase.OnBag
                && ahead.Phase is RunnerPhase.Returning or RunnerPhase.OnBag)
                Finish(true, "all-runners-returned", "Both runners advanced and returned to their own bags.");
            return;
        }
        if (Lesson.Objective == "runner-send-halt-return")
        {
            // Each bit is a human order accepted by the same runner on the live basepath.
            if (owned && pad.KeysBag == 2 && Match.SelectedRunner?.Id == before.Id)
                _runnerSelected = true;
            if (owned && _runnerSelected && pad.StickBag == 3 && runner.HumanSent
                && runner.Phase == RunnerPhase.Advancing && runner.Feet > before.Feet)
                _runnerSent = true;
            if (owned && _runnerSent && pad.Freeze && pad.StickBag == runner.NextBag
                && runner.Held && runner.Feet > 0)
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
                Finish(true, "runner-dashed", "Your dash accelerated the batter-runner along the first-base path.");
        }
        else if (Lesson.Objective == "human-slide")
        {
            if (owned && (pad.WestDown || pad.SouthDown) && !before.ForceSlide
                && before.Phase != RunnerPhase.Sliding && runner.ForceSlide && runner.Phase == RunnerPhase.Sliding)
                _humanSlide = true;
            if (_humanSlide && runner.Phase == RunnerPhase.Sliding && runner.Feet > before.Feet)
                Finish(true, "runner-slid", "Your runner slid along the first-base path near the bag.");
        }
    }
}
