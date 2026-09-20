namespace GrandSluggers.Sim;

/// <summary>Choice lessons pair a player's bag command with the live out and runner identity.</summary>
public sealed partial class TutorialSession
{
    bool _opponentLeftEarly;
    bool _rundownSeen;
    bool _humanCloseOffPress;

    void ResetOutEvidence()
    {
        _opponentLeftEarly = false;
        _rundownSeen = false;
        _humanCloseOffPress = false;
        if (Lesson.Objective == "human-double-off") _lessonRunner = Match.Second?.Id ?? "";
    }

    void ObserveRundown() { if (Lesson.Objective == "human-rundown-tag" && Match.LivePlay.InRundown) _rundownSeen = true; }

    void ObserveCloseOffense(bool iconBefore, LivePadInput pad, bool owned, LivePlayCommandResult result)
    {
        if (Lesson.Objective != "human-close-offense") return;
        if (owned && iconBefore && pad.SouthDown) _humanCloseOffPress = true;
        if (result.CompletedPlay is not { } play) return;
        var runnerSafe = play.Outcome?.Moves.Any(m => m.Runner.Id == _secondRunner && m.FromBag == 2 && m.ToBag == 3) == true
            && play.Outcome.OutsMade.All(o => o.Runner.Id != _secondRunner);
        var success = _humanCloseOffPress && runnerSafe;
        Finish(success, success ? "close-runner-safe" : "close-runner-out",
            success ? "Your press won the close play and the runner reached third safely."
                : "Send the runner, then press as the close-play icon appears to beat the tag.");
    }

    void TickDoubledOffOpponent()
    {
        if (Lesson.Objective != "human-double-off" || _opponentLeftEarly) return;
        var live = Match.LivePlay;
        if (!live.Active || Match.RunnerAt(2)?.Who.Id != _lessonRunner) return;
        // A tutorial opponent takes a real, early send on the fly. The player's defense seat
        // still owns the catch and the throw that may beat this body back to second.
        live.Apply(LivePlayCommand.Tick(1e-6, LivePadInput.Dead,
            new LivePadInput(KeysBag: 2, StickBag: 3), false, LivePlayCommandSource.Cpu));
        _opponentLeftEarly = true;
    }

    void EvaluateOutObjective(LivePlayCommandResult result)
    {
        if (Lesson.Objective == "human-force-home")
        {
            var moment = Match.LivePlay.LastMoment;
            var named = Match.Runners.FirstOrDefault(r => r.Who.Id == _thirdRunner);
            var success = _throws.FirstOrDefault() == 4 && moment is { Verdict: InPlay.ThrowVerdict.ForceOut, Bag: 4 }
                && moment.Runner?.Id == _thirdRunner && named?.Phase == RunnerPhase.Out;
            if (success || result.CompletedPlay is not null)
                Finish(success, success ? "forced-home" : "force-home-missed",
                    success ? "Your throw reached home before the forced runner from third."
                        : "With every bag occupied, throw home while the force is still live.");
            return;
        }
        if (result.CompletedPlay is not { } play) return;
        if (Lesson.Objective == "human-choice-second")
        {
            var outs = play.Outcome?.OutsMade;
            var chosen = _throws.Count > 0 && _throws[0] == 2;
            var forced = outs?.Any(o => o.Type == OutType.Force && o.Bag == 2
                && o.Runner.Id == _firstRunner) == true;
            var batterSafe = outs?.All(o => o.Runner.Id != _batter) == true
                && play.Outcome?.BatterToBag >= 1;
            var good = chosen && forced && batterSafe;
            Finish(good, good ? "choice-at-second" : "choice-missed",
                good ? "Your throw forced the lead runner at second while the batter reached first."
                    : "Choose second for the force; the named lead runner must be retired there.");
        }
        else if (Lesson.Objective == "human-double-off")
        {
            var outs = play.Outcome?.OutsMade;
            var catchOut = outs?.Any(o => o.Type == OutType.Catch && o.Runner.Id == _batter) == true;
            var doubled = outs?.Any(o => o.Type == OutType.Force && o.Bag == 2
                && o.Runner.Id == _lessonRunner && o.FromBag == 2) == true;
            var success = _opponentLeftEarly && _throws.Contains(2) && catchOut && doubled;
            Finish(success, success ? "runner-doubled-off" : "double-off-missed",
                success ? "You caught the fly and threw behind the runner before they retouched second."
                    : "Catch the fly, then throw back to second before the runner returns.");
        }
    }
}
