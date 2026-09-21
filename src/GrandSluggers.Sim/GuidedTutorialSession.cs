namespace GrandSluggers.Sim;

/// <summary>Evidence from accepted actions on the ordinary front-of-house screens.</summary>
public enum GuidedAction { LeftHandedRosterDrop, BattingOrderChanged, GlovePositionChanged,
    TwoPhysicalSeatsBound, CallTimeOpened, BookOpened, MatchRestarted, SeatLost, SeatRecovered, StickRecalibrated }

public sealed class GuidedTutorialSession
{
    readonly string _profile;
    readonly HashSet<GuidedAction> _actions = [];
    LineupSeat _lostSeat = LineupSeat.Cpu;
    public TutorialLesson Lesson { get; }
    public TutorialProgress Progress { get; }
    public TutorialPhase Phase { get; private set; } = TutorialPhase.Brief;
    public TutorialFeedback? Feedback { get; private set; }
    public int Successes => Progress.Count(Lesson, _profile);
    public bool Passed => Progress.Has(Lesson, _profile);

    public GuidedTutorialSession(TutorialLesson lesson, string profile, TutorialProgress progress)
    {
        var objective = lesson.Id switch
        {
            "T-G01" => "guided-lineup", "T-G05" => "guided-seats", "T-G06" => "guided-pause",
            "T-G06-R" => "guided-recovery", "T-G06-C" => "guided-calibration", _ => ""
        };
        if (lesson.Status != "implemented" || lesson.Objective != objective || objective == ""
            || !lesson.Profiles.Contains(profile))
            throw new ArgumentException("Unsupported guided tutorial", nameof(lesson));
        Lesson = lesson; _profile = profile; Progress = progress;
    }

    public void Begin()
    {
        if (Phase != TutorialPhase.Brief) throw new InvalidOperationException("Begin requires a brief.");
        _actions.Clear(); _lostSeat = LineupSeat.Cpu; Feedback = null; Phase = TutorialPhase.Attempt;
    }

    public void Retry()
    {
        if (Phase == TutorialPhase.Exited) throw new InvalidOperationException("Exited lesson.");
        _actions.Clear(); _lostSeat = LineupSeat.Cpu; Feedback = null; Phase = TutorialPhase.Brief;
    }

    public void Exit() { _actions.Clear(); _lostSeat = LineupSeat.Cpu; Phase = TutorialPhase.Exited; }

    /// <summary>Only the owner of an accepted screen transition calls this. Repeated edges do not count twice.</summary>
    public bool Observe(GuidedAction action)
    {
        var required = Required();
        if (Phase != TutorialPhase.Attempt || !required.Contains(action)
            || action is GuidedAction.SeatLost or GuidedAction.SeatRecovered) return false;
        var next = required.FirstOrDefault(a => !_actions.Contains(a));
        if (next != action) return false;
        return Record(action);
    }

    public bool ObserveSeatLost(LineupSeat seat)
    {
        if (Lesson.Id != "T-G06-R" || Phase != TutorialPhase.Attempt || seat == LineupSeat.Cpu
            || _actions.Contains(GuidedAction.SeatLost)) return false;
        _lostSeat = seat;
        return Record(GuidedAction.SeatLost);
    }

    public bool ObserveSeatRecovered(LineupSeat seat)
    {
        if (Lesson.Id != "T-G06-R" || Phase != TutorialPhase.Attempt || seat != _lostSeat
            || !_actions.Contains(GuidedAction.SeatLost)) return false;
        return Record(GuidedAction.SeatRecovered);
    }

    bool Record(GuidedAction action)
    {
        _actions.Add(action);
        if (!Required().All(_actions.Contains)) return true;
        Progress.Record(Lesson, _profile);
        Feedback = new TutorialFeedback(true, "guided-complete", "Completed the actions on the live screen.");
        Phase = TutorialPhase.Feedback;
        return true;
    }

    public IReadOnlyList<GuidedAction> Missing => Required().Where(a => !_actions.Contains(a)).ToArray();

    IReadOnlyList<GuidedAction> Required() => Lesson.Id switch
    {
        "T-G01" => [GuidedAction.LeftHandedRosterDrop, GuidedAction.BattingOrderChanged, GuidedAction.GlovePositionChanged],
        "T-G05" => [GuidedAction.TwoPhysicalSeatsBound],
        "T-G06" => [GuidedAction.CallTimeOpened, GuidedAction.BookOpened, GuidedAction.MatchRestarted],
        "T-G06-R" => [GuidedAction.SeatLost, GuidedAction.SeatRecovered],
        "T-G06-C" => [GuidedAction.StickRecalibrated],
        _ => []
    };
}
