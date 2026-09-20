namespace GrandSluggers.Sim;

/// <summary>Steal lessons arm real runner bodies before the CPU pitch and resolve the live catcher throw.</summary>
public sealed partial class TutorialSession
{
    readonly HashSet<int> _humanStealArms = [];
    string _stealRunner = "";
    string _homeStealRunner = "";
    bool _humanHomeSend;
    bool _tutorialCatcherThrewSecond;

    void ResetStealEvidence()
    {
        _humanStealArms.Clear();
        _stealRunner = ""; _homeStealRunner = Lesson.Objective == "human-double-steal" ? Match.Third?.Id ?? "" : "";
        _humanHomeSend = false; _tutorialCatcherThrewSecond = false;
    }

    void BeginStealDefense()
    {
        _stealRunner = Match.RunnerAt(1)?.Who.Id ?? "";
        if (_stealRunner.Length == 0 || !Match.StartStealAt(1))
            throw new InvalidDataException("Tutorial catcher setup could not arm its CPU runner.");
        BeginStealPitch(new LiveSeats(false, true, true, false));
    }

    public bool ArmSteal(int bag, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy != "steal-offense" || bag is < 1 or > 3
            || Match.LivePlay.Active || Elapsed >= StealWindupStartsAt + Motion.PitchRelease) return false;
        var runner = Match.RunnerAt(bag);
        var windup = Elapsed < StealWindupStartsAt ? -1 : Elapsed - StealWindupStartsAt;
        if (runner is null || !Match.SelectRunner(bag)) return false;
        var armed = Match.ToggleSteal(windup);
        _inputs.Add(new(Elapsed, source, StealBag: bag));
        if (source == LivePlayCommandSource.Human && !Demonstration)
        {
            if (armed) _humanStealArms.Add(bag); else _humanStealArms.Remove(bag);
        }
        if (bag == 1) _stealRunner = runner.Who.Id;
        if (bag == 3) _homeStealRunner = runner.Who.Id;
        return true;
    }

    void BeginStealPitch(LiveSeats seats)
    {
        if (Match.BeginAtBat(CpuPitch, Take, out _, out var pitch) || pitch is null || !Match.StealThrowPending)
            throw new InvalidDataException("Tutorial steal pitch did not create a runner throw play.");
        var result = Match.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch, seats, LivePlayCommandSource.System));
        if (!result.Snapshot.Active) throw new InvalidDataException("Tutorial steal runner play did not start.");
        Match.LivePlay.Recording = true;
    }

    void TickDelayedStealOpponent(double seconds)
    {
        if (Lesson.Objective != "human-double-steal" || _tutorialCatcherThrewSecond) return;
        var live = Match.LivePlay;
        if (!live.Active || !live.HoldsBall || live.Throwing || live.GlovePos != "C") return;
        // The tutorial opponent commits one ordinary catcher command to second. The separate CPU
        // source keeps that throw out of human evidence; subsequent defense remains uncommanded.
        live.Apply(LivePlayCommand.Tick(seconds, new LivePadInput(KeysBag: 2, SouthDown: true),
            LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
        _tutorialCatcherThrewSecond = live.Throwing && live.ThrowBag == 2;
    }

    void ObserveDelayedHomeSend(LivePadInput pad, bool owned, bool thirdWasOnBag)
    {
        if (!owned || Lesson.Objective != "human-double-steal" || !thirdWasOnBag
            || pad.KeysBag != 3 || pad.StickBag != 4 || _homeStealRunner.Length == 0) return;
        var runner = Match.Runners.FirstOrDefault(r => r.Who.Id == _homeStealRunner);
        if (runner is { HumanSent: true, Phase: RunnerPhase.Advancing }) _humanHomeSend = true;
    }

    void EvaluateStealPlay(LivePlayCommandResult result)
    {
        if (result.CompletedPlay is not { } play) return;
        var outcome = play.Outcome;
        if (Lesson.Objective == "human-steal")
        {
            var success = _humanStealArms.Contains(1) && _stealRunner.Length > 0
                && outcome?.RunnerResult == RunnerPlayResult.StolenBase
                && outcome.Moves.Any(m => m.Runner.Id == _stealRunner && m.FromBag == 1 && m.ToBag == 2);
            Finish(success, success ? "stole-second" : "steal-missed",
                success ? "Your armed runner broke on the pitch and reached second safely."
                    : "Arm the runner in the windup so their break can beat the catcher throw.");
        }
        else if (Lesson.Objective == "human-double-steal")
        {
            var firstMoved = outcome?.Moves.Any(m => m.Runner.Id == _stealRunner && m.FromBag == 1 && m.ToBag == 2) == true;
            var homeScored = play.RunsScored > 0 && play.Scorers.Contains(_content.Must(_homeStealRunner).Name);
            var success = _humanStealArms.Contains(1) && _humanHomeSend && firstMoved
                && homeScored;
            Finish(success, success ? "double-steal-resolved" : "double-steal-missed",
                success ? "Both runners broke; one stole second and the other crossed home."
                    : "Arm the runner at first, then send the runner at third as the catcher's throw goes to second.");
        }
        else if (Lesson.Objective == "human-catcher-tag")
        {
            var tagged = outcome?.OutsMade.Any(o => o.Runner.Id == _stealRunner && o.Type == OutType.Tag && o.Bag == 2) == true;
            var success = _throws.Contains(2) && tagged && outcome?.RunnerResult == RunnerPlayResult.CaughtStealing;
            Finish(success, success ? "caught-stealing" : "catcher-missed",
                success ? "Your catcher throw reached second before the runner and the tag was made."
                    : "Send the catcher's throw to second before the runner reaches the cover.");
        }
    }
}
